using System;
using Orleans;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Clustering.Redis;

namespace Microsoft.Extensions.Hosting
{
    public static class RedisClusteringISiloBuilderExtensions
    {
        public static ISiloBuilder UseRedisClustering(this ISiloBuilder builder, Action<RedisOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                if (configuration != null)
                {
                    services.Configure(configuration);
                }

                services.AddRedis();
            });
        }

        public static ISiloBuilder UseRedisClustering(this ISiloBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .Configure<RedisOptions>(options => { options.Database = db; options.ConnectionString = redisConnectionString; })
                .AddRedis());
        }

        internal static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<RedisMembershipTable>();
            services.AddSingleton<IMembershipTable>(sp => sp.GetRequiredService<RedisMembershipTable>());
            return services;
        }
    }
}
