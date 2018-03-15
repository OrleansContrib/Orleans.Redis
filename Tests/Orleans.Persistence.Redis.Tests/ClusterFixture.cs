using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using StackExchange.Redis;
using System;

namespace Orleans.Persistence.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        public readonly TestCluster Cluster;
        public readonly IClusterClient Client;
        
        public readonly IDatabase Database;

        public ClusterFixture()
        {
            Console.WriteLine("Initializing Orleans TestCluster");

            var builder = new TestClusterBuilder(1);
            builder.Options.ServiceId = Guid.NewGuid();
            builder.Options.ClusterId = "TestCluster";
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
            builder.AddClientBuilderConfigurator<ClientConfigurator>();
            Cluster = builder.Build();

            Cluster.Deploy();
            Cluster.InitializeClient();
            Client = Cluster.Client;

            var redisOptions = ConfigurationOptions.Parse("localhost:6379");
            var connection = ConnectionMultiplexer.ConnectAsync(redisOptions).Result;
            Database = connection.GetDatabase();
            Console.WriteLine("Initialized Orleans TestCluster");
        }

        public class SiloConfigurator : ISiloBuilderConfigurator
        {
            public void Configure(ISiloHostBuilder builder)
            {
                builder.AddMemoryGrainStorageAsDefault();
                builder.AddRedisGrainStorage("REDIS-JSON", optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.UseJson = true;
                }));
                builder.AddRedisGrainStorage("REDIS-BINARY");

                builder.AddRedisGrainStorage("PubSubStore");
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
        }
    }
}
