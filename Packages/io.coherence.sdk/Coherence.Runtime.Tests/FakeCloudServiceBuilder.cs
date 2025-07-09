// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;
    using Common;
    using Moq;

    /// <summary>
    /// Can be used to <see cref="Build"/> a fake <see cref="CloudService"/> object for use in a test.
    /// </summary>
    internal sealed class FakeCloudServiceBuilder : IDisposable
    {
        private CloudCredentialsPair cloudCredentialsPair;
        private CloudService cloudService;
        private MockPlayerAccountProviderBuilder playerAccountProviderBuilder;
        private bool shouldMockAuthClient = true;
        private IAuthClientInternal authClient;
        private MockAuthClientBuilder authClientBuilder;
        private bool autoLoginAsGuest;
        private bool buildExecuted;

        public MockAuthClientBuilder AuthClientBuilder => authClientBuilder ??= new();
        public MockRequestFactoryBuilder RequestFactoryBuilder { get; } = new();
        public MockRuntimeSettingsBuilder RuntimeSettingsBuilder { get; } = new();
        public MockPlayerAccountProviderBuilder PlayerAccountProviderBuilder => playerAccountProviderBuilder ??= new MockPlayerAccountProviderBuilder().SetServicesBuilder(this);

        public IAuthClientInternal AuthClient
        {
            get
            {
                if (authClient is not null)
                {
                    return authClient;
                }

                if (shouldMockAuthClient)
                {
                    return authClient = AuthClientBuilder.AuthClient;
                }

                authClient = Coherence.Cloud.AuthClient.ForPlayer(RequestFactory, PlayerAccountProvider);
                return authClient;
            }
        }

        public IRequestFactoryInternal RequestFactory => RequestFactoryBuilder.RequestFactory;
        public IRuntimeSettings RuntimeSettings => RuntimeSettingsBuilder.RuntimeSettings;
        public IPlayerAccountProvider PlayerAccountProvider => PlayerAccountProviderBuilder.PlayerAccountProvider;

        public CloudCredentialsPair CloudCredentialsPair => cloudCredentialsPair ??= new(AuthClient, RequestFactory);

        public CloudService CloudService => Build();

        public FakeCloudServiceBuilder SetShouldMockAuthClient(bool shouldMockAuthClient)
        {
            this.shouldMockAuthClient = shouldMockAuthClient;
            return this;
        }

        public FakeCloudServiceBuilder SetAuthClient(IAuthClientInternal authClient)
        {
            this.authClient = authClient;
            this.shouldMockAuthClient = authClient is null;
            return this;
        }

        public FakeCloudServiceBuilder SetUniqueId(CloudUniqueId uniqueId)
        {
            PlayerAccountProviderBuilder.SetUniqueId(uniqueId);
            return this;
        }

        public FakeCloudServiceBuilder SetProjectId(string projectId)
        {
            PlayerAccountProviderBuilder.SetProjectId(projectId);
            RuntimeSettingsBuilder.SetProjectID(projectId);
            return this;
        }

        public FakeCloudServiceBuilder SetupRuntimeSettings(Action<MockRuntimeSettingsBuilder> setupRuntimeSettingsBuilder)
        {
            setupRuntimeSettingsBuilder(RuntimeSettingsBuilder);
            return this;
        }

        public FakeCloudServiceBuilder SetupAuthClient(Action<MockAuthClientBuilder> setupAuthClientBuilder)
        {
            setupAuthClientBuilder(AuthClientBuilder);
            return this;
        }

        public FakeCloudServiceBuilder SetupRequestFactory(Action<MockRequestFactoryBuilder> setupRequestFactoryBuilder)
        {
            setupRequestFactoryBuilder(RequestFactoryBuilder);
            return this;
        }

        public FakeCloudServiceBuilder SetupPlayerAccountProvider(Action<MockPlayerAccountProviderBuilder> setupPlayerAccountProviderBuilder)
        {
            setupPlayerAccountProviderBuilder(PlayerAccountProviderBuilder);
            return this;
        }

        public FakeCloudServiceBuilder SetAutoLoginAsGuest(bool autoLoginAsGuest)
        {
            if (autoLoginAsGuest)
            {
                shouldMockAuthClient = false;
            }

            this.autoLoginAsGuest = autoLoginAsGuest;
            return this;
        }

        public CloudService Build()
        {
            if (buildExecuted)
            {
                return cloudService ?? throw new NullReferenceException($"{GetType().Name}.Build was called again while previous Build execution is still in progress!");
            }

            buildExecuted = true;
            var gameServices = new Mock<GameServices>().Object;
            var rooms = new Mock<CloudRooms>().Object;
            var worlds = new Mock<WorldsService>().Object;
            var analyticsClient = new Mock<AnalyticsClient>().Object;
            var gameServers = new Mock<GameServersService>().Object;
            cloudService = new(CloudCredentialsPair, RuntimeSettings, PlayerAccountProvider, gameServices, rooms, worlds, analyticsClient, gameServers);

            if (autoLoginAsGuest)
            {
                AuthClient.LoginAsGuest();
            }

            return cloudService;
        }

        public void Dispose()
        {
            // Calling cloudService.Dispose() will result in Dispose also being called on all its dependencies
            // (GameServices, Rooms, Worlds, AuthClient, RequestFactory, PlayerAccountProvider), so there's no need to
            // dispose them separately.
            cloudService?.Dispose();
            cloudService = null;
            buildExecuted = false;
        }
    }
}
