using System;
using Orleans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Clustering.Redis;

namespace Orleans.Hosting
{
    /// <summary>
    /// Hosting extensions for the Redis clustering provider.
    /// </summary>
    public static class RedisClusteringISiloHostBuilderExtensions
    {
        /// <summary>
        /// Configures Redis as the clustering provider.
        /// </summary>
        public static ISiloHostBuilder UseRedisClustering(this ISiloHostBuilder builder, Action<RedisClusteringOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                if (configuration != null)
                {
                    services.Configure<RedisClusteringOptions>(configuration);
                }

                services.AddRedis();
            });
        }

        /// <summary>
        /// Configures Redis as the clustering provider.
        /// </summary>
        public static ISiloHostBuilder UseRedisClustering(this ISiloHostBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .Configure<RedisClusteringOptions>(options => { options.Database = db; options.ConnectionString = redisConnectionString; })
                .AddRedis());
        }
    }
}
