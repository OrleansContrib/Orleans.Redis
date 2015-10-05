namespace Orleans.StorageProviders.RedisStorage.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Runtime.Host;
    using GrainInterfaces;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestGrains()
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


        // code to initialize and clean up an Orleans Silo

        private static SiloHost siloHost;
        private static AppDomain hostDomain;

        private static void InitSilo(string[] args)
        {
            siloHost = new SiloHost("Primary");
            siloHost.ConfigFileName = "DevTestServerConfiguration.xml";
            siloHost.DeploymentId = "1";
            siloHost.InitializeOrleansSilo();
            var ok = siloHost.StartOrleansSilo();
            if (!ok)
                throw new SystemException(string.Format("Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
        }

        [ClassInitialize]
        public static void GrainTestsClassInitialize(TestContext testContext)
        {
            hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
            });

            GrainClient.Initialize("DevTestClientConfiguration.xml");
        }

        [ClassCleanup]
        public static void GrainTestsClassCleanUp()
        {
            hostDomain.DoCallBack(() => {
                siloHost.Dispose();
                siloHost = null;
                AppDomain.Unload(hostDomain);
            });
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/F /IM vstest.executionengine.x86.exe",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(startInfo);
        }
    }
}
