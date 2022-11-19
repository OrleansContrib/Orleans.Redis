using System;

namespace Orleans.Persistence.Redis.TestGrainInterfaces
{
    [GenerateSerializer]
    public class StreamItem
    {
        [Id(0)]
        public string Message { get; set; }
    }
}