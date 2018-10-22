using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using StackExchange.Redis;
using Orleans.Configuration;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Orleans.Clustering.Redis.Test")]

namespace Orleans.Clustering.Redis
{
    internal class RedisMembershipTable : IMembershipTable
    {
        private readonly IDatabase _db;
        private readonly RedisOptions _redisOptions;
        private readonly ClusterOptions _clusterOptions;
        public ILoggerFactory LoggerFactory { get; }
        public ILogger Logger { get; }

        public RedisMembershipTable(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> redisOptions, IOptions<ClusterOptions> clusterOptions, ILoggerFactory loggerFactory)
        {
            _redisOptions = redisOptions.Value;
            _db = multiplexer.GetDatabase(_redisOptions.Database);
            _clusterOptions = clusterOptions.Value;
            LoggerFactory = loggerFactory;
            Logger = loggerFactory?.CreateLogger<RedisMembershipTable>();
            Logger?.LogInformation("In RedisMembershipTable constructor");
        }

        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            Logger?.Debug($"{nameof(DeleteMembershipTableEntries)}: {ClusterKey}");
            await _db.KeyDeleteAsync(ClusterKey);
        }

        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            Logger?.Debug($"{nameof(InitializeMembershipTable)}: {tryInitTableVersion}");
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
            Logger?.Debug($"{nameof(InsertRow)}: {Serialize(entry)}, {Serialize(tableVersion)}");

            var currentTable = await ReadAll();
            if (tableVersion.Version <= currentTable.Version.Version || currentTable.Contains(entry.SiloAddress))
                return false;
            var etag = $"{tableVersion.Version}";
            return await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), Serialize(new VersionedEntry(entry, tableVersion) { ResourceVersion = etag }));
        }

        private RedisKey ClusterKey => $"{_clusterOptions.ClusterId}";


        public async Task<MembershipTableData> ReadAll()
        {
            Logger?.Debug(nameof(ReadAll));
            var data = _db.HashGetAll(ClusterKey).Select(x => Deserialize<VersionedEntry>(x.Value));

            if (!data.Any())
            {
                return await Task.FromResult(new MembershipTableData(new TableVersion(0, "0")));
            }

            var highestVersion = data.OrderByDescending(x => x.TableVersion.Version).First();
            var mtd = new MembershipTableData(data.Select(x => Tuple.Create(x.Entry, x.ResourceVersion)).ToList(), new TableVersion(highestVersion.TableVersion.Version, highestVersion.ResourceVersion));
            mtd.SupressDuplicateDeads();
            foreach (var item in mtd.Members.ToArray())
            {
                if (item.Item1.Status == SiloStatus.Dead)
                {
                    await _db.HashDeleteAsync(ClusterKey, item.Item1.SiloAddress.ToString());
                }
            }
            Logger?.LogInformation(mtd.ToString());
            return await Task.FromResult(mtd);
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            Logger?.Debug($"{nameof(ReadRow)}: {key.ToString()}");
            var val = await _db.HashGetAsync(ClusterKey, key.ToString());
            if (val.HasValue)
            {
                var entry = Deserialize<VersionedEntry>(val);
                return await Task.FromResult(new MembershipTableData(Tuple.Create(entry.Entry, entry.ResourceVersion), new TableVersion(entry.TableVersion.Version, entry.ResourceVersion)));
            }
            return await Task.FromResult(new MembershipTableData(new TableVersion(0, "0")));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            Logger?.Debug($"{nameof(UpdateIAmAlive)}: {Serialize(entry)}");

            if (_db.HashExists(ClusterKey, entry.SiloAddress.ToString()))
            {
                var record = Deserialize<VersionedEntry>(await _db.HashGetAsync(ClusterKey, entry.SiloAddress.ToString()));
                record.Entry.IAmAliveTime = DateTime.UtcNow;
                await _db.HashSetAsync(ClusterKey, record.Entry.SiloAddress.ToString(), Serialize(record));
            }
            else
            {
                await InsertRow(entry, new TableVersion(0, "0"));
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            Logger?.Debug($"{nameof(UpdateRow)}");
            var currentTable = await ReadAll();
            if (tableVersion.Version <= currentTable.Version.Version)// || currentTable.Version.VersionEtag != tableVersion.VersionEtag)
                return false;

            await _db.HashSetAsync(ClusterKey, entry.SiloAddress.ToString(), Serialize(new VersionedEntry(entry, tableVersion) { ResourceVersion = etag }));
            return true;
        }
    }
}