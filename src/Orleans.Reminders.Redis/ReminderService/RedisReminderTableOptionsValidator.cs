using Microsoft.Extensions.Options;

using Orleans.Runtime;

namespace Orleans.Runtime.ReminderService
{
    /// <summary>
    /// Class RedisReminderTableOptionsValidator.
    /// Implements the <see cref="Orleans.IConfigurationValidator" />
    /// </summary>
    /// <seealso cref="Orleans.IConfigurationValidator" />
    public class RedisReminderTableOptionsValidator : IConfigurationValidator
    {
        /// <summary>
        /// The options
        /// </summary>
        private readonly RedisReminderTableOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReminderTableOptionsValidator" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public RedisReminderTableOptionsValidator(IOptions<RedisReminderTableOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Validates system configuration and throws an exception if configuration is not valid.
        /// </summary>
        /// <exception cref="OrleansConfigurationException">
        /// </exception>
        public void ValidateConfiguration()
        {
            var msg = $"Configuration for {nameof(RedisReminderTable)} is invalid";
            if (_options == null)
                throw new OrleansConfigurationException($"{msg} - {nameof(RedisReminderTableOptions)} is null");
            if (string.IsNullOrWhiteSpace(_options.DataConnectionString))
                throw new OrleansConfigurationException($"{msg} - {nameof(_options.DataConnectionString)} is null or empty");

            // host:port delimiter
            if (!_options.DataConnectionString.Contains(":"))
                throw new OrleansConfigurationException($"{msg} - {nameof(_options.DataConnectionString)} invalid format: {_options.DataConnectionString}, should contain host and port delimited by ':'");
        }
    }
}