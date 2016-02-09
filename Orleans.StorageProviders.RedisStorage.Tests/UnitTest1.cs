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

    [DeploymentItem("DevTestServerConfiguration.xml")]
    [DeploymentItem("DevTestClientConfiguration.xml")]
    [DeploymentItem("OrleansProviders.dll")]
    [DeploymentItem("Orleans.StorageProviders.RedisStorage.GrainClasses.dll")]
    [DeploymentItem("RedisStorage.dll")]

    [TestClass]
    public class UnitTest1 : TestingSiloHost
    {
        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        public UnitTest1()
            : base(new TestingSiloOptions
            {
                StartFreshOrleans = true,
                SiloConfigFile = new FileInfo("DevTestServerConfiguration.xml"),
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
        public async Task TestStaticIdentifierGrains()
        {
            // insert your grain test code here
            var grain = GrainClient.GrainFactory.GetGrain<IGrain1>(1234);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12345, now, guid, GrainClient.GrainFactory.GetGrain<IGrain1>(2222));
            var result = await grain.Get();
            Assert.AreEqual("string value", result.Item1);
            Assert.AreEqual(12345, result.Item2);
            Assert.AreEqual(now, result.Item3);
            Assert.AreEqual(guid, result.Item4);
            Assert.AreEqual(2222, result.Item5.GetPrimaryKeyLong());
        }


        [TestMethod]
        public async Task TestGrains()
        {
            var rnd = new Random();
            var rndId1 = rnd.Next();
            var rndId2 = rnd.Next();



            // insert your grain test code here
            var grain = GrainClient.GrainFactory.GetGrain<IGrain1>(rndId1);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12345, now, guid, GrainClient.GrainFactory.GetGrain<IGrain1>(rndId2));
            var result = await grain.Get();
            Assert.AreEqual("string value", result.Item1);
            Assert.AreEqual(12345, result.Item2);
            Assert.AreEqual(now, result.Item3);
            Assert.AreEqual(guid, result.Item4);
            Assert.AreEqual(rndId2, result.Item5.GetPrimaryKeyLong());
        }

        [TestMethod]
        public void JustSetValuesTest()
        {
            var rnd = new Random();
            var rndId1 = rnd.Next();
            var rndId2 = rnd.Next();

            // insert your grain test code here
            var grain = GrainClient.GrainFactory.GetGrain<IGrain1>(rndId1);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            grain.Set("string value", 0, now, guid, GrainClient.GrainFactory.GetGrain<IGrain1>(rndId2)).Wait();
        }

        [TestMethod]
        public void GetAndSetWithWaitTest()
        {
            var rnd = new Random();
            var rndId1 = rnd.Next();
            var rndId2 = rnd.Next();

            // insert your grain test code here
            var grain = GrainClient.GrainFactory.GetGrain<IGrain1>(rndId1);
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            grain.Set("string value", 0, now, guid, GrainClient.GrainFactory.GetGrain<IGrain1>(rndId2)).Wait();

            var tGet = grain.Get();
            tGet.Wait();
            var result = tGet.Result;
            Assert.AreEqual("string value", result.Item1);
            Assert.AreEqual(0, result.Item2);
            Assert.AreEqual(now, result.Item3);
            Assert.AreEqual(guid, result.Item4);
            Assert.AreEqual(rndId2, result.Item5.GetPrimaryKeyLong());
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
            var strmId = Guid.NewGuid();

            var streamProv = GrainClient.GetStreamProvider("SMSProvider");
            IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");

            StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                (e, t) => { return TaskDone.Done; },
                e => { return TaskDone.Done; });

            var sh = await stream.GetAllSubscriptionHandles();

            Assert.AreEqual<int>(1, sh.Count());
        }


        // code to initialize and clean up an Orleans Silo

        //private static SiloHost siloHost;
        //private static AppDomain hostDomain;

        //private static void InitSilo(string[] args)
        //{
        //    siloHost = new SiloHost("Primary");
        //    siloHost.ConfigFileName = "DevTestServerConfiguration.xml";
        //    siloHost.DeploymentId = "1";
        //    siloHost.InitializeOrleansSilo();
        //    var ok = siloHost.StartOrleansSilo();
        //    if (!ok)
        //        throw new SystemException(string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
        //}

        //[ClassInitialize]
        //public static void GrainTestsClassInitialize(TestContext testContext)
        //{
        //    hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
        //    {
        //        AppDomainInitializer = InitSilo,
        //        ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
        //    });

        //    GrainClient.Initialize("DevTestClientConfiguration.xml");
        //}

        //[ClassCleanup]
        //public static void GrainTestsClassCleanUp()
        //{
        //    hostDomain.DoCallBack(() => {
        //        siloHost.Dispose();
        //        siloHost = null;
        //        AppDomain.Unload(hostDomain);
        //    });
        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = "taskkill",
        //        Arguments = "/F /IM vstest.executionengine.x86.exe",
        //        UseShellExecute = false,
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //    };
        //    Process.Start(startInfo);
        //}
    }
}
