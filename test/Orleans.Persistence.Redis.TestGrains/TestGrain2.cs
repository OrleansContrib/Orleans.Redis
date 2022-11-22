using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Orleans.Persistence.Redis.TestGrains
{
    [StorageProvider(ProviderName = "Redis")]
    public class TestGrain2 : Grain<TestGrainState2>, ITestGrain2
    {
        public Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, ITestGrain grainValue)
        {
            State.StringValue = stringValue;
            State.IntValue = intValue;
            State.DateTimeValue = dateTimeValue;
            State.GuidValue = guidValue;
            State.GrainValue = grainValue;
            return WriteStateAsync();
        }

        public async Task<Tuple<string, int, DateTime, Guid, ITestGrain>> Get()
        {
            await ReadStateAsync();
            return new Tuple<string, int, DateTime, Guid, ITestGrain>(
              State.StringValue,
              State.IntValue,
              State.DateTimeValue,
              State.GuidValue,
              State.GrainValue);
        }

        public Task Clear()
        {
            return ClearStateAsync();
        }

        public Task<GrainId> GetId()
        {
            return Task.FromResult(this.GetGrainId());
        }
    }
}
