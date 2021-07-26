namespace Orleans.Configuration
{
    /// <summary>
    /// Redis reminder options.
    /// </summary>
    public class RedisReminderTableOptions
    {

        /// <summary>
        /// The connection string.
        /// </summary>
        public string ConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// The database number.
        /// </summary>
        public int? DatabaseNumber { get; set; }

    }
}
