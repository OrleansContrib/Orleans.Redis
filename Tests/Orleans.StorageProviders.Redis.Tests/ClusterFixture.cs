using Orleans.Providers.Streams.SimpleMessageStream;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProviders.Redis.Tests
{
    public class ClusterFixture : IDisposable
    {
        public readonly TestCluster Cluster;
        public readonly IClusterClient Client;

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
            Console.WriteLine("Initialized Orleans TestCluster");
        }

        public void Dispose()
        {
            Client.Dispose();
            Cluster.StopAllSilos();
        }
    }
}
