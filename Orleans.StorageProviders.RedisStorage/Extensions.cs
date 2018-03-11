using Orleans.Runtime.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.StorageProviders
{
    public static class Extensions
    {
        public static ClusterConfiguration AddRedisStorageProvider(
            this ClusterConfiguration config, 
            string name, 
            string hostPort = "localhost", 
            int dbNumber = 0, 
            bool useJson = false
        )
        {
            var props = new Dictionary<string, string>()
            {
                { RedisStorageProvider.REDIS_CONNECTION_STRING, hostPort },
                { RedisStorageProvider.REDIS_DATABASE_NUMBER, dbNumber.ToString() },
                { RedisStorageProvider.USE_JSON_FORMAT_PROPERTY, useJson ? "true" : "false" }
            };
            config.Globals.RegisterStorageProvider<RedisStorageProvider>(name, props);
            return config;
        }
    }
}
