using System;

namespace Orleans.Persistence.Redis.TestGrainInterfaces
{
    [Serializable]
    public class StreamItem
    {
        public string Message { get; set; }
    }
}