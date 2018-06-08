using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Clustering.Redis
{
    internal class RedisMembershipTable : IMembershipTable
    {
        private readonly IDatabase _db;
        private readonly ClusterOptions _config;

        public RedisMembershipTable(IDatabase db, ClusterOptions config)
        {
            _db = db;
            _config = config;
        }
        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            _db.KeyDelete(clusterId);
            await Task.CompletedTask;
        }

        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            await Task.CompletedTask;
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), JsonConvert.SerializeObject(new VersionedEntry(entry, tableVersion)));
        }

        private RedisKey ClusterKey => $"{_config.ClusterId}:{_config.ServiceId}";


        public async Task<MembershipTableData> ReadAll()
        {
            var data = _db.HashGetAll(ClusterKey).Select(x => JsonConvert.DeserializeObject<VersionedEntry>(x.Value));
            var mtd = new MembershipTableData(data.Select(x => Tuple.Create(x.Entry, x.ResourceVersion)).ToList(), data.First().TableVersion);
            return await Task.FromResult(mtd);
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            var entry = JsonConvert.DeserializeObject<VersionedEntry>(await _db.HashGetAsync(ClusterKey, key.ToString()));
            return await Task.FromResult(new MembershipTableData(Tuple.Create(entry.Entry, entry.ResourceVersion), entry.TableVersion));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var record = JsonConvert.DeserializeObject<VersionedEntry>(await _db.HashGetAsync(ClusterKey, entry.SiloAddress.ToString()));
            record.Entry.IAmAliveTime = DateTime.Now;
            await _db.HashSetAsync(ClusterKey, record.Entry.SiloAddress.ToString(), JsonConvert.SerializeObject(record));
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), JsonConvert.SerializeObject(new VersionedEntry(entry, tableVersion) { ResourceVersion = etag }));
        }
    }
}