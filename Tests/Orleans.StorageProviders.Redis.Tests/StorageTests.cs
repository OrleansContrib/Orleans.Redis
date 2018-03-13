using Newtonsoft.Json;
using Orleans.Storage;
using Orleans.StorageProviders.Redis.TestGrainInterfaces;
using Orleans.StorageProviders.Redis.TestGrains;
using Orleans.Streams;
using Orleans.TestingHost;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.StorageProviders.Redis.Tests
{
    public class StorageTests : IClassFixture<ClusterFixture>
    {
        private readonly ClusterFixture _fixture;

        private TestCluster _cluster => _fixture.Cluster;
        private IClusterClient _client => _fixture.Client;

        public StorageTests(ClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Binary_InitializeWithNoStateTest()
        {
            var grain = _cluster.GrainFactory.GetGrain<IBinaryTestGrain>(0);
            var result = await grain.Get();

            Assert.Equal(default(string), result.Item1);
            Assert.Equal(default(int), result.Item2);
            Assert.Equal(default(DateTime), result.Item3);
            Assert.Equal(default(Guid), result.Item4);
            Assert.Equal(default(IBinaryTestGrain), result.Item5);
        }

        [Fact]
        public async Task Binary_TestStaticIdentifierGrains()
        {
            // insert your grain test code here
            var grain = _cluster.GrainFactory.GetGrain<IBinaryTestGrain>(12345);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12345, now, guid, _cluster.GrainFactory.GetGrain<IBinaryTestGrain>(2222));
            var result = await grain.Get();
            Assert.Equal("string value", result.Item1);
            Assert.Equal(12345, result.Item2);
            Assert.Equal(now, result.Item3);
            Assert.Equal(guid, result.Item4);
            Assert.Equal(2222, result.Item5.GetPrimaryKeyLong());

            var grain2 = _cluster.GrainFactory.GetGrain<IBinaryTestGrain2>(12345);
            var result2 = await grain2.Get();

            Assert.Equal(default(string), result2.Item1);
            Assert.Equal(default(int), result2.Item2);
            Assert.Equal(default(DateTime), result2.Item3);
            Assert.Equal(default(Guid), result2.Item4);
            Assert.Equal(default(IBinaryTestGrain), result2.Item5);

            await grain2.Set("string value2", 12345, now, guid, _cluster.GrainFactory.GetGrain<IBinaryTestGrain>(2222));
            result2 = await grain2.Get();
            Assert.Equal("string value2", result2.Item1);
            Assert.Equal(12345, result2.Item2);
            Assert.Equal(now, result2.Item3);
            Assert.Equal(guid, result2.Item4);
            Assert.Equal(2222, result2.Item5.GetPrimaryKeyLong());

            await Task.WhenAll(new[] { grain2.Clear(), grain.Clear() });
        }

        [Fact]
        public async Task Json_InitializeWithNoStateTest()
        {
            var grain = _cluster.GrainFactory.GetGrain<IJsonTestGrain>(0);
            var result = await grain.Get();

            Assert.Equal(default(string), result.Item1);
            Assert.Equal(default(int), result.Item2);
            Assert.Equal(default(DateTime), result.Item3);
            Assert.Equal(default(Guid), result.Item4);
            Assert.Equal(default(IJsonTestGrain), result.Item5);
        }

        [Fact]
        public async Task Json_TestStaticIdentifierGrains()
        {
            // insert your grain test code here
            var grain = _cluster.GrainFactory.GetGrain<IJsonTestGrain>(12345);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12345, now, guid, _cluster.GrainFactory.GetGrain<IJsonTestGrain>(2222));
            var result = await grain.Get();
            Assert.Equal("string value", result.Item1);
            Assert.Equal(12345, result.Item2);
            Assert.Equal(now, result.Item3);
            Assert.Equal(guid, result.Item4);
            Assert.Equal(2222, result.Item5.GetPrimaryKeyLong());

            var grain2 = _cluster.GrainFactory.GetGrain<IJsonTestGrain2>(12345);
            var result2 = await grain2.Get();

            Assert.Equal(default(string), result2.Item1);
            Assert.Equal(default(int), result2.Item2);
            Assert.Equal(default(DateTime), result2.Item3);
            Assert.Equal(default(Guid), result2.Item4);
            Assert.Equal(default(IJsonTestGrain), result2.Item5);

            await grain2.Set("string value2", 12345, now, guid, _cluster.GrainFactory.GetGrain<IJsonTestGrain>(2222));
            result2 = await grain2.Get();
            Assert.Equal("string value2", result2.Item1);
            Assert.Equal(12345, result2.Item2);
            Assert.Equal(now, result2.Item3);
            Assert.Equal(guid, result2.Item4);
            Assert.Equal(2222, result2.Item5.GetPrimaryKeyLong());

            await Task.WhenAll(new[] { grain2.Clear(), grain.Clear() });
        }

        [Fact]
        public async Task Json_BackwardsCompatible_ETag_Writes()
        {
            var testState = "{\"$id\":\"1\",\"$type\":\"Orleans.StorageProviders.Redis.TestGrains.JsonTestGrainState, Orleans.StorageProviders.Redis.TestGrains\",\"StringValue\":\"string value\",\"IntValue\":12345,\"DateTimeValue\":\"2018-03-13T14:44:51.8538156Z\",\"GuidValue\":\"98d37743-457f-4ebd-8904-1eb366c6a95a\",\"GrainValue\":{\"$id\":\"2\",\"$type\":\"Orleans.StorageProviders.Redis.TestGrainInterfaces.OrleansCodeGenJsonTestGrainReference, Orleans.StorageProviders.Redis.TestGrainInterfaces\",\"GrainId\":\"000000000000000000000000000008ae03ffffffda80f122\",\"GenericArguments\":\"\"}}";

            var grain = _cluster.GrainFactory.GetGrain<IJsonTestGrain>(12345999);
            var grainRef = await grain.GetReference();
            var key = $"{grainRef.ToKeyString()}|json";
            await _fixture.Database.StringSetAsync(key, testState);
            
            var guid = Guid.Parse("98d37743-457f-4ebd-8904-1eb366c6a95a");
            var result = await grain.Get();
            Assert.Equal("string value", result.Item1);
            Assert.Equal(12345, result.Item2);
            Assert.NotEqual(default(DateTime), result.Item3);
            Assert.Equal(guid, result.Item4);
            Assert.Equal(2222, result.Item5.GetPrimaryKeyLong());

            await _fixture.Database.KeyDeleteAsync(key);
        }

        [Fact]
        public async Task StreamingPubSubStoreTest()
        {
            var strmId = Guid.NewGuid();

            var streamProv = _client.GetStreamProvider("SMSProvider");
            var stream = streamProv.GetStream<int>(strmId, "test1");

            var handle = await stream.SubscribeAsync(
                (e, t) => { return Task.CompletedTask; },
                e => { return Task.CompletedTask; });
        }

        [Fact]
        public async Task PubSubTest()
        {
            var tcs = new TaskCompletionSource<int>();
            var strmId = Guid.NewGuid();

            var streamProv = _client.GetStreamProvider("SMSProvider");
            var stream = streamProv.GetStream<int>(strmId, "test1");
            
            var handle = await stream.SubscribeAsync(
                (i, t) => {
                    if (i == 100)
                    {
                        Console.WriteLine($"PubSubTest: message number {i} - done!");
                        tcs.TrySetResult(1);
                    } else
                    {
                        Console.WriteLine($"PubSubTest: message number {i}");
                    }
                    return Task.CompletedTask;
                },
                e => { return Task.CompletedTask; }
            );

            var tasks = new Task[100];
            for (int i = 1; i <= 100; i++)
            {
                tasks[i - 1] = stream.OnNextAsync(i);
            }
            await Task.WhenAll(tasks);

            Task.Run(async () =>
            {
                await Task.Delay(5000);
                tcs.SetException(new Exception("Timeout"));
            }).Ignore();

            var result = await tcs.Task;
            Assert.Equal(1, result);

            var handles = await stream.GetAllSubscriptionHandles();
            Assert.Equal(1, handles.Count);
        }

        [Fact]
        public async Task PubSubStoreRetrievalTest()
        {
            //var strmId = Guid.NewGuid();
            var strmId = Guid.Parse("761E3BEC-636E-4F6F-A56B-9CC57E66B712");

            var streamProv = _client.GetStreamProvider("SMSProvider");
            IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");
            //IAsyncStream<int> streamIn = streamProv.GetStream<int>(strmId, "test1");

            for (int i = 0; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }

            StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                (e, t) =>
                {
                    Console.WriteLine(string.Format("{0}{1}", e, t));
                    return Task.CompletedTask;
                },
                e => { return Task.CompletedTask; });


            for (int i = 100; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }


            StreamSubscriptionHandle<int> handle2 = await stream.SubscribeAsync(
                (e, t) =>
                {
                    Console.WriteLine(string.Format("2222-{0}{1}", e, t));
                    return Task.CompletedTask;
                },
                e => { return Task.CompletedTask; });

            for (int i = 1000; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }

            var sh = await stream.GetAllSubscriptionHandles();

            Assert.Equal(2, sh.Count);

            IAsyncStream<int> stream2 = streamProv.GetStream<int>(strmId, "test1");

            for (int i = 10000; i < 25; i++)
            {
                await stream2.OnNextAsync(i);
            }

            StreamSubscriptionHandle<int> handle2More = await stream2.SubscribeAsync(
                (e, t) =>
                {
                    Console.WriteLine(string.Format("{0}{1}", e, t));
                    return Task.CompletedTask;
                },
                e => { return Task.CompletedTask; });

            for (int i = 10000; i < 25; i++)
            {
                await stream2.OnNextAsync(i);
            }
        }
    }
}
