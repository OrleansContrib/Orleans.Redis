using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Orleans.Runtime;
using Orleans.Providers;
using System.IO;
using Newtonsoft.Json;
using Orleans.Serialization;

namespace Orleans.StorageProviders
{
    public class RedisStorage : IStorageProvider
    {
        private ConnectionMultiplexer connectionMultiplexer;
        private IDatabase redisDatabase;

        private const string REDIS_CONNECTION_STRING = "RedisConnectionString";
        private const string REDIS_DATABASE_NUMBER = "DatabaseNumber";
        private const string USE_JSON_FORMAT_PROPERTY = "UseJsonFormat";


        private string serviceId;
        private bool useJsonFormat;
        private Newtonsoft.Json.JsonSerializerSettings jsonSettings;

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

            if (!config.Properties.ContainsKey(REDIS_CONNECTION_STRING) ||
                string.IsNullOrWhiteSpace(config.Properties[REDIS_CONNECTION_STRING]))
            {
                throw new ArgumentException("RedisConnectionString is not set.");
            }
            var connectionString = config.Properties[REDIS_CONNECTION_STRING];

            connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionString);

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

            if (config.Properties.ContainsKey(USE_JSON_FORMAT_PROPERTY))
                useJsonFormat = "true".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase);
            else
                useJsonFormat = false;


            jsonSettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            };

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
            return TaskDone.Done;
        }

        /// <summary> Read state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#ReadStateAsync"/>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, GrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();

            if (Log.IsVerbose3)
            {
                Log.Verbose3((int)ProviderErrorCode.RedisStorageProvider_ReadingData, "Reading: GrainType={0} Pk={1} Grainid={2} from Database={3}", grainType, primaryKey, grainReference, redisDatabase.Database);
            }

            RedisValue value = await redisDatabase.StringGetAsync(primaryKey);
            var data = new Dictionary<string, object>();
            if (value.HasValue)
            {
                if (!useJsonFormat)
                    data = JsonConvert.DeserializeObject<Dictionary<string, object>>(value, jsonSettings);
                else
                    data = SerializationManager.DeserializeFromByteArray<Dictionary<string, object>>(value);
            }

            grainState.SetAll(data);

            grainState.Etag = Guid.NewGuid().ToString();
        }

        /// <summary> Write state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#WriteStateAsync"/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, GrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) ProviderErrorCode.RedisStorageProvider_WritingData, "Writing: GrainType={0} PrimaryKey={1} Grainid={2} ETag={3} to Database={4}", grainType, primaryKey, grainReference, grainState.Etag, redisDatabase.Database);
            }
            var data = grainState.AsDictionary();

            if (!useJsonFormat)
            {
                byte[] payload = SerializationManager.SerializeToByteArray(data);
                await redisDatabase.StringSetAsync(primaryKey, payload);
            }
            else
            {
                var payload = JsonConvert.SerializeObject(data, jsonSettings);
                await redisDatabase.StringSetAsync(primaryKey, payload);
            }

        }

        /// <summary> Clear state data function for this storage provider. </summary>
        /// <remarks>
        /// </remarks>
        /// <see cref="IStorageProvider#ClearStateAsync"/>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, GrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) ProviderErrorCode.RedisStorageProvider_ClearingData, "Clearing: GrainType={0} Pk={1} Grainid={2} ETag={3} DeleteStateOnClear={4} from Table={5}", grainType, primaryKey, grainReference, grainState.Etag, redisDatabase.Database);
            }
            //remove from cache
            return redisDatabase.KeyDeleteAsync(primaryKey);
            //return TaskDone.Done;
        }

     
    }
}
