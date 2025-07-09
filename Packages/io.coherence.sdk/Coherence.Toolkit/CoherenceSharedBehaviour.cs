// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit.Internal
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using UnityEngine;

    /// <summary>
    /// Base class for components that are automatically attached to a hidden game object that survives scene transitions
    /// when the static <see cref="SharedInstance"/> property is accessed for the first time.
    /// </summary>
    /// <typeparam name="TSharedBehaviour"> The concrete type of the shared behaviour that derives from this base class. </typeparam>
    internal abstract class CoherenceSharedBehaviour<TSharedBehaviour> : CoherenceSharedBehaviourBase where TSharedBehaviour : CoherenceSharedBehaviour<TSharedBehaviour>
    {
        /// <summary>
        /// A single shared instance of <see cref="TSharedBehaviour"/>.
        /// </summary>
        private static TSharedBehaviour sharedInstance;

        /// <summary>
        /// Gets a reference to single shared instance of <see cref="TSharedBehaviour"/>.
        /// </summary>
        protected static TSharedBehaviour SharedInstance
        {
            get
            {
                if (!sharedInstance)
                {
                    sharedInstance = CreateSharedInstance<TSharedBehaviour>();
                }

                return sharedInstance;
            }
        }

        /// <summary>
        /// Destroys the shared instance of type <see cref="TSharedBehaviour"/>
        /// and assigns <see langword="null"/> into fields that held a reference to it.
        /// </summary>
        internal static void DisposeSharedInstance(bool immediate) => DisposeSharedInstance(ref sharedInstance, immediate);

        internal static bool TryGetSharedInstance([MaybeNullWhen(false), NotNullWhen(true)] out TSharedBehaviour sharedInstance) => sharedInstance = CoherenceSharedBehaviour<TSharedBehaviour>.sharedInstance;
    }

    /// <summary>
    /// Non-generic base class of CoherenceSharedBehaviour.
    /// <para>
    /// Encapsulates the logic for creating a single shared hidden game object,
    /// and attaching new shared behaviours into it on demand.
    /// </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal abstract class CoherenceSharedBehaviourBase : CoherenceBehaviour
    {
        /// <summary>
        /// Indicates whether the application is quitting.
        /// </summary>
        private static bool applicationIsQuitting;

        /// <summary>
        /// Causes <see cref="GameObject"/> to survive scene transitions.
        /// </summary>
        private const HideFlags SharedGameObjectHideFlags = HideFlags.HideAndDontSave;

        /// <summary>
        /// A single hidden game object into which all shared behaviours can be attached.
        /// </summary>
        private static GameObject sharedGameObject;

        private static int sharedInstancesTotalCount;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetState()
        {
            sharedInstancesTotalCount = 0;
            applicationIsQuitting = false;

            if (sharedGameObject)
            {
                DestroyImmediate(sharedGameObject);
            }

            sharedGameObject = null;
        }
#endif

        private protected static TSharedBehaviour CreateSharedInstance<TSharedBehaviour>() where TSharedBehaviour : CoherenceSharedBehaviourBase
        {
            if (applicationIsQuitting)
            {
                return default;
            }

            sharedInstancesTotalCount++;
            sharedGameObject ??= CreateSharedGameObject();
            return sharedGameObject.AddComponent<TSharedBehaviour>();
        }

        private static GameObject CreateSharedGameObject() => new("coherence")
        {
            // Hiding the game object to avoid introducing more clutter to the hierarchy.
            // If the ability to inspect the behaviours' states using the Inspector is needed,
            // this can be  set to None, and DontDestroyOnLoad can be used instead to make
            // the game object persist through scene transitions.
            hideFlags = SharedGameObjectHideFlags
        };

        /// <summary>
        /// Destroys the <see cref="Object"/> contained in the <see paramref="sharedInstance"/> variable and assigns
        /// <see langword="null"/> into it.
        /// </summary>
        private protected static void DisposeSharedInstance<TSharedBehaviour>(ref TSharedBehaviour sharedInstance, bool immediate) where TSharedBehaviour : CoherenceSharedBehaviourBase
        {
            sharedInstancesTotalCount--;

            Object objectToDestroy;
            if (sharedInstancesTotalCount <= 0)
            {
                objectToDestroy = sharedGameObject;
                sharedGameObject = null;
                sharedInstancesTotalCount = 0;
            }
            else
            {
                objectToDestroy = sharedInstance;
            }

            sharedInstance = null;

            if (immediate)
            {
                DestroyImmediate(objectToDestroy);
            }
            else
            {
                Destroy(objectToDestroy);
            }
        }

        private void OnApplicationQuit() => applicationIsQuitting = true;
    }
}
