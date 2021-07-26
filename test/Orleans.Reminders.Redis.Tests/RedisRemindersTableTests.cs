
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.Configuration;

using Xunit;

namespace Orleans.Reminders.Redis.Tests
{
    public class RedisRemindersTableTests : ReminderTableTestsBase, IClassFixture<ClusterFixture>
    {
        public RedisRemindersTableTests(ClusterFixture clusterFixture) : base(clusterFixture, CreateFilters())
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
            var reminderTable = clusterFixture.Cluster.ServiceProvider.GetService<IReminderTable>();
            if (reminderTable is not RedisReminderTable)
                throw new InvalidOperationException("RedisReminderTable not configured");
            return reminderTable;
        }

        [Fact]
        public void RemindersTable_PostgreSql_Init()
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
    }
}
