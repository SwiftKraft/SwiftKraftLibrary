// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;
    using static Coherence.Toolkit.CoherenceSync;

    public interface IConnectedEntityDriver
    {
        public bool HasStateAuthority { get; }
        public CoherenceSync ConnectedEntity { get; }

        public event ConnectedEntityChangeHandler ConnectedEntityChangeOverride;
        public event ConnectedEntitySentHandler DidSendConnectedEntity;
        void SetParent(Transform parent);
    }
}
