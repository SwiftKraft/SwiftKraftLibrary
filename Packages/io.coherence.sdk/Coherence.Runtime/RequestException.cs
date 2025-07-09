// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Runtime
{
    using Log;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using Newtonsoft.Json.Serialization;
#if UNITY
    using UnityEngine.Scripting;

    // This makes sure that the StringEnumConverter doesn't get stripped.
    [Preserve]
    internal static class JsonPreserve
    {
        [Preserve]
        internal static StringEnumConverter _ => new StringEnumConverter();
    }
#endif

    public class RequestException : Exception
    {
        public HttpStatusCode HttpStatusCode { get; }
        public int StatusCode => (int)HttpStatusCode;
        public ErrorCode ErrorCode { get; }
        public string UserMessage { get; }

        private readonly bool isGenericError;

        public RequestException(HttpStatusCode statusCode, string userMessage) : base(userMessage)
        {
            if (string.IsNullOrEmpty(userMessage))
            {
                throw new ArgumentException("Invalid argument", nameof(userMessage));
            }

            isGenericError = true;
            HttpStatusCode = statusCode;
        }

        public RequestException(ErrorCode errorCode, HttpStatusCode statusCode = default, string userMessage = null)
        {
            ErrorCode = errorCode;
            HttpStatusCode = statusCode;
            UserMessage = userMessage;
        }

        public RequestException(int statusCode, string userMessage) : this((HttpStatusCode)statusCode, userMessage) { }
        public RequestException(ErrorCode errorCode, int statusCode, string userMessage = null) : this(errorCode, (HttpStatusCode)statusCode, userMessage) { }

        public override string Message
        {
            get
            {
                if (isGenericError)
                {
                    var statusCode = HttpStatusCode.ToString();
                    var statusCodeInteger = StatusCode.ToString();
                    if (!string.Equals(statusCode, statusCodeInteger))
                    {
                        statusCode = $"{statusCode} ({statusCodeInteger})";
                    }

                    return $"{base.Message}, StatusCode: {statusCode}";
                }

                return ErrorCode switch
                {
                    ErrorCode.InvalidCredentials => "Invalid authentication credentials, please relog using the Authentication Service.",
                    ErrorCode.TooManyRequests => "Too many requests. Please try again in a moment.",
                    ErrorCode.ProjectNotFound => "Project not found. Please check that the runtime key is properly setup.",
                    ErrorCode.SchemaNotFound => GetSchemaNotFoundMessage(),
                    ErrorCode.RSVersionNotFound => "Replication server version not found. Please check that the version of the replication server is valid.",
                    ErrorCode.SimNotFound => "Simulator not found. Please check that the slug and the schema are valid and that the simulator has been uploaded.",
                    ErrorCode.MultiSimNotListening => "The multi-room simulator used for this room is not listening on the required ports. Please check your multi-room sim setup.",
                    ErrorCode.RoomsSimulatorsNotEnabled => "Simulator not enabled. Please make sure that simulators are enabled in the coherence Dashboard.",
                    ErrorCode.RoomsSimulatorsNotUploaded => "Simulator not uploaded. You can use the coherence Hub to build and upload Simulators.",
                    ErrorCode.RoomsVersionNotFound => "Version not found. Please make sure that client uses the correct 'sim-slug'.",
                    ErrorCode.RoomsSchemaNotFound => GetSchemaNotFoundMessage(),
                    ErrorCode.RoomsRegionNotFound => "Region not found. Please make sure that the selected region is enabled in the Dev Portal.",
                    ErrorCode.RoomsInvalidTagOrKeyValueEntry => "Validation of tag and key/value entries failed. Please check if number and size of entries is within limits.",
                    ErrorCode.RoomsCCULimit => "Room ccu limit for project exceeded.",
                    ErrorCode.RoomsNotFound => "Room not found. Please refresh room list.",
                    ErrorCode.RoomsInvalidSecret => "Invalid room secret. Please make sure that the secret matches the one received on room creation.",
                    ErrorCode.RoomsInvalidMaxPlayers => "Room Max Players must be a value between 1 and the upper limit configured on the project dashboard.",
                    ErrorCode.InvalidMatchMakingConfig => "Invalid matchmaking configuration. Please make sure that the matchmaking feature was properly configured in the Dev Portal.",
                    ErrorCode.ClientPermission => "The client has been restricted from accessing this feature. Please check the game services settings on the Dev Portal.",
                    ErrorCode.CreditLimit => "Monthly credit limit exceeded. Please check your organization credit usage in the Dev Portal.",
                    ErrorCode.InDeployment => "One or more online resources are currently being provisioned. Please retry the request.",
                    ErrorCode.FeatureDisabled => "Requested feature is disabled, make sure you enable it in the Game Services section of your coherence Dashboard.",
                    ErrorCode.InvalidRoomLimit => "Room max players limit must be between 1 and 100.",
                    ErrorCode.LobbyInvalidAttribute => "A specified Attribute is invalid.",
                    ErrorCode.LobbyNameTooLong => "Lobby name must be shorter than 64 characters.",
                    ErrorCode.LobbyTagTooLong => "Lobby tag must be shorter than 16 characters.",
                    ErrorCode.LobbyNotFound => "Requested Lobby wasn't found.",
                    ErrorCode.LobbyAttributeSizeLimit => "A specified Attribute has surpassed the allowed limits. " +
                                                         "Lobby limit: 2048. Player limit: 256. Attribute size is calculated off key length + value length of all attributes combined.",
                    ErrorCode.LobbyNameAlreadyExists => "A lobby with this name already exists.",
                    ErrorCode.LobbyRegionNotFound => "Specified region for this Lobby wasn't found.",
                    ErrorCode.LobbyInvalidSecret => "Invalid secret specified for lobby.",
                    ErrorCode.LobbyFull => "This lobby is currently full.",
                    ErrorCode.LobbyActionNotAllowed => "You're not allowed to perform this action on the lobby.",
                    ErrorCode.LobbyInvalidFilter => "The provided filter is invalid. You can use Filter.ToString to debug the built filter you're sending.",
                    ErrorCode.LobbyNotCompatible => GetSchemaNotFoundMessage(),
                    ErrorCode.LobbySimulatorNotEnabled => "Simulator not enabled. Please make sure that simulators are enabled in the coherence Dashboard.",
                    ErrorCode.LobbySimulatorNotUploaded => "Simulator not uploaded. You can use the coherence Hub to build and upload Simulators.",
                    ErrorCode.LobbyLimit => "You cannot join more than three lobbies simultaneously.",
                    ErrorCode.LoginInvalidUsername => "Username given is invalid. Only alphanumeric, dashes and underscore characters are allowed. It must start with a letter and end with a letter/number. No double dash/underscore characters are allowed (-- or __).",
#if UNITY
                    ErrorCode.RestrictedModeCapReached => $"Total player capacity for restricted mode server reached. See the documentation for more information: {DocumentationLinks.GetDocsUrl(DocumentationKeys.UnlockToken)}",
#else
                    ErrorCode.RestrictedModeCapReached => $"Total player capacity for restricted mode server reached.",
#endif

                    ErrorCode.LoginDisabled => "This authentication method is disabled. You can enable it in the project settings of your coherence Dashboard.",
                    ErrorCode.LoginInvalidApp => "The provided App ID is invalid. Please check the App ID in the coherence Dashboard.",
                    ErrorCode.OneTimeCodeExpired => "The provided ticket has expired.",
                    ErrorCode.LoginNotFound => "No player account has been linked to the authentication method that was used. Pass an 'autoSignup' value of True to automatically create a new player account if one does not exist yet.",
                    ErrorCode.LoginInvalidPassword => "Password given is invalid. Password cannot be empty.",
                    _ => !string.IsNullOrEmpty(UserMessage) ? UserMessage : "Unknown error",
                };
            }
        }

        public override string ToString() => $"{GetType().Name} ({HttpStatusCode}): {Message}" +
                                             (!string.IsNullOrEmpty(UserMessage) ? $"\n{UserMessage}" : "") +
                                             (!string.IsNullOrEmpty(StackTrace) ? $"\n{StackTrace}" : "");

        private static string GetSchemaNotFoundMessage()
        {
#if UNITY
            return
                $"Schema {RuntimeSettings.Instance.SchemaID.Substring(0, 5)} not found for coherence Project {RuntimeSettings.Instance.ProjectName}. Please check if the latest schema version was uploaded. " +
                "There are options to upload schemas automatically when building a Unity Player, entering Play Mode or after baking in the coherence Hub > Baking > Auto Upload Schema section.";
#else
            return $"Schema not found. Please check if the latest schema version was uploaded.";
#endif
        }

        public static bool TryParse(string response, int statusCode, out RequestException requestException, Logger logger)
        {
            if (string.IsNullOrEmpty(response))
            {
                requestException = null;
                return false;
            }

            ErrorResponse errorResponse;
            try
            {
                errorResponse = Coherence.Utils.CoherenceJson.DeserializeObject<ErrorResponse>(response);
            }
            catch
            {
                logger.Error(Error.RuntimeRequestResponse, ("response", response));
                requestException = null;
                return false;
            }

            if (errorResponse.ErrorCode == ErrorCode.Unknown)
            {
                logger.Debug($"Unsupported error response: {response}");

                // Lack of both error code and user message is treated as parse failure.
                if (string.IsNullOrEmpty(errorResponse.Hint))
                {
                    requestException = null;
                    return false;
                }
            }

            requestException = new RequestException(errorResponse.ErrorCode, statusCode, errorResponse.Hint);
            return true;
        }
    }

    internal struct ErrorResponse
    {
        [JsonProperty("error_code")]
        public ErrorCode ErrorCode;

        [JsonProperty("hint")]
        public string Hint;

        [OnError]
        internal void OnError(StreamingContext _, ErrorContext errorContext)
        {
            // Most likely new error code. As long as we have the UserMessage we can still show it to the user.
            if (errorContext.Path == "error_code")
            {
                errorContext.Handled = true;
            }
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ErrorCode
    {
        Unknown = 0,

        [EnumMember(Value = "ERR_TOO_MANY_REQUESTS")]
        TooManyRequests,

        [EnumMember(Value = "ERR_PROJECT_NOT_FOUND")]
        ProjectNotFound,

        [EnumMember(Value = "ERR_SCHEMA_NOT_FOUND")]
        SchemaNotFound,

        [EnumMember(Value = "ERR_RS_VERSION_NOT_FOUND")]
        RSVersionNotFound,

        [EnumMember(Value = "ERR_SIM_NOT_FOUND")]
        SimNotFound,

        [EnumMember(Value = "ERR_INVALID_CREDENTIALS")]
        InvalidCredentials,

        [EnumMember(Value = "ERR_ROOMS_SIMULATORS_NOT_ENABLED")]
        RoomsSimulatorsNotEnabled,

        [EnumMember(Value = "ERR_ROOMS_SIMULATOR_NOT_UPLOADED")]
        RoomsSimulatorsNotUploaded,

        [EnumMember(Value = "ERR_ROOMS_VERSION_NOT_FOUND")]
        RoomsVersionNotFound,

        [EnumMember(Value = "ERR_ROOMS_SCHEMA_NOT_FOUND")]
        RoomsSchemaNotFound,

        [EnumMember(Value = "ERR_ROOMS_REGION_NOT_FOUND")]
        RoomsRegionNotFound,

        [EnumMember(Value = "ERR_ROOMS_INVALID_TAG_OR_KV")]
        RoomsInvalidTagOrKeyValueEntry,

        [EnumMember(Value = "ERR_ROOMS_CCU_LIMIT_EXCEEDED")]
        RoomsCCULimit,

        [EnumMember(Value = "ERR_ROOMS_NOT_FOUND")]
        RoomsNotFound,

        [EnumMember(Value = "ERR_ROOMS_INVALID_SECRET")]
        RoomsInvalidSecret,

        [EnumMember(Value = "ERR_ROOMS_INVALID_MAX_PLAYERS")]
        RoomsInvalidMaxPlayers,

        [EnumMember(Value = "ERR_INVALID_MM_CONFIG")]
        InvalidMatchMakingConfig,

        [EnumMember(Value = "ERR_CLIENT_PERMISSION")]
        ClientPermission,

        [EnumMember(Value = "ERR_CREDIT_LIMIT_EXCEEDED")]
        CreditLimit,

        [EnumMember(Value = "ERR_IN_DEPLOYMENT")]
        InDeployment,

        // Sent by the local replication server.
        [EnumMember(Value = "ERR_INVALID_ROOM_LIMIT")]
        InvalidRoomLimit,

        [EnumMember(Value = "ERR_NOT_ENABLED")]
        FeatureDisabled,

        [EnumMember(Value = "ERR_LOBBY_REGION_NOT_FOUND")]
        LobbyRegionNotFound,

        [EnumMember(Value = "ERR_LOBBY_NOT_FOUND")]
        LobbyNotFound,

        [EnumMember(Value = "ERR_LOBBY_ATTR_INVALID")]
        LobbyInvalidAttribute,

        [EnumMember(Value = "ERR_LOBBY_ATTR_SIZE_LIMIT")]
        LobbyAttributeSizeLimit,

        [EnumMember(Value = "ERR_LOBBY_NAME_ALREADY_EXISTS")]
        LobbyNameAlreadyExists,

        [EnumMember(Value = "ERR_LOBBY_NAME_TOO_LONG")]
        LobbyNameTooLong,

        [EnumMember(Value = "ERR_LOBBY_TAG_TOO_LONG")]
        LobbyTagTooLong,

        [EnumMember(Value = "ERR_LOBBY_INVALID_SECRET")]
        LobbyInvalidSecret,

        [EnumMember(Value = "ERR_LOBBY_FULL")]
        LobbyFull,

        [EnumMember(Value = "ERR_LOBBY_ACTION_NOT_ALLOWED")]
        LobbyActionNotAllowed,

        [EnumMember(Value = "ERR_LOBBY_INVALID_SEARCH_FILTER")]
        LobbyInvalidFilter,

        [EnumMember(Value = "ERR_LOBBY_NOT_COMPATIBLE")]
        LobbyNotCompatible,

        [EnumMember(Value = "ERR_LOBBY_SIMULATORS_NOT_ENABLED")]
        LobbySimulatorNotEnabled,

        [EnumMember(Value = "ERR_LOBBY_SIMULATOR_NOT_UPLOADED")]
        LobbySimulatorNotUploaded,

        [EnumMember(Value = "ERR_LOBBY_PLAYER_JOIN_LIMIT")]
        LobbyLimit,

        [EnumMember(Value = "ERR_INVALID_USER_NAME")]
        LoginInvalidUsername,

        [Obsolete("Use " + nameof(LoginInvalidPassword) +" instead.")]
        [Deprecated("3/2025", 1, 6, 0, Reason="Replaced by LoginInvalidPassword.")]
        [EnumMember(Value = "ERR_WEAK_PASSWORD")]
        LoginWeakPassword,

        [EnumMember(Value = "ERR_RESTRICTED_MODE_CAP_REACHED")]
        RestrictedModeCapReached,

        [EnumMember(Value = "ERR_MULTI_SIM_NOT_LISTENING")]
        MultiSimNotListening,

        [EnumMember(Value = "ERR_DISABLED")]
        LoginDisabled,

        [EnumMember(Value = "ERR_INVALID_APP")]
        LoginInvalidApp,

        [EnumMember(Value = "ERR_NOT_FOUND")]
        LoginNotFound,

        [EnumMember(Value = "ERR_INVALID_PASSWORD")]
        LoginInvalidPassword,

        [EnumMember(Value = "ERR_CODE_EXPIRED")]
        OneTimeCodeExpired,

        [EnumMember(Value = "ERR_CODE_NOT_FOUND")]
        OneTimeCodeNotFound,

        [EnumMember(Value = "ERR_IDENTITY_LIMIT")]
        IdentityLimit,

        [EnumMember(Value = "ERR_IDENTITY_NOT_FOUND")]
        IdentityNotFound,

        [EnumMember(Value = "ERR_IDENTITY_REMOVAL")]
        IdentityRemoval,

        [EnumMember(Value = "ERR_IDENTITY_TAKEN")]
        IdentityTaken,

        [EnumMember(Value = "ERR_IDENTITY_TOTAL_LIMIT")]
        IdentityTotalLimit,

        [EnumMember(Value = "ERR_INVALID_CONFIG")]
        InvalidConfig,

        [EnumMember(Value = "ERR_INVALID_INPUT")]
        InvalidInput,

        [EnumMember(Value = "ERR_PASSWORD_NOT_SET")]
        PasswordNotSet,

        [EnumMember(Value = "ERR_USERNAME_NOT_AVAILABLE")]
        UsernameNotAvailable
    }
}
