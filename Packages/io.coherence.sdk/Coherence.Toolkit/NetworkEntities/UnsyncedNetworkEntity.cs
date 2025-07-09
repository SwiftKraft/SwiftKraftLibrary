// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Entities;

    public struct UnsyncedNetworkEntity
    {
        public NetworkEntityState EntityState;
        public ComponentUpdates Updates;
        public uint? LOD;
        public string UniqueUUID;
    }
}

