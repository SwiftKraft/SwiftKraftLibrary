// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    /// <summary>
    /// Exact copy of error ids and error names from RS (in Go).
    /// Since the ids will not change in the future (only new ones will be added) this list is stable.
    /// Only errors which were of importance to the client were added (for integration-tests).
    /// </summary>
    public enum RSError
    {
        HTTPAPIResponse = 6,
        HTTPAPIStatusError = 7,
        ErrorEntityManagerCreateEntityRequest = 37,
        EntityManagerDestroyEntityRequest = 38,
        ReplicationRunError = 122,
    }
}
