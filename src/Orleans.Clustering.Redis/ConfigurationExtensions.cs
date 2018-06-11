using System;
using Orleans;
using Orleans.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Orleans.Messaging;
using Orleans.Runtime;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                    .AddSingleton<GatewayOptions>(new GatewayOptions() { GatewayListRefreshPeriod = GatewayOptions.DEFAULT_GATEWAY_LIST_REFRESH_PERIOD })
                    .AddRedis()
                    .AddAll<IGatewayListProvider, RedisGatewayListProvider>(context =>
                        new RedisGatewayListProvider(context.GetService<IMembershipTable>(), context.GetService<GatewayOptions>(), context.GetService<ILogger<RedisGatewayListProvider>>()));
            });
        }
        public static IClientBuilder UseRedisMembership(this IClientBuilder builder, string redisConnectionString, int db = 0)
        {
            builder.Configure<ClusterMembershipOptions>(x => x.ValidateInitialConnectivity = true);
            return builder.ConfigureServices(services => services
                .AddSingleton<RedisOptions>(new RedisOptions { Database = db, ConnectionString = redisConnectionString })
                .AddSingleton<GatewayOptions>(new GatewayOptions() { GatewayListRefreshPeriod = GatewayOptions.DEFAULT_GATEWAY_LIST_REFRESH_PERIOD })
                .AddRedis()
                .AddAll<IGatewayListProvider, RedisGatewayListProvider>(context =>
                    new RedisGatewayListProvider(context.GetService<IMembershipTable>(), context.GetService<GatewayOptions>(),
                        context.GetService<ILoggerFactory>().CreateLogger<RedisGatewayListProvider>())));
        }
        private static IServiceCollection AddAll<TInterface, TConcrete>(this IServiceCollection services, Func<IServiceProvider, TConcrete> configuration)
            where TInterface : class
            where TConcrete : class, TInterface
        {
            if (configuration == null)
            {
                return services.AddSingleton<TInterface, TConcrete>();//.AddScoped<TInterface, TConcrete>().AddTransient<TInterface, TConcrete>().AddSingleton<TInterface, TConcrete>();
            }
            return services.AddSingleton<TInterface, TConcrete>(configuration);
        }
        private static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(context =>
                ConnectionMultiplexer.Connect(context.GetService<RedisOptions>().ConnectionString))
                .AddAll<IMembershipTable, RedisMembershipTable>(context =>
                {
                    var options = context.GetService<RedisOptions>();
                    var multiplexer = context.GetService<IConnectionMultiplexer>();
                    return new RedisMembershipTable(multiplexer.GetDatabase(options.Database), context.GetService<ClusterOptions>(), context.GetService<ILoggerFactory>().CreateLogger<RedisMembershipTable>());
                });
                //.AddAll<IMembershipTableGrain, RedisMembershipTableGrain>(context => new RedisMembershipTableGrain(Guid.NewGuid()));
            return services;
        }
    }

}
