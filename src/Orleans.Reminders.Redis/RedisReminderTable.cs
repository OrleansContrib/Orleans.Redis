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
        private static readonly RedisValue ETagHashKey = "ETag";
        private static readonly RedisValue DataHashKey = "Data";
        private readonly RedisKey GrainHashIndexKey;
        private readonly RedisReminderTableOptions _redisOptions;
        private readonly ClusterOptions _clusterOptions;
        private readonly ILogger _logger;
        private IConnectionMultiplexer _muxer;
        private IDatabase _db;

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

            GrainHashIndexKey = GetGrainHashIndexKey(_clusterOptions.ServiceId);
        }

        public async Task Init()
        {
            _muxer = await _redisOptions.CreateMultiplexer(_redisOptions);
            _db = _muxer.GetDatabase(_redisOptions.DatabaseNumber ?? 0);
        }

        public Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            // fetch reminder by reminder key
            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            // fetch reminders list by grain key

            throw new NotImplementedException();
        }

        public Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            // fetch reminders by grain hash range
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

        public async Task<string> UpsertRow(ReminderEntry entry)
        {

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.Debug("UpsertRow entry = {0}, etag = {1}", entry.ToString(), entry.ETag);
            }

            RedisKey reminderKey = GetReminderKey(_clusterOptions.ServiceId, entry.GrainRef, entry.ReminderName);
            RedisKey grainKey = GetGrainKey(_clusterOptions.ServiceId, entry.GrainRef);
            string newEtag = Guid.NewGuid().ToString();
            // Should we change it now?
            entry.ETag = newEtag;

            ReminderData data = new ReminderData
            {
                GrainReference =entry.GrainRef.ToKeyString(),
                ReminderName = entry.ReminderName,
                StartAt = entry.StartAt,
                Period = entry.Period
            };
            string payload = JsonConvert.SerializeObject(data, _jsonSettings);

            ITransaction tx = _db.CreateTransaction();
            HashEntry[] hashEntries = new HashEntry[] {
                new HashEntry(ETagHashKey, newEtag),
                new HashEntry(DataHashKey, payload)
            };
            Task task1 = tx.HashSetAsync(reminderKey, hashEntries);
            task1.Ignore();
            Task<bool> task2 = tx.HashSetAsync(grainKey, entry.ReminderName, payload);
            task2.Ignore();
            Task<bool> task3 = tx.SortedSetAddAsync(GrainHashIndexKey, payload, entry.GrainRef.GetUniformHashCode());
            task3.Ignore();

            bool success = await tx.ExecuteAsync();
            if (success)
            {
                return newEtag;
            }
            else
            {
                _logger.Warn(ErrorCode.ReminderServiceBase,
                    $"Intermediate error updating entry {entry.ToString()} to Redis.");
                throw new ReminderException("Failed to upsert reminder");
            }
        }

        public static RedisKey GetReminderKey(string serviceId, GrainReference grainRef, string reminderName)
        {
            return $"{serviceId}_{grainRef.ToKeyString()}_{reminderName}";
        }

        public static RedisKey GetGrainKey(string serviceId, GrainReference grainRef)
        {
            return $"{serviceId}_{grainRef.ToKeyString()}";
        }

        public static RedisKey GetGrainHashIndexKey(string serviceId)
        {
            return $"{serviceId}_GrainHashIndex";
        }
    }

    public class ReminderData
    {
        public string GrainReference { get; set; }
        public string ReminderName { get; set; }
        public DateTime StartAt { get; set; }
        public TimeSpan Period { get; set; }
    }
}
