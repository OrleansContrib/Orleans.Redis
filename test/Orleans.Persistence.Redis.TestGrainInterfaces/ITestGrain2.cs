using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Orleans.Persistence.Redis.TestGrainInterfaces
{
    public interface ITestGrain2 : IGrainWithIntegerKey
    {
        Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, ITestGrain grainValue);
        Task<Tuple<string, int, DateTime, Guid, ITestGrain>> Get();
        Task Clear();
        Task<GrainId> GetId();
    }
}
