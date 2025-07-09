// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#pragma warning disable 0649

namespace Coherence.Runtime
{
    using Cloud;
    using Newtonsoft.Json;

    internal interface IPlayerAccountOperationRequest
    {
        private const string GetAccountInfoMethod = "GET";
        private const string GetOneTimeCodeMethod = "POST";
        private const string SetMethod = "POST";
        private const string LinkMethod = "POST";
        private const string UnlinkMethod = "DELETE";
        private const string RemoveMethod = "DELETE";

        public static PlayerAccountOperationInfo<SetDisplayInfoRequest> SetDisplayInfo(string displayName, string imageUrl) => new(PlayerAccountOperationType.SetDisplayInfo, "/account/display", SetMethod, new()
        {
            DisplayName = displayName,
            AvatarUrl = imageUrl
        });

        public static PlayerAccountOperationInfo<SetDisplayInfoRequest> RemoveDisplayInfo() => new(PlayerAccountOperationType.SetDisplayInfo, "/account/display", RemoveMethod, new()
        {
            DisplayName = "",
            AvatarUrl = ""
        });

        public static PlayerAccountOperationInfo<GetAccountInfoRequest> GetAccountInfo() => new(PlayerAccountOperationType.GetAccountInfo, "/account/info", GetAccountInfoMethod, new());

        public static PlayerAccountOperationInfo<GetOneTimeCodeRequest> GetOneTimeCode() => new(PlayerAccountOperationType.GetOneTimeCode, "/link/code", GetOneTimeCodeMethod, new());

        public static PlayerAccountOperationInfo<SetUsernameRequest> SetUsername(string username, string password, bool force) => new(PlayerAccountOperationType.SetUsername, "/link/userpass", SetMethod, new()
        {
            Username = username,
            Password = password,
            Force = force
        });

        public static PlayerAccountOperationInfo<SetUsernameRequest> RemoveUsername(bool force) => new(PlayerAccountOperationType.SetUsername, "/link/userpass", RemoveMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<SetEmailRequest> SetEmail(string email) => new(PlayerAccountOperationType.SetEmail, "/account/email", SetMethod, new() { Email = email });

        public static PlayerAccountOperationInfo<SetEmailRequest> RemoveEmail() => new(PlayerAccountOperationType.SetEmail, "/account/email", RemoveMethod, new() { Email = "" });

        public static PlayerAccountOperationInfo<LinkSteamRequest> LinkSteam(string ticket, string identity, bool force) => new(PlayerAccountOperationType.LinkSteam, "/link/steam", LinkMethod, new()
        {
            Ticket = ticket,
            Identity = identity,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkSteamRequest> UnlinkSteam(bool force) => new(PlayerAccountOperationType.LinkSteam, "/link/steam", UnlinkMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<LinkXboxRequest> LinkEpicGames(string token, bool force) => new(PlayerAccountOperationType.LinkEpicGames, "/link/epic", LinkMethod, new()
        {
            Token = token,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkXboxRequest> UnlinkEpicGames(bool force) => new(PlayerAccountOperationType.LinkEpicGames, "/link/epic", UnlinkMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<LinkPlayStationRequest> LinkPlayStation(string token, bool force) => new(PlayerAccountOperationType.LinkPlayStation, "/link/psn", LinkMethod, new()
        {
            Token = token,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkPlayStationRequest> UnlinkPlayStation(bool force) => new(PlayerAccountOperationType.LinkPlayStation, "/link/psn", UnlinkMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<LinkXboxRequest> LinkXbox(string token, bool force) => new(PlayerAccountOperationType.LinkXbox, "/link/xbox", LinkMethod, new()
        {
            Token = token,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkXboxRequest> UnlinkXbox(bool force) => new(PlayerAccountOperationType.LinkXbox, "/link/xbox", UnlinkMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<LinkNintendoRequest> LinkNintendo(string token, bool force) => new(PlayerAccountOperationType.LinkNintendo, "/link/nintendo", LinkMethod, new()
        {
            Token = token,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkNintendoRequest> UnlinkNintendo(bool force) => new(PlayerAccountOperationType.LinkNintendo, "/link/nintendo", UnlinkMethod, null, $"?force={force}");

        public static PlayerAccountOperationInfo<LinkGuestRequest> LinkGuest(string guestId, bool force) => new(PlayerAccountOperationType.LinkGuest, "/link/guest", LinkMethod, new()
        {
            GuestId = guestId,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkGuestRequest> UnlinkGuest(string guestId, bool force) => new(PlayerAccountOperationType.LinkGuest, "/link/guest", UnlinkMethod, null, $"?guest_id={guestId}&force={force}");

        public static PlayerAccountOperationInfo<LinkJwtRequest> LinkJwt(string token, bool force) => new(PlayerAccountOperationType.LinkJwt, "/link/jwt", LinkMethod, new()
        {
            Token = token,
            Force = force
        });

        public static PlayerAccountOperationInfo<LinkJwtRequest> UnlinkJwt(bool force) => new(PlayerAccountOperationType.LinkJwt, "/link/jwt", UnlinkMethod, null, $"?force={force}");
    }

    internal struct SetUsernameRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("username")]
        public string Username;

        [JsonProperty("password")]
        public string Password;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct SetDisplayInfoRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("display_name")]
        public string DisplayName;

        [JsonProperty("avatar_url")]
        public string AvatarUrl;
    }

    internal struct GetAccountInfoRequest : IPlayerAccountOperationRequest { }

    internal struct GetOneTimeCodeRequest : IPlayerAccountOperationRequest { }

    internal struct SetEmailRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("email")]
        public string Email;
    }

    internal struct LinkSteamRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("ticket")]
        public string Ticket;

        [JsonProperty("identity")]
        public string Identity;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkEpicGamesRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkPlayStationRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkXboxRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkNintendoRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkGuestRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("guest_id")]
        public string GuestId;

        [JsonProperty("force")]
        public bool Force;
    }

    internal struct LinkJwtRequest : IPlayerAccountOperationRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("force")]
        public bool Force;
    }
}
