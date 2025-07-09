// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Connection;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
#if UNITY
    using UnityEngine;
#else
    using Coherence.Headless;
#endif

#pragma warning disable 649
    internal struct RoomCreationRequest
    {
        [JsonProperty("tags")]
        public string[] Tags;

        [JsonProperty("kv")]
        public Dictionary<string, string> KV;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("sim_slug")]
        public string SimSlug;

        [JsonProperty("sim_payload")]
        public string SimPayload;

        [JsonProperty("max_players")]
        public int MaxClients;

        [JsonProperty("find_or_create")]
        public bool FindOrCreate;
    }

    internal struct RoomMatchRequest
    {
        [JsonProperty("tags")]
        public string[] Tags;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("sim_slug")]
        public string SimSlug;
    }

    internal struct RoomUnlistRequest
    {
        [JsonProperty("secret")] public string Secret;
    }

    internal struct RegionFetchResponse
    {
        [JsonProperty("regions")]
        public string[] Regions;
    }

    internal struct RoomFetchResponse
    {
        [JsonProperty("rooms")]
        public RoomData[] Rooms;
    }

    public struct RoomMatchResponse
    {
        [JsonProperty("room")]
        public RoomData? Room;
    }

    internal struct LocalRoomCreationRequest
    {
        [JsonProperty("UniqueID")]
        public int UniqueID;

        [JsonProperty("MaxClients")]
        public int MaxClients;

        [JsonProperty("MaxEntities")]
        public int MaxEntities;

        [JsonProperty("OutStatsFreq")]
        public int OutStatsFreq;

        [JsonProperty("LogStatsFreq")]
        public int LogStatsFreq;

        [JsonProperty("SchemaName")]
        public string SchemaName; //Should be empty or "local"

        [JsonProperty("SchemaTimeout")]
        public int SchemaTimeout;

        [JsonProperty("SchemaUrls")]
        public string[] SchemaUrls;

        [JsonProperty("Schemas")]
        public string[] Schemas;

        [JsonProperty("DisconnectTimeout")]
        public int DisconnectTimeout;

        [JsonProperty("DebugStreams")]
        public bool DebugStreams;

        [JsonProperty("Frequency")]
        public int Frequency; //Should be 0

        [JsonProperty("MinQueryDistance")]
        public float MinQueryDistance;

        [JsonProperty("WebSupport")]
        public bool WebSupport;

        [JsonProperty("CleanupTimeout")]
        public int CleanupTimeout;

        [JsonProperty("ProjectID")]
        public string ProjectID;

        [JsonProperty("KVP")]
        public Dictionary<string, string> KeyValues;

        [JsonProperty("Tags")]
        public string[] Tags;

        [JsonProperty("Secret")]
        public string Secret;

        [JsonProperty("HostAuthority")]
        public string HostAuthority;
    }

    internal struct RemoveRoomRequest
    {
        [JsonProperty("RoomID")]
        public ushort RoomId;
    }

    public struct LocalRoomData
    {
        [JsonProperty("RoomID")]
        public ushort RoomID;

        [JsonProperty("Secret")]
        public string Secret;
    }

    public struct LocalRoomsListItem
    {
        public ulong UniqueID;
        public ushort ID;
        public int MaxClients;
        public string SchemaName;
        public int ConnectionCount;
        public string LastCheckTime;
        public string ProjectID;
        public Dictionary<string, string> KVP;
        public string[] Tags;
    }

    public struct LocalRoomsResponse
    {
        [JsonProperty("Rooms")]
        public LocalRoomsListItem[] Rooms;
    }

    public struct RoomData
    {
        public const string RoomNameKey = "name";

        [JsonProperty("room_id")]
        public ushort Id;

        [JsonProperty("unique_id")]
        public ulong UniqueId;

        [JsonProperty("host")]
        public RoomHostData Host;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("connected_players")]
        public int ConnectedPlayers;

        [JsonProperty("kv")]
        public Dictionary<string, string> KV;

        [JsonProperty("tags")]
        public string[] Tags;

        [JsonProperty("sim_slug")]
        public string SimSlug;

        [JsonProperty("secret")]
        public string Secret;

        [JsonProperty("created_at")]
        public string CreatedAt;

        public string AuthToken;

        private string roomName;
        public string RoomName
        {
            get
            {
                if (String.IsNullOrEmpty(roomName))
                {
                    roomName = ExtractRoomName();
                }

                return roomName;
            }
        }

        public static (EndpointData, bool isValid, string validationErrorMessage) GetRoomEndpointData(RoomData room)
        {
#if UNITY
            var simAuthToken = SimulatorUtility.AuthToken;
            if (!string.IsNullOrEmpty(simAuthToken))
            {
                room.AuthToken = simAuthToken;
            }
#endif

            var roomEndpoint = new EndpointData
            {
                host = room.Host.Ip,
                port = room.Host.Port,
                roomId = room.Id,
                uniqueRoomId = room.UniqueId,

                runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                schemaId = RuntimeSettings.Instance.SchemaID,
                region = room.Host.Region,
                authToken = room.AuthToken,
                roomSecret = room.Secret,
                simulatorType = EndpointData.SimulatorType.room.ToString(),
            };

            bool local = roomEndpoint.region == "local";

            (bool valid, string validationErrorMessage) = roomEndpoint.Validate();
            if (!valid)
            {
                return (roomEndpoint, false, validationErrorMessage);
            }

#if UNITY
            //Check for special addresses
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                roomEndpoint.port =
                    local ? RuntimeSettings.Instance.LocalRoomsWebPort : RuntimeSettings.Instance.RemoteWebPort;
            }
            else if (local)
            {
                roomEndpoint.port = RuntimeSettings.Instance.LocalRoomsUDPPort;
            }
#endif

            return (roomEndpoint, true, null);
        }

        public override string ToString()
        {
            return $"{Host.Ip}:{Host.Port}:{Id} ({ConnectedPlayers}/{MaxPlayers})";
        }

        private string ExtractRoomName()
        {
            if (KV != null && KV.TryGetValue(RoomNameKey, out string name))
            {
                return roomName = name;
            }
            else
            {
                return roomName = String.Empty;
            }
        }
    }

    public struct RoomHostData
    {
        [JsonProperty("ip")]
        public string Ip;

        [JsonProperty("port")]
        public int Port;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("rs_version")]
        public string RSVersion;

        public override string ToString()
        {
            return $"{nameof(Ip)}: {Ip}," +
                   $"{nameof(Port)}: {Port}," +
                   $"{nameof(Region)}: {Region}" +
                   $"{nameof(RSVersion)}: {RSVersion}";
        }
    }

    public class RoomCreationOptions
    {
        public int MaxClients = 10;
        public string[] Tags = new string[] { };
        public Dictionary<string, string> KeyValues = new Dictionary<string, string>();
        public string SimPayload = string.Empty;
        public bool FindOrCreate;

        public static RoomCreationOptions Default => new RoomCreationOptions();
    }

    public class SelfHostedRoomCreationOptions : RoomCreationOptions
    {
        public int UniqueId = new System.Random().Next(1, int.MaxValue);
        public int CleanupTimeout = 60;
        public int MaxEntities = 10 * 100;
        public string Secret = "devSecret";
        public string ProjectId = "local";
        public string[] Schemas = new string[] { };
        public HostAuthority HostAuthority;

        public bool UseDebugStreams
#if UNITY
            = RuntimeSettings.Instance.UseDebugStreams;
#else
            = false;
#endif

        public new static SelfHostedRoomCreationOptions Default => new SelfHostedRoomCreationOptions();

        internal static SelfHostedRoomCreationOptions FromRoomCreationOptions(RoomCreationOptions roomCreationOptions)
        {
            return new SelfHostedRoomCreationOptions()
            {
                MaxClients = roomCreationOptions.MaxClients,
                KeyValues = roomCreationOptions.KeyValues,
                Tags = roomCreationOptions.Tags,
            };
        }

        internal LocalRoomCreationRequest ToRequest()
        {
            return new LocalRoomCreationRequest()
            {
                UniqueID = UniqueId,
                MaxClients = MaxClients,
                MaxEntities = MaxEntities,
                OutStatsFreq = 1,
                LogStatsFreq = 1,
                SchemaName = string.Empty,
                SchemaTimeout = 60,
                SchemaUrls = new string[0],
                Schemas = Schemas,
                DisconnectTimeout = 6000,
                DebugStreams = UseDebugStreams,
                Frequency = 0,
                MinQueryDistance = 0.1f,
                WebSupport = true,
                CleanupTimeout = CleanupTimeout,
                ProjectID = ProjectId,
                KeyValues = KeyValues,
                Tags = Tags,
                Secret = Secret,
                HostAuthority = HostAuthority.GetCommaSeparatedFeatures(),
            };
        }
    }
#pragma warning restore 649
}
