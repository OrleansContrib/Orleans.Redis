using Orleans.StorageProviders.Redis.TestGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.StorageProviders.Redis.TestGrains
{
    public class JsonTestGrainState2
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public Guid GuidValue { get; set; }
        public IJsonTestGrain GrainValue { get; set; }
    }
}
