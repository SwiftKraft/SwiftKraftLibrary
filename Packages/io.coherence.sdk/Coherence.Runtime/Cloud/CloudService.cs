// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Runtime;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Runtime.Utils;
    using Log;

    /// <summary>
    ///     Runtime API to be able to interface with an Organization and Project from the coherence Cloud.
    ///     Check the coherence Cloud tab in the coherence Hub window for more details.
    /// </summary>
    public class CloudService : IDisposable
    {
        internal const string RequestIDHeader = "X-Coherence-Request-ID";
        internal const string ClientVersionHeader = "X-Coherence-Client";
        internal const string SchemaIdHeader = "X-Coherence-Schema-ID";
        internal const string RSVersionHeader = "X-Coherence-Engine";

        internal bool shouldDisposeRequestFactoryAndAuthClient;

        /// <summary>
        ///     Returns true when the Web Socket is connected.
        /// </summary>
        public bool IsConnectedToCloud => requestFactory.IsReady;

        /// <summary>
        ///     Returns true when the Web Socket is connected and when we are logged in to coherence Cloud.
        /// </summary>
        public bool IsLoggedIn => requestFactory.IsReady && authClient.LoggedIn;

        /// <summary>
        ///     RuntimeSettings that you pass through the constructor, if none is specified, RuntimeSettings.Instance will be used.
        /// </summary>
        public IRuntimeSettings RuntimeSettings => runtimeSettings;
        /// <summary>
        ///     Worlds REST service to fetch the available worlds that are online in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public WorldsService Worlds { get; }
        /// <summary>
        ///     Rooms REST service to fetch, create and delete rooms in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public CloudRooms Rooms { get; }
        /// <summary>
        /// Collection of Game Services that you can interact with in the coherence Dashboard under Project Settings > Services.
        /// </summary>
        public GameServices GameServices { get; }
        /// <summary>
        ///     GameServers REST service that controls game servers in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public IGameServersService GameServers { get; }

        internal AnalyticsClient AnalyticsClient { get; private set; }

        private readonly IRequestFactory requestFactory;
        private readonly IAuthClientInternal authClient;
        private readonly IRuntimeSettings runtimeSettings;
        private IPlayerAccountProvider playerAccountProvider;
        private bool shouldDisposePlayerAccountProvider;

        internal IRequestFactory RequestFactory => requestFactory;
        internal IAuthClientInternal AuthClient => authClient;
        internal IPlayerAccountProvider PlayerAccountProvider => playerAccountProvider;

        public static CloudService ForClient(IRuntimeSettings runtimeSettings = null)
            => ForClient(null, runtimeSettings, null, false);

        internal static CloudService ForClient([MaybeNull] IPlayerAccountProvider playerAccountProvider, IRuntimeSettings runtimeSettings = null, CloudUniqueId cloudUniqueId = default, bool autoLoginAsGuest = false)
        {
#if UNITY
            runtimeSettings ??= Coherence.RuntimeSettings.Instance;
#endif
            CloudService result = null;
            var shouldDisposePlayerAccountProvider = false;
            if (playerAccountProvider is null)
            {
                playerAccountProvider = new NewPlayerAccountProvider(() => result, cloudUniqueId, runtimeSettings);
                cloudUniqueId = playerAccountProvider.CloudUniqueId;
                shouldDisposePlayerAccountProvider = true;
            }

            var credentialsPair = CloudCredentialsFactory.ForClient(runtimeSettings, cloudUniqueId, playerAccountProvider);
            result = new(credentialsPair, runtimeSettings, playerAccountProvider) { shouldDisposeRequestFactoryAndAuthClient = true };
            result.playerAccountProvider = playerAccountProvider;
            result.shouldDisposePlayerAccountProvider = shouldDisposePlayerAccountProvider;

            if (autoLoginAsGuest)
            {
                result.authClient.LoginAsGuest().Then(task => Log.GetLogger<CloudService>().Warning(Warning.RuntimeCloudLoginFailedMsg, task.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }

            return result;
        }

#if UNITY
        internal static CloudService ForSimulator(IRuntimeSettings runtimeSettings = null)
        {
            runtimeSettings ??= Coherence.RuntimeSettings.Instance;
            CloudService result = null;
            var playerAccountProvider = new SimulatorPlayerAccountProvider(() => result);
            var credentialsPair = CloudCredentialsFactory.ForSimulator(runtimeSettings, playerAccountProvider);
            result = new(credentialsPair, runtimeSettings, playerAccountProvider) { shouldDisposeRequestFactoryAndAuthClient = !SimulatorUtility.UseSharedCloudCredentials };
            result.playerAccountProvider = playerAccountProvider;
            result.shouldDisposePlayerAccountProvider = true;
            return result;
        }
#endif

        [Obsolete("This constructor will be removed in a future version. " + nameof(ForClient) + " should be used instead.")]
        [Deprecated("08/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CloudService(string uniqueId = null, bool autoLoginAsGuest = true, IRuntimeSettings runtimeSettings = null)
            : this(CloudCredentialsFactory.ForClient(runtimeSettings, new CloudUniqueId(uniqueId)), runtimeSettings, null)
        {
            shouldDisposeRequestFactoryAndAuthClient = true;
            if (autoLoginAsGuest)
            {
                authClient.LoginAsGuest().Then(task => Log.GetLogger<CloudService>().Warning(Warning.RuntimeCloudLoginFailedMsg, task.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private CloudService(CloudCredentialsPair credentials, IRuntimeSettings runtimeSettings, IPlayerAccountProvider playerAccountProvider)
        {
            this.playerAccountProvider = playerAccountProvider;
            this.runtimeSettings = runtimeSettings;
            authClient = credentials.authClient;
            requestFactory = credentials.RequestFactory;
            GameServices = new(credentials);
            Rooms = new(credentials, runtimeSettings, playerAccountProvider);
            Worlds = new(credentials, runtimeSettings, playerAccountProvider);
            AnalyticsClient = new(playerAccountProvider, runtimeSettings, requestFactory);
            GameServers = new GameServersService(credentials, runtimeSettings);
        }

        /// <remarks>
        /// <para>
        /// This constructor is only meant to be used for testing purposes, enabling the injecting of test doubles;
        /// usually <see cref="ForClient"/> should be used instead.
        /// </para>
        /// <para>
        /// Note: Calling <see cref="Dispose"/> on this object will also result in Dispose being called on all the injected services.
        /// </para>
        /// </remarks>
        internal CloudService(CloudCredentialsPair credentials, IRuntimeSettings runtimeSettings, IPlayerAccountProvider playerAccountProvider, GameServices gameServices, CloudRooms rooms, WorldsService worlds, AnalyticsClient analyticsClient, GameServersService gameServers)
        {
            this.playerAccountProvider = playerAccountProvider;
            this.runtimeSettings = runtimeSettings;
            authClient = credentials.authClient;
            requestFactory = credentials.RequestFactory;
            GameServices = gameServices;
            Rooms = rooms;
            Worlds = worlds;
            AnalyticsClient = analyticsClient;
            GameServers = gameServers;
        }

        /// <summary>
        ///     IEnumerator you can use to wait for the CloudService to be ready within a Coroutine.
        /// </summary>
        public IEnumerator WaitForCloudServiceLoginRoutine()
        {
            while (!IsLoggedIn)
            {
                yield return null;
            }
        }

        /// <summary>
        ///     Async method you can use to wait for the CloudService to be ready.
        /// </summary>
        /// <returns>Returns true when the CloudService is ready.</returns>
        public async Task<bool> WaitForCloudServiceLoginAsync(int millisecondsPollDelay)
        {
            while (!IsLoggedIn)
            {
                await TimeSpan.FromMilliseconds(millisecondsPollDelay);
            }

            return true;
        }

        internal async Task<bool> WaitForCloudServiceLoginAsync()
        {
            while (!IsLoggedIn)
            {
                await Task.Yield();
            }

            return true;
        }

        public void Dispose()
        {
            GameServices.Dispose();
            Rooms.Dispose();
            Worlds.Dispose();

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                CloudCredentialsPair.Dispose(authClient, requestFactory);
            }

            if (shouldDisposePlayerAccountProvider)
            {
                shouldDisposePlayerAccountProvider = false;
                playerAccountProvider.Dispose();
            }
        }

        /// <param name="waitForOngoingOperationsToFinish">
        /// If true, then ongoing and queued cloud operations are allowed to finish before the services
        /// performing them are shut down; otherwise, the operations should be canceled immediately.
        /// </param>
        internal async ValueTask DisposeAsync(bool waitForOngoingOperationsToFinish)
        {
            var disposeGameServices = GameServices.DisposeAsync(waitForOngoingOperationsToFinish);
            var disposeRooms = Rooms.DisposeAsync();
            var disposeWorlds = Worlds.DisposeAsync();

            // Don't start disposing request factory until we've finished disposing all the services that depend on it.
            await disposeGameServices;
            await disposeRooms;
            await disposeWorlds;

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                await CloudCredentialsPair.DisposeAsync(authClient, requestFactory);
            }

            if (shouldDisposePlayerAccountProvider)
            {
                shouldDisposePlayerAccountProvider = false;
                playerAccountProvider.Dispose();
            }
        }
    }
}
