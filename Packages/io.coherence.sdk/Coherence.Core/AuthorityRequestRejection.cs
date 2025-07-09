// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using Entities;

    public struct AuthorityRequestRejection
    {
        public Entity EntityID;
        public AuthorityType AuthorityType;

        public AuthorityRequestRejection(Entity entityID, AuthorityType authorityType)
        {
            EntityID = entityID;
            AuthorityType = authorityType;
        }
    }
}
