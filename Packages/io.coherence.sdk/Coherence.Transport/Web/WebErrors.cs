// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Transport.Web
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime.Serialization;
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

    public struct JsError
    {
        [JsonProperty("statusCode")]
        public int StatusCode;
        [JsonProperty("errorMessage")]
        public string ErrorMessage;
        [JsonProperty("errorResponse", NullValueHandling = NullValueHandling.Ignore)]
        public ErrorResponse ErrorResponse;
        [JsonProperty("errorType", NullValueHandling = NullValueHandling.Ignore)]
        public ErrorType ErrorType;
    }

    public struct ErrorResponse
    {
        [JsonProperty("errorCode", NullValueHandling = NullValueHandling.Ignore)]
        public ErrorCode ErrorCode;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ErrorType
    {
        Unknown,
        [EnumMember(Value = "offerError")]
        OfferError,
        [EnumMember(Value = "channelError")]
        ChannelError
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ErrorCode
    {
        Unknown = 0,
        [EnumMember(Value = "err_invalid_challenge")]
        InvalidChallenge,
        [EnumMember(Value = "err_room_not_found")]
        RoomNotFound,
        [EnumMember(Value = "err_room_full")]
        RoomFull,
    }
}
