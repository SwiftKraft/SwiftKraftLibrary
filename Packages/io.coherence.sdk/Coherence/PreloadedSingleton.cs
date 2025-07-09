namespace Coherence
{
    using System;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [DefaultExecutionOrder(-100)]
    public abstract class PreloadedSingleton : ScriptableObject
    {
        public abstract bool IsActiveInstance { get; }
    }

    public abstract class PreloadedSingleton<T> : PreloadedSingleton where T : ScriptableObject
    {
        [Obsolete("Use Instance instead.")]
        [Deprecated("03/2023", 1, 2, 0, Reason = "Use Instance instead.")]
        public static T instance => Instance;

        private static T _instance;

        /// <summary>
        /// Same as <see cref="Instance"/> but don't throw if the instance is not valid.
        /// </summary>
        internal static T InstanceUnsafe => _instance;

        /// <summary>
        /// Instance
        /// </summary>
        /// <remarks>Accessing this property before Awake() is not advised because the asset may not be in a ready state.</remarks>
        /// <exception cref="ArgumentNullException">Throws if the singleton instance is not set</exception>
        public static T Instance
        {
            get
            {
                if (!_instance)
                {
                    throw new ArgumentNullException(nameof(Instance),
                        $"{typeof(T).Name} is a {nameof(PreloadedSingleton)}, which should be included in " +
                        $"PlayerSettings' Preloaded Assets. Recompile for Preloaded Assets to be updated by coherence. " +
                        $"If the issue persists, reimport all assets (Assets > Reimport All). If that doesn't solve " +
                        $"the issue, check the docs ({{ExternalLinks.KnownIssues}}) or reach to us on community.");
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public override bool IsActiveInstance => _instance == this;

        /// <summary>
        /// This method can destroy the object, e.g, when creating a duplicate of the asset.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_instance && !IsActiveInstance)
            {
#if UNITY_EDITOR
                // A worker process can create additional instances on memory. In such cases, we must not
                // assume the user has intended to create a new instance, hence we should not delete the instance.
                if (AssetDatabase.IsAssetImportWorkerProcess())
                {
                    return;
                }

                DestroyImmediate(this, allowDestroyingAssets: true);

                var path = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
#else
                Destroy(this);
#endif
            }
            else
            {
                _instance = this as T;
            }
        }

        // OnDisable is NOT called when the asset has been modified externally
        protected virtual void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
