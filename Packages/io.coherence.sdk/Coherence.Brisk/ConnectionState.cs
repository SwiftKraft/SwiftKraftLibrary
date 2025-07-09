// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    public enum ConnectionState : byte
    {
        Disconnected = 0,
        Opening = 1,
        Connecting = 2,
        Connected = 3
    };
}
