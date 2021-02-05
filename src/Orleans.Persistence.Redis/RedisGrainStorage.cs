using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Persistence.Redis;
using Orleans.Persistence.Redis.Serialization;
using Orleans.Runtime;
using StackExchange.Redis;
using Orleans.Storage;
using static System.FormattableString;

namespace Orleans.Persistence
{
    /// <summary>
    /// Redis-based grain storage provider
    /// </summary>
    public class RedisGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private const string WriteScript = "local etag = redis.call('HGET', @key, 'etag')\nif etag == false or etag == @etag then return redis.call('HMSET', @key, 'etag', @newEtag, 'data', @data) else return false end";
        private readonly string _serviceId;
        private readonly string _name;
        private readonly ILogger _logger;
        private readonly RedisStorageOptions _options;
        private readonly IRedisDataSerializer _serializer;

        private ConnectionMultiplexer _connection;
        private IDatabase _db;
        private ConfigurationOptions _redisOptions;
        private LuaScript _preparedWriteScript;

        /// <summary>
        /// Creates a new instance of the <see cref="RedisGrainStorage"/> type.
        /// </summary>
        public RedisGrainStorage(
            string name, 
            RedisStorageOptions options, 
            IRedisDataSerializer serializer,
            IOptions<ClusterOptions> clusterOptions, 
            ILogger<RedisGrainStorage> logger)
        {
            _name = name;
            _logger = logger;
            _options = options;
            _serializer = serializer;
            
            _serviceId = clusterOptions.Value.ServiceId;
        }

        /// <inheritdoc />
        public void Participate(ISiloLifecycle lifecycle)
        {
            var name = OptionFormattingUtilities.Name<RedisGrainStorage>(_name);
            lifecycle.Subscribe(name, _options.InitStage, Init, Close);
        }

        private async Task Init(CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var initMsg = string.Format("Init: Name={0} ServiceId={1} DatabaseNumber={2} UseJson={3} DeleteOnClear={4}",
                            _name, _serviceId, _options.DatabaseNumber, _options.UseJson, _options.DeleteOnClear);
                    _logger.LogDebug($"RedisGrainStorage {_name} is initializing: {initMsg}");
                }

                _redisOptions = ConfigurationOptions.Parse(_options.ConnectionString);
                _connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions);

                if (_options.DatabaseNumber.HasValue)
                {
                    _db = _connection.GetDatabase(_options.DatabaseNumber.Value);
                }
                else
                {
                    _db = _connection.GetDatabase();
                }

                _preparedWriteScript = LuaScript.Prepare(WriteScript);

                var loadTasks = new Task[_redisOptions.EndPoints.Count];
                for (int i = 0; i < _redisOptions.EndPoints.Count; i++)
                {
                    var endpoint = _redisOptions.EndPoints.ElementAt(i);
                    var server = _connection.GetServer(endpoint);

                    loadTasks[i] = _preparedWriteScript.LoadAsync(server);
                }
                await Task.WhenAll(loadTasks);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    timer.Stop();
                    _logger.LogDebug("Init: Name={0} ServiceId={1}, initialized in {2} ms",
                        _name, _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                _logger.LogError(ex, "Init: Name={0} ServiceId={1}, errored in {2} ms. Error message: {3}",
                    _name, _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"), ex.Message);
                throw ex;
            }
        }

        /// <inheritdoc />
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);

            try
            {
                var hashEntries = await _db.HashGetAllAsync(key);
                if (hashEntries.Count() == 2)
                {
                    var etagEntry = hashEntries.Single(e => e.Name == "etag");
                    var valueEntry = hashEntries.Single(e => e.Name == "data");

                    grainState.State = _serializer.DeserializeObject(grainState.State.GetType(), valueEntry.Value);

                    grainState.ETag = etagEntry.Value;
                }
                else
                {
                    grainState.ETag = Guid.NewGuid().ToString();
                }
            }
            catch (RedisServerException)
            {
                _logger.LogInformation("Encountered old format grain state for grain of type {grainType} if ID: {grainReference} when reading hash state for key {key}. Attempting to migrate.",
                    grainType,
                    grainReference,
                    key);
                // This is here for backwards compatibility where an earlier version stored only the data as a simple key without the etag.
                // So get the data from the key and deserialize.
                var simpleKeyValue = await _db.StringGetAsync(key).ConfigureAwait(false);
                if (simpleKeyValue.HasValue)
                {
                    grainState.State = _serializer.DeserializeObject(grainState.State.GetType(), simpleKeyValue);
                }
                
                // ETag does not exist in the storage so create a new one.
                grainState.ETag = Guid.NewGuid().ToString();
                
                // To allow WriteStateAsync to operate at all in these cases the original simple key must be deleted and a hash must be created in its place with the etag that was just assigned.
                // The original key must be deleted first because a hash cannot overwrite a simple key.
                // This will create a situation where if the delete succeeds and the HashSet fails the storage does not have the data before the grain state is again written.
                try
                {
                    await _db.KeyDeleteAsync(key).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Could not delete old grain state for grain of type {grainType} with ID: {grainReference} with key {key} while migrating new new structure. Will be retried on next read or write.", 
                        grainType,
                        grainReference,
                        key);
                    return;
                }

                try
                {
                    await _db.HashSetAsync(key, new[] {new HashEntry("etag", grainState.ETag), new HashEntry("data", simpleKeyValue)}).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "Could add hash state for grain of type {grainType} with ID: {grainReference} after deleting old state with {key} while migrating to new structure. Will be retried on next write.",
                        grainType,
                        grainReference,
                        key);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Failed to read grain state for {grainType} grain with ID: {grainReference} with redis key {key}.",
                    grainType, grainReference, key);
                throw new RedisStorageException(
                    Invariant(
                        $"Failed to read grain state for {grainType} grain with ID: {grainReference} with redis key {key}."),
                    e);
            }
        }

        /// <inheritdoc />
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);
            var newEtag = Guid.NewGuid().ToString();

            RedisResult response;
            try
            {
                response = await WriteToRedisAsync(grainState.State, grainState.ETag ?? "null", key, newEtag)
                    .ConfigureAwait(false);
            }
            catch (RedisServerException)
            {
                _logger.LogInformation(
                    "Encountered old format grain state when write hash state for key {key}. Attempting to migrate.",
                    key);
                try
                {
                    await _db.KeyDeleteAsync(key).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "Could not delete old grain state with key {key} while migrating new new structure.", key);
                    throw new RedisStorageException(Invariant(
                        $"Could not delete old grain state with key {key} while migrating new new structure."));
                }

                try
                {
                    response = await WriteToRedisAsync(grainState.State, grainState.ETag ?? "null", key, newEtag)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Failed to write grain state for {grainType} grain with ID: {grainReference} with redis key {key} while migrating to new structure.",
                        grainType, grainReference, key);
                    throw new RedisStorageException(
                        Invariant(
                            $"Failed to write grain state for {grainType} grain with ID: {grainReference} with redis key {key} while migrating to new structure."),
                        e);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Failed to write grain state for {grainType} grain with ID: {grainReference} with redis key {key}.",
                    grainType, grainReference, key);
                throw new RedisStorageException(
                    Invariant(
                        $"Failed to write grain state for {grainType} grain with ID: {grainReference} with redis key {key}."),
                    e);
            }
            
            if (response.IsNull)
            {
                throw new InconsistentStateException($"ETag mismatch - tried with ETag: {grainState.ETag}");
            }

            grainState.ETag = newEtag;
        }

        private async Task<RedisResult> WriteToRedisAsync(object state, string etag, string key, string newEtag)
        {
            var payload = _serializer.SerializeObject(state);
            var args = new {key, etag = etag, newEtag, data = payload};
            return await _db.ScriptEvaluateAsync(_preparedWriteScript, args).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);
            await _db.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        private string GetKey(GrainReference grainReference)
        {
            var format = _options.UseJson ? "json" : "binary";
            return $"{grainReference.ToKeyString()}|{format}";
        }

        private async Task Close(CancellationToken cancellationToken)
        {
            if (_connection is null)
            {
                return;
            }

            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
        }
    }
}
