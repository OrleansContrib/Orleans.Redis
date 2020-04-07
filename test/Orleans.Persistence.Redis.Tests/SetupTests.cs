using System.Net;
using Xunit;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Persistence.Redis.TestGrains;
using Orleans.Persistence.Redis.TestGrainInterfaces;

namespace Orleans.Persistence.Redis.Tests
{
    public class SetupTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("123")]
        public void StorageOptionsValidator(string connectionString)
        {
            var siloPort = 11111;
            int gatewayPort = 30000;
            var siloAddress = IPAddress.Loopback;
            
            var builder = new SiloHostBuilder();
            Assert.Throws<OrleansConfigurationException>(() => 
            {
                var silo = builder
                    .Configure<ClusterOptions>(options => options.ClusterId = "TESTCLUSTER")
                    .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
                    .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
                    .ConfigureApplicationParts(pm =>
                    {
                        pm.AddApplicationPart(typeof(JsonTestGrain).Assembly);
                        pm.AddApplicationPart(typeof(IJsonTestGrain).Assembly);
                    })
                    .AddRedisGrainStorage("Redis", optionsBuilder => optionsBuilder.Configure(options =>
                    {
                        options.DataConnectionString = connectionString;
                    }))
                    .Build();
            });
        }
    }
}
