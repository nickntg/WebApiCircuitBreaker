using System.Collections.Generic;
using WebApiCircuitBreaker.Core;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Extensions.Readers
{
    public class EmptyReader : IRuleReader
    {
        public IList<ConfigRule> ReadConfigRules()
        {
            return new List<ConfigRule>();
        }
    }
}
