// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public class InvalidBindingsAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            return info.InvalidBindings == 0;
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            return $"{config.name} has {info.InvalidBindings} invalid bindings.";
        }
    }
}
