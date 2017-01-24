using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApiCircuitBreaker.Core.Interfaces;

namespace WebApiCircuitBreaker.Core
{
    public class RuleManager
    {
        private readonly IRuleReader _reader;
        private readonly ILogger _logger;
        private IList<ConfigRule> _primary;
        private IList<ConfigRule> _secondary;
        private TimeSpan _loadingTimeSpan;
        private bool _isPrimary;

        public RuleManager(IRuleReader reader, ILogger logger, RuleLoadingStrategy loadingStrategy)
        {
            _reader = reader;
            _logger = logger;
            _isPrimary = false;
            LoadOnce();
            if (loadingStrategy.RuleLoadingInteval == RuleLoadingIntervalEnum.LoadAndRefreshPeriodically)
            {
                _loadingTimeSpan = loadingStrategy.RuleLoadingTimeSpan;
                Task.Factory.StartNew(LoadRules, TaskCreationOptions.LongRunning);
            }
        }

        public IList<ConfigRule> Rules => _isPrimary ? _primary : _secondary;

        private async Task LoadRules()
        {
            while (true)
            {
                await Task.Delay(_loadingTimeSpan);
                LoadOnce();
            }
        }

        private void LoadOnce()
        {
            var rules = _reader.ReadConfigRules(Environment.MachineName);
            if (_isPrimary)
            {
                _secondary = rules;
            }
            else
            {
                _primary = rules;
            }
            _isPrimary = !_isPrimary;
            _logger?.LogRulesLoaded($"Rules loaded at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
        }
    }
}
