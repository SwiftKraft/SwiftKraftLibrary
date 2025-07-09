// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using Entities;

    public struct AuthorityChange
    {
        public Entity EntityID;
        public AuthorityType NewAuthorityType;

        public AuthorityChange(Entity entityID, AuthorityType newAuthorityType)
        {
            EntityID = entityID;
            NewAuthorityType = newAuthorityType;
        }
    }
}
