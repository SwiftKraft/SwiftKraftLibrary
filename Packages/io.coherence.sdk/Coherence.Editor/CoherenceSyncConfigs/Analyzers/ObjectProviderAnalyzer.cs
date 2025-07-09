// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;

    public class ObjectProviderAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            return config && config.Provider != null && config.Provider.Validate(config);
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            var isNullProvider = config && config.Provider == null;

            var name = config ? config.name : $"A {nameof(CoherenceSyncConfig)}";
            var provider = config && config.Provider != null ? config.Provider.GetType().Name : "its provider";

            var error = isNullProvider
                ? $"{name} has null Object Provider. Check the inspector for more information."
                : $"{name} has problems in the serialized data of {provider}. Check the inspector for more information.";

            return error;
        }
    }
}
