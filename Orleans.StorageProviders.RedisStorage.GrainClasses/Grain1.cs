using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProviders.RedisStorage.GrainClasses
{
    using System;
    using System.Threading.Tasks;
    using GrainInterfaces;
    using Providers;

    public class MyState : GrainState
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public Guid GuidValue { get; set; }
        public IGrain1 GrainValue { get; set; }
    }

    [StorageProvider(ProviderName = "REDIS")]
    public class Grain1 : Grain<MyState>, IGrain1
    {
        public Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IGrain1 grainValue)
        {
            State.StringValue = stringValue;
            State.IntValue = intValue;
            State.DateTimeValue = dateTimeValue;
            State.GuidValue = guidValue;
            State.GrainValue = grainValue;
            return WriteStateAsync();
        }

        public Task<Tuple<string, int, DateTime, Guid, IGrain1>> Get()
        {
            return Task.FromResult(new Tuple<string, int, DateTime, Guid, IGrain1>(
              State.StringValue,
              State.IntValue,
              State.DateTimeValue,
              State.GuidValue,
              State.GrainValue));
        }
    }
}