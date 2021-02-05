using System;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    /// <summary>
    /// Defines an interface for Redis data serializer
    /// </summary>
    public interface IRedisDataSerializer
    {
        /// <summary>
        /// Serializes the item as a <see cref="RedisValue"/>. The actual backing type is determined by the serializer implementation.
        /// </summary>
        /// <param name="item">The object to serialize.</param>
        /// <returns>The serialized data as <see cref="RedisValue"/>.</returns>
        RedisValue SerializeObject(object item);

        /// <summary>
        /// Deserializes the serialized object data.
        /// </summary>
        /// <param name="type">The type of the deserialized instance.</param>
        /// <param name="serializedValue">The serialized data.</param>
        /// <returns>The deserialized instance.</returns>
        object DeserializeObject(Type type, RedisValue serializedValue);
    }
}