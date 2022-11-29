using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using System;
using System.Threading.Tasks;
using OrleansCodeGen.Orleans;

namespace Orleans.Persistence.Redis.TestGrains
{
    public class TestGrain : Grain, ITestGrain
    {
        private readonly IPersistentState<TestGrainState> _state;

        public TestGrain(
            [PersistentState("state", "Redis")] IPersistentState<TestGrainState> state
            )
        {
            _state = state;
        }
        
        public Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, ITestGrain grainValue)
        {
            _state.State.StringValue = stringValue;
            _state.State.IntValue = intValue;
            _state.State.DateTimeValue = dateTimeValue;
            _state.State.GuidValue = guidValue;
            _state.State.GrainValue = grainValue;
            return _state.WriteStateAsync();
        }

        public async Task<Tuple<string, int, DateTime, Guid, ITestGrain>> Get()
        {
            await _state.ReadStateAsync();
            return new Tuple<string, int, DateTime, Guid, ITestGrain>(
              _state.State.StringValue,
              _state.State.IntValue,
              _state.State.DateTimeValue,
              _state.State.GuidValue,
              _state.State.GrainValue);
        }

        public Task Clear()
        {
            return _state.ClearStateAsync();
        }

        public Task<GrainId> GetId()
        {
            return Task.FromResult(this.GetGrainId());
        }
    }
}
