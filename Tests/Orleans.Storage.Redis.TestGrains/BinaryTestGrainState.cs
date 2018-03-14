using Orleans.Storage.Redis.TestGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage.Redis.TestGrains
{
    public class BinaryTestGrainState
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public Guid GuidValue { get; set; }
        public IBinaryTestGrain GrainValue { get; set; }
    }
}
