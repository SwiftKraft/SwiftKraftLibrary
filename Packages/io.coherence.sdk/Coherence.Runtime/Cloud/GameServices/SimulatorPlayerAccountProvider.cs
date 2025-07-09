// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;

    internal sealed class SimulatorPlayerAccountProvider : IPlayerAccountProvider
    {
        private static readonly CloudUniqueId SimulatorInCloudUniqueId = new("SimulatorInCloud");

        private CloudService services;
        private readonly Func<CloudService> getServices;
        private string projectId;

        // If CloudService is initialized lazily, wait for it to become available.
        public bool IsReady => Services is not null;

        public string ProjectId
        {
            get
            {
                if (string.IsNullOrEmpty(projectId) && Services is { } services)
                {
                    projectId = services.RuntimeSettings.ProjectID;
                }

                return projectId;
            }
        }

        public CloudUniqueId CloudUniqueId => SimulatorInCloudUniqueId;

        private CloudService Services => services ??= getServices();

        public SimulatorPlayerAccountProvider(Func<CloudService> getServices) => this.getServices = getServices;
        public SimulatorPlayerAccountProvider(CloudService services) => this.services = services;

        public PlayerAccount GetPlayerAccount(LoginInfo loginInfo)
        {
            if (PlayerAccount.Find(loginInfo) is { } existingPlayerAccount)
            {
                existingPlayerAccount.CloudUniqueId = SimulatorInCloudUniqueId;
                if (ProjectId is { Length: > 0 } projectId)
                {
                    existingPlayerAccount.projectId = projectId;
                }

                existingPlayerAccount.Services ??= Services;
                return existingPlayerAccount;
            }

            var newPlayerAccount = new PlayerAccount(loginInfo, CloudUniqueId, ProjectId, Services);
            PlayerAccount.Register(newPlayerAccount);
            return newPlayerAccount;
        }

        public void Dispose() { }
    }
}
