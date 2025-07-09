// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using Runtime;
    using Prefs;

    /// <summary>
    /// Specifies information used to <see cref="AuthClient.Login"/> using <see cref="AuthClient"/>.
    /// </summary>
    internal readonly struct LoginInfo : IEquatable<LoginInfo>
    {
        private readonly string usernameGuestIdOrIdentity;
        private readonly string passwordTokenTicketOrCode;

        public LoginType LoginType { get; }
        public bool AutoSignup { get; }

        public string Username => LoginType is LoginType.Password or LoginType.LegacyGuest ? usernameGuestIdOrIdentity : "";
        public GuestId GuestId => LoginType is LoginType.Guest ? usernameGuestIdOrIdentity : default(GuestId);
        public string Password => LoginType is LoginType.Password or LoginType.LegacyGuest ? passwordTokenTicketOrCode : "";
        public string Token => LoginType is LoginType.PlayStation or LoginType.Xbox or LoginType.Nintendo or LoginType.Jwt or LoginType.EpicGames ? passwordTokenTicketOrCode : "";
        public string Ticket => LoginType is LoginType.Steam ? passwordTokenTicketOrCode : "";
        public string Identity => LoginType is LoginType.Steam ? usernameGuestIdOrIdentity : "";
        public SessionToken SessionToken => LoginType is LoginType.SessionToken ? new(passwordTokenTicketOrCode) : SessionToken.None;
        public string OneTimeCode => LoginType is LoginType.OneTimeCode ? passwordTokenTicketOrCode : "";
        public bool IsGuest => LoginType is LoginType.Guest or LoginType.LegacyGuest;

        private LoginInfo(LoginType loginType, string usernameGuestIdOrIdentity, string passwordTokenTicketOrCode, bool autoSignup)
        {
            LoginType = loginType;
            AutoSignup = autoSignup;
            this.usernameGuestIdOrIdentity = usernameGuestIdOrIdentity ?? "";
            this.passwordTokenTicketOrCode = passwordTokenTicketOrCode ?? "";
        }

        public static LoginInfo WithPassword(string username, string password, bool autoSignup) => new(LoginType.Password, username, password, autoSignup);
        public static LoginInfo WithSteam(string ticket, string identity) => new(LoginType.Steam, identity, ticket, true);
        public static LoginInfo WithEpicGames(string token) => new(LoginType.EpicGames, "", token, true);
        public static LoginInfo WithPlayStation(string token) => new(LoginType.PlayStation, "", token, true);
        public static LoginInfo WithXbox(string token) => new(LoginType.Xbox, "", token, true);
        public static LoginInfo WithNintendo(string token) => new(LoginType.Nintendo, "", token, true);
        public static LoginInfo WithJwt(string token) => new(LoginType.Jwt, "", token, true);
        public static LoginInfo WithSessionToken(SessionToken sessionToken) => new(LoginType.SessionToken, "", sessionToken, true);
        public static LoginInfo WithOneTimeCode(string code) => new(LoginType.OneTimeCode, "", code, false);
        public static LoginInfo ForGuest(IPlayerAccountProvider playerAccountProvider, bool preferLegacyLoginData) => ForGuest(playerAccountProvider.ProjectId, playerAccountProvider.CloudUniqueId, preferLegacyLoginData);
        internal static LoginInfo ForGuest(GuestId guestId) => new(LoginType.Guest, guestId, "", true);

        internal static LoginInfo ForGuest(string projectId, CloudUniqueId uniqueId, bool preferLegacyLoginData)
        {
            if (preferLegacyLoginData)
            {
                var usernamePrefsKey = LegacyLoginData.GetUsernamePrefsKey(projectId, uniqueId);
                var passwordPrefsKey = LegacyLoginData.GetGuestPasswordPrefsKey(projectId, uniqueId);
                if (Prefs.GetString(usernamePrefsKey, null) is { Length: > 0 } username
                    && Prefs.GetString(passwordPrefsKey, null) is { Length: > 0 } password)
                {
                    return new(LoginType.LegacyGuest, username, password, true);
                }
            }

            return new(LoginType.Guest, GuestId.GetOrCreate(projectId, uniqueId), "", true);
        }

        public static bool operator ==(LoginInfo x, LoginInfo y) => x.Equals(y);
        public static bool operator !=(LoginInfo x, LoginInfo y) => !x.Equals(y);

        public bool Equals(LoginInfo other)
        {
            if (LoginType == other.LoginType)
            {
                return string.Equals(usernameGuestIdOrIdentity, other.usernameGuestIdOrIdentity)
                       && string.Equals(passwordTokenTicketOrCode, other.passwordTokenTicketOrCode);
            }

            if (LoginType is LoginType.LegacyGuest && other.LoginType is LoginType.Guest)
            {
                return GuestId.FromLegacyLoginData(Username, Password).Equals(other.GuestId);
            }

            if (LoginType is LoginType.Guest && other.LoginType is LoginType.LegacyGuest)
            {
                return GuestId.FromLegacyLoginData(other.Username, other.Password).Equals(GuestId);
            }

            return false;
        }

        public override bool Equals(object obj) => obj is LoginInfo other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(usernameGuestIdOrIdentity, passwordTokenTicketOrCode, (int)LoginType);

#pragma warning disable CS8524
        public override string ToString() => LoginType + ": { " + LoginType switch
#pragma warning restore CS8524
        {
            LoginType.LegacyGuest => $"Username:\"{Username}\", Password:\"{Password}\"",
            LoginType.Guest => $"GuestId:\"{GuestId}\"",
            LoginType.Password => $"Username:\"{Username}\", Password:\"{Password}\"",
            LoginType.SessionToken => $"SessionToken:\"{SessionToken}\"",
            LoginType.OneTimeCode => $"OneTimeCode:\"{OneTimeCode}\"",
            LoginType.Jwt => $"Token:\"{Token}\"",
            LoginType.Steam => $"Ticket:\"{Ticket}\", Identity:\"{Identity}\"",
            LoginType.EpicGames => $"Token:\"{Token}\"",
            LoginType.PlayStation => $"Token:\"{Token}\"",
            LoginType.Xbox => $"Token:\"{Token}\"",
            LoginType.Nintendo => $"Token:\"{Token}\"",
        } + " }";
    }
}
