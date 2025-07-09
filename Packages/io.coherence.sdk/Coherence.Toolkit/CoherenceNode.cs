// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using Log;
    using Logger = Log.Logger;
    using Bindings;
    using System.Linq;
    using Serializer;

    /// <summary>
    /// Component to synchronize hierarchy changes between two entities on the same hierarchy.
    /// </summary>
    /// <remarks>
    /// Instantiation of networked entities happens on the root of the scene by default
    /// (using <see cref="DefaultInstantiator"/>, which instantiates through <see cref="UnityEngine.Object.Instantiate{T}(T,Vector3,Quaternion)"/>).
    /// This might create scenarios where an entity exists within a hierarchy on the client that created it,
    /// but when replicated on other clients, it is placed outside of that hierarchy.
    ///
    /// This component allows to parent the entity accordingly, based on the sibling indexes (see <see cref="Transform.GetSiblingIndex"/>) of the entity and its parents.
    /// There is no resolution based on <see cref="UnityEngine.Object.name"/>.
    /// If hierarchies differ between clients, hierarchy changes might not be applied correctly.
    ///
    /// The client needs to have authority over the children to be able to set its parent.
    /// Both objects involved (parent and child) need to be different network entities i.e., use <see cref="CoherenceSync"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceNode)]
    [RequireComponent(typeof(CoherenceSync))]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/components/coherence-node")]
    public class CoherenceNode : MonoBehaviour
    {
        static readonly char[] commaSeparator = { ',' };

        internal IConnectedEntityDriver sync { get; set; }

        internal Logger logger { get; set; }

        /// <summary>
        /// A comma-separated string of <see cref="Transform"/> sibling indexes.
        /// </summary>
        /// <remarks>
        /// Holds the sibling indexes necessary to determine the parent to attach to, relative to the closest outer <see cref="CoherenceSync"/>
        /// found within the hierarchy.
        ///
        /// For example:
        /// Given `A` (this <see cref="CoherenceSync"/>, which uses <see cref="CoherenceNode"/>) and `B` (another <see cref="CoherenceSync"/> that `A` will be parented into),
        /// a <see cref="path"/> of `1,0` would parent `A` under `right-hand` on the following transform hierarchy:
        ///
        /// ```text
        /// (*) B (CoherenceSync entity)
        ///     (0) left-arm
        ///         (0) left-hand
        ///     (1) right-arm
        ///         (0) right-hand
        ///             A (CoherenceSync entity + CoherenceNode)
        /// ```
        /// </remarks>
        /// <seealso cref="Transform.GetSiblingIndex"/>
        [Sync]
        public string path;

        /// <summary>
        /// Used to keep track of whether <cref>path</cref> has been applied.
        /// </summary>
        [Sync]
        public int pathDirtyCounter;

        private ValueBinding<string> pathBinding;
        private ValueBinding<string> PathBinding => pathBinding ??= (sync as ICoherenceSync)?.Bindings
            .First(b => b.UnityComponent == this && b.Name == nameof(path)) as ValueBinding<string>;

        private ValueBinding<int> pathDirtyCounterBinding;
        private ValueBinding<int> PathDirtyCounterBinding => pathDirtyCounterBinding ??= (sync as ICoherenceSync)?.Bindings
            .First(b => b.UnityComponent == this && b.Name == nameof(pathDirtyCounter)) as ValueBinding<int>;

        private int lastAppliedPathDirtyCounter;

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceNode()
        {
        }

        private void Awake()
        {
            logger ??= Log.GetLogger<CoherenceNode>();

            sync ??= GetComponent<CoherenceSync>();

            if (sync != null)
            {
                sync.DidSendConnectedEntity += DidSendConnectedEntity;
            }
        }

        private void Start()
        {
            if (!sync.HasStateAuthority)
            {
                UpdateHierarchy();
            }
        }

        private void OnDisable()
        {
            lastAppliedPathDirtyCounter = 0;
            pathDirtyCounter = 0;
            path = string.Empty;
        }

        private void OnDestroy() => logger?.Dispose();

        private void DidSendConnectedEntity(CoherenceSync newConnectedEntity)
        {
            if (!sync.HasStateAuthority)
            {
                logger.Warning(Warning.ToolkitNodeNotLocallySimulated,
                        $"The method '{nameof(DidSendConnectedEntity)}' should only be called on a locally simulated entity.");
                return;
            }

            path = CalculatePath(newConnectedEntity.SelfOrNull()?.transform, transform);

            if (path.Length > OutProtocolBitStream.SHORT_STRING_MAX_SIZE)
            {
                logger.Warning(Warning.ToolkitNodePathTooLong,
                    ("gameObject", gameObject.name), ("maxPathSize", OutProtocolBitStream.SHORT_STRING_MAX_SIZE));

                path = string.Empty;
            }

            pathDirtyCounter++;
        }

        internal void UpdateHierarchy()
        {
            if (!sync.HasStateAuthority &&
                pathDirtyCounter != lastAppliedPathDirtyCounter)
            {
                PlaceInHierarchy(sync.ConnectedEntity.SelfOrNull()?.transform, path);
            }
        }

        private void PlaceInHierarchy(Transform start, string aPath)
        {
            if (start != null)
            {
                var newParent = ChildAtPath(logger, start, aPath);
                sync.SetParent(newParent);
            }
            else
            {
                sync.SetParent(null);
            }

            lastAppliedPathDirtyCounter = pathDirtyCounter;
        }

        internal void ApplyBindings()
        {
            PathBinding.SetToLastSample();
            PathDirtyCounterBinding.SetToLastSample();
        }

        internal void MakeBindingsReadyToSend()
        {
            PathBinding.MarkAsReadyToSend();
            PathDirtyCounterBinding.MarkAsReadyToSend();
        }

        #region Static Helpers
        private static Transform ChildAtPath(Logger logger, Transform startingTransform, string path)
        {
            if (startingTransform == null)
            {
                return null;
            }

            var child = startingTransform;
            var childIndexes = IndexPathFromString(logger, path);

            foreach (int index in childIndexes)
            {
                if (index < 0 || index >= child.childCount)
                {
                    logger.Warning(Warning.ToolkitNodeCantFindChildAtPath,
                        $"We can't find child at index {index} of '{child}', because it has only {child.childCount} children.");

                    return startingTransform;
                }

                child = child.GetChild(index);
            }

            return child;
        }

        private static int[] IndexPathFromString(Logger logger, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<int>();
            }

            var sections = path.Split(commaSeparator);
            var indexes = new int[sections.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                var reverse = indexes.Length - 1 - i;

                if (int.TryParse(sections[reverse], out int index))
                {
                    indexes[i] = index;
                }
                else
                {
                    logger.Warning(Warning.ToolkitNodeFailedToParseSection,
                        $"We failed to parse '{sections[reverse]}' as an int.");

                    return new int[] { };
                }
            }

            return indexes;
        }

        private static string CalculatePath(Transform fromParent, Transform toChild)
        {
            if (fromParent == null)
            {
                return "";
            }

            var path = new List<string>();
            var at = toChild.parent;

            while (at != null && at != fromParent)
            {
                path.Add(at.GetSiblingIndex().ToString());
                at = at.parent;
            }

            return string.Join(new string(commaSeparator), path);
        }
        #endregion
    }
}
