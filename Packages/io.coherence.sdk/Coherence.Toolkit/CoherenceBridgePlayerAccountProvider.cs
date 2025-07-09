// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Toolkit
{
    using System.Diagnostics.CodeAnalysis;
    using Cloud;
    using Common;

    internal sealed class CoherenceBridgePlayerAccountProvider : IPlayerAccountProvider
    {
        private readonly CoherenceBridge bridge;
        private PlayerAccount playerAccount;
        private CloudUniqueId uniqueId;
        private string projectId;
        private bool shouldReleaseUniqueId;

        public bool IsReady => bridge.CloudService is not null;

        [DisallowNull]
        public string ProjectId
        {
            get
            {
                if (projectId is not null)
                {
                    return projectId;
                }

                if (RuntimeSettings?.ProjectID is { Length: > 0 } projectIdFromSettings)
                {
                    projectId = projectIdFromSettings;
                    return projectId;
                }

                return "";
            }
        }

        [DisallowNull]
        public CloudUniqueId CloudUniqueId
        {
            get
            {
                if (uniqueId != CloudUniqueId.None)
                {
                    return uniqueId;
                }

                uniqueId = bridge.UniqueId;
                if (uniqueId == CloudUniqueId.None && ProjectId is { Length: > 0 } validProjectId)
                {
                    uniqueId = CloudUniqueIdPool.Get(validProjectId);
                    shouldReleaseUniqueId = true;
                }

                return uniqueId;
            }
        }

        [MaybeNull]
        private IRuntimeSettings RuntimeSettings
        {
            get
            {
                if (bridge.CloudService?.RuntimeSettings is { } bridgeSettings)
                {
                    return bridgeSettings;
                }

#if UNITY
                var singletonSettings = Coherence.RuntimeSettings.Instance;
                if (singletonSettings)
                {
                    return singletonSettings;
                }
#endif

                return null;
            }
        }

        public CoherenceBridgePlayerAccountProvider(CoherenceBridge bridge) => this.bridge = bridge;

        public PlayerAccount GetPlayerAccount(LoginInfo loginInfo)
        {
            if (playerAccount != null)
            {
                return playerAccount;
            }

            PlayerAccount existingPlayerAccount;
            if (bridge.PlayerAccountAutoConnect is CoherenceBridgePlayerAccount.Main)
            {
                existingPlayerAccount = PlayerAccount.Main;
            }
            else
            {
                existingPlayerAccount = PlayerAccount.Find(loginInfo);
            }

            if (existingPlayerAccount is not null)
            {
                existingPlayerAccount.AddLoginInfo(loginInfo);
                existingPlayerAccount.CloudUniqueId = CloudUniqueId;
                existingPlayerAccount.projectId = ProjectId;
                existingPlayerAccount.Services ??= bridge.CloudService;
                playerAccount = existingPlayerAccount;
                return playerAccount;
            }

            playerAccount = new(loginInfo, CloudUniqueId, ProjectId, bridge.CloudService);
            PlayerAccount.Register(playerAccount);
            return playerAccount;
        }

        public void Dispose()
        {
            if (playerAccount != null)
            {
                playerAccount.Dispose();
            }

            if (shouldReleaseUniqueId)
            {
                shouldReleaseUniqueId = false;
                CloudUniqueIdPool.Release(ProjectId, CloudUniqueId);
            }
        }
    }
}
