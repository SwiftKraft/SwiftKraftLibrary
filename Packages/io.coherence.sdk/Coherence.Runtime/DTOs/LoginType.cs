// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    // Using bit field compatible member values just in case, for future-proofing.
    using static Coherence.Utils.FlagsValues;

    internal enum LoginType
    {
        Guest = _1,

        /// <summary>
        /// Username and password.
        /// </summary>
        Password = _2,
        SessionToken = _3,
        OneTimeCode = _4,

        /// <summary>
        /// JSON Web Token.
        /// </summary>
        Jwt = _5,
        Steam = _6,
        EpicGames = _7,
        PlayStation = _8,
        Xbox = _9,
        Nintendo = _10,

        LegacyGuest = _32,
    }

    internal static class LoginTypeExtensions
    {
#pragma warning disable CS8524
        internal static string GetBasePath(this LoginType loginType) => loginType switch
#pragma warning restore CS8524
        {
            LoginType.Steam => "/login/steam",
            LoginType.EpicGames => "/login/epic",
            LoginType.PlayStation => "/login/psn",
            LoginType.Xbox => "/login/xbox",
            LoginType.Nintendo => "/login/nintendo",
            LoginType.SessionToken => "/session",
            LoginType.Password => "/account",
            LoginType.LegacyGuest => "/account",
            LoginType.Guest => "/login/guest",
            LoginType.OneTimeCode => "/login/code",
            LoginType.Jwt => "/login/jwt",
        };
    }
}
