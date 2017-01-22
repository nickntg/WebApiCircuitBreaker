using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Core.Tests.Unit
{
    [TestFixture]
    public class RuleManagerTests
    {
        [Test]
        public void EnsureSingleRuleRead()
        {
            var ruleManager = new RuleManager(new IncrementalReader(), null,
                new RuleLoadingStrategy
                {
                    RuleLoadingInteval = RuleLoadingIntervalEnum.LoadOnce,
                    RuleLoadingTimeSpan = new TimeSpan(0, 0, 0, 0, 10)
                });

            Assert.AreEqual(1, ruleManager.Rules.Count);

            Thread.Sleep(100);

            Assert.AreEqual(1, ruleManager.Rules.Count);
        }

        [Test]
        public void EnsureRuleRollover()
        {
            var ruleManager = new RuleManager(new IncrementalReader(), null,
                new RuleLoadingStrategy
                {
                    RuleLoadingInteval = RuleLoadingIntervalEnum.LoadAndRefreshPeriodically,
                    RuleLoadingTimeSpan = new TimeSpan(0, 0, 0, 0, 500)
                });

            Assert.AreEqual(1, ruleManager.Rules.Count);

            Thread.Sleep(600);

            Assert.AreEqual(2, ruleManager.Rules.Count);

            Thread.Sleep(600);

            Assert.AreEqual(3, ruleManager.Rules.Count);
        }
    }

    internal class IncrementalReader : IRuleReader
    {
        private readonly IList<ConfigRule> _rules;

        public IncrementalReader()
        {
            _rules = new List<ConfigRule>();
        }

        public IList<ConfigRule> ReadConfigRules(string machineName)
        {
            _rules.Add(new ConfigRule());
            return _rules;
        }
    }
}
