// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEngine;

    public class ObjectReferenceAnalyzer : CoherenceSyncConfigAnalyzer
    {
        public override bool ValidateConfig(CoherenceSyncConfig config, EntryInfo info)
        {
            if (config == null || config.EditorTarget == null)
            {
                return true;
            }

            ICoherenceSync sync = null;

            if (config.EditorTarget is GameObject go)
            {
                sync = go.GetComponent<ICoherenceSync>();
            }
            else
            {
                sync = config.EditorTarget as ICoherenceSync;
            }

            return sync != null && sync.CoherenceSyncConfig == config;
        }

        protected override string GetErrorMessageInternal(CoherenceSyncConfig config, EntryInfo info)
        {
            return $"{config.name} has no CoherenceSync component or is not referenced in the CoherenceSync owner. Check the inspector and target Object for more information.";
        }
    }
}
