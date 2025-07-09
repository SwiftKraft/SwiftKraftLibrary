// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;

    /// <summary>
    /// Responsible for providing <see cref="PlayerAccount">player accounts</see> upon request.
    /// <remarks>
    /// The same <see cref="PlayerAccount"/> should always be returned for the same login info.
    /// </remarks>
    /// </summary>
    internal interface IPlayerAccountProvider : IDisposable
    {
        bool IsReady => true;
        string ProjectId { get; }
        CloudUniqueId CloudUniqueId { get; }
        PlayerAccount GetPlayerAccount(LoginInfo loginInfo);
    }
}
