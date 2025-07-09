// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using JWT.Algorithms;
    using JWT.Builder;
    using System;

    public static class AuthToken
    {
        /// <summary>
        ///     This secret is used during local development only! There is no security threat with this value leaking
        ///     anywhere.
        /// </summary>
        public const string LocalDevelopmentSecret = "local-development";

        /// <summary>
        ///     Creates an authentication token with a random User ID that can be used for authentication when connecting to a
        ///     local (development) replication server.
        /// </summary>
        /// <remarks>DO NOT USE IN PRODUCTION - AUTHENTICATION WILL FAIL</remarks>
        public static string ForLocalDevelopment(ConnectionType connectionType)
        {
            return ForLocalDevelopment(Guid.NewGuid().ToString("N"), connectionType);
        }

        /// <summary>
        ///     Creates an authentication token for a given user that can be used for authentication when connecting to a local
        ///     (development) replication server.
        /// </summary>
        /// <remarks>DO NOT USE IN PRODUCTION - AUTHENTICATION WILL FAIL</remarks>
        public static string ForLocalDevelopment(string playUserId, ConnectionType connectionType)
        {
            return Custom(playUserId, connectionType, LocalDevelopmentSecret);
        }

        /// <summary>
        ///     Creates a custom authentication token for a given user that can be used for authentication when
        ///     connecting to a self-hosted replication server. Secret must match the secret passed to the
        ///     replication server executable.
        /// </summary>
        public static string Custom(string playUserId, ConnectionType connectionType, string secret)
        {
            return JwtBuilder.Create()
#pragma warning disable CS0618
                .WithAlgorithm(new HMACSHA256Algorithm())
#pragma warning restore CS0618
                .WithSecret(secret)
                .AddClaim("sub", playUserId)
                .AddClaim("pid", "local")
                .AddClaim("type", connectionType.ToString().ToLower())
                .Encode();
        }
    }
}
