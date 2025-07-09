// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    /// <summary>
    /// Specifies the types of <see cref="PlayerAccount"/>-related operations that can be performed.
    /// </summary>
    internal enum PlayerAccountOperationType
    {
        SetUsername,
        SetDisplayInfo,
        SetEmail,
        GetOneTimeCode,
        LinkSteam,
        LinkEpicGames,
        LinkPlayStation,
        LinkXbox,
        LinkNintendo,
        LinkJwt,
        LinkGuest,
        GetAccountInfo
    }
}
