using System;
using System.Text.Json;
using Orleans.Serialization;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    /// <summary>
    /// Redis data serializer backed by <see cref="JsonSerializer"/>.
    /// </summary>
    public class NativeJsonRedisDataSerializer : IRedisDataSerializer
    {
        private readonly JsonSerializerOptions _jsonSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeJsonRedisDataSerializer"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureJsonSerializerOptions"></param>
        public NativeJsonRedisDataSerializer(IServiceProvider services, Action<JsonSerializerOptions> configureJsonSerializerOptions = null)
        {
            //_jsonSettings = OrleansJsonSerializerOptions.GetDefaultSerializerSettings(services);
            configureJsonSerializerOptions?.Invoke(_jsonSettings);
        }

        /// <inheritdoc />
        public string FormatSpecifier => "json";

        /// <inheritdoc />
        public RedisValue SerializeObject(object item)
        {
            return JsonSerializer.Serialize(item, _jsonSettings);
        }

        /// <inheritdoc />
        //public object DeserializeObject(Type type, RedisValue serializedValue)
        //{
        //    return JsonSerializer.Deserialize(serializedValue, type, _jsonSettings);
        //}

        /// <inheritdoc />
        public T DeserializeObject<T>(RedisValue serializedValue)
        {
            return JsonSerializer.Deserialize<T>(serializedValue, _jsonSettings);
        }
    }
}