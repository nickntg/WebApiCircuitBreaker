using System.Collections.Generic;

namespace WebApiCircuitBreaker.Core.Interfaces
{
    public interface IRuleReader
    {
        IList<ConfigRule> ReadConfigRules(string machineName);
    }
}
