// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    /// <summary>
    /// Specifies different ways that a coherence bridge can automatically connect to coherence Cloud.
    /// </summary>
    public enum CoherenceBridgePlayerAccount
    {
        None = 1,
        [InspectorName("Main Player Account")]
        Main = 2,
        AutoLoginAsGuest = 3
    }
}
