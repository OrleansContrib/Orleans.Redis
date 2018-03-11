using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProviders.Redis.TestGrainInterfaces
{
    public interface IBinaryTestGrain2 : IGrainWithIntegerKey
    {
        Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IBinaryTestGrain grainValue);
        Task<Tuple<string, int, DateTime, Guid, IBinaryTestGrain>> Get();
        Task Clear();
    }
}
