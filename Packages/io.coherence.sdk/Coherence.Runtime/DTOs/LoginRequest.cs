// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#pragma warning disable 0649

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

    internal struct GuestLoginRequest
    {
        [JsonProperty("guest_id")]
        public string GuestId;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct PasswordLoginRequest
    {
        [JsonProperty("username")]
        public string Username;

        [JsonProperty("password")]
        public string Password;

        [JsonProperty("autosignup")]
        public bool Autosignup;
    }

    internal struct SteamLoginRequest
    {
        [JsonProperty("ticket")]
        public string Ticket;

        [JsonProperty("identity")]
        public string Identity;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct EpicGamesLoginRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct PlayStationLoginRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct XboxLoginRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct NintendoLoginRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }

    internal struct OneTimeCodeLoginRequest
    {
        [JsonProperty("code")]
        public string Code;
    }

    internal struct JwtLoginRequest
    {
        [JsonProperty("token")]
        public string Token;

        [JsonProperty("autosignup")]
        public bool AutoSignup;
    }
}
