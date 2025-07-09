// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Threading.Tasks;

    public class GameServices : IDisposable
    {
        /// <summary>
        ///     Log in the coherence Cloud.
        /// </summary>
        public IAuthClient AuthService { get; }

        public MatchmakerClient MatchmakerService { get; }

        /// <summary>
        /// Service that can be used to save data in coherence Cloud and restore it later.
        /// </summary>
        public CloudStorage CloudStorage { get; }

        /// <summary>
        /// The old cloud-backed key-value store service. Superseded by <see cref="CloudStorage"/>.
        /// <remarks>
        /// To use this service, you must have 'Player Key-Value Store' enabled in Project Settings in your coherence Dashboard.
        /// </remarks>
        /// </summary>
        public KvStoreClient KvStoreService { get; }

        internal readonly IAuthClientInternal authService;

        internal GameServices() { } // for test doubles

        public GameServices(CloudCredentialsPair credentialsPair)
        {
            AuthService = credentialsPair.AuthClient;
            authService = credentialsPair.authClient;
            MatchmakerService = new( credentialsPair.RequestFactory, authService);
            CloudStorage = new(credentialsPair.RequestFactory, authService, credentialsPair.requestFactory.Throttle, cloudStorage => new(cloudStorage));
            KvStoreService = new( credentialsPair.RequestFactory, authService);
        }

        public void Dispose()
        {
            KvStoreService?.Dispose();
            ((IDisposable)CloudStorage)?.Dispose();
        }

        internal async ValueTask DisposeAsync(bool waitForOngoingOperationsToFinish)
        {
            KvStoreService.Dispose();
            await CloudStorage.DisposeAsync(waitForOngoingOperationsToFinish);
        }
    }
}
