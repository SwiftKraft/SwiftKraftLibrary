// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Log;
    using UnityEngine;
    using Entities;

    /// <summary>
    /// Filters what entities are replicated to the client based on tags.
    /// </summary>
    /// <remarks>
    /// This query does not take position of entities into account,
    /// use <see cref="Coherence.Toolkit.CoherenceLiveQuery"/> for that.
    /// </remarks>
    [AddComponentMenu("coherence/Queries/Coherence Tag Query")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceQuery)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/components/coherence-tag-query")]
    public sealed class CoherenceTagQuery : CoherenceQuery
    {
        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceTagQuery()
        {
        }

        /// <summary>
        /// The string to use as the filter.
        /// </summary>
        /// <remarks>
        /// Only entities that use this tag will be received.
        ///
        /// Not associated with GameObject's <see cref="UnityEngine.GameObject.tag"/>.
        /// </remarks>
        [CoherenceTag]
        public string coherenceTag;

        private string lastTag;
        private bool tagIsSet;

        protected override bool NeedsUpdate => coherenceTag != lastTag;

        protected override void UpdateQuery(bool queryActive = true)
        {
            var missingTag = string.IsNullOrEmpty(coherenceTag);

            if (missingTag && queryActive)
            {
                Logger.Warning(Warning.ToolkitTagQueryMissingTag,
                    ("object", name));
            }

            if (missingTag || !queryActive)
            {
                if (EntityID != Entity.InvalidRelative && tagIsSet)
                {
                    Impl.RemoveTagQuery(Client, EntityID);
                    tagIsSet = false;
                }
            }
            else
            {
                Impl.UpdateTagQuery(Client, EntityID, coherenceTag, bridge.NetworkTime.ClientSimulationFrame);
                tagIsSet = true;
            }

            lastTag = coherenceTag;
        }
    }
}
