using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

[assembly: InternalsVisibleTo("Orleans.Clustering.Redis.Test")]

namespace Orleans.Clustering.Redis
{
    [Serializable]
    public class RedisClusteringException : Exception
    {
        /// <inheritdoc/>
        public RedisClusteringException() : base() { }

        /// <inheritdoc/>
        public RedisClusteringException(string message) : base(message) { }

        /// <inheritdoc/>
        public RedisClusteringException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc/>
        protected RedisClusteringException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}