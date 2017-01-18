using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Core
{
    public class CircuitBreaker : DelegatingHandler
    {
        private readonly IList<ConfigRule> _rules;
        private readonly ILogger _logger;
         
        public CircuitBreaker(IRuleReader reader, ILogger logger)
        {
            _rules = reader.ReadConfigRules();
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
