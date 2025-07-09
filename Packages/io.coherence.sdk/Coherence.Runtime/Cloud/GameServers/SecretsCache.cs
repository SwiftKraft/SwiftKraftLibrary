// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;

    public class SecretsCache
    {
        private readonly Dictionary<ulong, string> cache = new();

        public void Add(ulong serverId, string secret)
        {
            cache.Add(serverId, secret);
        }

        public string Get(ulong serverId)
        {
            cache.TryGetValue(serverId, out var secret);
            return secret;
        }

        public void Remove(ulong serverId)
        {
            cache.Remove(serverId);
        }
    }
}
