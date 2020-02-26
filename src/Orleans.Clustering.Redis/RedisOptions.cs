using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Orleans.Clustering.Redis
{
    public class RedisOptions
    {
        public int Database { get; set; }

        public string ConnectionString { get; set; } = "localhost:6379";

        public Func<RedisOptions, Task<IConnectionMultiplexer>> CreateMultiplexer { get; set; } = DefaultCreateMultiplexer;

        public static async Task<IConnectionMultiplexer> DefaultCreateMultiplexer(RedisOptions options)
        {
            return await ConnectionMultiplexer.ConnectAsync(options.ConnectionString);
        }
    }
}