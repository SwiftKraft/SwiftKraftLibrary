// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Coherence.Entities;
    using NUnit.Framework;
    using System;
    using System.Reflection;
    using UnityEngine;

    public static class UnityTestExtensions
    {
        public static void CallAwake(this MonoBehaviour monoBehaviour)
        {
            Call(monoBehaviour, "Awake");
        }

        public static void CallStart(this MonoBehaviour monoBehaviour)
        {
            Call(monoBehaviour, "Start");
        }

        public static void CallOnEnable(this MonoBehaviour monoBehaviour)
        {
            Call(monoBehaviour, "OnEnable");
        }

        private static void Call(MonoBehaviour monoBehaviour, string methodName)
        {
            MethodInfo method = monoBehaviour.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(monoBehaviour, Array.Empty<object>());
        }

        public static void SetBridge(this CoherenceSync sync, ICoherenceBridge bridge)
        {
            FieldInfo field = sync.GetType().GetField("bridge",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(sync, bridge);

            PropertyInfo loggerProperty = sync.GetType().GetProperty(nameof(CoherenceSync.logger),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            loggerProperty.SetValue(sync, new Coherence.Log.UnityLogger());
        }

        public static void ConnectBridge(this CoherenceSync sync, ICoherenceBridge bridge)
        {
            var onConnectedInternalEvent = bridge.GetType().GetEvent(nameof(bridge.OnConnectedInternal),
                BindingFlags.Instance | BindingFlags.Public);
            var handleConnectedMethod =
                sync.GetType().GetMethod("HandleConnected", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler =
                Delegate.CreateDelegate(onConnectedInternalEvent.EventHandlerType, sync, handleConnectedMethod);
            onConnectedInternalEvent.AddEventHandler(bridge, handler);
        }

        public static void SetEntityID(this CoherenceSync sync, Entity serializeEntityID)
        {
            PropertyInfo entityIDProperty = sync.GetType().GetProperty(nameof(CoherenceSync.EntityState),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            entityIDProperty.SetValue(sync, new NetworkEntityState(serializeEntityID, AuthorityType.Full, false, false, sync, string.Empty));
        }

        public static void SetUpdater(this CoherenceSync sync, ICoherenceSyncUpdater updater)
        {
            var field = sync.GetType().GetField(CoherenceSync.Property.updater,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(sync, updater);
        }

        public static void SetClient(this CoherenceBridge bridge, IClient client)
        {
            var clientProperty = bridge.GetType().GetProperty("Client");
            clientProperty.SetValue(bridge, client);
        }

        public static void SetClient(this EntitiesManager entitiesManager, IClient client)
        {
            var clientProperty =
                typeof(EntitiesManager).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            clientProperty.SetValue(entitiesManager, client);
        }

        public static void SetEntitiesManager(this CoherenceBridge bridge, EntitiesManager entitiesManager)
        {
            var managerProperty = bridge.GetType().GetProperty("EntitiesManager");
            managerProperty.SetValue(bridge, entitiesManager);
        }
    }
}
