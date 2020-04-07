namespace Orleans.Persistence
{
    public class RedisStorageOptions
    {
        public string DataConnectionString { get; set; } = "localhost:6379";

        public bool UseJson { get; set; }

        public bool DeleteOnClear { get; set; }

        public int? DatabaseNumber { get; set; }

        /// <summary>
        /// Stage of silo lifecycle where storage should be initialized.  Storage must be initialzed prior to use.
        /// </summary>
        public int InitStage { get; set; } = DEFAULT_INIT_STAGE;
        public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;
    }
}