// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;

    /// <summary>
    /// <para>
    /// Can be used to <see cref="Build"/> a fake <see cref="PlayerAccount"/> object for use in a test.
    /// </para>
    /// <para>
    /// <see cref="PlayerAccount.Services"/> are faked using <see cref="CloudServiceBuilder"/>.
    /// </para>
    /// </summary>
    internal sealed class FakePlayerAccountBuilder : IDisposable
    {
        private LoginInfo? loginInfo;
        private PlayerAccount playerAccount;
        private bool buildExecuted;

        public FakeCloudServiceBuilder CloudServiceBuilder { get; } = new();
        public PlayerAccount PlayerAccount => Build();

        public FakePlayerAccountBuilder SetUniqueId(CloudUniqueId uniqueId)
        {
            CloudServiceBuilder.SetUniqueId(uniqueId);
            return this;
        }

        public FakePlayerAccountBuilder SetProjectId(string projectId)
        {
            CloudServiceBuilder.SetProjectId(projectId);
            return this;
        }

        public FakePlayerAccountBuilder SetLoginInfo(LoginInfo loginInfo)
        {
            this.loginInfo = loginInfo;
            CloudServiceBuilder.PlayerAccountProviderBuilder.SetLoginInfo(loginInfo);
            return this;
        }

        public FakePlayerAccountBuilder SetupCloudService(Action<FakeCloudServiceBuilder> setupCloudServiceBuilder)
        {
            setupCloudServiceBuilder(CloudServiceBuilder);
            return this;
        }

        public PlayerAccount Build()
        {
            if (buildExecuted)
            {
                return playerAccount ?? throw new NullReferenceException($"{GetType().Name}.Build was called again while previous Build execution is still in progress!");
            }

            buildExecuted = true;
            playerAccount = CloudServiceBuilder.PlayerAccountProviderBuilder.PlayerAccountProvider.GetPlayerAccount(loginInfo ??= CloudServiceBuilder.PlayerAccountProviderBuilder.LoginInfo);
            return playerAccount;
        }

        public FakePlayerAccountBuilder Build(out PlayerAccount playerAccount)
        {
            playerAccount = Build();
            return this;
        }

        public void Dispose()
        {
            playerAccount?.Dispose();
            playerAccount = null;
            buildExecuted = false;
            CloudServiceBuilder.Dispose();
        }

        public static implicit operator PlayerAccount(FakePlayerAccountBuilder builder) => builder.Build();
    }
}
