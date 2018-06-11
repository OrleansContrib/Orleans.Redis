using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using StackExchange.Redis;
using Orleans.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Orleans.Clustering.Redis
{
    internal class RedisMembershipTable : IMembershipTable
    {
        private readonly IDatabase _db;
        private readonly ClusterOptions _config;
        private readonly ILogger<RedisMembershipTable> _logger;

        public RedisMembershipTable(IDatabase db, ClusterOptions config, ILogger<RedisMembershipTable> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _logger.LogInformation("In RedisMembershipTable constructor");
        }

        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            _logger.Debug($"{nameof(DeleteMembershipTableEntries)}: {clusterId}");
            await _db.KeyDeleteAsync(clusterId);
            await Task.CompletedTask;
        }

        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            _logger.Debug($"{nameof(InitializeMembershipTable)}: {tryInitTableVersion}");
            await Task.CompletedTask;
        }
        private string Serialize<T>(T value)
        {
			return JsonConvert.SerializeObject(value, 
				new IPEndPointJsonConverter(), new SiloAddressJsonConverter());
        }

		private T Deserialize<T>(string json)
		{
			return JsonConvert.DeserializeObject<T>(json, 
				new IPEndPointJsonConverter(), new SiloAddressJsonConverter());
		}
        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            _logger.Debug($"{nameof(InsertRow)}: {Serialize(entry)}, {Serialize(tableVersion)}");
            return await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), Serialize(new VersionedEntry(entry, tableVersion)));
        }

        private RedisKey ClusterKey => $"{_config.ClusterId}:{_config.ServiceId}";


        public async Task<MembershipTableData> ReadAll()
        {
            _logger.Debug(nameof(ReadAll));
            var data = _db.HashGetAll(ClusterKey).Select(x => Deserialize<VersionedEntry>(x.Value));
            if (!data.Any())
            {
                return await Task.FromResult(new MembershipTableData(new TableVersion(1, "v1")));
            }
            var mtd = new MembershipTableData(data.Select(x => Tuple.Create(x.Entry, x.ResourceVersion)).ToList(), data.First().TableVersion);
            mtd.SupressDuplicateDeads();
            foreach (var item in mtd.Members.ToArray())
            {
                if (item.Item1.Status == SiloStatus.Dead)
                {
                    _db.HashDelete(ClusterKey, item.Item1.SiloAddress.ToString());
                }
            }
            _logger.LogInformation(mtd.ToString());
            return await Task.FromResult(mtd);
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            _logger.Debug($"{nameof(ReadRow)}: {key.ToString()}");
            var val = await _db.HashGetAsync(ClusterKey, key.ToString());
            if (val.HasValue)
            {
                var entry = Deserialize<VersionedEntry>(val);
                return await Task.FromResult(new MembershipTableData(Tuple.Create(entry.Entry, entry.ResourceVersion), entry.TableVersion));
            }
            return await Task<MembershipTableData>.FromResult((MembershipTableData)new MembershipTableData(new TableVersion(1, "etag1")));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            _logger.Debug($"{nameof(UpdateIAmAlive)}: {Serialize(entry)}");

            if (_db.HashExists(ClusterKey, entry.SiloAddress.ToString()))
            {
                var record = Deserialize<VersionedEntry>(await _db.HashGetAsync(ClusterKey, entry.SiloAddress.ToString()));
                record.Entry.IAmAliveTime = DateTime.Now;
                await _db.HashSetAsync(ClusterKey, record.Entry.SiloAddress.ToString(), Serialize(record));
            }
            else
            {
                await InsertRow(entry, new TableVersion(1, "v1"));
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            _logger.Debug($"{nameof(UpdateRow)}");
            await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), Serialize(new VersionedEntry(entry, tableVersion) { ResourceVersion = etag }));
            return await Task.FromResult(true);
        }
    }
}