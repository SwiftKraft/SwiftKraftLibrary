// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Connection;
    using Entities;

    public struct AuthorityRequest
    {
        public Entity EntityID;
        public ClientID RequesterID;
        public AuthorityType AuthorityType;

        public AuthorityRequest(Entity entityID, ClientID requesterID, AuthorityType authorityType)
        {
            EntityID = entityID;
            RequesterID = requesterID;
            AuthorityType = authorityType;
        }
    }
}
