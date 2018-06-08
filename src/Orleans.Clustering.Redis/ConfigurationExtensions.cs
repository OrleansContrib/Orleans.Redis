using System;
using Orleans;
using Orleans.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Microsoft.Extensions.Options;
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

        public static ISiloHostBuilder UserRedisMembership(this ISiloHostBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .AddSingleton(new RedisOptions { Database = db, ConnectionString = redisConnectionString })
                .AddRedis());
        }

        public static IClientBuilder UseRedisGatewayList(this IClientBuilder builder, Action<RedisOptions> configuration)
        {
            return builder.ConfigureServices(services =>
            {
                var options = new RedisOptions();
                configuration?.Invoke(options);                
                services.AddSingleton(options).AddRedis()
                    .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>();
            });
        }
        public static IClientBuilder UseRedisGatewayList(this IClientBuilder builder, string redisConnectionString, int db = 0)
        {
            return builder.ConfigureServices(services => services
                .AddSingleton<RedisOptions>(new RedisOptions { Database = db, ConnectionString = redisConnectionString })
                .AddRedis()
                .AddSingleton<IGatewayListProvider, RedisGatewayListProvider>());
        }

        private static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(context => ConnectionMultiplexer.Connect(context.GetService<RedisOptions>().ConnectionString))
                .AddSingleton<IMembershipTable>(context =>
                {
                    var options = context.GetService<RedisOptions>();
                    var multiplexer = context.GetService<IConnectionMultiplexer>();
                    return new RedisMembershipTable(multiplexer.GetDatabase(options.Database), context.GetService<ClusterOptions>());
                });

            return services;
        }
    }
}
