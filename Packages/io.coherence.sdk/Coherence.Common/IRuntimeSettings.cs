// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using Transport;

    public interface IRuntimeSettings
    {
        IVersionInfo VersionInfo { get; }
        bool IsWebGL { get; }
        string ApiEndpoint { get; }
        string WebSocketEndpoint { get; }
        string RuntimeKey { get; }
        string OrganizationID { get; }
        string ProjectID { get; }
        string ProjectName { get; }
        string SchemaID { get; }
        string SimulatorSlug { get; }
        string LocalHost { get; }
        int LocalWorldUDPPort { get; }
        int LocalWorldWebPort { get; }
        int RemoteWebPort { get; }
        int LocalRoomsUDPPort { get; }
        int LocalRoomsWebPort { get; }
        int APIPort { get; }
        string LocalHttpServerHost { get; }
        int LocalHttpServerPort { get; }
        bool LocalDevelopmentMode { get; }
        TransportType TransportType { get; }
        TransportConfiguration TransportConfiguration { get; }
        bool UseDebugStreams { get; }
        bool DisableKeepAlive { get; }
    }
}
