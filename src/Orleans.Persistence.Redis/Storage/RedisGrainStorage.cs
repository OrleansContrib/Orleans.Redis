using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Persistence.Redis;
using Orleans.Runtime;
using Orleans.Storage;
using StackExchange.Redis;
using static System.FormattableString;

namespace Orleans.Persistence
{
    /// <summary>
    /// Redis-based grain storage provider
    /// </summary>
    public class RedisGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private const string WriteScript = "local etag = redis.call('HGET', KEYS[1], 'etag')\nif etag == false or etag == ARGV[1] then return redis.call('HMSET', KEYS[1], 'etag', ARGV[2], 'data', ARGV[3]) else return false end";
        private const int ReloadWriteScriptMaxCount = 3;

        private readonly string _serviceId;
        private readonly string _name;
        private readonly ILogger _logger;
        private readonly RedisStorageOptions _options;
        private IGrainStorageSerializer _grainStorageSerializer;

        private ConnectionMultiplexer _connection;
        private IDatabase _db;
        private ConfigurationOptions _redisOptions;
        private LuaScript _preparedWriteScript;
        private byte[] _preparedWriteScriptHash;

        /// <summary>
        /// Creates a new instance of the <see cref="RedisGrainStorage"/> type.
        /// </summary>
        public RedisGrainStorage(
            string name,
            RedisStorageOptions options,
            IGrainStorageSerializer grainStorageSerializer,
            IOptions<ClusterOptions> clusterOptions,
            ILogger<RedisGrainStorage> logger)
        {
            _name = name;
            _logger = logger;
            _options = options;
            _grainStorageSerializer = grainStorageSerializer;

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
                    var initMsg = string.Format("Init: Name={0} ServiceId={1} DatabaseNumber={2} DeleteOnClear={3}",
                            _name, _serviceId, _options.DatabaseNumber, _options.DeleteOnClear);
                    _logger.LogDebug($"RedisGrainStorage {_name} is initializing: {initMsg}");
                }

                _redisOptions = ConfigurationOptions.Parse(_options.ConnectionString);
                _connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions).ConfigureAwait(false);

                if (_options.DatabaseNumber.HasValue)
                {
                    _db = _connection.GetDatabase(_options.DatabaseNumber.Value);
                }
                else
                {
                    _db = _connection.GetDatabase();
                }

                _preparedWriteScript = LuaScript.Prepare(WriteScript);
                _preparedWriteScriptHash = await LoadWriteScriptAsync().ConfigureAwait(false);

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
                throw;
            }
        }

        private async Task<byte[]> LoadWriteScriptAsync()
        {
            Debug.Assert(_connection is not null);
            Debug.Assert(_preparedWriteScript is not null);
            Debug.Assert(_redisOptions.EndPoints.Count > 0);

            System.Net.EndPoint[] endPoints = _connection.GetEndPoints();
            var loadTasks = new Task<LoadedLuaScript>[endPoints.Length];
            for (int i = 0; i < endPoints.Length; i++)
            {
                var endpoint = endPoints.ElementAt(i);
                var server = _connection.GetServer(endpoint);

                loadTasks[i] = _preparedWriteScript.LoadAsync(server);
            }
            await Task.WhenAll(loadTasks).ConfigureAwait(false);
            return loadTasks[0].Result.Hash;
        }

        /// <inheritdoc />
        public async Task ReadStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
        {
            var key = GetKey(grainId);

            try
            {
                var hashEntries = await _db.HashGetAllAsync(key).ConfigureAwait(false);
                if (hashEntries.Length == 2)
                {
                    var etagEntry = hashEntries.Single(e => e.Name == "etag");
                    var valueEntry = hashEntries.Single(e => e.Name == "data");

                    grainState.State = _grainStorageSerializer.Deserialize<T>(valueEntry.Value);

                    grainState.ETag = etagEntry.Value;
                }
                else
                {
                    grainState.ETag = Guid.NewGuid().ToString();
                }
            }
            catch (RedisServerException rse) when (rse.Message is not null && rse.Message.StartsWith("WRONGTYPE ", StringComparison.Ordinal))
            {
                // HGETALL returned error 'WRONGTYPE Operation against a key holding the wrong kind of value'
                _logger.LogInformation("Encountered old format grain state for grain of type {GrainType} with id {GrainId} when reading hash state for key {Key}. Attempting to migrate.",
                    grainType, grainId, key);

                // This is here for backwards compatibility where an earlier version stored only the data as a simple key without the etag.
                // So get the data from the key and deserialize.
                var simpleKeyValue = await _db.StringGetAsync(key).ConfigureAwait(false);
                if (simpleKeyValue.HasValue)
                {
                    grainState.State = _grainStorageSerializer.Deserialize<T>(simpleKeyValue);
                }

                // ETag does not exist in the storage so create a new one.
                var etag = Guid.NewGuid().ToString();
                grainState.ETag = etag;

                if (!await MigrateAsync(key, etag, simpleKeyValue).ConfigureAwait(false))
                {
                    _logger.LogError(
                        "Unexpected error while migrating grain state to new storage for grain of type {GrainType} with id {GrainId} and storage key {Key}. Will retry on next operation.",
                        grainType, grainId, key);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Failed to read grain state for {GrainType} grain with id {GrainId} and storage key {Key}.",
                    grainType, grainId, key);
                throw new RedisStorageException(Invariant($"Failed to read grain state for {grainType} grain with id {grainId} and storage key {key}."), e);
            }
        }

        /// <inheritdoc />
        public async Task WriteStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
        {
            var etag = grainState.ETag ?? "null";
            var key = GetKey(grainId);
            var newEtag = Guid.NewGuid().ToString();

            RedisValue payload = default;
            RedisResult writeWithScriptResponse = null;
            try
            {
                payload = _grainStorageSerializer.Serialize(grainState.State).ToString();
                writeWithScriptResponse = await WriteToRedisUsingPreparedScriptAsync(payload,
                        etag: etag,
                        key: key,
                        newEtag: newEtag)
                    .ConfigureAwait(false);
            }
            catch (RedisServerException rse) when (rse.Message is not null && rse.Message.Contains(" WRONGTYPE ")) // ordinal comparison
            {
                // EVALSHA returned error like 'ERR Error running script (call to f_4ebd809f882ff80026566f2d7dc8674009a3d14d): @user_script:1: WRONGTYPE Operation against a key holding the wrong kind of value'
                _logger.LogInformation("Encountered old format grain state for grain of type {GrainType} with id {GrainId} when writing hash state for key {Key}. Attempting to migrate.",
                    grainType, grainId, key);

                if (!await MigrateAsync(key, newEtag, payload).ConfigureAwait(false))
                {
                    _logger.LogError(
                        "Failed to write grain state for {GrainType} grain with id {GrainId} with storage key {Key} while migrating to new structure",
                        grainType, grainId, key);
                    throw new RedisStorageException(
                        Invariant($"Failed to write grain state for {grainType} grain with id {grainId} with storage key {key} while migrating to new structure"));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Failed to write grain state for {grainType} grain with ID: {grainId} with redis key {key}.", grainType, grainId, key);
                throw new RedisStorageException(
                    Invariant($"Failed to write grain state for {grainType} grain with ID: {grainId} with redis key {key}."), e);
            }

            if (writeWithScriptResponse is not null && writeWithScriptResponse.IsNull)
            {
                throw new InconsistentStateException($"ETag mismatch - tried with ETag: {grainState.ETag}");
            }

            grainState.ETag = newEtag;
        }

        private async Task<bool> MigrateAsync(string key, string etag, RedisValue payload)
        {
            var tx = _db.CreateTransaction();

            // To allow WriteStateAsync to operate at all in these cases the original simple key must be deleted and a hash must be created in its place with the etag that was just assigned.
            // The original key must be deleted first because a hash cannot overwrite a simple key.
            // This will create a situation where if the delete succeeds and the HashSet fails the storage does not have the data before the grain state is again written.
            _ = tx.KeyDeleteAsync(key);
            _ = tx.HashSetAsync(key, new[] { new HashEntry("etag", etag), new HashEntry("data", payload) });
            return await tx.ExecuteAsync().ConfigureAwait(false);
        }

        private Task<RedisResult> WriteToRedisUsingPreparedScriptAsync(RedisValue payload, string etag, string key, string newEtag)
        {
            var keys = new RedisKey[] { key };
            var args = new RedisValue[] { etag, newEtag, payload };
            return WriteToRedisUsingPreparedScriptAsync(attemptNum: 0);


            async Task<RedisResult> WriteToRedisUsingPreparedScriptAsync(int attemptNum)
            {
                try
                {
                    return await _db.ScriptEvaluateAsync(_preparedWriteScriptHash, keys, args).ConfigureAwait(false);
                }
                catch (RedisServerException rse) when (rse.Message is not null && rse.Message.StartsWith("NOSCRIPT ", StringComparison.Ordinal))
                {
                    // EVALSHA returned error 'NOSCRIPT No matching script. Please use EVAL.'.
                    // This means that SHA1 cache of Lua scripts is cleared at server side, possibly because of Redis server rebooted after Init() method was called. Need to reload Lua script.
                    // Several attempts are made just in case (e.g. if Redis server is rebooted right after previous script reload).
                    if (attemptNum >= ReloadWriteScriptMaxCount)
                    {
                        throw;
                    }

                    await LoadWriteScriptAsync().ConfigureAwait(false);
                    return await WriteToRedisUsingPreparedScriptAsync(attemptNum: attemptNum + 1)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public async Task ClearStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
        {
            var key = GetKey(grainId);
            await _db.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        private string GetKey(GrainId grainId)
        {
            //return $"{grainId}|{_grainStorageSerializer.GetType().Name}";
            return grainId.ToString();
        }

        private async Task Close(CancellationToken cancellationToken)
        {
            if (_connection is null) return;

            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
        }
    }
}
