using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage
{
    public static class RedisGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            IOptionsSnapshot<RedisStorageOptions> optionsSnapshot = services.GetRequiredService<IOptionsSnapshot<RedisStorageOptions>>();
            return ActivatorUtilities.CreateInstance<RedisGrainStorage>(services, optionsSnapshot.Get(name), name);
        }
    }
}
