using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using RedisInside;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Orleans.Persistence.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        public readonly TestCluster Cluster;
        public readonly IClusterClient Client;
        
        public readonly IDatabase Database;
        private readonly RedisInside.Redis _redis;

        public ClusterFixture()
        {
            _redis = new RedisInside.Redis();
            Console.WriteLine(_redis.Endpoint.ToString());

            Console.WriteLine("Initializing Orleans TestCluster");
            var builder = new TestClusterBuilder(1);
            builder.Options.ServiceId = "Service";
            builder.Options.ClusterId = "TestCluster";
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
            builder.AddClientBuilderConfigurator<ClientConfigurator>();

            //this is one of the only ways to be able to pass data (the redis connection string) to the silo(s) that TestCluster will startup
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { nameof(RedisInside.Redis), _redis.Endpoint.ToString() }
                });
            });

            Cluster = builder.Build();

            Cluster.Deploy();
            Cluster.InitializeClient();
            Client = Cluster.Client;

            var redisOptions = ConfigurationOptions.Parse(_redis.Endpoint.ToString());
            var connection = ConnectionMultiplexer.ConnectAsync(redisOptions).Result;
            Database = connection.GetDatabase();
            Console.WriteLine("Initialized Orleans TestCluster");
        }

        public class SiloConfigurator : ISiloBuilderConfigurator
        {
            public void Configure(ISiloHostBuilder builder)
            {
                //get the redis connection string from the testcluster's config
                var redisEP = builder.GetConfigurationValue(nameof(RedisInside.Redis));

                builder.AddMemoryGrainStorageAsDefault();
                builder.AddRedisGrainStorage("REDIS-JSON", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.UseJson = true;
                    options.DataConnectionString = redisEP;
                }));
                builder.AddRedisGrainStorage("REDIS-BINARY", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.UseJson = false;
                    options.DataConnectionString = redisEP;
                }));

                builder.AddRedisGrainStorage("PubSubStore", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.UseJson = false;
                    options.DataConnectionString = redisEP;
                }));

                builder.AddSimpleMessageStreamProvider("SMSProvider");
            }
        }

        public class ClientConfigurator : IClientBuilderConfigurator
        {
            public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
            {
                clientBuilder.AddSimpleMessageStreamProvider("SMSProvider");
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
