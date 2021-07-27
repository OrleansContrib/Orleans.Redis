using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Orleans.Configuration;
using Orleans.Internal;
using Orleans.Reminders.Redis.TestGrainInterfaces;
using Orleans.Runtime;
using Orleans.TestingHost.Utils;

using Xunit;

namespace Orleans.Reminders.Redis.Tests
{
    [Collection("Default")]
    public abstract class ReminderTableTestsBase : IDisposable
    {
        private readonly ILogger logger;

        private readonly IReminderTable remindersTable;
        protected ClusterFixture clusterFixture;
        protected ILoggerFactory loggerFactory;
        protected IOptions<ClusterOptions> clusterOptions;

        protected const string testDatabaseName = "OrleansReminderTest";//for relational storage

        protected ReminderTableTestsBase(ClusterFixture clusterFixture, LoggerFilterOptions filters)
        {
            this.clusterFixture = clusterFixture;

            //fixture.InitializeConnectionStringAccessor(GetConnectionString);
            loggerFactory = TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log", filters);
            logger = loggerFactory.CreateLogger<ReminderTableTestsBase>();
            string serviceId = Guid.NewGuid().ToString();
            string clusterId = "test-" + serviceId;

            logger.Info("ClusterId={0}", clusterId);
            clusterOptions = Options.Create(new ClusterOptions { ClusterId = clusterId, ServiceId = serviceId });

            IReminderTable rmndr = CreateRemindersTable();
            rmndr.Init().WithTimeout(TimeSpan.FromMinutes(1)).Wait();
            remindersTable = rmndr;
        }

        public virtual void Dispose()
        {
            //if (remindersTable != null && SiloInstanceTableTestConstants.DeleteEntriesAfterTest)
            //{
            //    remindersTable.TestOnlyClearTable().Wait();
            //}
        }

        protected abstract IReminderTable CreateRemindersTable();

        protected virtual string GetAdoInvariant()
        {
            return null;
        }

        protected async Task RemindersParallelUpsert()
        {
            var upserts = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
            {
                ReminderEntry reminder = CreateReminder(MakeTestGrainReference(), i.ToString());
                return Task.WhenAll(Enumerable.Range(1, 5).Select(j =>
                {
                    return RetryHelper.RetryOnExceptionAsync<string>(5, RetryOperation.Sigmoid, async () =>
                    {
                        return await remindersTable.UpsertRow(reminder);
                    });
                }));
            }));
            Assert.DoesNotContain(upserts, i => i.Distinct().Count() != 5);
        }

        protected async Task ReminderSimple()
        {
            ReminderEntry reminder = CreateReminder(MakeTestGrainReference(), "0");
            await remindersTable.UpsertRow(reminder);

            ReminderEntry readReminder = await remindersTable.ReadRow(reminder.GrainRef, reminder.ReminderName);

            string etagTemp = reminder.ETag = readReminder.ETag;

            Assert.Equal(JsonConvert.SerializeObject(readReminder), JsonConvert.SerializeObject(reminder));

            Assert.NotNull(etagTemp);

            reminder.ETag = await remindersTable.UpsertRow(reminder);

            bool removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, etagTemp);
            Assert.False(removeRowRes, "should have failed. Etag is wrong");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, "bla", reminder.ETag);
            Assert.False(removeRowRes, "should have failed. reminder name is wrong");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, reminder.ETag);
            Assert.True(removeRowRes, "should have succeeded. Etag is right");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainRef, reminder.ReminderName, reminder.ETag);
            Assert.False(removeRowRes, "should have failed. reminder shouldn't exist");
        }

        protected async Task RemindersRange(int iterations = 1000)
        {
            await Task.WhenAll(Enumerable.Range(1, iterations).Select(async i =>
            {
                GrainReference grainRef = MakeTestGrainReference();

                await RetryHelper.RetryOnExceptionAsync<Task>(10, RetryOperation.Sigmoid, async () =>
                {
                    await remindersTable.UpsertRow(CreateReminder(grainRef, i.ToString()));
                    return Task.CompletedTask;
                });
            }));

            ReminderTableData rows = await remindersTable.ReadRows(0, uint.MaxValue);

            Assert.Equal(rows.Reminders.Count, iterations);

            rows = await remindersTable.ReadRows(0, 0);

            Assert.Equal(rows.Reminders.Count, iterations);

            uint[] remindersHashes = rows.Reminders.Select(r => r.GrainRef.GetUniformHashCode()).ToArray();

            SafeRandom random = new SafeRandom();

            await Task.WhenAll(Enumerable.Range(0, iterations).Select(i =>
                TestRemindersHashInterval(remindersTable, (uint)random.Next(), (uint)random.Next(),
                    remindersHashes)));
        }

        private async Task TestRemindersHashInterval(IReminderTable reminderTable, uint beginHash, uint endHash,
            uint[] remindersHashes)
        {
            Task<ReminderTableData> rowsTask = reminderTable.ReadRows(beginHash, endHash);
            IEnumerable<uint> expectedHashes = beginHash < endHash
                ? remindersHashes.Where(r => r > beginHash && r <= endHash)
                : remindersHashes.Where(r => r > beginHash || r <= endHash);

            HashSet<uint> expectedSet = new HashSet<uint>(expectedHashes);
            IEnumerable<uint> returnedHashes = (await rowsTask).Reminders.Select(r => r.GrainRef.GetUniformHashCode());
            HashSet<uint> returnedSet = new HashSet<uint>(returnedHashes);

            Assert.True(returnedSet.SetEquals(expectedSet));
        }

        private static ReminderEntry CreateReminder(GrainReference grainRef, string reminderName)
        {
            DateTime now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            return new ReminderEntry
            {
                GrainRef = grainRef,
                Period = TimeSpan.FromMinutes(1),
                StartAt = now,
                ReminderName = reminderName
            };
        }

        private GrainReference MakeTestGrainReference()
        {
            GrainReference grainRef = this.clusterFixture.Client.GetGrain<IReminderTestGrain>(Guid.NewGuid()).GetReference().Result;
            return grainRef;
        }
    }
}
