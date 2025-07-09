// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Entities;
    using System;

    public interface IClientConnectionManager
    {
        void GetPrefab(ClientID clientId, ConnectionType connectionType, Action<ICoherenceSync> onLoaded);
        void Add(CoherenceClientConnection connection);
        bool Remove(Entity entityID);
    }
}
