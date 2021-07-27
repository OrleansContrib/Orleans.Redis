using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Orleans.Configuration;
using Orleans.Runtime;

using StackExchange.Redis;

namespace Orleans.Reminders.Redis
{
    internal class RedisReminderTable : IReminderTable
    {
        private const string ETagHashKey = "ETag";
        private const string ETagDataKey = "Data";
        private IConnectionMultiplexer _muxer;
        private IDatabase _db;
        private readonly RedisReminderTableOptions _redisOptions;
        private readonly ClusterOptions _clusterOptions;
        private readonly ILogger _logger;

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        };

        public RedisReminderTable(
            ILogger<RedisReminderTable> logger,
            IOptions<ClusterOptions> clusterOptions,
            IOptions<RedisReminderTableOptions> redisOptions)
        {
            _redisOptions = redisOptions.Value;
            _clusterOptions = clusterOptions.Value;
            _logger = logger;
        }

        public async Task Init()
        {
            _muxer = await _redisOptions.CreateMultiplexer(_redisOptions);
            _db = _muxer.GetDatabase(_redisOptions.DatabaseNumber ?? 0);
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            throw new NotImplementedException();
        }

        public Task TestOnlyClearTable()
        {
            throw new NotImplementedException();
        }

        public Task<string> UpsertRow(ReminderEntry entry)
        {
            RedisKey rowKey = ConstructRowKey(_clusterOptions.ServiceId, entry.GrainRef, entry.ReminderName);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Debug("UpsertRow entry = {0}, etag = {1}", entry.ToString(), entry.ETag);
            }

            var oldEtag = entry.ETag;
            var newEtag = Guid.NewGuid().ToString();
            var payload = JsonConvert.SerializeObject(entry, _jsonSettings);

            // Do we need to change it now?
            //entry.ETag = newEtag;

            _db.
            ITransaction tx = _db.CreateTransaction();
            ConditionResult versionCondition;
            if (entry.ETag is not null)
            {
                versionCondition = tx.AddCondition(Condition.HashEqual(rowKey, ETagHashKey, oldEtag));

                var hashEntries = new HashEntry[] {
                    new HashEntry(ETagHashKey, newEtag),
                    new HashEntry()
                }
                tx.HashSetAsync(rowKey).Ignore();
            }
            else
            {

            }

            var success = await tx.ExecuteAsync();


            throw new NotImplementedException();
        }

        public static string ConstructRowKey(string serviceId, GrainReference grainRef, string reminderName)
        {
            //return $"{serviceId}_{grainRef.ToKeyString()}_{reminderName}";
            uint grainHash = grainRef.GetUniformHashCode();
            return $"{serviceId}_{grainHash}_{grainRef.ToKeyString()}_{reminderName}";
        }
    }

    public record ReminderDataRedis()
}
