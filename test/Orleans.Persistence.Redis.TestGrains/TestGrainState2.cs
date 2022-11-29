using Orleans.Persistence.Redis.TestGrainInterfaces;
using System;

namespace Orleans.Persistence.Redis.TestGrains
{
    [GenerateSerializer]
    public class TestGrainState2
    {
        [Id(0)]
        public string StringValue { get; set; }
        [Id(1)]
        public int IntValue { get; set; }
        [Id(2)]
        public DateTime DateTimeValue { get; set; }
        [Id(3)]
        public Guid GuidValue { get; set; }
        [Id(4)]
        public ITestGrain GrainValue { get; set; }
    }
}
