// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using System;

    public static class Constants
    {
        public static readonly TimeSpan minBackoff = TimeSpan.FromSeconds(1.05f);
        public static readonly TimeSpan maxBackoff = TimeSpan.FromSeconds(10);
    }
}
