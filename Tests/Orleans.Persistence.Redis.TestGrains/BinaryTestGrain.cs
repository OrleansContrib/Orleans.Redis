using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Orleans.Persistence.Redis.TestGrains
{
    [StorageProvider(ProviderName = "REDIS-BINARY")]
    public class BinaryTestGrain : Grain<BinaryTestGrainState>, IBinaryTestGrain
    {
        public Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IBinaryTestGrain grainValue)
        {
            State.StringValue = stringValue;
            State.IntValue = intValue;
            State.DateTimeValue = dateTimeValue;
            State.GuidValue = guidValue;
            State.GrainValue = grainValue;
            return WriteStateAsync();
        }

        public async Task<Tuple<string, int, DateTime, Guid, IBinaryTestGrain>> Get()
        {
            await ReadStateAsync();
            return new Tuple<string, int, DateTime, Guid, IBinaryTestGrain>(
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

        public Task<GrainReference> GetReference()
        {
            return Task.FromResult(GrainReference);
        }
    }
}
