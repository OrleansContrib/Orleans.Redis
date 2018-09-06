using System;
using Orleans;
using Orleans.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using StackExchange.Redis;
using Orleans.Messaging;

namespace Orleans.Clustering.Redis
{
    public static class ConfigurationExtensions
    {
        public static ISiloHostBuilder UseRedisMembership(this ISiloHostBuilder builder, Action<RedisOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                var options = new RedisOptions();
                configuration?.Invoke(options);
                services.AddSingleton(options).AddRedis();
            });
        }

        public static ISiloHostBuilder UseRedisMembership(this ISiloHostBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .AddSingleton(new RedisOptions { Database = db, ConnectionString = redisConnectionString })
                .AddRedis());
        }

        public static IClientBuilder UseRedisMembership(this IClientBuilder builder, Action<RedisOptions> configuration)
        {
            builder.Configure<ClusterMembershipOptions>(x => x.ValidateInitialConnectivity = true);
            return builder.ConfigureServices(services =>
            {
                var options = new RedisOptions();
                configuration?.Invoke(options);

                services
                    .AddSingleton(options)
                    .AddSingleton(new GatewayOptions() { GatewayListRefreshPeriod = GatewayOptions.DEFAULT_GATEWAY_LIST_REFRESH_PERIOD })
                    .AddRedis()
                    .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>();
            });
        }

        public static IClientBuilder UseRedisMembership(this IClientBuilder builder, string redisConnectionString, int db = 0)
        {
            builder.Configure<ClusterMembershipOptions>(x => x.ValidateInitialConnectivity = true);
            return builder.ConfigureServices(services => services
                .Configure<RedisOptions>(opt =>
                {
                    opt.ConnectionString = redisConnectionString;
                    opt.Database = db;
                })
                .AddSingleton(new GatewayOptions() { GatewayListRefreshPeriod = GatewayOptions.DEFAULT_GATEWAY_LIST_REFRESH_PERIOD })
                .AddRedis()
                .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>());
        }

        private static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(context =>
                ConnectionMultiplexer.Connect(context.GetService<RedisOptions>().ConnectionString))
                .AddSingleton<IMembershipTable, RedisMembershipTable>();
            return services;
        }
    }

}
