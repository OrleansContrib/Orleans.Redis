﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Orleans.Reminders.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        private readonly ConnectionMultiplexer _redis;

        public ClusterFixture()
        {
            var builder = new TestClusterBuilder(1);
            builder.Options.ServiceId = "Service";
            builder.Options.ClusterId = "TestCluster";
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();

            var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            var redisConnectionString = $"{redisHost}:{redisPort}, allowAdmin=true";

            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "RedisConnectionString", redisConnectionString }
                });
            });

            Cluster = builder.Build();

            Cluster.Deploy();
            Cluster.InitializeClient();
            Client = Cluster.Client;

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            _redis = ConnectionMultiplexer.ConnectAsync(redisOptions).Result;
            this.Database = _redis.GetDatabase();
        }

        public TestCluster Cluster { get; }
        public IClusterClient Client { get; }
        public IDatabase Database { get; }

        public class SiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder builder)
            {
                //get the redis connection string from the testcluster's config
                var redisEP = builder.GetConfigurationValue("RedisConnectionString");

                builder.UseRedisReminderService(options =>
                {
                    options.ConnectionString = redisEP;
                });
            }
        }

        public void Dispose()
        {
            Database.ExecuteAsync("FLUSHALL").Wait();
            Client.Dispose();
            Cluster.StopAllSilos();
            _redis?.Dispose();
        }
    }
}
