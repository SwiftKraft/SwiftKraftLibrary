// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Entities;
    using Log;
    using System.Collections.Generic;

    // TODO: Merge with RSL counterpart
    internal class RefsResolver
    {
        private readonly Logger logger;
        public IReadOnlyList<Entity> ResolvableEntities => resolvableEntities;
        private readonly List<Entity> resolvableEntities = new List<Entity>(8);

        /// <summary>
        /// Set of entities which are referers in the current resolving instance
        /// </summary>
        private readonly HashSet<Entity> localKnownEntities = new HashSet<Entity>(8);

        /// <summary>
        /// Reversed graph of references in the current resolving instance
        /// </summary>
        private readonly Dictionary<Entity, List<Entity>> referencedEntities = new Dictionary<Entity, List<Entity>>(8);

        /// <summary>
        /// Set of unresolvable entities in the current resolving instance
        /// </summary>
        private readonly HashSet<Entity> unresolvableEntities = new HashSet<Entity>(8);

        public RefsResolver(Logger logger)
        {
            this.logger = logger.With<RefsResolver>();
        }

        public void Resolve(List<RefsInfo> info, IEntityRegistry knownEntities)
        {
            Clear();

            BuildLocalKnownEntities(info);
            BuildReferrersMapAndMarkDirectlyUnresolvable(info, knownEntities);
            MarkUnresolvableChains(knownEntities);
            MarkResolvable(info);
        }

        private void BuildLocalKnownEntities(List<RefsInfo> info)
        {
            foreach (var refsInfo in info)
            {
                localKnownEntities.Add(refsInfo.Referer);
            }
        }

        private void BuildReferrersMapAndMarkDirectlyUnresolvable(List<RefsInfo> info, IEntityRegistry knownEntities)
        {
            foreach (RefsInfo refsInfo in info)
            {
                for (var i = 0; i < refsInfo.ReferencedEntities.Count; i++)
                {
                    var referencedEntity = refsInfo.ReferencedEntities[i];
                    if (!referencedEntity.IsValid)
                    {
                        continue;
                    }

                    if (!referencedEntities.TryGetValue(referencedEntity, out List<Entity> referrers))
                    {
                        referrers = new List<Entity>(2);
                        referencedEntities.Add(referencedEntity, referrers);
                    }

                    referrers.Add(refsInfo.Referer);

                    if (IsDirectlyUnresolvable(refsInfo, knownEntities))
                    {
                        logger.Trace("Directly unresolvable entity", ("entity", refsInfo.Referer));
                        unresolvableEntities.Add(refsInfo.Referer);
                    }
                }
            }
        }

        private void MarkUnresolvableChains(IEntityRegistry knownEntities)
        {
            foreach (KeyValuePair<Entity, List<Entity>> kv in referencedEntities)
            {
                Entity referenced = kv.Key;
                List<Entity> referrers = kv.Value;

                if (IsEntityUnresolvable(referenced) && !knownEntities.EntityExists(referenced))
                {
                    foreach (Entity referrer in referrers)
                    {
                        MarkUnresolvable(referrer, knownEntities);
                    }
                }
            }
        }

        private void MarkResolvable(List<RefsInfo> info)
        {
            foreach (var refsInfo in info)
            {
                if (IsEntityUnresolvable(refsInfo.Referer))
                {
                    continue;
                }

                logger.Trace("Found resolvable entity", ("entity", refsInfo.Referer));
                resolvableEntities.Add(refsInfo.Referer);
            }
        }

        private bool IsDirectlyUnresolvable(RefsInfo refsInfo, IEntityRegistry knownEntities)
        {
            for (var i = 0; i < refsInfo.ReferencedEntities.Count; i++)
            {
                var referencedEntity = refsInfo.ReferencedEntities[i];
                if (!referencedEntity.IsValid)
                {
                    continue;
                }

                if (!knownEntities.EntityExists(referencedEntity) && !localKnownEntities.Contains(referencedEntity))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsEntityUnresolvable(in Entity entity)
        {
            return unresolvableEntities.Contains(entity);
        }

        private void MarkUnresolvable(in Entity entity, IEntityRegistry knownEntities)
        {
            if (IsEntityUnresolvable(entity))
            {
                return;
            }

            logger.Trace("Entity marked as unresolvable", ("entity", entity));
            unresolvableEntities.Add(entity);

            if (knownEntities.EntityExists(entity))
            {
                return;
            }

            if (referencedEntities.TryGetValue(entity, out List<Entity> referrers))
            {
                foreach (Entity referrer in referrers)
                {
                    MarkUnresolvable(referrer, knownEntities);
                }
            }
        }

        private void Clear()
        {
            localKnownEntities.Clear();
            referencedEntities.Clear();
            unresolvableEntities.Clear();
            resolvableEntities.Clear();
        }
    }
}
