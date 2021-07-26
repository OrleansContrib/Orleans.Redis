using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Reminders.Redis.TestGrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Orleans.Reminders.Redis.TestGrains
{
    public class BinaryTestGrain : Grain, ITestGrain
    {
        public Task<GrainReference> GetReference()
        {
            return Task.FromResult(GrainReference);
        }
    }
}
