// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System;

    public enum ComponentState
    {
        [Obsolete] Construct = 1,
        Update = 2,
        Destruct = 3,
    }
}
