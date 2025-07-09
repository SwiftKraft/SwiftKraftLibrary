// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
#if UNITY
    using UnityEngine;
#endif

    internal static class CloudCredentialsFactory
    {
#if UNITY
        private static CloudCredentialsPair sharedSimulatorCredentials;
#endif

#if UNITY_EDITOR
        // Support Enter Play Mode Options: Disable Reload Domain in the Unity Editor
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => sharedSimulatorCredentials = null;
#endif

        internal static CloudCredentialsPair ForClient(IRuntimeSettings runtimeSettings, IPlayerAccountProvider playerAccountProvider) => ForClient(runtimeSettings, CloudUniqueId.None, playerAccountProvider);

        internal static CloudCredentialsPair ForClient(IRuntimeSettings runtimeSettings, CloudUniqueId uniqueId = default) => ForClient(runtimeSettings, uniqueId, null);

        internal static CloudCredentialsPair ForClient(IRuntimeSettings runtimeSettings, CloudUniqueId uniqueId, IPlayerAccountProvider playerAccountProvider)
        {
            var useWebsocket = true;
#if UNITY && !COHERENCE_HAS_NN_WEBSOCKET
            useWebsocket = Application.platform != RuntimePlatform.Switch;
#endif

            var newRequestFactory = new RequestFactory(runtimeSettings, useWebsocket);
            playerAccountProvider ??= new NewPlayerAccountProvider(uniqueId, runtimeSettings);
            var newAuthClient = AuthClient.ForPlayer(newRequestFactory, playerAccountProvider);
            return new(newAuthClient, newRequestFactory);
        }

#if UNITY
        internal static CloudCredentialsPair ForSimulator(IRuntimeSettings runtimeSettings, SimulatorPlayerAccountProvider playerAccountProvider)
        {
            if (sharedSimulatorCredentials is not null)
            {
                return sharedSimulatorCredentials;
            }

            var newRequestFactory = new RequestFactory(runtimeSettings);
            var newAuthClient = AuthClient.ForSimulator(newRequestFactory, playerAccountProvider);
            newAuthClient.BeingDisposed += OnSharedAuthClientBeingDisposed;

            var credentialsPair = new CloudCredentialsPair(newAuthClient, newRequestFactory);

            // If we're fed an authentication token for a simulator, we force a single AuthClient/WebSocket instance for every CloudService
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                sharedSimulatorCredentials = credentialsPair;
            }

            return credentialsPair;

            static void OnSharedAuthClientBeingDisposed() => sharedSimulatorCredentials = null;
        }
#endif
    }
}
