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
    public class MyGrainTests : TestingSiloHost
    {
        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        public MyGrainTests()
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
        public void InitializeWithNoStateTest()
        {
            Trace.Write(this.GetType().Name);


            var dt = new DateTime();
            var grain = GrainClient.GrainFactory.GetGrain<IGrain1>(0);
            var resultT = grain.Get();
            resultT.Wait();

            Assert.AreEqual<string>(null, resultT.Result.Item1);
            Assert.AreEqual<int>(0, resultT.Result.Item2);
            Assert.AreEqual<DateTime>(dt, resultT.Result.Item3);
            Assert.AreEqual<Guid>(Guid.Empty, resultT.Result.Item4);
            Assert.AreEqual(null, resultT.Result.Item5);
        }


        /// <summary>
        ///  THIS TEST SHOULD NOT BE THROWING AN EXCEPTION
        ///  its the json serializer meshing with the GrainState.Set disctory based on reflection
        ///  especially between int32 and int64
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(System.AggregateException))]
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
    }
}
