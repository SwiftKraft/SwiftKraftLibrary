// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Threading.Tasks;

    public class CloudCredentialsPair
    {
        public IAuthClient AuthClient;
        public IRequestFactory RequestFactory;

        internal readonly IAuthClientInternal authClient;
        internal readonly IRequestFactoryInternal requestFactory;

        public CloudCredentialsPair(AuthClient authClient, RequestFactory requestFactory)
        {
            AuthClient = authClient;
            this.authClient = authClient;
            RequestFactory = requestFactory;
            this.requestFactory = requestFactory;
        }

        internal CloudCredentialsPair(IAuthClientInternal authClient, IRequestFactoryInternal requestFactory)
        {
            AuthClient = authClient;
            this.authClient = authClient;
            RequestFactory = requestFactory;
            this.requestFactory = requestFactory;
        }

        public static void Dispose(IAuthClient authClient, IRequestFactory requestFactory)
        {
            if (authClient is IDisposable authClientDisposable)
            {
                authClientDisposable.Dispose();
            }

            if (requestFactory is IDisposable requestFactoryDisposable)
            {
                requestFactoryDisposable.Dispose();
            }
        }

        public static async ValueTask DisposeAsync(IAuthClient authClient, IRequestFactory requestFactory)
        {
            if (authClient is IDisposable authClientDisposable)
            {
                authClientDisposable.Dispose();
            }

            if (requestFactory is IAsyncDisposable requestFactoryAsyncDisposable)
            {
                await requestFactoryAsyncDisposable.DisposeAsync();
            }
            else if (requestFactory is IDisposable requestFactoryDisposable)
            {
                requestFactoryDisposable.Dispose();
            }
        }
    }
}
