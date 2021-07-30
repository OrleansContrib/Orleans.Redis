using System;
using System.Collections.Generic;
using System.Globalization;
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
                : _muxer.GetDatabase();
        }

        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            (string from, string to) = GetFilter(grainRef, reminderName);
            RedisValue[] values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, from, to);
            return ConvertToEntry(values.Single());
        }

        public async Task<ReminderTableData> ReadRows(GrainReference key)
        {
            (string from, string to) = GetFilter(key);
            RedisValue[] values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, from, to);
            IEnumerable<ReminderEntry> records = values.Select(v => ConvertToEntry(v));
            return new ReminderTableData(records);
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            (string _, string from) = GetFilter(begin);
            (string _, string to) = GetFilter(end);
            IEnumerable<RedisValue> values;
            if (begin < end)
            {
                // -----begin******end-----
                values = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, from, to);
            }
            else
            {
                // *****end------begin*****
                RedisValue[] values1 = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, from, "[\"FFFFFFFF\",#");
                RedisValue[] values2 = await _db.SortedSetRangeByValueAsync(RemindersRedisKey, "[\"00000000\",\"", to);
                values = values1.Concat(values2);
            }

            IEnumerable<ReminderEntry> records = values.Select(v => ConvertToEntry(v));
            return new ReminderTableData(records);
        }

        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
            (RedisValue from, RedisValue to) = GetFilter(grainRef, reminderName, eTag);
            long removed = await _db.SortedSetRemoveRangeByValueAsync(RemindersRedisKey, from, to);
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

            (string etag, string value) = ConvertFromEntry(entry);
            (string from, string to) = GetFilter(entry.GrainRef, entry.ReminderName);

            ITransaction tx = _db.CreateTransaction();
            _db.SortedSetRemoveRangeByValueAsync(RemindersRedisKey, from, to).Ignore();
            _db.SortedSetAddAsync(RemindersRedisKey, value, 0).Ignore();
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

        private ReminderEntry ConvertToEntry(string reminderValue)
        {
            string[] segments = JsonConvert.DeserializeObject<string[]>(reminderValue);

            return new ReminderEntry
            {
                GrainRef = _grainReferenceConverter.GetGrainFromKeyString(segments[1]),
                ReminderName = segments[2],
                ETag = segments[3],
                StartAt = DateTime.Parse(segments[4], null, DateTimeStyles.RoundtripKind),
                Period = TimeSpan.Parse(segments[5]),
            };
        }

        private (string from, string to) GetFilter(uint grainHash)
        {
            return GetFilter(grainHash.ToString("X8"));
        }

        private (string from, string to) GetFilter(GrainReference grainRef)
        {
            return GetFilter(grainRef.GetUniformHashCode().ToString("X8"), grainRef.ToKeyString());
        }

        private (string from, string to) GetFilter(GrainReference grainRef, string reminderName)
        {
            return GetFilter(grainRef.GetUniformHashCode().ToString("X8"), grainRef.ToKeyString(), reminderName);
        }

        private (string from, string to) GetFilter(GrainReference grainRef, string reminderName, string eTag)
        {
            return GetFilter(grainRef.GetUniformHashCode().ToString("X8"), grainRef.ToKeyString(), reminderName, eTag);
        }

        private (string from, string to) GetFilter(params string[] segments)
        {
            string prefix = JsonConvert.SerializeObject(segments, _jsonSettings);
            prefix = prefix.Remove(prefix.Length - 1);
            string from = prefix + ",\"";
            string to = prefix + ",#";
            return (from, to);
        }


        private (string eTag, string value) ConvertFromEntry(ReminderEntry entry)
        {
            string grainHash = entry.GrainRef.GetUniformHashCode().ToString("X8");
            string eTag = Guid.NewGuid().ToString();
            string[] segments = new string[]
            {
                grainHash,
                entry.GrainRef.ToKeyString(),
                entry.ReminderName,
                eTag,
                entry.StartAt.ToString("O"),
                entry.Period.ToString()
            };

            return (eTag, JsonConvert.SerializeObject(segments, _jsonSettings));
        }
    }
}
