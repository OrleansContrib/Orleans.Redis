using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    public class SystemTextJsonRedisDataSerializerOptions {
        public JsonSerializerOptions JsonOptions { get; set; }
    }
    
    public class SystemTextJsonRedisDataSerializer : IRedisDataSerializer
    {
        SystemTextJsonRedisDataSerializerOptions _options;
        
        public SystemTextJsonRedisDataSerializer(IServiceProvider serviceProvider)
        {
            _options = serviceProvider.GetService<SystemTextJsonRedisDataSerializerOptions>();
        }
        
        public string FormatSpecifier { get; } = "sjson";
        public RedisValue SerializeObject(object item)
        {
            return JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), _options?.JsonOptions);
        }

        public object DeserializeObject(Type type, RedisValue serializedValue)
        {
            return JsonSerializer.Deserialize(serializedValue, type, _options?.JsonOptions);
        }
    }

    public static class SystemTextJsonRedisDataSerializerExtension
    {
        /// <summary>
        /// Use system text json serializer
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ISiloBuilder AddRedisGrainStorageJsonSerializer(this ISiloBuilder builder, JsonSerializerOptions options = null)
        {
            if (options != null)
            {
                builder.ConfigureServices(s =>
                {
                    s.AddSingleton(new SystemTextJsonRedisDataSerializerOptions()
                    {
                        JsonOptions = options
                    });
                });
            }

            builder.AddRedisGrainStorageSerializer<SystemTextJsonRedisDataSerializer>();

            return builder;
        }
    }
    
    
}