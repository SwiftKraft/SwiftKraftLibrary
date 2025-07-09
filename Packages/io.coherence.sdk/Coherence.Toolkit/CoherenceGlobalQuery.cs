// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;
    using Entities;

    /// <summary>
    /// Filters what entities are replicated to the client based being global.
    /// </summary>
    /// <remarks>
    /// This query does not take position of entities into account,
    /// use <see cref="Coherence.Toolkit.CoherenceLiveQuery"/> for that.
    /// </remarks>
    [AddComponentMenu("coherence/Queries/Coherence Global Query")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceQuery)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/components/coherenceglobalquery")]
    public sealed class CoherenceGlobalQuery : CoherenceQuery
    {
        private bool queryIsAdded;

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceGlobalQuery()
        {
        }

        protected override bool NeedsUpdate => false;

        protected override void UpdateQuery(bool queryActive = true)
        {
            if (queryActive)
            {
                Impl.AddGlobalQuery(Client, EntityID);
                queryIsAdded = true;
            }
            else
            {
                if (EntityID == Entity.InvalidRelative || !queryIsAdded)
                {
                    return;
                }

                Impl.RemoveGlobalQuery(Client, EntityID);
                queryIsAdded = false;
            }
        }
    }
}
