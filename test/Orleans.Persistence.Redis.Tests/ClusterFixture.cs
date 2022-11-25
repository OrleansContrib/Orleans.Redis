using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.TestingHost;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Orleans.Persistence.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        private readonly ConnectionMultiplexer _redis;

        public ClusterFixture()
        {
            var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
            var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            var redisConnectionString = $"{redisHost}:{redisPort}, allowAdmin=true";

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            _redis = ConnectionMultiplexer.ConnectAsync(redisOptions).Result;
            this.Database = _redis.GetDatabase();

            var builder = new TestClusterBuilder(1);
            builder.Options.ServiceId = "Service";
            builder.Options.ClusterId = "TestCluster";
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
            //builder.AddClientBuilderConfigurator<ClientConfigurator>();

            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "RedisConnectionString", redisConnectionString }
                });
            });

            Cluster = builder.Build();

            Cluster.Deploy();
            Cluster.InitializeClientAsync().Wait();
            Client = Cluster.Client;
        }

        public TestCluster Cluster { get; }
        public IClusterClient Client { get; }
        public IDatabase Database { get; }

        public class SiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder builder)
            {
                var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
                var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
                var redisConnectionString = $"{redisHost}:{redisPort}, allowAdmin=true";

                builder.AddMemoryGrainStorageAsDefault();
                builder.AddRedisGrainStorage("Redis", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = redisConnectionString;
                }));

                //builder.AddMemoryStreams<DefaultMemoryMessageBodySerializer>("MSProvider");
            }
        }

        //public class ClientConfigurator : IClientBuilderConfigurator
        //{
        //    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        //    {
        //        clientBuilder.AddMemoryStreams<DefaultMemoryMessageBodySerializer>("MSProvider");
        //    }
        //}

        public void Dispose()
        {
            Database.ExecuteAsync("FLUSHALL").Wait();
            //Client.Dispose();
            Cluster.StopAllSilos();
            _redis?.Dispose();
        }
    }
}
