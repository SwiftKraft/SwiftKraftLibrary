// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System.Collections.Generic;
    using Cloud;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Log;
    using Logger = Log.Logger;

    /// <summary>
    /// Given a component on a <see cref="CoherenceSync"/>, resolve a specific <see cref="CoherenceBridge" />.
    /// </summary>
    public delegate CoherenceBridge CoherenceBridgeResolver<in T>(T resolvingComponent) where T : MonoBehaviour;

    /// <summary>
    /// Registry of <see cref="CoherenceBridge"/> instances.
    /// </summary>
    public static class CoherenceBridgeStore
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBridgeStore()
        {
            masterBridge = null;
            bridges.Clear();
        }

        internal static readonly Dictionary<int, CoherenceBridge> bridges = new();

        internal static ICoherenceBridge instantiatingBridge;

        /// <summary>
        /// Overrides what <see cref="CoherenceBridge"/> to resolve to, given a <see cref="MonoBehaviour"/>
        /// </summary>
        /// <remarks>
        /// The order of resolution is as follows:
        /// <see cref="CoherenceBridge"/> provided by the per-instance <see cref="CoherenceBridgeResolver{T}"/> callback.
        /// <see cref="CoherenceBridge"/> provided by the global <see cref="CoherenceBridgeStore.BridgeResolve"/> callback.
        /// <see cref="CoherenceBridge"/> within the same scene as this entity.
        /// <see cref="CoherenceBridge"/> with a <see cref="CoherenceBridge.IsMain"/> checked.
        /// Otherwise, a new instance is created.
        /// </remarks>
        public static event CoherenceBridgeResolver<MonoBehaviour> BridgeResolve
        {
            add
            {
                if (bridgeResolve == null)
                {
                    bridgeResolve += value;
                }
                else
                {
                    logger.Error(Error.ToolkitBridgeStoreBridgeResolveTooManyCallbacks, $"{nameof(BridgeResolve)} can have only one callback");
                }
            }

            remove => bridgeResolve -= value;
        }

        private static CoherenceBridge masterBridge;

        private static event CoherenceBridgeResolver<MonoBehaviour> bridgeResolve;
        private static readonly Logger logger = Log.GetLogger(typeof(CoherenceBridgeStore));

        /// <summary>
        /// Holds a reference to the first <see cref="CoherenceBridge"/> registered as a main bridge.
        /// </summary>
        /// <seealso cref="CoherenceBridge.IsMain"/>
        public static CoherenceBridge MasterBridge => masterBridge;

        internal static void RegisterBridge(CoherenceBridge bridge, int id, bool isMaster)
        {
            if (isMaster && !masterBridge)
            {
                masterBridge = bridge;
                return;
            }

            bridges[id] = bridge;
        }

        internal static void RegisterBridge(CoherenceBridge bridge, Scene scene, bool isMaster) => RegisterBridge(bridge, scene.handle, isMaster);

        internal static void DeregisterBridge(int id) => _ = bridges.Remove(id);

        internal static void DeregisterBridge(CoherenceBridge bridge)
        {
            var remove = new List<int>(1);

            foreach (var pair in bridges)
            {
                if (pair.Value == bridge)
                {
                    remove.Add(pair.Key);
                }
            }

            foreach (var id in remove)
            {
                bridges.Remove(id);
            }
        }

        /// <summary>
        /// Get a reference to a registered <see cref="CoherenceBridge"/>.
        /// </summary>
        /// <param name="sceneHandle">The handle of the scene associated with the bridge.</param>
        /// <param name="bridge">The bridge reference.</param>
        /// <returns>
        /// <see langword="true"/> if the registered reference could be fetched.
        /// <see langword="false"/> otherwise.
        /// </returns>
        /// <seealso cref="Scene.handle"/>
        public static bool TryGetBridge(int sceneHandle, out CoherenceBridge bridge)
        {
            if (instantiatingBridge != null)
            {
                bridge = (CoherenceBridge)instantiatingBridge;

                return true;
            }

            if (!bridges.TryGetValue(sceneHandle, out bridge))
            {
                bridge = masterBridge;
            }

            return bridge;
        }

        /// <summary>
        /// Get a reference to a <see cref="CoherenceBridge"/> connected to coherence Cloud
        /// using a particular <paramref name="playerAccount"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a <see cref="CoherenceBridge"/>'s 'Player Account' is set to 'Auto Login As Guest',
        /// it will be connected with the logged-in guest player account.
        /// </para>
        /// <para>
        /// When a <see cref="CoherenceBridge"/>'s 'Player Account' is set to 'Main',
        /// it will be connected with <see cref="PlayerAccount.Main">main player account</see>
        /// after an operation to log into <see cref="CoherenceCloud">coherence Cloud</see>
        /// has been initiated anywhere in the codebase.
        /// </para>
        /// <para>
        /// When a <see cref="CoherenceBridge"/>'s 'Player Account' is set to 'None', it can be
        /// connected with a <see cref="PlayerAccount"/> by assigning its <see cref="PlayerAccount.Services">cloud services</see>
        /// to the <see cref="CoherenceBridge.CloudService">CoherenceBridge.CloudService</see> property.
        /// </para>
        /// </remarks>
        /// <param name="playerAccount">
        /// The <see cref="PlayerAccount"/> whose <see cref="PlayerAccount.Services">cloud services</see> the bridge is <see cref="CoherenceBridge.CloudService">using</see>.
        /// </param>
        /// <param name="bridge">
        /// When this method returns, contains the bridge that is using the cloud services of the <paramref name="playerAccount"/>,
        /// if any was found; otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a bridge that is connected with the player account was found; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetBridge(PlayerAccount playerAccount, out CoherenceBridge bridge)
        {
            var cloudService = playerAccount.Services;
            foreach (var someBridge in bridges.Values)
            {
                if (someBridge.CloudServiceEquals(cloudService))
                {
                    bridge = someBridge;
                    return true;
                }
            }

            bridge = null;
            return false;
        }

        /// <summary>
        /// Get a reference to a registered <see cref="CoherenceBridge"/>.
        /// </summary>
        /// <param name="scene">The scene associated with the bridge.</param>
        /// <param name="bridge">The bridge reference.</param>
        /// <returns>
        /// <see langword="true"/> if the registered reference could be fetched.
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetBridge(Scene scene, out CoherenceBridge bridge)
        {
            if (instantiatingBridge != null)
            {
                bridge = (CoherenceBridge)instantiatingBridge;

                return true;
            }

            if (!bridges.TryGetValue(scene.handle, out bridge))
            {
                bridge = masterBridge;
            }

            return bridge;
        }

        /// <summary>
        /// Get a reference to a registered <see cref="CoherenceBridge"/>.
        /// </summary>
        /// <param name="scene">The scene associated with the bridge.</param>
        /// <param name="resolver">The resolver to use. Use <see langword="null"/> to skip.</param>
        /// <param name="component">The component to send through the <see cref="resolver"/>.</param>
        /// <param name="bridge">The bridge reference.</param>
        /// <returns>
        /// <see langword="true"/> if the registered reference could be fetched.
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetBridge<T>(Scene scene, CoherenceBridgeResolver<T> resolver, T component, out CoherenceBridge bridge) where T : MonoBehaviour
        {
            bridge = resolver?.Invoke(component);
            if (bridge)
            {
                return bridge;
            }

            bridge = bridgeResolve?.Invoke(component);
            if (bridge)
            {
                return bridge;
            }

            if (instantiatingBridge != null)
            {
                bridge = (CoherenceBridge)instantiatingBridge;

                return true;
            }

            if (!bridges.TryGetValue(scene.handle, out bridge))
            {
                bridge = masterBridge;
            }

            return bridge;
        }
    }
}
