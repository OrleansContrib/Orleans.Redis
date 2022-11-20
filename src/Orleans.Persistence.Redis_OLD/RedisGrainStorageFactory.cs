using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Storage;
using System;
using Orleans.Persistence.Redis.Serialization;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;
using System.Runtime.CompilerServices;

namespace Orleans.Persistence
{
    /// <summary>
    /// Factory used to create instances of Redis grain storage.
    /// </summary>
    public static class RedisGrainStorageFactory
    {
        /// <summary>
        /// Creates a grain storage instance.
        /// </summary>
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            IOptionsMonitor<RedisStorageOptions> options = services.GetRequiredService<IOptionsMonitor<RedisStorageOptions>>();

            IRedisDataSerializer serializer;
            var redisStorageOptions = options.Get(name);

            switch (redisStorageOptions.Formatter)
            {
                case "native":
                case "json":
                    serializer = new NativeJsonRedisDataSerializer(services);
                    break;

                case "newtonsoft":
                    serializer = new NewtonsoftJsonRedisDataSerializer(services);
                    break;

                case "binary":
                    serializer = new BinaryFormatterRedisDataSerializer();
                    break;

                default:
                    throw new OrleansConfigurationException("Invalid formatter");
            }         

            return ActivatorUtilities.CreateInstance<RedisGrainStorage>(services, serializer, redisStorageOptions, name);
        }
    }
}
