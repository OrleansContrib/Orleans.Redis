using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Orleans.Serialization;
using StackExchange.Redis;

namespace Orleans.Persistence.Redis.Serialization
{
    /// <summary>
    /// Redis data serializer backed by <see cref="BinaryFormatter"/>.
    /// </summary>
    public class BinaryFormatterRedisDataSerializer : IRedisDataSerializer
    {
        private readonly BinaryFormatter _formatter;

        /// <summary>
        /// Initializes a new instance of <see cref="BinaryFormatterRedisDataSerializer"/>.
        /// </summary>
        public BinaryFormatterRedisDataSerializer()
        {
            _formatter = new BinaryFormatter();
        }

        /// <inheritdoc />
        public string FormatSpecifier => "binary";

        /// <inheritdoc />
        public RedisValue SerializeObject(object item)
        {
            using (MemoryStream ms = new())
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                _formatter.Serialize(ms, item);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                return ms.ToString();
            }
        }

        /// <inheritdoc />
        //public object DeserializeObject(Type type, RedisValue serializedValue)
        //{
        //    return _serializationManager.DeserializeFromByteArray<object>(serializedValue);
        //}

        /// <inheritdoc />
        public T DeserializeObject<T>(RedisValue serializedValue)
        {
            using (MemoryStream ms = new())
            {
                ms.Write(Encoding.UTF8.GetBytes(serializedValue.ToString()));
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                return (T)_formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }
    }
}