// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    internal class HubModuleManager : ScriptableSingleton<HubModuleManager>
    {
        public enum AssignStrategy
        {
            IgnoreAssigned,
            ForceReassign,
        }

        private static readonly TypeCache.TypeCollection hubModuleTypes;
        private static readonly IEnumerable<Type> hubModuleTypesOrdered;

        [Serializable]
        private class ModuleConnection : IEquatable<ModuleConnection>
        {
            public HubModule module;
            public ScriptableObject connection;

            public bool Equals(ModuleConnection other)
            {
                return other != null && module == other.module && connection == other.connection;
            }
        }

        [SerializeField] private List<ModuleConnection> moduleConnections = new List<ModuleConnection>();

        static HubModuleManager()
        {
            hubModuleTypes = TypeCache.GetTypesWithAttribute<HubModuleAttribute>();
            hubModuleTypesOrdered = hubModuleTypes.OrderByDescending(m => m.GetCustomAttribute<HubModuleAttribute>().Priority);
        }

        private bool TryGetInstantiatedHubModule(Type type, out HubModule instance)
        {
            // find if there's an instance available loaded already
            var instantiatedHubModules = Resources.FindObjectsOfTypeAll(type);

            var hubModules = moduleConnections.Select(mc => mc.module);

            foreach (var hubModule in hubModules)
            {
                if (instantiatedHubModules.Contains(hubModule))
                {
                    instance = hubModule;
                    return true;
                }
            }

            if (instantiatedHubModules.Length > 0)
            {
                instance = instantiatedHubModules[0] as HubModule;
                return false;
            }

            instance = default;
            return false;
        }

        public int Purge()
        {
            return moduleConnections.RemoveAll(mc => !mc.connection || !mc.module || !hubModuleTypes.Contains(mc.module.GetType()));
        }

        public void AssignAllModules(EditorWindow connection, AssignStrategy strategy = AssignStrategy.IgnoreAssigned)
        {
            foreach (var type in hubModuleTypesOrdered)
            {
                AssignModule(type, connection, out HubModule _, strategy);
            }
        }

        private int IndexOfModuleConnection(HubModule module)
        {
            for (int i = 0; i < moduleConnections.Count; i++)
            {
                var mc = moduleConnections[i];
                if (mc.module == module)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool AssignModule(Type type, EditorWindow connection, out ModuleConnection moduleConnection, AssignStrategy strategy = AssignStrategy.IgnoreAssigned)
        {
            if (!connection)
            {
                moduleConnection = default;
                return false;
            }

            // discard types that don't inherit from HubModule
            if (!typeof(HubModule).IsAssignableFrom(type))
            {
                moduleConnection = default;
                return false;
            }

            if (!TryGetInstantiatedHubModule(type, out HubModule module))
            {
                if (!module)
                {
                    module = CreateInstance(type) as HubModule;
                    module.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            return AssignModule(module, connection, out moduleConnection, strategy);
        }

        private bool AssignModule(HubModule module, EditorWindow connection, out ModuleConnection moduleConnection, AssignStrategy strategy = AssignStrategy.IgnoreAssigned)
        {
            var idx = IndexOfModuleConnection(module);
            moduleConnection = idx != -1 ? moduleConnections[idx] : new ModuleConnection { module = module, connection = null };

            if (idx != -1 && strategy == AssignStrategy.IgnoreAssigned)
            {
                return false;
            }

            var changed = moduleConnection.connection != connection;
            moduleConnection.connection = connection;

            if (idx == -1)
            {
                moduleConnections.Add(moduleConnection);

                // TODO reorder
            }

            module.Host = connection;
            if (connection is CoherenceHub)
            {
                module.OnRegisteredByHub();
            }
            else if (connection is BaseModuleWindow)
            {
                module.OnRegisteredByModuleWindow();
            }

            return changed;
        }

        private void AssignModule(Type type, EditorWindow connection, out HubModule module, AssignStrategy strategy)
        {
            _ = AssignModule(type, connection, out ModuleConnection mc, strategy);
            module = mc.module;
        }

        public void AssignModule<T>(EditorWindow connection, out T module, AssignStrategy strategy = AssignStrategy.ForceReassign) where T : HubModule
        {
            AssignModule(typeof(T), connection, out HubModule m, strategy);
            module = (T)m;
        }

        public void ReleaseModules(ScriptableObject connection)
        {
            var modules = moduleConnections.Where(mc => mc.connection == connection).Select(mc => mc.module).ToList();
            foreach (var module in modules)
            {
                ReleaseModule(module);
            }
        }

        public void ReleaseModule(HubModule module)
        {
            var idx = IndexOfModuleConnection(module);
            if (idx == -1)
            {
                return;
            }

            var mc = moduleConnections[idx];
            if (mc.connection is CoherenceHub)
            {
                _ = moduleConnections.Remove(mc);
                DestroyImmediate(module);
                return;
            }
            //If module is not hosted by coherenceHub, but is Standalone
            else if (mc.module.GetType().GetCustomAttribute<HubModuleAttribute>() == null)
            {
                _ = moduleConnections.Remove(mc);
                DestroyImmediate(module);
                return;
            }

            var hubs = Resources.FindObjectsOfTypeAll<CoherenceHub>();
            if (hubs != null && hubs.Length > 0)
            {
                if (hubModuleTypes.Contains(mc.module.GetType()))
                {
                    mc.connection = hubs[0];
                    mc.module.OnRegisteredByHub();
                }
            }
            else
            {
                _ = moduleConnections.Remove(mc);
                DestroyImmediate(module);
            }
        }

        public HubModule[] GetActiveModules(ScriptableObject connection)
        {
            return moduleConnections.Where(mc => mc.connection == connection).Select(mc => mc.module).ToArray();
        }

        public HubModule GetActiveModule(ScriptableObject connection)
        {
            return GetActiveModules(connection).FirstOrDefault();
        }

        public T GetActiveModule<T>() where T : HubModule
        {
            var res = moduleConnections.FirstOrDefault(mc => mc.module && mc.module is T);
            return res != null ? (T)res.module : default;
        }
    }
}
