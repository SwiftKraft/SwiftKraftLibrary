// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.PlayerLoop
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.LowLevel;
    using UnityEngine.PlayerLoop;

    internal static class CoherenceLoop
    {
        internal class BridgeList<T> where T : ICoherenceBridge
        {
            private readonly List<T> bridges = new();
            private readonly List<T> toAdd = new();
            private readonly List<T> toRemove = new();

            public void QueueAdd(T bridge)
            {
                if (bridges.Contains(bridge))
                {
                    return;
                }

                toAdd.Add(bridge);
                toRemove.Remove(bridge);
            }

            public void QueueRemove(T bridge)
            {
                toRemove.Add(bridge);
                toAdd.Remove(bridge);
            }

            public void Clear()
            {
                bridges.Clear();
            }

            public IReadOnlyList<T> Resolve()
            {
                foreach (var bridge in toRemove)
                {
                    bridges.Remove(bridge);
                }

                foreach (var bridge in toAdd)
                {
                    if (bridges.Contains(bridge))
                    {
                        continue;
                    }

                    bridges.Add(bridge);
                }

                toAdd.Clear();
                toRemove.Clear();

                return bridges.AsReadOnly();
            }
        }

        private static readonly BridgeList<CoherenceBridge> Bridges = new();

        /// <summary>
        /// <see cref="PlayerLoopSystem.UpdateFunction"/> delegates created once, at static time, to avoid per-frame allocations.
        /// </summary>
        private static class UpdateFunctions
        {
            public static readonly PlayerLoopSystem.UpdateFunction ReceiveFromNetwork = CoherenceReceiver.ReceiveFromNetwork;
            public static readonly PlayerLoopSystem.UpdateFunction InterpolateUpdate = CoherenceInterpolation.InterpolateUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction InterpolateFixedUpdate = CoherenceInterpolation.InterpolateFixedUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction InterpolateLateUpdate = CoherenceInterpolation.InterpolateLateUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction SampleUpdate = CoherenceSampler.SampleUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction SampleFixedUpdate = CoherenceSampler.SampleFixedUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction SampleLateUpdate = CoherenceSampler.SampleLateUpdate;
            public static readonly PlayerLoopSystem.UpdateFunction SyncAndSend = CoherenceSender.SyncAndSend;
        }

        [RuntimeInitializeOnLoadMethod]
        public static void Inject()
        {
            Bridges.Clear();

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (playerLoop.subSystemList == null)
            {
                playerLoop = PlayerLoop.GetDefaultPlayerLoop();
            }

            // Receive
            InsertBeforeCallback(playerLoop.subSystemList,
                typeof(Update), typeof(Update.ScriptRunBehaviourUpdate),
                typeof(CoherenceReceiver), UpdateFunctions.ReceiveFromNetwork);
            InsertBeforeCallback(playerLoop.subSystemList,
                typeof(FixedUpdate), typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate),
                typeof(CoherenceReceiver), UpdateFunctions.ReceiveFromNetwork);

            // Interpolate
            InsertBeforeCallback(playerLoop.subSystemList,
                typeof(Update), typeof(Update.ScriptRunBehaviourUpdate),
                typeof(CoherenceInterpolation), UpdateFunctions.InterpolateUpdate);
            InsertBeforeCallback(playerLoop.subSystemList,
                typeof(FixedUpdate), typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate),
                typeof(CoherenceInterpolation), UpdateFunctions.InterpolateFixedUpdate);
            InsertBeforeCallback(playerLoop.subSystemList,
                typeof(PreLateUpdate), typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate),
                typeof(CoherenceInterpolation), UpdateFunctions.InterpolateLateUpdate);

            // Sample
            InsertAfterCallback(playerLoop.subSystemList,
                typeof(Update), typeof(Update.ScriptRunDelayedTasks), // after coroutines
                typeof(CoherenceSampler), UpdateFunctions.SampleUpdate);
            InsertAfterCallback(playerLoop.subSystemList,
                typeof(FixedUpdate), typeof(FixedUpdate.ScriptRunDelayedFixedFrameRate), // after collision, triggers and coroutines
                typeof(CoherenceSampler), UpdateFunctions.SampleFixedUpdate);
            InsertAfterCallback(playerLoop.subSystemList,
                typeof(PreLateUpdate), typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate),
                typeof(CoherenceSampler), UpdateFunctions.SampleLateUpdate);

            // Send
            InsertAfterCallback(playerLoop.subSystemList,
                typeof(PreLateUpdate), typeof(CoherenceSampler),
                typeof(CoherenceSender), UpdateFunctions.SyncAndSend);

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        private static void InsertBeforeCallback(PlayerLoopSystem[] systems, Type loopType, Type stepType, Type callbackType, PlayerLoopSystem.UpdateFunction callback)
        {
            var index = Array.FindIndex(systems, system => system.type == loopType);
            if (index == -1)
            {
                throw new ArgumentException($"PlayerLoop does not contain {loopType}");
            }

            ref var steps = ref systems[index].subSystemList;
            var newSteps = new PlayerLoopSystem[steps.Length + 1];
            var stepIndex = Array.FindIndex(steps, step => step.type == stepType);
            if (stepIndex == -1)
            {
                throw new ArgumentException($"PlayerLoop does not contain {loopType}");
            }

            // Duplicate check
            if (Array.FindIndex(steps, step => step.type == callbackType) != -1)
            {
                return;
            }

            var newStep = new PlayerLoopSystem
            {
                type = callbackType,
                updateDelegate = callback,
            };

            // Copy all steps until the step we're interested in, add our step, copy the rest
            Array.Copy(steps, 0, newSteps, 0, stepIndex);
            newSteps[stepIndex] = newStep;
            Array.Copy(steps, stepIndex, newSteps, stepIndex + 1, steps.Length - stepIndex);

            systems[index].subSystemList = newSteps;
        }

        private static void InsertAfterCallback(PlayerLoopSystem[] systems, Type loopType, Type stepType, Type callbackType, PlayerLoopSystem.UpdateFunction callback)
        {
            var index = Array.FindIndex(systems, system => system.type == loopType);
            if (index == -1)
            {
                throw new ArgumentException($"PlayerLoop does not contain {loopType}");
            }

            ref var steps = ref systems[index].subSystemList;
            var newSteps = new PlayerLoopSystem[steps.Length + 1];
            var stepIndex = Array.FindIndex(steps, step => step.type == stepType);
            if (stepIndex == -1)
            {
                throw new ArgumentException($"PlayerLoop does not contain {loopType}");
            }

            // Duplicate check
            if (Array.FindIndex(steps, step => step.type == callbackType) != -1)
            {
                return;
            }

            var newStep = new PlayerLoopSystem
            {
                type = callbackType,
                updateDelegate = callback,
            };

            // Copy all steps until the step we're interested in, add our step, copy the rest
            Array.Copy(steps, 0, newSteps, 0, stepIndex + 1);
            newSteps[stepIndex + 1] = newStep;
            Array.Copy(steps, stepIndex + 1, newSteps, stepIndex + 2, steps.Length - (stepIndex + 1));

            systems[index].subSystemList = newSteps;
        }

        public static void AddBridge(CoherenceBridge bridge)
        {
            Bridges.QueueAdd(bridge);
        }

        public static void RemoveBridge(CoherenceBridge bridge)
        {
            Bridges.QueueRemove(bridge);
        }

        internal static class CoherenceSender
        {
            internal static void SyncAndSend()
            {
                CoherenceBridge activeBridge = null;
                var bridges = Bridges.Resolve();

                try
                {
                    foreach (var bridge in bridges)
                    {
                        activeBridge = bridge;

                        bridge.SyncAndSend();
                    }
                }
                catch (Core.SerializerException e)
                {
                    var go = activeBridge?.EntitiesManager.EntityIdToGameObject(e.EntityId);
                    activeBridge?.Logger.Error(Log.Error.ToolkitPlayerLoopSendSerializerException, e.ToStringEx(go?.name));
                }
                catch (Exception e)
                {
                    activeBridge?.Logger.Error(Log.Error.ToolkitPlayerLoopSendException, e.ToString());
                }
            }
        }

        internal static class CoherenceInterpolation
        {
            internal static void InterpolateUpdate() => Interpolate(CoherenceSync.InterpolationLoop.Update);
            internal static void InterpolateFixedUpdate() => Interpolate(CoherenceSync.InterpolationLoop.FixedUpdate);
            internal static void InterpolateLateUpdate() => Interpolate(CoherenceSync.InterpolationLoop.LateUpdate);

            private static void Interpolate(CoherenceSync.InterpolationLoop interpolationLoop)
            {
                CoherenceBridge activeBridge = null;
                var bridges = Bridges.Resolve();

                try
                {
                    foreach (var bridge in bridges)
                    {
                        activeBridge = bridge;

                        bridge.Interpolate(interpolationLoop);
                        bridge.InvokeCallbacks(interpolationLoop);
                    }
                }
                catch (Exception e)
                {
                    activeBridge?.Logger.Error(Log.Error.ToolKitPlayerLoopInterpolationException, e.ToString());
                }
            }
        }

        internal static class CoherenceSampler
        {
            internal static void SampleUpdate() => Sample(CoherenceSync.InterpolationLoop.Update);
            internal static void SampleFixedUpdate() => Sample(CoherenceSync.InterpolationLoop.FixedUpdate);
            internal static void SampleLateUpdate() => Sample(CoherenceSync.InterpolationLoop.LateUpdate);

            private static void Sample(CoherenceSync.InterpolationLoop loop)
            {
                CoherenceBridge activeBridge = null;
                var bridges = Bridges.Resolve();

                try
                {
                    foreach (var bridge in bridges)
                    {
                        activeBridge = bridge;

                        bridge.Sample(loop);
                    }
                }
                catch (Exception e)
                {
                    activeBridge?.Logger.Error(Log.Error.ToolkitPlayerLoopSamplerException, e.ToString());
                }
            }
        }

        internal static class CoherenceReceiver
        {
            internal static void ReceiveFromNetwork()
            {
                CoherenceBridge activeBridge = null;
                var bridges = Bridges.Resolve();

                try
                {
                    foreach (var bridge in bridges)
                    {
                        activeBridge = bridge;

                        bridge.ReceiveFromNetwork();
                    }
                }
                catch (Exception e)
                {
                    activeBridge?.Logger.Error(Log.Error.ToolkitPlayerLoopReceiveException, e.ToString());
                }
            }
        }
    }
}
