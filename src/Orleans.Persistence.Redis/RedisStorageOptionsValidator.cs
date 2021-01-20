using Orleans.Runtime;

namespace Orleans.Persistence
{
    internal class RedisStorageOptionsValidator : IConfigurationValidator
    {
        private RedisStorageOptions _options;
        private string _name;

        public RedisStorageOptionsValidator(RedisStorageOptions options, string name)
        {
            this._options = options;
            this._name = name;
        }

        public void ValidateConfiguration()
        {
            var msg = $"Configuration for {nameof(RedisGrainStorage)} - {_name} is invalid";
            if (_options == null)
            {
                throw new OrleansConfigurationException($"{msg} - {nameof(RedisStorageOptions)} is null");
            }

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new OrleansConfigurationException($"{msg} - {nameof(_options.ConnectionString)} is null or empty");
            }

            // host:port delimiter
            if (!_options.ConnectionString.Contains(":"))
            {
                throw new OrleansConfigurationException($"{msg} - {nameof(_options.ConnectionString)} invalid format: {_options.ConnectionString}, should contain host and port delimited by ':'");
            }
        }
    }
}