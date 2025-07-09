// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit.Tests
{
    using Connection;
    using Moq;

    /// <summary>
    /// Extension methods for <see cref="Mock{IClient}"/> to help with testing.
    /// </summary>
    internal static class MockClientExtensions
    {
        /// <summary>
        /// Raise the <see cref="IClient.OnDisconnected"/> event.
        /// </summary>
        public static void OnDisconnected(this Mock<IClient> mockClient, ConnectionCloseReason reason = ConnectionCloseReason.Unknown) => mockClient.Raise(client => client.OnDisconnected += null, reason);
    }
}
