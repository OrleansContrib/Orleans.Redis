using System;
using System.Threading.Tasks;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using Orleans.TestingHost;
using Xunit;

namespace Orleans.Persistence.Redis.Tests
{
    public class JsonPubSubTests : IClassFixture<ClusterFixtureJsonPubSub>
    {
        private readonly ClusterFixtureJsonPubSub _fixture;

        private TestCluster _cluster => _fixture.Cluster;
        private IClusterClient _client => _fixture.Client;

        public JsonPubSubTests(ClusterFixtureJsonPubSub fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task PubSubStorageJson_GrainStreamingWorks()
        {
            var grainId = Guid.NewGuid();

            var grain = _client.GetGrain<IStreamingTestGrain>(grainId);

            var lastItem = await grain.GetLastItem();
            Assert.Null(lastItem);
            
            var streamProv = _client.GetStreamProvider("SMSProvider");
            var stream = streamProv.GetStream<StreamItem>(grainId, "StreamItems");
            
            var item1 = new StreamItem
            {
                Message = "First message"
            };

            await stream.OnNextAsync(item1);
            
            lastItem = await grain.GetLastItem();
            Assert.NotNull(lastItem);
            Assert.Equal(item1.Message, lastItem.Message);

            await grain.Deactivate();
            
            var item2 = new StreamItem
            {
                Message = "Second message"
            };
            
            await stream.OnNextAsync(item2);
            
            grain = _client.GetGrain<IStreamingTestGrain>(grainId);
            
            lastItem = await grain.GetLastItem();
            Assert.NotNull(lastItem);
            Assert.Equal(item2.Message, lastItem.Message);
            // Is there a way to force the PubSubRendezvousGrain to reactivate in tests to validate state restore?
        }
        
    }
}