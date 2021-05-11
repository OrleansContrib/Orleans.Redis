using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;
using StackExchange.Redis;
using Orleans.Storage;

namespace Orleans.Persistence
{
    /// <summary>
    /// Redis-based grain storage provider
    /// </summary>
    public class RedisGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private const string _writeScript = "local etag = redis.call('HGET', KEYS[1], 'etag')\nif etag == false or etag == ARGV[1] then return redis.call('HMSET', KEYS[1], 'etag', ARGV[2], 'data', ARGV[3]) else return false end";
        private readonly string _serviceId;
        private readonly string _name;
        private readonly SerializationManager _serializationManager;
        private readonly ILogger _logger;
        private readonly RedisStorageOptions _options;
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        };
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
            SerializationManager serializationManager,
            IOptions<ClusterOptions> clusterOptions,
            ILogger<RedisGrainStorage> logger)
        {
            _name = name;
            _logger = logger;
            _options = options;
            _serializationManager = serializationManager;
            _serviceId = clusterOptions.Value.ServiceId;
        }

        /// <inheritdoc />
        public void Participate(ISiloLifecycle lifecycle)
        {
            var name = OptionFormattingUtilities.Name<RedisGrainStorage>(_name);
            lifecycle.Subscribe(name, _options.InitStage, Init, Close);
        }

        /// <inheritdoc />
        public async Task Init(CancellationToken cancellationToken)
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

                _preparedWriteScript = LuaScript.Prepare(_writeScript);

                var loadTasks = new Task<LoadedLuaScript>[_redisOptions.EndPoints.Count];
                for (int i = 0; i < _redisOptions.EndPoints.Count; i++)
                {
                    var endpoint = _redisOptions.EndPoints.ElementAt(i);
                    var server = _connection.GetServer(endpoint);

                    loadTasks[i] = _preparedWriteScript.LoadAsync(server);
                }
                await Task.WhenAll(loadTasks);
                _preparedWriteScriptHash = loadTasks.First().Result.Hash;

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
                    if (_options.UseJson)
                    {
                        grainState.State = JsonConvert.DeserializeObject(valueEntry.Value, grainState.State.GetType(), _jsonSettings);
                    }
                    else
                    {
                        grainState.State = _serializationManager.DeserializeFromByteArray<object>(valueEntry.Value);
                    }

                    grainState.ETag = etagEntry.Value;
                }
                else
                {
                    grainState.ETag = Guid.NewGuid().ToString();
                }
            }
            catch (RedisServerException)
            {
                var stringValue = await _db.StringGetAsync(key);
                if (stringValue.HasValue)
                {
                    if (_options.UseJson)
                    {
                        grainState.State = JsonConvert.DeserializeObject(stringValue, grainState.State.GetType(), _jsonSettings);
                    }
                    else
                    {
                        grainState.State = _serializationManager.DeserializeFromByteArray<object>(stringValue);
                    }
                }
                grainState.ETag = Guid.NewGuid().ToString();
            }
        }

        /// <inheritdoc />
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);
            var newEtag = Guid.NewGuid().ToString();

            RedisResult response;
            if (_options.UseJson)
            {
                var payload = JsonConvert.SerializeObject(grainState.State, _jsonSettings);
                var args = new RedisValue[] { grainState.ETag ?? "null", newEtag, payload };
                response = await _db.ScriptEvaluateAsync(_preparedWriteScriptHash, new RedisKey[] { key }, args);
            }
            else
            {
                var payload = _serializationManager.SerializeToByteArray(grainState.State);
                var args = new RedisValue[] { grainState.ETag ?? "null", newEtag, payload };
                response = await _db.ScriptEvaluateAsync(_preparedWriteScriptHash, new RedisKey[] { key }, args);
            }

            if (response.IsNull)
            {
                throw new InconsistentStateException($"ETag mismatch - tried with ETag: {grainState.ETag}");
            }

            grainState.ETag = newEtag;
        }

        /// <inheritdoc />
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);
            await _db.KeyDeleteAsync(key);
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

            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
