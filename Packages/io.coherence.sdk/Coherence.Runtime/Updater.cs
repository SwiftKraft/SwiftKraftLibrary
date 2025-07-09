// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cloud;
#if UNITY
    using UnityEngine;
#endif

    interface IUpdatable
    {
        void Update();
    }

    class Updater
#if UNITY
        : MonoBehaviour
#endif
    {
        static Updater instance;

        private readonly List<WeakReference<IUpdatable>> updateList = new List<WeakReference<IUpdatable>>();

#if UNITY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        internal static void Init()
        {
            if (instance != null)
            {
                return;
            }

#if UNITY
            var go = new GameObject("Coherence.Play");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            instance = go.AddComponent<Updater>();
#else
            instance = new Updater();
#endif
        }

        internal static void RegisterForUpdate(IUpdatable item)
        {
            Init();
            instance.updateList.Add(new WeakReference<IUpdatable>(item));
        }

        internal static void DeregisterForUpdate(IUpdatable item)
        {
            for (int i = instance.updateList.Count - 1; i >= 0; i--)
            {
                WeakReference<IUpdatable> itemRef = instance.updateList[i];
                if (itemRef.TryGetTarget(out IUpdatable inside))
                {
                    if (item == inside)
                    {
                        instance.updateList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        internal static void UpdateInstance()
        {
            Init();

            if (instance != null)
            {
                instance.Update();
            }
        }

        void Update()
        {
            for (int i = updateList.Count - 1; i >= 0; i--)
            {
                WeakReference<IUpdatable> itemRef = updateList[i];
                if (itemRef.TryGetTarget(out IUpdatable item))
                {
                    item.Update();
                }
                else
                {
                    updateList.RemoveAt(i);
                }
            }
        }
    }
}
