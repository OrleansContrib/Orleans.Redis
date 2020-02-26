using System;
using Orleans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Clustering.Redis;

namespace Orleans.Hosting
{
    public static class RedisClusteringISiloHostBuilderExtensions
    {
        public static ISiloHostBuilder UseRedisClustering(this ISiloHostBuilder builder, Action<RedisOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                if (configuration != null)
                {
                    services.Configure<RedisOptions>(configuration);
                }

                services.AddRedis();
            });
        }

        public static ISiloHostBuilder UseRedisClustering(this ISiloHostBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .Configure<RedisOptions>(options => { options.Database = db; options.ConnectionString = redisConnectionString; })
                .AddRedis());
        }
    }
}
