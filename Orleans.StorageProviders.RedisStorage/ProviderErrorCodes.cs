namespace Orleans.StorageProviders
{
    internal enum ProviderErrorCode
    {
        RedisProviderBase = 300000,
        RedisStorageprovider_ProviderName = RedisProviderBase + 200,
        RedisStorageProvider_ReadingData = RedisProviderBase + 300,
        RedisStorageProvider_WritingData = RedisProviderBase + 400,
        RedisStorageProvider_ClearingData = RedisProviderBase + 500
    }
}
