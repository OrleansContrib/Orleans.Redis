using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Messaging;
using Orleans.Runtime;
using Orleans.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Orleans.Clustering.Redis
{
    internal class RedisGatewayListProvider : IGatewayListProvider
    {
        public TimeSpan MaxStaleness => GatewayOptions.GatewayListRefreshPeriod;
        public bool IsUpdatable => false;
        public GatewayOptions GatewayOptions { get; }
        public ILoggerFactory LoggerFactory { get; }
        public ILogger Logger { get; }

        private RedisMembershipTable _table;

        public RedisGatewayListProvider(IMembershipTable table, GatewayOptions options, ILoggerFactory loggerFactory)
        {
            GatewayOptions = options;
            LoggerFactory = loggerFactory;
            Logger = loggerFactory?.CreateLogger<RedisGatewayListProvider>();
            Logger?.LogInformation("In RedisGatewayListProvider constructor");
            _table = table as RedisMembershipTable;
        }

        public async Task<IList<Uri>> GetGateways()
        {
            Logger?.Debug($"{nameof(GetGateways)}");
            var all = await _table.ReadAll();
            var result = all.Members
               .Where(x => x.Item1.Status == SiloStatus.Active && x.Item1.ProxyPort != 0)
               .Select(x =>
                {
                    x.Item1.SiloAddress.Endpoint.Port = x.Item1.ProxyPort;
                    return x.Item1.SiloAddress.ToGatewayUri();
                }).ToList();
            return await Task.FromResult(result);
        }

        public async Task InitializeGatewayListProvider()
        {
            Logger?.Debug($"{nameof(InitializeGatewayListProvider)}");
            await Task.CompletedTask;
        }
    }
}