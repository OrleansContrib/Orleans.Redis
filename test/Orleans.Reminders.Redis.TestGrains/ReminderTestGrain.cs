using System.Threading.Tasks;

using Orleans.Reminders.Redis.TestGrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Reminders.Redis.TestGrains
{
    public class ReminderTestGrain : Grain, IReminderTestGrain
    {
        public Task<GrainReference> GetReference()
        {
            return Task.FromResult(GrainReference);
        }
    }
}
