// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;

    /// <summary>
    /// Can be used to <see cref="Build"/> a <see cref="CloudStorage"/> that uses mock <see cref="IRequestFactory"/>
    /// and <see cref="IAuthClientInternal"/> objects for use in a test.
    /// </summary>
    internal sealed class FakeCloudStorageBuilder
    {
        private MockRequestFactoryBuilder requestFactoryBuilder;
        private MockAuthClientBuilder authClient;
        private CloudStorage cloudStorage;
        private Func<CloudStorage, StorageOperationQueue> operationQueueFactory;

        public MockRequestFactoryBuilder RequestFactory => requestFactoryBuilder;
        public MockAuthClientBuilder AuthClient => authClient;

        public FakeCloudStorageBuilder()
        {
            requestFactoryBuilder = new();
            authClient = new();
        }

        public FakeCloudStorageBuilder WithStorageOperationQueue(Func<CloudStorage, StorageOperationQueue> operationQueueFactory)
        {
            this.operationQueueFactory = operationQueueFactory;
            return this;
        }

        public CloudStorage Build()
        {
            if (cloudStorage is null)
            {
                var requestFactory = requestFactoryBuilder.Build();
                cloudStorage = new (requestFactory, authClient.Build(), requestFactory.Throttle, operationQueueFactory);
            }

            return cloudStorage;
        }

        public static implicit operator CloudStorage(FakeCloudStorageBuilder builder) => builder.Build();
    }
}
