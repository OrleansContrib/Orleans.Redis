using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Orleans.Configuration;
using Orleans.Runtime.ReminderService;

namespace Orleans.Hosting
{
    /// <summary>
    /// Class SiloHostBuilderReminderExtensions.
    /// </summary>
    public static class SiloHostBuilderReminderExtensions
    {
        /// <summary>
        /// Uses the redis reminder service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns>ISiloHostBuilder.</returns>
        public static ISiloHostBuilder UseRedisReminderService(
            this ISiloHostBuilder builder,
            Action<RedisReminderTableOptions> configureOptions)
        {
            return builder.UseRedisReminderService(ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Uses the redis reminder service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns>ISiloHostBuilder.</returns>
        public static ISiloHostBuilder UseRedisReminderService(
            this ISiloHostBuilder builder,
            Action<OptionsBuilder<RedisReminderTableOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseRedisReminderService(configureOptions));
        }

        /// <summary>
        /// Uses the redis reminder service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns>ISiloBuilder.</returns>
        public static ISiloBuilder UseRedisReminderService(
            this ISiloBuilder builder,
            Action<RedisReminderTableOptions> configureOptions)
        {
            return builder.UseRedisReminderService(ob => ob.Configure(configureOptions));
        }

        /// <summary>
        /// Uses the redis reminder service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns>ISiloBuilder.</returns>
        public static ISiloBuilder UseRedisReminderService(
            this ISiloBuilder builder,
            Action<OptionsBuilder<RedisReminderTableOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseRedisReminderService(configureOptions));
        }

        /// <summary>
        /// Uses the redis reminder service.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns>IServiceCollection.</returns>
        public static IServiceCollection UseRedisReminderService(this IServiceCollection services, Action<OptionsBuilder<RedisReminderTableOptions>> configureOptions)
        {
            configureOptions(services.AddOptions<RedisReminderTableOptions>());
            services.AddSingleton<IReminderTable, RedisReminderTable>();
            services.ConfigureFormatter<RedisReminderTableOptions>();
            services.AddTransient<IConfigurationValidator, RedisReminderTableOptionsValidator>();
            return services;
        }
    }
}