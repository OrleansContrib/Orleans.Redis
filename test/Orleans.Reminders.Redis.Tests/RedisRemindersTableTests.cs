
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.TestingHost;

using Xunit;

namespace Orleans.Reminders.Redis.Tests
{
    public class RedisRemindersTableTests : ReminderTableTestsBase, IClassFixture<ClusterFixture>
    {
        public RedisRemindersTableTests(ClusterFixture clusterFixture)
            : base(clusterFixture, CreateFilters())
        {
        }

        private static LoggerFilterOptions CreateFilters()
        {
            LoggerFilterOptions filters = new LoggerFilterOptions();
            filters.AddFilter(nameof(RedisRemindersTableTests), LogLevel.Trace);
            return filters;
        }

        protected override IReminderTable CreateRemindersTable()
        {
            RedisReminderTable reminderTable = ((InProcessSiloHandle)clusterFixture.Cluster.Primary).SiloHost.Services.GetService<IReminderTable>() as RedisReminderTable;
            if (reminderTable == null)
            {
                throw new InvalidOperationException("RedisReminderTable not configured");
            }

            return reminderTable;
        }

        [Fact]
        public void RemindersTable_Redis_Init()
        {
        }

        [Fact]
        public async Task RemindersTable_Redis_RemindersRange()
        {
            await RemindersRange(iterations: 50);
        }

        [Fact]
        public async Task RemindersTable_Redis_RemindersParallelUpsert()
        {
            await RemindersParallelUpsert();
        }

        [Fact]
        public async Task RemindersTable_Redis_ReminderSimple()
        {
            await ReminderSimple();
        }

        [Theory]
        [InlineData("aa:bb")]
        [InlineData("aa_bb")]
        public async Task ReminderWithSpecialName(string reminderName)
        {
            await ReminderSimple(MakeTestGrainReference(), reminderName);
        }

        [Theory]
        [InlineData("aa:bb")]
        [InlineData("aa_bb")]
        public async Task ReminderWithSpecialGrainId(string grainId)
        {
            await ReminderSimple(MakeTestGrainReference(grainId), "0");
        }

        [Fact]
        public async Task ReadNonExistentReminder()
        {
            ReminderEntry reminder = await remindersTable.ReadRow(MakeTestGrainReference(), "ThereIsNoReminder");
            Assert.Null(reminder);
        }
    }
}
