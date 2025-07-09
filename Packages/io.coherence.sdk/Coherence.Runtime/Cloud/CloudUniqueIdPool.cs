// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif


namespace Coherence.Cloud
{
#if UNITY
    using UnityEngine;
#endif
    using Prefs;
    using Runtime;
    using System;
    using System.Collections.Generic;

    internal class CloudUniqueIdPool
    {
        private static Dictionary<string, CloudUniqueIdPool> idPoolsForProject =
            new Dictionary<string, CloudUniqueIdPool>();

        private List<string> allIdsPool = new List<string>();
        private Stack<string> inUseIdsPool = new Stack<string>();

#if UNITY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPoolInUnity()
        {
            idPoolsForProject.Clear();
        }
#endif

        public static CloudUniqueId Get(string projectId)
        {
            CloudUniqueIdPool idPool = GetIdPoolForProject(projectId);

            if (idPool.inUseIdsPool.Count <= 0)
            {
                return new(GenerateNewId(projectId, idPool));
            }

            var uniqueId = idPool.inUseIdsPool.Pop();

            return new(uniqueId);
        }

        internal static bool TryGet(string projectId, out string uniqueId)
        {
            CloudUniqueIdPool idPool = GetIdPoolForProject(projectId);

            if (idPool.inUseIdsPool.Count <= 0)
            {
                uniqueId = null;
                return false;
            }

            uniqueId = idPool.inUseIdsPool.Pop();
            return true;
        }

        public static void Release(string projectId, string idToRelease)
        {
            CloudUniqueIdPool guestAccountPool = GetIdPoolForProject(projectId);

            guestAccountPool.inUseIdsPool.Push(idToRelease);
        }

        internal static void RemoveProjectPool(string projectId)
        {
            idPoolsForProject.Remove(projectId);
            Prefs.DeleteKey(GetKeyForProject(projectId));
        }

        private static CloudUniqueIdPool GetIdPoolForProject(string projectId)
        {
            if (idPoolsForProject.TryGetValue(projectId, out CloudUniqueIdPool idPool))
            {
                return idPool;
            }

            idPool = new CloudUniqueIdPool();
            idPoolsForProject[projectId] = idPool;

            InitializeIdPool(projectId, idPool);

            return idPool;
        }

        private static void InitializeIdPool(string projectId, CloudUniqueIdPool idPool)
        {
            var key = GetKeyForProject(projectId);
            var json = Prefs.GetString(key);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            idPool.allIdsPool = Utils.CoherenceJson.DeserializeObject<List<string>>(json) ?? new List<string>();

            idPool.inUseIdsPool = new Stack<string>();

            for (int i = idPool.allIdsPool.Count - 1; i >= 0; i--)
            {
                idPool.inUseIdsPool.Push(idPool.allIdsPool[i]);
            }
        }

        private static string GenerateNewId(string projectId, CloudUniqueIdPool idPool)
        {
            var newId = Guid.NewGuid().ToString();

            idPool.allIdsPool.Add(newId);

            Prefs.SetString(GetKeyForProject(projectId), Utils.CoherenceJson.SerializeObject(idPool.allIdsPool));
            return newId;
        }

        private static string GetKeyForProject(string projectId)
        {
            return Runtime.Utils.PrefsUtils.Format(PrefsKeys.UniqueIdPool, projectId);
        }
    }
}
