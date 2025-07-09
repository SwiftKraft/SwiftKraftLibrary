// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public class NetworkComponentsAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            return info.NetworkComponents <= BakeUtil.MaxUniqueComponentsBound;
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            return $"{config.name} will create {info.NetworkComponents} Network Components at runtime, this is limited to {BakeUtil.MaxUniqueComponentsBound}. " +
                   "Check the CoherenceSync inspector and Configure window for more details.";
        }
    }
}
