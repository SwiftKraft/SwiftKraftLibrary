// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence.Connection;
    using UnityEngine.SceneManagement;

    [System.Serializable]
    public class CoherenceSceneLoaderConfig
    {
        public ConnectionType connectionType = ConnectionType.Client;

        public string sceneName;
        public LocalPhysicsMode localPhysicsMode = ~LocalPhysicsMode.None;
        public UnloadSceneOptions unloadSceneOptions = UnloadSceneOptions.None;
    }
}
