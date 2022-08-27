using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Storage;
using System;
using Orleans.Persistence.Redis.Serialization;
using Orleans.Runtime;
using Orleans.Serialization;

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

            IRedisDataSerializer serializer = services.GetService<IRedisDataSerializer>();
            var redisStorageOptions = options.Get(name);

            if (serializer == null)
            {
                if (redisStorageOptions.UseJson)
                {
                    serializer =  new NewtonsoftJsonRedisDataSerializer(services.GetService<ITypeResolver>(),
                        services.GetService<IGrainFactory>(), redisStorageOptions.ConfigureJsonSerializerSettings);
                }
                else
                {
                    serializer =  new SerializationManagerRedisDataSerializer(services.GetService<SerializationManager>());
                }
            }


            return ActivatorUtilities.CreateInstance<RedisGrainStorage>(services, serializer, redisStorageOptions, name);
        }
    }
}
