using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using StackExchange.Redis;

namespace Orleans.StorageProviders
{
    public class RedisStorageProvider : IStorageProvider
    {
        private ConnectionMultiplexer connectionMultiplexer;
        private IDatabase redisDatabase;

        internal const string REDIS_CONNECTION_STRING = "RedisConnectionString";
        internal const string REDIS_DATABASE_NUMBER = "DatabaseNumber";
        internal const string USE_JSON_FORMAT_PROPERTY = "UseJsonFormat";

        private const string _writeScript = "local etag = redis.call('HGET', @key, 'etag')\nif etag == false or etag == @etag then return redis.call('HMSET', @key, 'etag', @newEtag, 'data', @data) else return false end";

        private string serviceId;
        private bool useJsonFormat;

        private JsonSerializerSettings jsonSettings { get; } = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        };

        private SerializationManager serializationManager;
        private ConfigurationOptions _options;
        private LuaScript _preparedWriteScript;

        /// <summary> Name of this storage provider instance. </summary>
        /// <see cref="IProvider#Name"/>
        public string Name { get; private set; }

        /// <summary> Logger used by this storage provider instance. </summary>
        /// <see cref="IStorageProvider#Log"/>
        public Logger Log { get; private set; }


        /// <summary> Initialization function for this storage provider. </summary>
        /// <see cref="IProvider#Init"/>
        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            serviceId = providerRuntime.ServiceId.ToString();
            this.serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();

            if (!config.Properties.ContainsKey(REDIS_CONNECTION_STRING) ||
                string.IsNullOrWhiteSpace(config.Properties[REDIS_CONNECTION_STRING]))
            {
                throw new ArgumentException("RedisConnectionString is not set.");
            }
            var connectionString = config.Properties[REDIS_CONNECTION_STRING];
            _options = ConfigurationOptions.Parse(connectionString);

            connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_options);

            if (!config.Properties.ContainsKey(REDIS_DATABASE_NUMBER) ||
                string.IsNullOrWhiteSpace(config.Properties[REDIS_DATABASE_NUMBER]))
            {
                //do not throw an ArgumentException but use the default database
                redisDatabase = connectionMultiplexer.GetDatabase();
            }
            else
            {
                var databaseNumber = Convert.ToInt16(config.Properties[REDIS_DATABASE_NUMBER]);
                redisDatabase = connectionMultiplexer.GetDatabase(databaseNumber);
            }
            
            _preparedWriteScript = LuaScript.Prepare(_writeScript);

            var loadTasks = new Task[_options.EndPoints.Count];
            for (int i = 0; i < _options.EndPoints.Count; i++)
            {
                var endpoint = _options.EndPoints.ElementAt(i);
                var server = connectionMultiplexer.GetServer(endpoint);

                loadTasks[i] = _preparedWriteScript.LoadAsync(server);
            }
            await Task.WhenAll(loadTasks);

            //initialize to use the default of JSON storage (this is to provide backwards compatiblity with previous version
            useJsonFormat = true;

            if (config.Properties.ContainsKey(USE_JSON_FORMAT_PROPERTY))
                useJsonFormat = "true".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase);
           
            Log = providerRuntime.GetLogger("StorageProvider.RedisStorage." + serviceId);
        }

        // Internal method to initialize for testing
        internal void InitLogger(Logger logger)
        {
            Log = logger;
        }

        /// <summary> Shutdown this storage provider. </summary>
        /// <see cref="IStorageProvider#Close"/>
        public Task Close()
        {
            connectionMultiplexer.Dispose();
            return Task.CompletedTask;
        }
        
        /// <summary> Read state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#ReadStateAsync"/>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = GetKey(grainReference);

            if (Log.IsVerbose3)
            {
                Log.Verbose3((int)ProviderErrorCode.RedisStorageProvider_ReadingData, "Reading: GrainType={0} Pk={1} Grainid={2} from Database={3}", 
                    grainType, primaryKey, grainReference, redisDatabase.Database);
            }
            
            try
            {
                var hashEntries = await redisDatabase.HashGetAllAsync(primaryKey);
                if (hashEntries.Count() == 2)
                {
                    var etagEntry = hashEntries.Single(e => e.Name == "etag");
                    var valueEntry = hashEntries.Single(e => e.Name == "data");
                    if (useJsonFormat)
                        grainState.State = JsonConvert.DeserializeObject(valueEntry.Value, grainState.State.GetType(), jsonSettings);
                    else
                        grainState.State = serializationManager.DeserializeFromByteArray<object>(valueEntry.Value);
                    grainState.ETag = etagEntry.Value;
                } else
                {
                    grainState.ETag = Guid.NewGuid().ToString();
                }
            }
            catch (RedisServerException)
            {
                var stringValue = await redisDatabase.StringGetAsync(primaryKey);
                if (stringValue.HasValue)
                {
                    if (useJsonFormat)
                        grainState.State = JsonConvert.DeserializeObject(stringValue, grainState.State.GetType(), jsonSettings);
                    else
                        grainState.State = serializationManager.DeserializeFromByteArray<object>(stringValue);
                }
                grainState.ETag = Guid.NewGuid().ToString();
            }
        }

        /// <summary> Write state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#WriteStateAsync"/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = GetKey(grainReference);

            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) ProviderErrorCode.RedisStorageProvider_WritingData, "Writing: GrainType={0} PrimaryKey={1} Grainid={2} ETag={3} to Database={4}", 
                    grainType, key, grainReference, grainState.ETag, redisDatabase.Database);
            }

            RedisResult response = null;
            var newEtag = Guid.NewGuid().ToString();
            if (useJsonFormat)
            {
                var payload = JsonConvert.SerializeObject(grainState.State, jsonSettings);
                var args = new { key, etag = grainState.ETag ?? "null", newEtag, data = payload };
                response = await redisDatabase.ScriptEvaluateAsync(_preparedWriteScript, args);
            }
            else
            {
                var payload = serializationManager.SerializeToByteArray(grainState.State);
                var args = new { key, etag = grainState.ETag ?? "null", newEtag, data = payload };
                response = await redisDatabase.ScriptEvaluateAsync(_preparedWriteScript, args);
            }

            if (response.IsNull)
                throw new Exception("ETag mismatch");

            grainState.ETag = newEtag;
        }

        /// <summary> Clear state data function for this storage provider. </summary>
        /// <remarks>
        /// </remarks>
        /// <see cref="IStorageProvider#ClearStateAsync"/>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = GetKey(grainReference);
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) ProviderErrorCode.RedisStorageProvider_ClearingData, "Clearing: GrainType={0} Pk={1} Grainid={2} ETag={3} to Database={4}", 
                    grainType, primaryKey, grainReference, grainState.ETag, redisDatabase.Database);
            }
            //remove from cache
            return redisDatabase.KeyDeleteAsync(primaryKey);
        }

        private string GetKey(GrainReference grainReference)
        {
            var format = useJsonFormat ? "json" : "binary";
            return $"{grainReference.ToKeyString()}|{format}";
        }
    }
}
