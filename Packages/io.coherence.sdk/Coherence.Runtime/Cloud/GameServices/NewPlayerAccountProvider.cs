// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using Common;
    using Log;

    internal sealed class NewPlayerAccountProvider : IPlayerAccountProvider
    {
        private string projectId;
        private CloudUniqueId uniqueId;
        private CloudService services;
        private readonly Func<CloudService> getServices;
        private bool shouldReleaseGuid;
        private readonly List<PlayerAccount> createdPlayerAccounts = new(1);

        // If CloudService is initialized lazily, wait for it to become available.
        public bool IsReady => Services is not null || getServices is null;

        public CloudUniqueId CloudUniqueId
        {
            get
            {
                if (uniqueId == CloudUniqueId.None && !string.IsNullOrEmpty(projectId))
                {
                    shouldReleaseGuid = true;
                    uniqueId = CloudUniqueIdPool.Get(projectId);
                }

                return uniqueId;
            }
        }

        public string ProjectId
        {
            get
            {
                if (string.IsNullOrEmpty(projectId))
                {
                     if (Services?.RuntimeSettings is { } serviceRuntimeSettings)
                     {
                         projectId = serviceRuntimeSettings.ProjectID;
                     }
    #if UNITY
                     else if (RuntimeSettings.Instance)
                     {
                         projectId = RuntimeSettings.Instance.ProjectID;
                     }
    #endif
                }

                return projectId;
            }
        }

        private CloudService Services
        {
            get
            {
                if (services is null && getServices is not null)
                {
                    services = getServices();
                }

                return services;
            }
        }

        public NewPlayerAccountProvider(Func<CloudService> getServices, CloudUniqueId uniqueId = default, IRuntimeSettings runtimeSettings = null)
        {
            projectId = runtimeSettings?.ProjectID;
            this.uniqueId = uniqueId;
            this.getServices = getServices;
        }

        public NewPlayerAccountProvider(CloudService services, CloudUniqueId uniqueId = default, IRuntimeSettings runtimeSettings = null)
        {
            projectId = runtimeSettings?.ProjectID;
            this.uniqueId = uniqueId;
            this.services = services;
        }

        public NewPlayerAccountProvider(CloudUniqueId uniqueId = default, IRuntimeSettings runtimeSettings = null) : this(default(CloudService), uniqueId, runtimeSettings) { }

        public NewPlayerAccountProvider(string projectId, CloudUniqueId uniqueId)
        {
            this.projectId = projectId;
            this.uniqueId = uniqueId;
            getServices = null;
        }

        public NewPlayerAccountProvider(Func<CloudService> getServices, string projectId)
        {
            this.projectId = projectId;
            this.getServices = getServices;
        }

        public PlayerAccount GetPlayerAccount(LoginInfo loginInfo)
        {
            if (PlayerAccount.Find(loginInfo) is { } existingPlayerAccount)
            {
                return existingPlayerAccount;
            }

            if (!IsReady)
            {
                Log.GetLogger<AuthClient>().Warning(Warning.PlayerAccountProviderNotReady, $"{nameof(NewPlayerAccountProvider)}.{nameof(GetPlayerAccount)} was called with {nameof(IsReady)} still false. A new playerAccount will be created with null {nameof(PlayerAccount.Services)}.");
            }

            var newPlayerAccount = new PlayerAccount(loginInfo, CloudUniqueId, ProjectId, Services);
            createdPlayerAccounts.Add(newPlayerAccount);
            PlayerAccount.Register(newPlayerAccount);
            uniqueId = newPlayerAccount.CloudUniqueId;
            newPlayerAccount.AddLoginInfo(loginInfo);
            return newPlayerAccount;
        }

        public void Dispose()
        {
            if (shouldReleaseGuid)
            {
                CloudUniqueIdPool.Release(ProjectId, uniqueId);
                shouldReleaseGuid = false;
            }

            foreach (var playerAccount in createdPlayerAccounts)
            {
                playerAccount.Dispose();
            }
            createdPlayerAccounts.Clear();
        }
    }
}
