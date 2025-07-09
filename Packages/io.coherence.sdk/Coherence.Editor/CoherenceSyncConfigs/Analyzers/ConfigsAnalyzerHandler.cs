// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class ConfigsAnalyzerHandler
    {
        private List<CoherenceSyncConfigAnalyzer> analyzers = new List<CoherenceSyncConfigAnalyzer>();

        private EntryInfo entriesInfo;
        private Dictionary<CoherenceSyncConfig, EntryInfo> entriesDictionary;

        public EntryInfo AllEntriesInfo => entriesInfo;

        public ConfigsAnalyzerHandler()
        {
            var types = typeof(CoherenceSyncConfigAnalyzer).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CoherenceSyncConfigAnalyzer))).ToList();

            foreach (var type in types)
            {
                analyzers.Add(Activator.CreateInstance(type) as CoherenceSyncConfigAnalyzer);
            }

            RefreshConfigsInfo();
        }

        public void RefreshConfigsInfo()
        {
            entriesInfo = CoherenceSyncConfigUtils.GetNetworkedObjectsInfo(out entriesDictionary);
        }

        public bool TryGetInfo(CoherenceSyncConfig config, out EntryInfo info)
        {
            return entriesDictionary.TryGetValue(config, out info);
        }

        public bool Validate(CoherenceSyncConfig config)
        {
            bool result = true;

            entriesDictionary.TryGetValue(config, out var info);

            foreach (var analyzer in analyzers)
            {
                result &= analyzer.ValidateConfig(config, info);
            }

            return result;
        }

        public string GetCompoundedErrorMessage(CoherenceSyncConfig config)
        {
            StringBuilder errorMessage = new StringBuilder();

            entriesDictionary.TryGetValue(config, out var info);

            var errors = new List<string>();

            foreach (var analyzer in analyzers)
            {
                var msg = analyzer.GetErrorMessage(config, info);

                if (string.IsNullOrEmpty(msg))
                {
                    continue;
                }

                errors.Add(msg);
            }

            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];

                errorMessage.AppendLine(error);

                if (i != errors.Count - 1)
                {
                    errorMessage.AppendLine(string.Empty);
                }
            }

            return errorMessage.ToString();
        }
    }
}
