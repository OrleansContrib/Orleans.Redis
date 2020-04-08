using System;
using StackExchange.Redis;
namespace Orleans.Clustering.Redis.Test
{
    public class MultiplexerFixture : IDisposable
    {
        public RedisClusteringOptions DatabaseOptions { get; private set; }
        public IConnectionMultiplexer Multiplexer { get; private set; }

        public MultiplexerFixture() { }

        public void Initialize(RedisClusteringOptions options)
        {
            DatabaseOptions = options;
            Multiplexer = ConnectionMultiplexer.Connect(options.ConnectionString);
        }

        public void Dispose()
        {
            Multiplexer?.Close();
        }
    }
}
