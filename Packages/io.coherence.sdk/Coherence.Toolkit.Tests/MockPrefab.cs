// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using Bindings;
    using Bindings.ValueBindings;
    using Entities;
    using Log;
    using ProtocolDef;
    using Utils;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    using Logger = Log.Logger;

    public class MockPrefab
    {
        public CoherenceSync sync;

        public static MockPrefab New()
        {
            var gameObject = new GameObject();
            var sync = gameObject.AddComponent<CoherenceSync>();
            var mockPrefab = new MockPrefab();
            mockPrefab.sync = sync;

            return mockPrefab;
        }
    }
}
