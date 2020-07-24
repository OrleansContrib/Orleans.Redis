namespace Orleans.Runtime.ReminderService
{
    /// <summary>
    /// Class RedisReminderTableOptions.
    /// </summary>
    public class RedisReminderTableOptions
    {
        /// <summary>
        /// The connection string.
        /// </summary>
        /// <value>The data connection string.</value>
        public string DataConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// Whether or not to use JSON for serialization.
        /// </summary>
        /// <value><c>true</c> if [use json]; otherwise, <c>false</c>.</value>
        public bool UseJson { get; set; }

        /// <summary>
        /// The database number.
        /// </summary>
        /// <value>The database number.</value>
        public int? DatabaseNumber { get; set; }
    }
}