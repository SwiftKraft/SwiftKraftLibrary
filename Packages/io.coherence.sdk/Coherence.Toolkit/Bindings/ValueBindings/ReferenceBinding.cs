namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using Log;
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;

    public class ReferenceBinding : ValueBinding<Entity>
    {
        protected ReferenceBinding() { }
        public ReferenceBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override Entity Value
        {
            get => MapToEntityId(GetValueUsingReflection());
            set => SetValueUsingReflection(MapToUnityObject(value));
        }

        public override void InvokeValueSyncCallback()
        {
            if (!valueSyncPrepared)
            {
                return;
            }

            try
            {
                _ = GetCallbackMethodInfo()?.Invoke(UnityComponent, new object[]
                {
                    MapToUnityObject(valueSyncOld),
                    MapToUnityObject(valueSyncNew)
                });
            }
            catch (Exception handlerException)
            {
                Logger.Error(Error.ToolkitBindingOnValueSyncedException,
                    ("exception", handlerException));
            }

            valueSyncPrepared = false;
        }

        private Entity MapToEntityId(object target)
        {
            if (MonoAssemblyRuntimeType == typeof(GameObject)) return coherenceSync.CoherenceBridge.UnityObjectToEntityId((GameObject)target);
            if (MonoAssemblyRuntimeType == typeof(RectTransform)) return coherenceSync.CoherenceBridge.UnityObjectToEntityId((RectTransform)target);
            if (MonoAssemblyRuntimeType == typeof(Transform)) return coherenceSync.CoherenceBridge.UnityObjectToEntityId((Transform)target);
            if (MonoAssemblyRuntimeType == typeof(CoherenceSync)) return coherenceSync.CoherenceBridge.UnityObjectToEntityId((CoherenceSync)target);
            throw new Exception("unexpected type: " + MonoAssemblyRuntimeType);
        }

        private object MapToUnityObject(Entity entityID)
        {
            if (MonoAssemblyRuntimeType == typeof(GameObject)) return coherenceSync.CoherenceBridge.EntityIdToGameObject(entityID);
            if (MonoAssemblyRuntimeType == typeof(CoherenceSync)) return coherenceSync.CoherenceBridge.EntityIdToCoherenceSync(entityID);
            if (MonoAssemblyRuntimeType == typeof(Transform)) return coherenceSync.CoherenceBridge.EntityIdToTransform(entityID);
            if (MonoAssemblyRuntimeType == typeof(RectTransform)) return coherenceSync.CoherenceBridge.EntityIdToRectTransform(entityID);
            throw new Exception("unexpected type: " + MonoAssemblyRuntimeType);
        }

        protected override bool DiffersFrom(Entity first, Entity second)
        {
            return first != second;
        }
    }
}
