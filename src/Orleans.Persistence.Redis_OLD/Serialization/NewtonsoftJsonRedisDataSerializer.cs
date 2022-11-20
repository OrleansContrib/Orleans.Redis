using System;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    /// <summary>
    /// Redis data serializer backed by <see cref="JsonConvert"/>.
    /// </summary>
    public class NewtonsoftJsonRedisDataSerializer : IRedisDataSerializer
    {
        private readonly JsonSerializerSettings _jsonSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="NewtonsoftJsonRedisDataSerializer"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureJsonSerializeSettings"></param>
        public NewtonsoftJsonRedisDataSerializer(IServiceProvider services, Action<JsonSerializerSettings> configureJsonSerializeSettings = null)
        {
            _jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(services);
            configureJsonSerializeSettings?.Invoke(_jsonSettings);
        }

        /// <inheritdoc />
        public string FormatSpecifier => "json";

        /// <inheritdoc />
        public RedisValue SerializeObject(object item)
        {
            return JsonConvert.SerializeObject(item, _jsonSettings);
        }

        /// <inheritdoc />
        //public object DeserializeObject(Type type, RedisValue serializedValue)
        //{
        //    return JsonConvert.DeserializeObject(serializedValue, type, _jsonSettings);
        //}

        /// <inheritdoc />
        public T DeserializeObject<T>(RedisValue serializedValue)
        {
            return JsonConvert.DeserializeObject<T>(serializedValue, _jsonSettings);
        }
    }
}