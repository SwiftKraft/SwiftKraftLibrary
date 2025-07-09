// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public abstract class CoherenceSyncConfigAnalyzer
    {
        public string GetErrorMessage(CoherenceSyncConfig config, EntryInfo info)
        {
            return !ValidateConfig(config, info) ? GetErrorMessageInternal(config, info) : string.Empty;
        }

        public abstract bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info);
        protected abstract string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info);
    }
}

