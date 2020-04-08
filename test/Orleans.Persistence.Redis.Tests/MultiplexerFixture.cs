using StackExchange.Redis;
using System;

namespace Orleans.Persistence.Redis.Tests
{
    public class MultiplexerFixture : IDisposable
    {
        public IConnectionMultiplexer Multiplexer { get; private set; }

        public MultiplexerFixture() { }

        public void Initialize(ConfigurationOptions options)
        {
            Multiplexer = ConnectionMultiplexer.Connect(options);
        }

        public void Dispose()
        {
            Multiplexer?.Close();
        }
    }
}
