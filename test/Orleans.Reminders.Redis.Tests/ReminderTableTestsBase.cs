using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Orleans.Clustering.Redis.Test;
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

        protected readonly IReminderTable remindersTable;
        protected ClusterFixture clusterFixture;
        protected ILoggerFactory loggerFactory;
        protected IOptions<ClusterOptions> clusterOptions;


        protected ReminderTableTestsBase(ClusterFixture clusterFixture, LoggerFilterOptions filters)
        {
            this.clusterFixture = clusterFixture;

            loggerFactory = TestingUtils.CreateDefaultLoggerFactory($"{GetType()}.log", filters);
            logger = loggerFactory.CreateLogger<ReminderTableTestsBase>();
            string serviceId = Guid.NewGuid().ToString();
            string clusterId = "test-" + serviceId;

            logger.LogInformation("ClusterId={ClusterId}", clusterId);
            clusterOptions = Options.Create(new ClusterOptions { ClusterId = clusterId, ServiceId = serviceId });

            IReminderTable rmndr = CreateRemindersTable();
            rmndr.Init().WithTimeout(TimeSpan.FromMinutes(1)).Wait();
            remindersTable = rmndr;
        }

        public virtual void Dispose()
        {
        }

        protected abstract IReminderTable CreateRemindersTable();

        protected async Task RemindersParallelUpsert()
        {
            string[][] upserts = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
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
            await ReminderSimple(MakeTestGrainReference(), "0");
        }

        protected async Task ReminderSimple(GrainId grainId, string reminderName)
        {
            ReminderEntry reminder = CreateReminder(grainId, reminderName);
            await remindersTable.UpsertRow(reminder);

            ReminderEntry readReminder = await remindersTable.ReadRow(reminder.GrainId, reminder.ReminderName);

            string etagTemp = reminder.ETag = readReminder.ETag;

            Assert.Equal(JsonConvert.SerializeObject(readReminder), JsonConvert.SerializeObject(reminder));

            Assert.NotNull(etagTemp);

            reminder.ETag = await remindersTable.UpsertRow(reminder);

            bool removeRowRes = await remindersTable.RemoveRow(reminder.GrainId, reminder.ReminderName, etagTemp);
            Assert.False(removeRowRes, "should have failed. Etag is wrong");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainId, "bla", reminder.ETag);
            Assert.False(removeRowRes, "should have failed. reminder name is wrong");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainId, reminder.ReminderName, reminder.ETag);
            Assert.True(removeRowRes, "should have succeeded. Etag is right");
            removeRowRes = await remindersTable.RemoveRow(reminder.GrainId, reminder.ReminderName, reminder.ETag);
            Assert.False(removeRowRes, "should have failed. reminder shouldn't exist");
        }

        protected async Task RemindersRange(int iterations = 1000)
        {
            await Task.WhenAll(Enumerable.Range(1, iterations).Select(async i =>
            {
                GrainId grainId = MakeTestGrainReference();

                await RetryHelper.RetryOnExceptionAsync<Task>(10, RetryOperation.Sigmoid, async () =>
                {
                    await remindersTable.UpsertRow(CreateReminder(grainId, i.ToString()));
                    return Task.CompletedTask;
                });
            }));

            ReminderTableData rows = await remindersTable.ReadRows(0, uint.MaxValue);

            Assert.Equal(rows.Reminders.Count, iterations);

            rows = await remindersTable.ReadRows(0, 0);

            Assert.Equal(rows.Reminders.Count, iterations);

            uint[] remindersHashes = rows.Reminders.Select(r => r.GrainId.GetUniformHashCode()).ToArray();

            Random random = new Random();

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
            IEnumerable<uint> returnedHashes = (await rowsTask).Reminders.Select(r => r.GrainId.GetUniformHashCode());
            HashSet<uint> returnedSet = new HashSet<uint>(returnedHashes);

            Assert.True(returnedSet.SetEquals(expectedSet));
        }

        protected static ReminderEntry CreateReminder(GrainId grainId, string reminderName)
        {
            DateTime now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            return new ReminderEntry
            {
                GrainId = grainId,
                Period = TimeSpan.FromMinutes(1),
                StartAt = now,
                ReminderName = reminderName
            };
        }

        protected GrainId MakeTestGrainReference()
        {
            return MakeTestGrainReference(Guid.NewGuid().ToString());
        }
        protected GrainId MakeTestGrainReference(string grainId)
        {
            return clusterFixture.Client.GetGrain<IReminderTestGrain>(grainId).GetGrainId();
        }
    }
}
