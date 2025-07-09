// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using Common;
    using Moq;
    using Transport;

    /// <summary>
    /// Can be used to <see cref="Build"/> a mock <see cref="IRuntimeSettings"/> object for use in a test.
    /// </summary>
    internal sealed class MockRuntimeSettingsBuilder
    {
        private bool isWebGL = false;
        private string apiEndpoint = "ApiEndpoint";
        private string WebSocketEndpoint = "WebSocketEndpoint";
        private string runtimeKey = "RuntimeKey";
        private string organizationID = "OrganizationID";
        private string projectId = "ProjectID";
        private string projectName = "ProjectName";
        private string schemaID = "SchemaID";
        private string simulatorSlug = "SimulatorSlug";
        private string localHost = "LocalHost";
        private int localWorldUDPPort = 1;
        private int localWorldWebPort = 2;
        private int remoteWebPort = 3;
        private int localRoomsUDPPort = 4;
        private int localRoomsWebPort = 5;
        private int apiPort = 6;
        private string localHttpServerHost = "LocalHttpServerHost";
        private int localHttpServerPort = 7;
        private bool localDevelopmentMode = true;
        private TransportType transportType = TransportType.UDPWithTCPFallback;
        private TransportConfiguration transportConfiguration = TransportConfiguration.Default;
        private bool useDebugStreams = false;
        private bool disableKeepAlive = false;
        private IRuntimeSettings runtimeSettings;

        public MockVersionInfoBuilder VersionInfoBuilder { get; } = new();
        public Mock<IRuntimeSettings> Mock { get; } = new(MockBehavior.Strict);
        public IRuntimeSettings RuntimeSettings => Build();

        public MockRuntimeSettingsBuilder SetIsWebGL(bool isWebGL)
        {
            this.isWebGL = isWebGL;
            return this;
        }

        public MockRuntimeSettingsBuilder SetApiEndpoint(string apiEndpoint)
        {
            this.apiEndpoint = apiEndpoint;
            return this;
        }

        public MockRuntimeSettingsBuilder SetWebSocketEndpoint(string WebSocketEndpoint)
        {
            this.WebSocketEndpoint = WebSocketEndpoint;
            return this;
        }

        public MockRuntimeSettingsBuilder SetRuntimeKey(string runtimeKey)
        {
            this.runtimeKey = runtimeKey;
            return this;
        }

        public MockRuntimeSettingsBuilder SetOrganizationID(string organizationID)
        {
            this.organizationID = organizationID;
            return this;
        }

        public MockRuntimeSettingsBuilder SetProjectID(string projectId)
        {
            this.projectId = projectId;
            return this;
        }

        public MockRuntimeSettingsBuilder SetProjectName(string projectName)
        {
            this.projectName = projectName;
            return this;
        }

        public MockRuntimeSettingsBuilder SetSchemaID(string schemaID)
        {
            this.schemaID = schemaID;
            return this;
        }

        public MockRuntimeSettingsBuilder SetSimulatorSlug(string simulatorSlug)
        {
            this.simulatorSlug = simulatorSlug;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalHost(string localHost)
        {
            this.localHost = localHost;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalWorldUDPPort(int localWorldUDPPort)
        {
            this.localWorldUDPPort = localWorldUDPPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalWorldWebPort(int localWorldWebPort)
        {
            this.localWorldWebPort = localWorldWebPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetRemoteWebPort(int remoteWebPort)
        {
            this.remoteWebPort = remoteWebPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalRoomsUDPPort(int localRoomsUDPPort)
        {
            this.localRoomsUDPPort = localRoomsUDPPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalRoomsWebPort(int localRoomsWebPort)
        {
            this.localRoomsWebPort = localRoomsWebPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetAPIPort(int apiPort)
        {
            this.apiPort = apiPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalHttpServerHost(string localHttpServerHost)
        {
            this.localHttpServerHost = localHttpServerHost;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalHttpServerPort(int localHttpServerPort)
        {
            this.localHttpServerPort = localHttpServerPort;
            return this;
        }

        public MockRuntimeSettingsBuilder SetLocalDevelopmentMode(bool localDevelopmentMode)
        {
            this.localDevelopmentMode = localDevelopmentMode;
            return this;
        }

        public MockRuntimeSettingsBuilder SetTransportType(TransportType transportType)
        {
            this.transportType = transportType;
            return this;
        }

        public MockRuntimeSettingsBuilder SetTransportConfiguration(TransportConfiguration transportConfiguration)
        {
            this.transportConfiguration = transportConfiguration;
            return this;
        }

        public MockRuntimeSettingsBuilder SetUseDebugStreams(bool useDebugStreams)
        {
            this.useDebugStreams = useDebugStreams;
            return this;
        }

        public MockRuntimeSettingsBuilder SetDisableKeepAlive(bool disableKeepAlive)
        {
            this.disableKeepAlive = disableKeepAlive;
            return this;
        }

        public IRuntimeSettings Build()
        {
            if (runtimeSettings is not null)
            {
                return runtimeSettings;
            }

            Mock.Setup(settings => settings.VersionInfo).Returns(VersionInfoBuilder.Build());
            Mock.Setup(settings => settings.IsWebGL).Returns(isWebGL);
            Mock.Setup(settings => settings.ApiEndpoint).Returns(apiEndpoint);
            Mock.Setup(settings => settings.WebSocketEndpoint).Returns(WebSocketEndpoint);
            Mock.Setup(settings => settings.RuntimeKey).Returns(runtimeKey);
            Mock.Setup(settings => settings.OrganizationID).Returns(organizationID);
            Mock.Setup(settings => settings.ProjectID).Returns(projectId);
            Mock.Setup(settings => settings.ProjectName).Returns(projectName);
            Mock.Setup(settings => settings.SchemaID).Returns(schemaID);
            Mock.Setup(settings => settings.SimulatorSlug).Returns(simulatorSlug);
            Mock.Setup(settings => settings.LocalHost).Returns(localHost);
            Mock.Setup(settings => settings.LocalWorldUDPPort).Returns(localWorldUDPPort);
            Mock.Setup(settings => settings.LocalWorldWebPort).Returns(localWorldWebPort);
            Mock.Setup(settings => settings.RemoteWebPort).Returns(remoteWebPort);
            Mock.Setup(settings => settings.LocalRoomsUDPPort).Returns(localRoomsUDPPort);
            Mock.Setup(settings => settings.LocalRoomsWebPort).Returns(localRoomsWebPort);
            Mock.Setup(settings => settings.APIPort).Returns(apiPort);
            Mock.Setup(settings => settings.LocalHttpServerHost).Returns(localHttpServerHost);
            Mock.Setup(settings => settings.LocalHttpServerPort).Returns(localHttpServerPort);
            Mock.Setup(settings => settings.LocalDevelopmentMode).Returns(localDevelopmentMode);
            Mock.Setup(settings => settings.TransportType).Returns(transportType);
            Mock.Setup(settings => settings.TransportConfiguration).Returns(transportConfiguration);
            Mock.Setup(settings => settings.UseDebugStreams).Returns(useDebugStreams);
            Mock.Setup(settings => settings.DisableKeepAlive).Returns(disableKeepAlive);
            runtimeSettings = Mock.Object;
            return runtimeSettings;
        }
    }
}
