using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IGrainReferenceConverter _grainReferenceConverter;
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
            IOptions<RedisReminderTableOptions> redisOptions,
            IGrainReferenceConverter grainReferenceConverter)
        {
            _redisOptions = redisOptions.Value;
            _clusterOptions = clusterOptions.Value;
            _logger = logger;

            GrainHashIndexKey = GetGrainHashIndexKey(_clusterOptions.ServiceId);
            _grainReferenceConverter = grainReferenceConverter;
        }

        public async Task Init()
        {
            _muxer = await _redisOptions.CreateMultiplexer(_redisOptions);
            _db = _muxer.GetDatabase(_redisOptions.DatabaseNumber ?? 0);
        }

        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            // fetch reminder by reminder key
            RedisKey reminderKey = GetReminderKey(_clusterOptions.ServiceId, grainRef, reminderName);
            RedisValue value = await _db.HashGetAsync(reminderKey, DataHashKey);
            ReminderData data = JsonConvert.DeserializeObject<ReminderData>(value.ToString());
            return data.ToEntry(_grainReferenceConverter);
        }

        public async Task<ReminderTableData> ReadRows(GrainReference key)
        {
            // fetch reminders list by grain key
            RedisKey grainKey = GetGrainKey(_clusterOptions.ServiceId, key);
            RedisValue[] values = await _db.HashValuesAsync(grainKey);
            IEnumerable<ReminderEntry> records = values
                .Select(v =>
                    JsonConvert.DeserializeObject<ReminderData>(v.ToString())
                        .ToEntry(_grainReferenceConverter)
                );
            return new ReminderTableData(records);
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            // fetch reminders by grain hash range
            IEnumerable<RedisValue> values;
            if (begin < end)
            {
                // -----begin******end-----
                values = await _db.SortedSetRangeByScoreAsync(GrainHashIndexKey, begin, end, Exclude.Start);
            }
            else
            {
                // *****end------begin*****
                RedisValue[] values1 = await _db.SortedSetRangeByScoreAsync(GrainHashIndexKey, start: begin, exclude: Exclude.Start);
                RedisValue[] values2 = await _db.SortedSetRangeByScoreAsync(GrainHashIndexKey, stop: end);
                values = values1.Concat(values2);
            }

            IEnumerable<ReminderEntry> records = values
                .Select(v =>
                    JsonConvert.DeserializeObject<ReminderData>(v.ToString())
                        .ToEntry(_grainReferenceConverter)
                );
            return new ReminderTableData(records);
        }

        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            RedisKey reminderKey = GetReminderKey(_clusterOptions.ServiceId, grainRef, reminderName);
            RedisKey grainKey = GetGrainKey(_clusterOptions.ServiceId, grainRef);

            // get the original value, used for sorted set removal later
            RedisValue value = await _db.HashGetAsync(reminderKey, DataHashKey);
            if (!value.HasValue) return false;

            ITransaction tx = _db.CreateTransaction();
            tx.AddCondition(Condition.HashEqual(reminderKey, ETagHashKey, eTag));
            tx.SortedSetRemoveAsync(GrainHashIndexKey, value).Ignore(); // delete value in grain hash set
            tx.HashDeleteAsync(grainKey, reminderName).Ignore(); // delete value in grain hash set
            tx.KeyDeleteAsync(reminderKey).Ignore(); // delete reminder key
            bool success = await tx.ExecuteAsync();

            return success;
        }

        public async Task TestOnlyClearTable()
        {
            await _db.ExecuteAsync("FLUSHDB");
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

            ReminderData data = ReminderData.Create(entry);
            string payload = JsonConvert.SerializeObject(data, _jsonSettings);

            ITransaction tx = _db.CreateTransaction();
            HashEntry[] hashEntries = new HashEntry[] {
                new HashEntry(ETagHashKey, newEtag),
                new HashEntry(DataHashKey, payload)
            };
            tx.HashSetAsync(reminderKey, hashEntries).Ignore();
            tx.HashSetAsync(grainKey, entry.ReminderName, payload).Ignore();
            tx.SortedSetAddAsync(GrainHashIndexKey, payload, entry.GrainRef.GetUniformHashCode()).Ignore();

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

    internal class ReminderData
    {
        public string ETag { get; set; }
        public string GrainReference { get; set; }
        public string ReminderName { get; set; }
        public DateTime StartAt { get; set; }
        public TimeSpan Period { get; set; }

        public static ReminderData Create(ReminderEntry entry)
        {
            ReminderData data = new ReminderData
            {
                ETag = entry.ETag,
                GrainReference = entry.GrainRef.ToKeyString(),
                ReminderName = entry.ReminderName,
                StartAt = entry.StartAt,
                Period = entry.Period
            };
            return data;
        }

        public ReminderEntry ToEntry(IGrainReferenceConverter grainReferenceConverter)
        {
            ReminderEntry entry = new ReminderEntry
            {
                ETag = ETag,
                GrainRef = grainReferenceConverter.GetGrainFromKeyString(GrainReference),
                Period = Period,
                ReminderName = ReminderName,
                StartAt = StartAt
            };
            return entry;
        }
    }
}
