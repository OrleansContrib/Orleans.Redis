using System;
using StackExchange.Redis;
namespace Orleans.Clustering.Redis.Test
{
    public class MultiplexerFixture : IDisposable
    {
        public RedisOptions DatabaseOptions { get; private set; }
        public IConnectionMultiplexer Multiplexer { get; private set; }

        public MultiplexerFixture() { }

        public void Initialize(RedisOptions options)
        {
            DatabaseOptions = options;
            Multiplexer = ConnectionMultiplexer.Connect(DatabaseOptions.ConnectionString + ", allowAdmin=true");
        }

        public void Dispose()
        {
            Multiplexer.Close();
        }
    }
}
