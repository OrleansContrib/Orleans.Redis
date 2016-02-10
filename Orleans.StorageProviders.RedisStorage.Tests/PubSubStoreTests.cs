using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Orleans.TestingHost;
using Orleans.StorageProviders.RedisStorage.GrainInterfaces;
using Orleans.Streams;
using System.Linq;

namespace Orleans.StorageProviders.RedisStorage.Tests
{

    [DeploymentItem("DevTestServerConfigurationBinaryFormat.xml")]
    [DeploymentItem("DevTestClientConfiguration.xml")]
    [DeploymentItem("OrleansProviders.dll")]
    [DeploymentItem("Orleans.StorageProviders.RedisStorage.GrainClasses.dll")]
    [DeploymentItem("RedisStorage.dll")]

    [TestClass]
    public class PubSubStoreTests : TestingSiloHost
    {
        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        public PubSubStoreTests()
            : base(new TestingSiloOptions
            {
                StartFreshOrleans = true,
                SiloConfigFile = new FileInfo("DevTestServerConfigurationBinaryFormat.xml"),
            },
            new TestingClientOptions()
            {
                ClientConfigFile = new FileInfo("DevTestClientConfiguration.xml")
            })
        {
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Optional. 
            // By default, the next test class which uses TestignSiloHost will
            // cause a fresh Orleans silo environment to be created.
            StopAllSilos();
        }

        [TestMethod]
        public async Task StreamingPubSubStoreTest()
        {
            var strmId = Guid.NewGuid();

            var streamProv = GrainClient.GetStreamProvider("SMSProvider");
            IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");

            StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                (e, t) => { return TaskDone.Done; },
                e => { return TaskDone.Done; });
        }


        [TestMethod]
        public async Task PubSubStoreRetrievalTest()
        {
            //var strmId = Guid.NewGuid();
            var strmId = Guid.Parse("761E3BEC-636E-4F6F-A56B-9CC57E66B712");

            var streamProv = GrainClient.GetStreamProvider("SMSProvider");
            IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");
            //IAsyncStream<int> streamIn = streamProv.GetStream<int>(strmId, "test1");


            for (int i = 0; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }


            StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                (e, t) =>
                {
                    Trace.WriteLine(string.Format("{0}{1}", e, t));
                    return TaskDone.Done;
                },
                e => { return TaskDone.Done; });

            //StreamSubscriptionHandle<int> handleIn = await streamIn.SubscribeAsync(
            //    (e, t) =>
            //    {
            //        Trace.WriteLine(string.Format("{0}{1}", e, t));
            //        return TaskDone.Done;
            //    },
            //    e => { return TaskDone.Done; });

            //await handle.ResumeAsync(
            //    (e, t) =>
            //    {
            //        Trace.WriteLine(string.Format("{0}{1}", e, t));
            //        return TaskDone.Done;
            //    },
            //    e => { return TaskDone.Done; });



            for (int i = 100; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }


            StreamSubscriptionHandle<int> handle2 = await stream.SubscribeAsync(
                (e, t) =>
                {
                    Trace.WriteLine(string.Format("2222-{0}{1}", e, t));
                    return TaskDone.Done;
                },
                e => { return TaskDone.Done; });

            for (int i = 1000; i < 25; i++)
            {
                await stream.OnNextAsync(i);
            }

            //await handle2.ResumeAsync(
            //    (e, t) =>
            //    {
            //        Trace.WriteLine(string.Format("{0}{1}", e, t));
            //        return TaskDone.Done;
            //    },
            //    e => { return TaskDone.Done; });

            var sh = await stream.GetAllSubscriptionHandles();

            Assert.AreEqual<int>(2, sh.Count());


            //await handle.UnsubscribeAsync();
            //var sh1 = await stream.GetAllSubscriptionHandles();

            //Assert.AreEqual<int>(1, sh1.Count());

            //await handle2.UnsubscribeAsync();
            //var sh2 = await stream.GetAllSubscriptionHandles();

            //Assert.AreEqual<int>(0, sh2.Count());


            //var silos = base.GetActiveSilos();
            //foreach (var silo in silos)
            //{
            //    base.RestartSilo(silo);
            //}

            IAsyncStream<int> stream2 = streamProv.GetStream<int>(strmId, "test1");

            for (int i = 10000; i < 25; i++)
            {
                await stream2.OnNextAsync(i);
            }

            StreamSubscriptionHandle<int> handle2More = await stream2.SubscribeAsync(
                (e, t) =>
                {
                    Trace.WriteLine(string.Format("{0}{1}", e, t));
                    return TaskDone.Done;
                },
                e => { return TaskDone.Done; });

            for (int i = 10000; i < 25; i++)
            {
                await stream2.OnNextAsync(i);
            }


        }
    }
}
