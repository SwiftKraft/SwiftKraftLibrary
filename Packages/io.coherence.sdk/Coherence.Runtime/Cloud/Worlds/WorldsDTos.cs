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
    using Connection;
    using Newtonsoft.Json;
    using Runtime;
#if UNITY
    using UnityEngine;
#endif

    public class WorldsResolverException : System.Exception
    {
        public Result ErrorCode;

        public WorldsResolverException(Result code, string message) : base(message)
        {
            ErrorCode = code;
        }
    }

#pragma warning disable 649
    public struct WorldData
    {
        [JsonProperty("id")]
        public ulong WorldId;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("host")]
        public HostData Host;

        [JsonProperty("tags")]
        public string[] Tags;

        [JsonProperty("region")]
        public string Region;

        public string AuthToken;

        public string RoomSecret;

        public override string ToString() => $"{Host.Ip}:{Host.UDPPort} ({WorldId})";

        public static (EndpointData, bool isValid, string validationErrorMessage) GetWorldEndpoint(WorldData world)
        {
#if UNITY
            var simAuthToken = SimulatorUtility.AuthToken;
            if (!string.IsNullOrEmpty(simAuthToken))
            {
                world.AuthToken = simAuthToken;
            }
#endif

            var worldEndpoint = new EndpointData()
            {
                host = world.Host.Ip,
                port = world.Host.UDPPort,
                worldId = world.WorldId,
                region = world.Region,
#if UNITY
                runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                schemaId = RuntimeSettings.Instance.SchemaID,
#endif
                authToken = world.AuthToken,
                simulatorType = EndpointData.SimulatorType.world.ToString(),

                roomSecret = world.RoomSecret,
            };

#if UNITY
            bool local = worldEndpoint.host == RuntimeSettings.Instance.LocalHost;

            (bool valid, string validationErrorMessage) = worldEndpoint.Validate();
            if (!valid)
            {
                return (worldEndpoint, false, validationErrorMessage);
            }

            //Check for special addresses
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                worldEndpoint.port =
                    local ? RuntimeSettings.Instance.LocalWorldWebPort : RuntimeSettings.Instance.RemoteWebPort;
            }
            else if (local)
            {
                worldEndpoint.port = RuntimeSettings.Instance.LocalWorldUDPPort;
            }
#endif
            return (worldEndpoint, true, null);
        }

        public static WorldData GetLocalWorld(string ip)
        {
            return new WorldData()
            {
                WorldId = 123,
                Name = "LocalWorld",
                Host = new HostData
                {
                    Ip = ip,
                    Region = "local",
                    SigPort = 0,
                    SigURL = "",
                    UDPPort = 32001,
                    WebPort = 32002,
                },
                Tags = null,
                Region = "local",
                RoomSecret = "",
            };
        }
    }

    public struct HostData
    {
        [JsonProperty("ip")]
        public string Ip;

        [JsonProperty("udp_port")]
        public int UDPPort;

        [JsonProperty("sig_url")]
        public string SigURL;

        [JsonProperty("sig_port")]
        public int SigPort;

        [JsonProperty("web_port")]
        public int WebPort;

        [JsonProperty("region")]
        public string Region;

        public override string ToString()
        {
            return $"{nameof(Ip)}: {Ip}," +
                   $"{nameof(UDPPort)}: {UDPPort}," +
                   $"{nameof(SigURL)}: {SigURL}," +
                   $"{nameof(SigPort)}: {SigPort}," +
                   $"{nameof(WebPort)}: {WebPort}" +
                   $"{nameof(Region)}: {Region}";
        }
    }
#pragma warning restore 649
}
