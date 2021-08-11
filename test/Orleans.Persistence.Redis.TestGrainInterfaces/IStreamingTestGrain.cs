using System.Threading.Tasks;
using Orleans.Streams;

namespace Orleans.Persistence.Redis.TestGrainInterfaces
{
    public interface IStreamingTestGrain : IGrainWithGuidKey, IAsyncObserver<StreamItem>
    {
        public Task<StreamItem> GetLastItem();

        public Task Deactivate();
    }
}