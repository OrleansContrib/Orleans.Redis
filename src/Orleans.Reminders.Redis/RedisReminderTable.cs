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
        private readonly RedisKey RemindersRedisKey;
        private readonly RedisReminderTableOptions _redisOptions;
        private readonly ClusterOptions _clusterOptions;
        private readonly ILogger _logger;
        private readonly IGrainReferenceConverter _grainReferenceConverter;
        private IConnectionMultiplexer _muxer;
        private IDatabase _db;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
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

            RemindersRedisKey = $"{_clusterOptions.ServiceId}_Reminders";
            _grainReferenceConverter = grainReferenceConverter;
        }

        public async Task Init()
        {
            _muxer = await _redisOptions.CreateMultiplexer(_redisOptions);
            _db = _redisOptions.DatabaseNumber.HasValue
                ? _muxer.GetDatabase(_redisOptions.DatabaseNumber.Value)
                : _muxer.GetDatabase(_redisOptions.DatabaseNumber.Value);
        }

        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            // fetch reminder by reminder id
            string filter = $"{GetReminderId(grainRef, reminderName)}:";
            RedisValue[] values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, filter, filter + ((char)0xFF));
            // if we need to check there should be only single values?
            return ConvertToEntry(values.Single());
        }

        public async Task<ReminderTableData> ReadRows(GrainReference key)
        {
            string filter = $"{key.GetUniformHashCode():X8}_{key.ToKeyString()}_";
            RedisValue[] values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, filter, filter + ((char)0xFF));
            IEnumerable<ReminderEntry> records = values.Select(v => ConvertToEntry(v));
            return new ReminderTableData(records);
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            RedisValue filterBegin = $"{begin:X8}_";
            RedisValue filterEnd = $"{end:X8}" + (char)('_' + 1);
            IEnumerable<RedisValue> values;
            if (begin < end)
            {
                // -----begin******end-----
                values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, filterBegin, filterEnd);
            }
            else
            {
                // *****end------begin*****
                RedisValue[] values1 = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, filterBegin, "FFFFFFFF" + ((char)0xFF));
                RedisValue[] values2 = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, "00000000_", filterEnd);
                values = values1.Concat(values2);
            }

            IEnumerable<ReminderEntry> records = values.Select(v => ConvertToEntry(v));
            return new ReminderTableData(records);
        }

        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            string filter = $"{GetReminderId(grainRef, reminderName)}:{eTag}:";
            long removed = await _db.SortedSetRemoveRangeByValueAsync(RemindersRedisKey, filter, filter + ((char)0xFF));
            return removed > 0;
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

            if (entry.ReminderName.IndexOf(':') != -1)
            {
                throw new ReminderException($"ReminderName should not contain ':' for Redis Reminder Provider");
            }

            (string reminderId, string etag, RedisValue reminderValue) = ConvertFromEntry(entry);
            string filter = $"{reminderId}:";

            ITransaction tx = _db.CreateTransaction();
            _db.SortedSetRemoveRangeByValueAsync(RemindersRedisKey, filter, filter + ((char)0xFF)).Ignore();
            _db.SortedSetAddAsync(RemindersRedisKey, reminderValue, 0).Ignore();
            bool success = await tx.ExecuteAsync();
            if (success)
            {
                return etag;
            }
            else
            {
                _logger.Warn(ErrorCode.ReminderServiceBase,
                    $"Intermediate error updating entry {entry} to Redis.");
                throw new ReminderException("Failed to upsert reminder");
            }
        }

        private static string GetReminderId(GrainReference grainRef, string reminderName)
        {
            uint grainHash = grainRef.GetUniformHashCode();
            return $"{grainHash:X8}_{grainRef.ToKeyString()}_{reminderName}";
        }

        private ReminderEntry ConvertToEntry(RedisValue reminderValue)
        {
            string value = reminderValue.ToString();
            string[] valueSegments = value.Split(new[] { ':' }, 3);
            string reminderId = valueSegments[0];
            string etag = valueSegments[1];
            string payload = valueSegments[2];

            string[] idSegments = reminderId.Split(new[] { '_' }, 3);
            string grainRef = idSegments[1];
            string reminderName = idSegments[2];

            ReminderData data = JsonConvert.DeserializeObject<ReminderData>(payload);

            return new ReminderEntry
            {
                GrainRef = _grainReferenceConverter.GetGrainFromKeyString(grainRef),
                Period = data.Period,
                ReminderName = reminderName,
                StartAt = data.StartAt,
                ETag = etag,
            };
        }

        public (string reminderId, string eTag, RedisValue reminderValue) ConvertFromEntry(ReminderEntry entry)
        {
            string reminderId = GetReminderId(entry.GrainRef, entry.ReminderName);

            string eTag = Guid.NewGuid().ToString();

            ReminderData data = new()
            {
                StartAt = entry.StartAt,
                Period = entry.Period,
            };
            string payload = JsonConvert.SerializeObject(data, _jsonSettings);

            return (reminderId, eTag, $"{reminderId}:{eTag}:{payload}");
        }
    }

    internal class ReminderData
    {
        public DateTime StartAt { get; set; }
        public TimeSpan Period { get; set; }
    }
}
