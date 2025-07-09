// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    /// <summary>
    /// Exact copy of warning ids and warning names from RS (in Go).
    /// Since the ids will not change in the future (only new ones will be added) this list is stable.
    /// Only warnings which were of importance to the client were added (for integration-tests).
    /// </summary>
    public enum RSWarning
    {
        BriskFrequencyConnectionThrottled = 12,
    }
}

