using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Orleans.MultiCluster;

namespace Orleans.Clustering.Redis
{
    [Serializable, OneInstancePerCluster]
    internal class RedisMembershipTableGrain : Grain, IMembershipTable, IMembershipTableGrain, IGrainWithGuidKey, IAddressable
    {
        private IMembershipTable _table;

        public override async Task OnActivateAsync()
        {
            _table = base.ServiceProvider.GetService<IMembershipTable>();
            Console.WriteLine($"{nameof(OnActivateAsync)}: Table type: {_table.GetType()}");
            await base.OnActivateAsync();
        }
        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            await _table.DeleteMembershipTableEntries(clusterId);
        }

        public async Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            await _table.InitializeMembershipTable(tryInitTableVersion);
        }

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return await _table.InsertRow(entry, tableVersion);
        }

        public async Task<MembershipTableData> ReadAll()
        {
            return await _table.ReadAll();
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return await _table.ReadRow(key);
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            await _table.UpdateIAmAlive(entry);
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return await _table.UpdateRow(entry, etag, tableVersion);
        }
    }
}