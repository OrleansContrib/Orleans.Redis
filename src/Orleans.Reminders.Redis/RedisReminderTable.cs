using System;
using System.Threading.Tasks;

using Orleans.Runtime;

namespace Orleans.Reminders.Redis
{
    internal class RedisReminderTable : IReminderTable
    {
        public Task Init()
        {
            throw new NotImplementedException();
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            throw new NotImplementedException();
        }

        public Task TestOnlyClearTable()
        {
            throw new NotImplementedException();
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
