// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using Common;
    using Moq;

    /// <summary>
    /// Can be used to <see cref="Build"/> a mock <see cref="IVersionInfo"/> object for use in a test.
    /// </summary>
    internal sealed class MockVersionInfoBuilder
    {
        private string sdk = "Sdk";
        private string sdkRevisionHash = "SdkRevisionHash";
        private string sdkRevisionOrVersion = "SdkRevisionOrVersion";
        private string engine = "Engine";
        private string docsSlug = "DocsSlug";
        private bool useUnpublishedDocsUrl = false;
        private IVersionInfo versionInfo;

        public Mock<IVersionInfo> Mock { get; } = new(MockBehavior.Strict);
        public IVersionInfo VersionInfo => Build();

        public MockVersionInfoBuilder SetSdk(string sdk)
        {
            this.sdk = sdk;
            return this;
        }

        public MockVersionInfoBuilder SetSdkRevisionHash(string sdkRevisionHash)
        {
            this.sdkRevisionHash = sdkRevisionHash;
            return this;
        }

        public MockVersionInfoBuilder SetSdkRevisionOrVersion(string sdkRevisionOrVersion)
        {
            this.sdkRevisionOrVersion = sdkRevisionOrVersion;
            return this;
        }

        public MockVersionInfoBuilder SetEngine(string engine)
        {
            this.engine = engine;
            return this;
        }

        public MockVersionInfoBuilder SetDocsSlug(string docsSlug)
        {
            this.docsSlug = docsSlug;
            return this;
        }

        public MockVersionInfoBuilder SetUseUnpublishedDocsUrl(bool useUnpublishedDocsUrl)
        {
            this.useUnpublishedDocsUrl = useUnpublishedDocsUrl;
            return this;
        }

        public IVersionInfo Build()
        {
            if (versionInfo is not null)
            {
                return versionInfo;
            }

            Mock.Setup(info => info.Sdk).Returns(sdk);
            Mock.Setup(info => info.SdkRevisionHash).Returns(sdkRevisionHash);
            Mock.Setup(info => info.SdkRevisionOrVersion).Returns(sdkRevisionOrVersion);
            Mock.Setup(info => info.Engine).Returns(engine);
            Mock.Setup(info => info.DocsSlug).Returns(docsSlug);
            versionInfo = Mock.Object;
            return versionInfo;
        }
    }
}
