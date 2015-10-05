namespace Orleans.StorageProviders.RedisStorage.GrainInterfaces
{
    using System;
    using System.Threading.Tasks;

    public interface IGrain1 : IGrainWithIntegerKey
    {
        Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IGrain1 grainValue);
        Task<Tuple<string, int, DateTime, Guid, IGrain1>> Get();
    }
}