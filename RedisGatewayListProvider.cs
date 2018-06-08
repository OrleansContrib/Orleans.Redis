using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Messaging;
using Orleans.Runtime;
using Orleans.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Orleans.Clustering.Redis
{
    internal class RedisGatewayListProvider : IGatewayListProvider
    {
        public RedisGatewayListProvider(RedisMembershipTable table, IOptions<GatewayOptions> gatewayOptions){
            GatewayOptions = gatewayOptions.Value;
            _table = table;
        }
        public TimeSpan MaxStaleness => GatewayOptions.GatewayListRefreshPeriod;

        public bool IsUpdatable => true;
        public GatewayOptions GatewayOptions { get; }

        private RedisMembershipTable _table;

        public async Task<IList<Uri>> GetGateways()
        {
            var all =await  _table.ReadAll();
            return await Task.FromResult(all.Members.Select(x=>x.Item1.SiloAddress.ToGatewayUri()).ToList());
        }

        public async Task InitializeGatewayListProvider()
        {
            await Task.FromResult(0);
        }
    }
}