// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence.Connection;
    using UnityEngine.SceneManagement;

    public class CoherenceSceneData
    {
        public string SceneName { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public EndpointData EndpointData { get; set; }
        public LocalPhysicsMode LocalPhysicsMode { get; set; }
    }
}
