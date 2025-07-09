namespace Coherence.Toolkit
{
    using Coherence.Common;
    using Coherence.Log;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class FloatingOriginManager
    {
        /// <summary>
        /// Maximum abs position of an entity/liveQuery/floatingOrigin
        /// </summary>
        public const float WorldPositionMaxRange = float.MaxValue / 2;

        /// <summary>
        /// Value at which 64-bit precision loses sub-1mm precision
        /// </summary>
        public const double FloatingOriginPreciseRange = 1E13;

        /// <summary>
        /// Invoked at the end of the floating point origin shifting operation started
        /// via <see cref="SetFloatingOrigin"/> or <see cref="TranslateFloatingOrigin"/>,
        /// after all objects have been shifted..
        /// </summary>
        public Action<FloatingOriginShiftArgs> OnFloatingOriginShifted;

        /// <summary>
        /// Invoked after <see cref="OnFloatingOriginShifted"/>
        /// </summary>
        public Action<FloatingOriginShiftArgs> OnAfterFloatingOriginShifted;

        private readonly IClient client;
        private readonly IEntitiesManager entitiesManager;
        private readonly Coherence.Log.Logger logger;

        internal FloatingOriginManager(IClient client, IEntitiesManager entitiesManager, Coherence.Log.Logger logger)
        {
            this.client = client;
            this.entitiesManager = entitiesManager;
            this.logger = logger.With<FloatingOriginManager>();
        }

        /// <summary>
        /// Sets the floating point origin absolute position to a new value.
        /// Only works if bridge <see cref="isConnected"/>, otherwise nothing happens.
        /// </summary>
        /// <param name="newOrigin">New floating point origin absolute position</param>
        /// <returns>Returns true if the floating origin was shifted, false if bridge was not connected.</returns>
        public bool SetFloatingOrigin(Vector3d newOrigin)
        {
            if (!client.IsConnected())
            {
                return false;
            }

            if (!newOrigin.IsWithinRange(FloatingOriginPreciseRange))
            {
                logger.Warning(Warning.ToolkitFloatingOriginOutOfRange);
            }

            if (!newOrigin.IsWithinRange(WorldPositionMaxRange))
            {
                throw new ArgumentOutOfRangeException($"Floating origin position is outside of the supported range " +
                    $"[{-WorldPositionMaxRange}, {WorldPositionMaxRange}].");
            }

            var oldOrigin = client.GetFloatingOrigin();

            var delta = newOrigin - oldOrigin;

            client.SetFloatingOrigin(newOrigin);

            ShiftNetworkedObjectPositions(delta);

            OnFloatingOriginShifted?.Invoke(new FloatingOriginShiftArgs(oldOrigin, newOrigin));

            OnAfterFloatingOriginShifted?.Invoke(new FloatingOriginShiftArgs(oldOrigin, newOrigin));

            return true;
        }

        /// <summary>
        /// Returns the current absolute position of the floating point origin.
        /// </summary>
        public Vector3d GetFloatingOrigin()
        {
            return client.GetFloatingOrigin();
        }

        /// <summary>
        /// Moves the floating point origin by a <paramref name="translation"/> vector.
        /// Only works if bridge <see cref="isConnected"/>, otherwise nothing happens.
        /// </summary>
        /// <returns>Returns true if the floating origin was shifted, false if bridge was not connected.</returns>
        public bool TranslateFloatingOrigin(Vector3d translation)
        {
            if (!client.IsConnected())
            {
                return false;
            }

            var absolute = client.GetFloatingOrigin() + translation;
            SetFloatingOrigin(absolute);

            return true;
        }

        /// <inheritdoc cref="TranslateFloatingOrigin(Vector3d)"/>
        public bool TranslateFloatingOrigin(Vector3 translation)
        {
            return TranslateFloatingOrigin(translation.ToVector3d());
        }

        private void ShiftNetworkedObjectPositions(Vector3d delta)
        {
            var shiftedEntities = new List<NetworkEntityState>();

            var networkedEntities = new List<NetworkEntityState>(entitiesManager.NetworkEntities);
            foreach (var networkedEntity in networkedEntities)
            {
                // Some entities are not shifted, depending on parenting and configuration
                if (!networkedEntity.Sync.ShouldShift())
                {
                    // Flush position, even though relative position hasn't changed
                    // so the RS can calculate new absolute position
                    // (this could be optimized by skipping it here and doing it manually on RS)
                    if (!networkedEntity.Sync.HasParentWithCoherenceSync && networkedEntity.HasStateAuthority)
                    {
                        networkedEntity.Sync.Updater.TryFlushPosition(true);
                    }

                    continue;
                }

                // Shift
                if (networkedEntity.Sync.ShiftOrigin(delta))
                {
                    // Only add entities that were not destroyed
                    shiftedEntities.Add(networkedEntity);
                }
            }

            // Invoke callbacks after all entities have been shifted, to prevent race conditions
            foreach (var entity in shiftedEntities)
            {
                var oldPosition = entity.Sync.coherencePosition - delta.ToUnityVector3();
                var newPosition = entity.Sync.coherencePosition;
                entity.Sync.OnFloatingOriginShifted?.Invoke(oldPosition, newPosition);
            }
        }
    }

    public struct FloatingOriginShiftArgs
    {
        public Vector3d OldOrigin;
        public Vector3d NewOrigin;
        public Vector3d Delta => NewOrigin - OldOrigin;

        public FloatingOriginShiftArgs(Vector3d oldOrigin, Vector3d newOrigin)
        {
            OldOrigin = oldOrigin;
            NewOrigin = newOrigin;
        }
    }
}
