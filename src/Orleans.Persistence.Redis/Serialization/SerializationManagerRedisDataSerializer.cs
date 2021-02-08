using System;
using Orleans.Serialization;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    /// <summary>
    /// Redis data serializer backed by <see cref="SerializationManager"/>.
    /// </summary>
    public class SerializationManagerRedisDataSerializer : IRedisDataSerializer
    {
        private readonly SerializationManager _serializationManager;

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationManagerRedisDataSerializer"/>.
        /// </summary>
        /// <param name="serializationManager"></param>
        public SerializationManagerRedisDataSerializer(SerializationManager serializationManager)
        {
            _serializationManager = serializationManager;
        }

        /// <inheritdoc />
        public string FormatSpecifier => "binary";

        /// <inheritdoc />
        public RedisValue SerializeObject(object item)
        {
            return _serializationManager.SerializeToByteArray(item);
        }

        /// <inheritdoc />
        public object DeserializeObject(Type type, RedisValue serializedValue)
        {
            return _serializationManager.DeserializeFromByteArray<object>(serializedValue);
        }
    }
}