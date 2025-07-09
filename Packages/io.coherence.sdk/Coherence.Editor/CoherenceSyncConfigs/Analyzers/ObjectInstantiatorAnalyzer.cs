// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public class ObjectInstantiatorAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            return config != null && config.Instantiator != null;
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            if (config)
            {
                return $"{config.name} has null Object Instantiator. Check the inspector for more information.";
            }
            else
            {
                return $"A {nameof(CoherenceSyncConfig)} has null Object Instantiator. Check the inspector for more information.";
            }
        }
    }
}
