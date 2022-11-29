using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Persistence.Redis.TestGrainInterfaces;
using Orleans.Streams;

namespace Orleans.Persistence.Redis.TestGrains
{
    public class StreamingTestGrain : Grain, IStreamingTestGrain
    {
        private StreamItem _lastItem;
        
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var streamProvider = this.GetStreamProvider("MSProvider");

            var streamItemStream = streamProvider.GetStream<StreamItem>("StreamItems", this.GetPrimaryKey());

            var handles = await streamItemStream.GetAllSubscriptionHandles();
            if (handles.Any())
            {
                foreach (var handle in handles)
                {
                    await handle.ResumeAsync(this);
                }
            }
            else
            {
                await streamItemStream.SubscribeAsync(this);
            }
        }

        public Task<StreamItem> GetLastItem()
        {
            return Task.FromResult(_lastItem);
        }

        public Task Deactivate()
        {
            DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public Task OnNextAsync(StreamItem item, StreamSequenceToken token = null)
        {
            _lastItem = item;
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}