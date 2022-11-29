using Newtonsoft.Json;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using Orleans.Persistence.Redis.TestGrains;
using Orleans.Storage;
using Orleans.Streams;
using Orleans.TestingHost;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Persistence.Redis.Tests
{
    public class StorageTests : IClassFixture<ClusterFixture>
    {
        private readonly ClusterFixture _fixture;

        private TestCluster _cluster => _fixture.Cluster;
        //private IClusterClient _client => _fixture.Client;

        public StorageTests(ClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InitializeWithNoStateTest()
        {
            var grain = _cluster.GrainFactory.GetGrain<ITestGrain>(0);
            var result = await grain.Get();

            Assert.Equal(default(string), result.Item1);
            Assert.Equal(default(int), result.Item2);
            Assert.Equal(default(DateTime), result.Item3);
            Assert.Equal(default(Guid), result.Item4);
            Assert.Equal(default(ITestGrain), result.Item5);
        }

        [Fact]
        public async Task TestStaticIdentifierGrains()
        {
            var grain = _cluster.GrainFactory.GetGrain<ITestGrain>(12345);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12345, now, guid, _cluster.GrainFactory.GetGrain<ITestGrain>(2222));
            var result = await grain.Get();
            Assert.Equal("string value", result.Item1);
            Assert.Equal(12345, result.Item2);
            Assert.Equal(now, result.Item3);
            Assert.Equal(guid, result.Item4);
            Assert.Equal(2222, result.Item5.GetPrimaryKeyLong());

            var grain2 = _cluster.GrainFactory.GetGrain<ITestGrain2>(12345);
            var result2 = await grain2.Get();
            Assert.Equal(default(string), result2.Item1);
            Assert.Equal(default(int), result2.Item2);
            Assert.Equal(default(DateTime), result2.Item3);
            Assert.Equal(default(Guid), result2.Item4);
            Assert.Equal(default(ITestGrain), result2.Item5);

            await grain2.Set("string value2", 12345, now, guid, _cluster.GrainFactory.GetGrain<ITestGrain>(2222));
            result2 = await grain2.Get();
            Assert.Equal("string value2", result2.Item1);
            Assert.Equal(12345, result2.Item2);
            Assert.Equal(now, result2.Item3);
            Assert.Equal(guid, result2.Item4);
            Assert.Equal(2222, result2.Item5.GetPrimaryKeyLong());
        }

        [Fact]
        public async Task TestRedisScriptCacheClearBeforeGrainWriteState()
        {
            var grain = _cluster.GrainFactory.GetGrain<ITestGrain>(1111);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();

            await _fixture.Database.ExecuteAsync("SCRIPT", "FLUSH", "SYNC");
            await grain.Set("string value", 12345, now, guid, _cluster.GrainFactory.GetGrain<ITestGrain>(2222));

            var result = await grain.Get();
            Assert.Equal("string value", result.Item1);
            Assert.Equal(12345, result.Item2);
            Assert.Equal(now, result.Item3);
            Assert.Equal(guid, result.Item4);
            Assert.Equal(2222, result.Item5.GetPrimaryKeyLong());
        }

        [Fact]
        public async Task Double_Activation_ETag_Conflict_Simulation()
        {
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            var grain = _cluster.GrainFactory.GetGrain<ITestGrain>(54321);
            var grainId = grain.GetGrainId();

            var stuff = await grain.Get();
            var scheduler = TaskScheduler.Current;

            var key = grainId.ToString(); // $"{grainId}|json";
            await _fixture.Database.HashSetAsync(key, new[] { new HashEntry("etag", "derp") });

            var otherGrain = _cluster.GrainFactory.GetGrain<ITestGrain>(2222);
            await Assert.ThrowsAsync<InconsistentStateException>(() => grain.Set("string value", 12345, now, guid, otherGrain));
        }
    }
}
