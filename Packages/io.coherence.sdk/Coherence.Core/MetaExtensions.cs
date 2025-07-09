// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Entities;

    public static class MetaExtensions
    {
        private static AuthorityType GetAuthorityType(bool hasStateAuthority, bool hasInputAuthority)
        {
            if (hasStateAuthority)
            {
                return hasInputAuthority ? AuthorityType.Full : AuthorityType.State;
            }

            return hasInputAuthority ? AuthorityType.Input : AuthorityType.None;
        }

        public static AuthorityType Authority(this EntityWithMeta meta) =>
            GetAuthorityType(meta.HasStateAuthority, meta.HasInputAuthority);

        public static AuthorityType Authority(this SerializedMeta meta) =>
            GetAuthorityType(meta.HasStateAuthority, meta.HasInputAuthority);
    }
}
