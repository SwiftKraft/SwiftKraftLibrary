// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public class NullObjectAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            return config != null && config.EditorTarget != null;
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            if (config)
            {
                return $"{config.name} is null or its missing a Unity asset.";
            }
            else
            {
                return $"A {nameof(CoherenceSyncConfig)} is null or its missing a Unity asset.";
            }
        }
    }
}
