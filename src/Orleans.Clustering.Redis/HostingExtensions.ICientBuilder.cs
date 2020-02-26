using System;
using Orleans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Clustering.Redis;

namespace Orleans.Hosting
{
    public static class RedisClusteringIClientBuilderExtensions
    {
        public static IClientBuilder UseRedisClustering(this IClientBuilder builder, Action<RedisOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                if (configuration != null)
                {
                    services.Configure(configuration);
                }

                services
                    .AddRedis()
                    .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>();
            });
        }

        public static IClientBuilder UseRedisClustering(this IClientBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .Configure<RedisOptions>(opt =>
                {
                    opt.ConnectionString = redisConnectionString;
                    opt.Database = db;
                })
                .AddRedis()
                .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>());
        }

    }
}
