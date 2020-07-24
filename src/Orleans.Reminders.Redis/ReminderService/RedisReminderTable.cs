using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

using StackExchange.Redis;

namespace Orleans.Runtime.ReminderService
{
    /// <summary>
    /// Class RedisReminderTable.
    /// Implements the <see cref="Orleans.IReminderTable" />
    /// </summary>
    /// <seealso cref="Orleans.IReminderTable" />
    public class RedisReminderTable : IReminderTable
    {
        /// <summary>
        /// The grain reference converter
        /// </summary>
        private readonly IGrainReferenceConverter _grainReferenceConverter;
        /// <summary>
        /// The serialization manager
        /// </summary>
        private readonly SerializationManager _serializationManager;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<RedisReminderTable> _logger;
        /// <summary>
        /// The storage options
        /// </summary>
        private readonly RedisReminderTableOptions _storageOptions;
        /// <summary>
        /// The service identifier
        /// </summary>
        private readonly string _serviceId;
        /// <summary>
        /// The redis options
        /// </summary>
        private ConfigurationOptions _redisOptions;
        /// <summary>
        /// The connection
        /// </summary>
        private ConnectionMultiplexer _connection;
        /// <summary>
        /// The database
        /// </summary>
        private IDatabase _db;
        /// <summary>
        /// The server
        /// </summary>
        private IServer _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisReminderTable"/> class.
        /// </summary>
        /// <param name="grainReferenceConverter">The grain reference converter.</param>
        /// <param name="clusterOptions">The cluster options.</param>
        /// <param name="serializationManager">The serialization manager.</param>
        /// <param name="storageOptions">The storage options.</param>
        /// <param name="logger">The logger.</param>
        public RedisReminderTable(
            IGrainReferenceConverter grainReferenceConverter,
            IOptions<ClusterOptions> clusterOptions,
            SerializationManager serializationManager,
            IOptions<RedisReminderTableOptions> storageOptions,
            ILogger<RedisReminderTable> logger)
        {
            _grainReferenceConverter = grainReferenceConverter;
            _serializationManager = serializationManager;
            _logger = logger;
            _storageOptions = storageOptions.Value;
            _serviceId = clusterOptions.Value.ServiceId;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Init()
        {
            var timer = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation($"RedisReminderTable is initializing: ServiceId={_serviceId} DatabaseNumber={_storageOptions.DatabaseNumber}");

                _redisOptions = ConfigurationOptions.Parse(_storageOptions.DataConnectionString);
                _connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions);

                _db = _storageOptions.DatabaseNumber.HasValue
                    ? _connection.GetDatabase(_storageOptions.DatabaseNumber.Value)
                    : _connection.GetDatabase();
                _server = _connection.GetServer(_storageOptions.DataConnectionString.Split(',')[0]);

                timer.Stop();
                _logger.LogInformation(
                    $"Init: ServiceId={_serviceId}, initialized in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            }
            catch (Exception ex)
            {
                timer.Stop();
                _logger.LogError(ex,
                    $"Init: ServiceId={_serviceId}, errored in {timer.Elapsed.TotalMilliseconds:0.00} ms. Error message: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="grainReference">The grain reference.</param>
        /// <returns>System.String.</returns>
        private string GetKey(GrainReference grainReference) => $"{grainReference.ToKeyString()}|{grainReference.GetUniformHashCode()}|Reminder|{(_storageOptions.UseJson ? "json" : "binary")}";

        /// <summary>
        /// Reads the rows.
        /// </summary>
        /// <param name="grainReference">The grain reference.</param>
        /// <returns>Task&lt;ReminderTableData&gt;.</returns>
        public async Task<ReminderTableData> ReadRows(GrainReference grainReference)
        {
            var timer = Stopwatch.StartNew();
            var result = new ReminderTableData();
            var key = GetKey(grainReference);

            var hashEntries = await _db.HashGetAllAsync(key);
            foreach (var hashEntry in hashEntries)
            {
                var info = _storageOptions.UseJson
                    ? JsonConvert.DeserializeObject<ReminderEntryInfo>(hashEntry.Value)
                    : _serializationManager.DeserializeFromByteArray<ReminderEntryInfo>(hashEntry.Value);
                result.Reminders.Add(info.ToEntry(_grainReferenceConverter));
            }

            timer.Stop();
            _logger.LogInformation(
                $"ReadRows: PrimaryKey={key} GrainId={grainReference} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            return result;
        }

        /// <summary>
        /// Return all rows that have their GrainReference's.GetUniformHashCode() in the range (start, end]
        /// </summary>
        /// <param name="begin">The begin.</param>
        /// <param name="end">The end.</param>
        /// <returns>Task&lt;ReminderTableData&gt;.</returns>
        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            var timer = Stopwatch.StartNew();
            var result = new ReminderTableData();
            var pattern = $"*|Reminder|{(_storageOptions.UseJson ? "json" : "binary")}";
            var keys = _storageOptions.DatabaseNumber.HasValue
                ? _server.Keys(_storageOptions.DatabaseNumber.Value, pattern)
                : _server.Keys(pattern: pattern);

            if (begin < end)
            {
                keys = keys.Where(x =>
                {
                    var hash = uint.Parse(((string)x).Split('|')[1]);
                    return hash > begin && hash <= end;
                });
            }
            else
            {
                keys = keys.Where(x =>
                {
                    var hash = uint.Parse(((string)x).Split('|')[1]);
                    return hash > begin || hash <= end;
                });
            }

            foreach (var key in keys)
            {
                var hashEntries = await _db.HashGetAllAsync(key);
                foreach (var hashEntry in hashEntries)
                {
                    var info = _storageOptions.UseJson
                        ? JsonConvert.DeserializeObject<ReminderEntryInfo>(hashEntry.Value)
                        : _serializationManager.DeserializeFromByteArray<ReminderEntryInfo>(hashEntry.Value);
                    result.Reminders.Add(info.ToEntry(_grainReferenceConverter));
                }
            }

            timer.Stop();
            _logger.LogInformation(
                $"ClearTable: ServiceId={_serviceId} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            return result;
        }

        /// <summary>
        /// Reads the row.
        /// </summary>
        /// <param name="grainReference">The grain reference.</param>
        /// <param name="reminderName">Name of the reminder.</param>
        /// <returns>Task&lt;ReminderEntry&gt;.</returns>
        public async Task<ReminderEntry> ReadRow(GrainReference grainReference, string reminderName)
        {
            var timer = Stopwatch.StartNew();
            var key = GetKey(grainReference);

            var hashEntry = await _db.HashGetAsync(key, reminderName);
            ReminderEntry reminderEntry = default;
            if (hashEntry.HasValue)
            {
                var info = _storageOptions.UseJson
                    ? JsonConvert.DeserializeObject<ReminderEntryInfo>(hashEntry)
                    : _serializationManager.DeserializeFromByteArray<ReminderEntryInfo>(hashEntry);
                reminderEntry = info.ToEntry(_grainReferenceConverter);
            }

            timer.Stop();
            _logger.LogInformation(
                $"ReadRow: PrimaryKey={key} GrainId={grainReference} ReminderName={reminderName} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            return reminderEntry;
        }

        /// <summary>
        /// Upserts the row.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            var timer = Stopwatch.StartNew();
            var key = GetKey(entry.GrainRef);
            var info = ReminderEntryInfo.FromEntry(entry);
            info.ETag = Guid.NewGuid().ToString();

            if (_storageOptions.UseJson)
                await _db.HashSetAsync(key, info.ReminderName, JsonConvert.SerializeObject(info));
            else
                await _db.HashSetAsync(key, info.ReminderName, _serializationManager.SerializeToByteArray(info));

            timer.Stop();
            _logger.LogInformation(
                $"UpsertRow: PrimaryKey={key} GrainId={entry.GrainRef} ReminderName={entry.ReminderName} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            return info.ETag;
        }

        /// <summary>
        /// Removes the row.
        /// </summary>
        /// <param name="grainReference">The grain reference.</param>
        /// <param name="reminderName">Name of the reminder.</param>
        /// <param name="eTag">The e tag.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> RemoveRow(GrainReference grainReference, string reminderName, string eTag)
        {
            var timer = Stopwatch.StartNew();
            var key = GetKey(grainReference);

            var hashEntry = await _db.HashGetAsync(key, reminderName);
            var remove = false;
            if (hashEntry.HasValue)
            {
                var info = _storageOptions.UseJson
                ? JsonConvert.DeserializeObject<ReminderEntryInfo>(hashEntry)
                : _serializationManager.DeserializeFromByteArray<ReminderEntryInfo>(hashEntry);
                remove = info.ETag == eTag;
            }

            if (remove)
            {
                await _db.HashDeleteAsync(key, reminderName);
            }

            timer.Stop();
            _logger.LogInformation(
                $"RemoveRow: PrimaryKey={key} GrainId={grainReference} ReminderName={reminderName} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
            return remove;
        }

        /// <summary>
        /// Tests the only clear table.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task TestOnlyClearTable()
        {
            var timer = Stopwatch.StartNew();
            var pattern = $"*|Reminder|{(_storageOptions.UseJson ? "json" : "binary")}";
            var keys = _storageOptions.DatabaseNumber.HasValue
                ? _server.Keys(_storageOptions.DatabaseNumber.Value, pattern)
                : _server.Keys(pattern: pattern);

            await _db.KeyDeleteAsync(keys.ToArray());

            timer.Stop();
            _logger.LogInformation(
                $"ClearTable: ServiceId={_serviceId} from Database={_db.Database}, finished in {timer.Elapsed.TotalMilliseconds:0.00} ms");
        }
    }
}