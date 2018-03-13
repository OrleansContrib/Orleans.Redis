using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;
using Orleans.Providers.Streams.SimpleMessageStream;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;
using Orleans.TestingHost;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProviders.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        public readonly TestCluster Cluster;
        public readonly IClusterClient Client;
        
        public readonly IDatabase Database;

        public ClusterFixture()
        {
            Console.WriteLine("Initializing Orleans TestCluster");
            var options = new TestClusterOptions();
            options.ClusterConfiguration.AddRedisStorageProvider("REDIS-BINARY");
            options.ClusterConfiguration.AddRedisStorageProvider("REDIS-JSON", useJson: true);
            
            options.ClusterConfiguration.AddRedisStorageProvider("PubSubStore");
            options.ClusterConfiguration.Globals.RegisterStreamProvider<SimpleMessageStreamProvider>("SMSProvider");
            
            options.ClientConfiguration.RegisterStreamProvider<SimpleMessageStreamProvider>("SMSProvider");

            options.ClusterConfiguration.Globals.ClusterId = "TestCluster";
            
            var cluster = new TestCluster(options);

            cluster.Deploy();
            cluster.InitializeClient();
            Cluster = cluster;
            Client = cluster.Client;

            var redisOptions = ConfigurationOptions.Parse("localhost:6379");
            var connection = ConnectionMultiplexer.ConnectAsync(redisOptions).Result;
            Database = connection.GetDatabase();
            Console.WriteLine("Initialized Orleans TestCluster");
        }

        public void Dispose()
        {
            Database.ExecuteAsync("FLUSHALL").Wait();
            Client.Dispose();
            Cluster.StopAllSilos();
        }
    }
}
