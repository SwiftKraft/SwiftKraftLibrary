// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;

    /// <summary>
    /// Paths that are needed at runtime.
    /// Also see Coherence.Editor.Constants
    /// </summary>
    public static class Constants
    {
        public static readonly string localHost = "127.0.0.1";
        public static readonly int localWorldUDPPort = 32001;
        public static readonly int localWorldWebPort = 32002;
        public static readonly int localRoomsUDPPort = 42001;
        public static readonly int localRoomsWebPort = 42002;
        public static readonly int apiPort = 64001;
        public static readonly int worldsApiPort = 64002;
        public static readonly int remoteWebPort = 443;
        public static readonly int localHttpServerPort = 42006;
        public static readonly string localHttpServerHost = "localhost";
        public static readonly string apiEndpoint = "https://api.prod.coherence.io/v1/play";
    }
}
