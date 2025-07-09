// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using Entities;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Filters what entities are replicated, based on position.
    /// </summary>
    /// <remarks>
    /// This component is required for entities to get replicated.
    ///
    /// When the <see cref="GameObject"/> moves, the associated <see cref="CoherenceLiveQuery"/> moves accordingly.
    ///
    /// Using a live query is a great optimization if there's lots of entities,
    /// but only the few contained within the live query area are relevant.
    /// </remarks>
    [AddComponentMenu("coherence/Queries/Coherence Live Query")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceQuery)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/components/coherence-live-query")]
    public sealed class CoherenceLiveQuery : CoherenceQuery
    {
        internal static class Properties
        {
            public const string Extent = nameof(extent);
            public const string ExtentUpdateThreshold = nameof(extentUpdateThreshold);
            public const string DistanceUpdateThreshold = nameof(distanceUpdateThreshold);
        }

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceLiveQuery()
        {
        }

        [FormerlySerializedAs("radius")]
        [SerializeField]
        [Min(1)]
        private float extent;

        /// <summary>
        /// Defines the active area of the live query.
        /// </summary>
        /// <remarks>
        /// Half the length of the cube's edges.
        /// Set to 0 to consider all entities within the scene, independently of their position.
        /// </remarks>
        public float Extent
        {
            get => extent;
            set => extent = value;
        }

        /// <inheritdoc cref="Extent"/>
        [Deprecated("21/11/2024", 1, 4, 0)]
        [Obsolete("Use Extent instead.")]
        public float radius
        {
            get => Extent;
            set => Extent = value;
        }

        [SerializeField]
        [Tooltip("Difference in the magnitude of the extent at which to trigger an update on the live query. Only relevant when the area is constrained.")]
        [Min(0)]
        private float extentUpdateThreshold = .01f;

        /// <summary>
        /// Magnitude difference at which to trigger an update on the live query.
        /// </summary>
        /// <remarks>
        /// This can be useful if you're resizing the live query area gradually,
        /// and want to optimize the number of times the query needs updating.
        /// </remarks>
        public float ExtentUpdateThreshold
        {
            get => extentUpdateThreshold;
            set => extentUpdateThreshold = value;
        }

        [SerializeField]
        [Tooltip("Distance since last update at which an update on the live query is triggered.")]
        [Min(0)]
        private float distanceUpdateThreshold = .01f;

        /// <summary>
        /// Distance since last update at which an update on the live query is triggered.
        /// </summary>
        /// <remarks>
        /// This can be useful if you're moving the live query constantly,
        /// and want to optimize the number of times the query needs updating.
        /// </remarks>
        public float DistanceUpdateThreshold
        {
            get => distanceUpdateThreshold;
            set => distanceUpdateThreshold = value;
        }

        private Vector3 lastPosition;
        private float lastExtent;
        private Transform cachedTransform;
        private bool queryIsAdded;

        protected override void Reset()
        {
            base.Reset();

            extent = 0f;
            extentUpdateThreshold = .01f;
            distanceUpdateThreshold = .01f;
        }

        protected override void Awake()
        {
            cachedTransform = transform;
            lastPosition = cachedTransform.position;
            lastExtent = Extent;

            base.Awake();
        }

        private bool IsExtentPastThreshold => Mathf.Abs(extent - lastExtent) > extentUpdateThreshold;
        private bool IsDistancePastThreshold => (cachedTransform.position - lastPosition).sqrMagnitude > distanceUpdateThreshold * distanceUpdateThreshold;
        private bool IsPastAnyThreshold => IsDistancePastThreshold || IsExtentPastThreshold;
        private bool IsChangingMode => extent <= 0 && lastExtent > 0 || extent > 0 && lastExtent <= 0;

        protected override bool NeedsUpdate => IsChangingMode || IsPastAnyThreshold;

        protected override void UpdateQuery(bool queryActive = true)
        {
            if (queryActive)
            {
                var position = cachedTransform.position;

                Impl.UpdateLiveQuery(Client, EntityID, Extent, position, bridge.NetworkTime.ClientSimulationFrame);
                queryIsAdded = true;

                lastPosition = position;
                lastExtent = Extent;
            }
            else
            {
                if (EntityID == Entity.InvalidRelative || !queryIsAdded)
                {
                    return;
                }

                Impl.RemoveLiveQuery(Client, EntityID);
                queryIsAdded = false;

                lastExtent = -1;
            }
        }
    }
}
