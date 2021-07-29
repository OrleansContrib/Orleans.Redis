
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.Configuration;
using Orleans.Runtime;
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
            LoggerFilterOptions filters = new();
            filters.AddFilter(nameof(RedisRemindersTableTests), LogLevel.Trace);
            return filters;
        }

        protected override IReminderTable CreateRemindersTable()
        {
            var reminderTable = ((InProcessSiloHandle)clusterFixture.Cluster.Primary).SiloHost.Services.GetService<IReminderTable>();
            if (reminderTable is not RedisReminderTable)
                throw new InvalidOperationException("RedisReminderTable not configured");
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

        [Fact]
        public void UpsertRowShouldThrowWhenReminderNameInvalid()
        {
            ReminderEntry reminder = CreateReminder(MakeTestGrainReference(), "aa:bb");
            Assert.ThrowsAsync<ReminderException>(() => remindersTable.UpsertRow(reminder));
        }
    }
}
