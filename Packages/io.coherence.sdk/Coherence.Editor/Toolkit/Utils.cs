// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System.Diagnostics.CodeAnalysis;
    using UnityEditor;
    using UnityEngine;
    using Coherence.Toolkit;
    using Coherence.Simulator;
    using Coherence.UI;
    using UnityEditor.SceneManagement;

    internal static class Utils
    {
        public static GameObject CreateInstance<T>(string name, GameObject parent) where T : Component
        {
            var go = ObjectFactory.CreateGameObject(name, typeof(T));
            GameObjectCreationCommands.Place(go, parent);
            return go;
        }

        public static GameObject CreateInstance<T>(string name) where T : Component
        {
            return CreateInstance<T>(name, null);
        }

        // global

        [MenuItem("GameObject/coherence/Bridge", false, 10)]
        public static void AddBridgeInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "bridge"));
            _ = CreateInstance<CoherenceBridge>(nameof(CoherenceBridge), menuCommand.context as GameObject);
        }

        // queries

        [MenuItem("GameObject/coherence/Live Query", false, 20)]
        public static void AddLiveQueryInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "live_query"));
            _ = CreateInstance<CoherenceLiveQuery>(nameof(CoherenceLiveQuery), menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/coherence/Tag Query", false, 21)]
        public static void AddTagQueryInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "tag_query"));
            _ = CreateInstance<CoherenceTagQuery>(nameof(CoherenceTagQuery), menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/coherence/Global Query", false, 22)]
        public static void AddGlobalQueryInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "global_query"));
            _ = CreateInstance<CoherenceGlobalQuery>(nameof(CoherenceGlobalQuery), menuCommand.context as GameObject);
        }

        // scene loading

        [MenuItem("GameObject/coherence/Scene Loader", false, 40)]
        public static void AddCoherenceSceneLoaderInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "scene_loader"));
            _ = CreateInstance<CoherenceSceneLoader>(nameof(CoherenceSceneLoader), menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/coherence/Scene (Listener)", false, 41)]
        public static void AddCoherenceSceneInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "scene"));
            _ = CreateInstance<CoherenceScene>(nameof(CoherenceScene), menuCommand.context as GameObject);
        }

#if COHERENCE_ENABLE_MULTI_ROOM_SIMULATOR
        // events

        [MenuItem("GameObject/coherence/Events/Connection Event Handler", false, 40)]
        public static void AddConnectionEventHandlerInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "connection_event_handler"));
#pragma warning disable CS0618 // Type or member is obsolete
            _ = CreateInstance<ConnectionEventHandler>(nameof(ConnectionEventHandler), menuCommand.context as GameObject);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [MenuItem("GameObject/coherence/Events/Simulator Event Handler", false, 41)]
        public static void AddSimulatorEventHandlerInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "simulator_event_handler"));
#pragma warning disable CS0618 // Type or member is obsolete
            _ = CreateInstance<SimulatorEventHandler>(nameof(SimulatorEventHandler), menuCommand.context as GameObject);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // mrs

        [MenuItem("GameObject/coherence/Multi-Room Simulators/Multi-Room Simulator", false, 50)]
        public static void AddSimulatorRoomJoinerInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "mrs"));
#pragma warning disable CS0618 // Type or member is obsolete
            _ = CreateInstance<MultiRoomSimulator>(nameof(MultiRoomSimulator), menuCommand.context as GameObject);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [MenuItem("GameObject/coherence/Multi-Room Simulators/Local Forwarder", false, 51)]
        public static void AddMultiRoomSimulatorLocalForwarderInstanceInScene(MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "mrs_local_forwarder"));
#pragma warning disable CS0618 // Type or member is obsolete
            _ = CreateInstance<MultiRoomSimulatorLocalForwarder>(nameof(MultiRoomSimulatorLocalForwarder), menuCommand.context as GameObject);
#pragma warning restore CS0618 // Type or member is obsolete
        }
#endif

        // sims

        [MenuItem("GameObject/coherence/Auto Simulator Connection", false, 60)]
        public static void AddAutoSimulatorConnection([DisallowNull] MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "auto_sim_connection"));
            _ = CreateInstance<AutoSimulatorConnection>(nameof(AutoSimulatorConnection), menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/coherence/Import Connect Dialog...", false, 100)]
        public static void AddSampleDialogInScene([DisallowNull] MenuCommand menuCommand)
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "gameobject"), ("item", "connect_dialog"));
            SampleDialogPickerWindow.ShowWindow("Connect Dialog", menuCommand.context as GameObject);
        }
    }
}
