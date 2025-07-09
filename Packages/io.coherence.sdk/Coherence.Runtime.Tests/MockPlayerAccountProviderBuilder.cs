// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using Cloud;
    using Moq;

    /// <summary>
    /// Can be used to <see cref="Build"/> a mock <see cref="IPlayerAccountProvider"/> object for use in a test.
    /// </summary>
    internal sealed class MockPlayerAccountProviderBuilder
    {
        private bool isReady = true;
        private string projectId = "ProjectId";
        private CloudUniqueId uniqueId;
        private IPlayerAccountProvider playerAccountProvider;
        private LoginInfo? loginInfo;
        private readonly Dictionary<LoginInfo, PlayerAccount> playerAccounts = new();
        private bool buildExecuted;

        public Mock<IPlayerAccountProvider> Mock { get; } = new();

        private CloudUniqueId UniqueId => uniqueId != CloudUniqueId.None ? uniqueId : (uniqueId = new(Guid.NewGuid().ToString("N")));
        public LoginInfo LoginInfo => loginInfo ??= LoginInfo.ForGuest(PlayerAccountProvider, false);
        public IPlayerAccountProvider PlayerAccountProvider => Build();
        public FakeCloudServiceBuilder ServicesBuilder { get; set; }

        public MockPlayerAccountProviderBuilder SetIsReady(bool isReady)
        {
            this.isReady = isReady;
            return this;
        }

        public MockPlayerAccountProviderBuilder SetUniqueId(CloudUniqueId uniqueId)
        {
            this.uniqueId = uniqueId;
            return this;
        }

        public MockPlayerAccountProviderBuilder SetProjectId(string projectId)
        {
            this.projectId = projectId;
            return this;
        }

        public MockPlayerAccountProviderBuilder SetLoginInfo(LoginInfo loginInfo)
        {
            this.loginInfo = loginInfo;
            return this;
        }

        public MockPlayerAccountProviderBuilder SetServicesBuilder(FakeCloudServiceBuilder servicesBuilder)
        {
            ServicesBuilder = servicesBuilder;
            return this;
        }

        public IPlayerAccountProvider Build()
        {
            if (buildExecuted)
            {
                return playerAccountProvider ?? throw new NullReferenceException($"{GetType().Name}.Build was called again while previous Build execution is still in progress!");
            }

            buildExecuted = true;
            Mock.Setup(provider => provider.IsReady).Returns(isReady);
            Mock.Setup(provider => provider.ProjectId).Returns(projectId);
            Mock.Setup(provider => provider.CloudUniqueId).Returns(UniqueId);
            Mock.Setup(provider => provider.GetPlayerAccount(It.IsAny<LoginInfo>())).Returns((Func<LoginInfo, PlayerAccount>)GetOrCreatePlayerAccount);
            playerAccountProvider = Mock.Object;
            return playerAccountProvider;

            PlayerAccount GetOrCreatePlayerAccount(LoginInfo info)
            {
                if (!playerAccounts.TryGetValue(info, out var playerAccount))
                {
                    playerAccount = new(loginInfo ?? info, UniqueId, projectId, null);
                    playerAccounts.Add(info, playerAccount);
                    playerAccount.Services = ServicesBuilder.CloudService;
                }

                return playerAccount;
            }
        }

        public static implicit operator Mock<IPlayerAccountProvider>(MockPlayerAccountProviderBuilder builder)
        {
            builder.Build();
            return builder.Mock;
        }
    }
}
