using System.Collections.Generic;
using System.Linq;
using RestSharp;
using WebApiCircuitBreaker.Core;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Extensions.Readers
{
    public class JsonHttpReader : IRuleReader
    {
        public IList<ConfigRule> ReadConfigRules(string machineName)
        {
            var o = new RestClient(Properties.Settings.Default.RulesUrl);
            var r = new RestRequest(Method.GET) { RequestFormat = DataFormat.Json };
            var response = o.Execute<List<ConfigRuleRest>>(r);
            return response.Data.Select(item => item.ToConfigRule()).ToList();
        }
    }

    internal class ConfigRuleRest
    {
        public string RuleName { get; set; }

        public bool IsActive { get; set; }

        public List<string> ApplicableServers { get; set; }

        public ApplicabilityScopeEnum ApplicabilityScope { get; set; }

        public RouteScopeEnum RouteScope { get; set; }

        public List<string> WhiteList { get; set; }

        public List<string> BlackList { get; set; }

        public LimitInfo LimitInfo { get; set; }

        public EnforcementInfo EnforcementInfo { get; set; }

        public ConfigRule ToConfigRule()
        {
            return new ConfigRule
            {
                LimitInfo = LimitInfo,
                RuleName = RuleName,
                EnforcementInfo = EnforcementInfo,
                ApplicabilityScope = ApplicabilityScope,
                IsActive = IsActive,
                RouteScope = RouteScope,
                ApplicableServers = ApplicableServers == null ? null : new HashSet<string>(ApplicableServers),
                BlackList = BlackList == null ? null : new HashSet<string>(BlackList),
                WhiteList = WhiteList == null ? null : new HashSet<string>(WhiteList)
            };
        }
    }
}
