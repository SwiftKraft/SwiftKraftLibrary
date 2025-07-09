// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    [Serializable]
    public struct EndpointData
    {
        public const string LocalRegion = "local";

        public string host;
        public int port;
        public string authToken;
        public string runtimeKey;
        public ushort roomId;
        public ulong uniqueRoomId;
        public ulong worldId;
        public string region;
        public string schemaId;
        public string simulatorType;
        public string roomSecret;
        public string rsVersion;

        /// <summary>
        ///     If set to true the system won't overwrite the <see cref="authToken" /> when the <see cref="region" /> is set
        ///     to 'local'.
        /// </summary>
        public bool customLocalToken;

        public enum SimulatorType
        {
            world,
            room
        }

        public string GetHostAndPort()
        {
            var builder = new UriBuilder(host);
            builder.Port = port;

            return builder.Uri.ToString();
        }

        /// <summary>
        /// Returns (true, null) if the EndpointData is valid.
        /// Otherwise (false, <error message>).
        /// </summary>
        /// <param name="ignoreIpAddressValidation">By default, IP address validation is ignored. Set to <see langword="false"/> to check IP address.</param>
        /// <returns></returns>
        public (bool isValid, string errorMessage) Validate(bool ignoreIpAddressValidation = true)
        {
            if (string.IsNullOrEmpty(host))
            {
                return (false, $"{nameof(EndpointData)} is missing its '{nameof(host)}'.\n[{ToString()}]");
            }

            if (port == 0)
            {
                return (false, $"{nameof(EndpointData)} has '{nameof(port)}' set to 0.\n[{ToString()}]");
            }

            if (string.IsNullOrEmpty(schemaId))
            {
                return (false, $"{nameof(EndpointData)} is missing its '{nameof(schemaId)}'.\n[{ToString()}]");
            }

            if (string.IsNullOrEmpty(authToken) && region != EndpointData.LocalRegion)
            {
                return (false, $"{nameof(EndpointData)} is missing its '{nameof(authToken)}'.\n[{ToString()}]");
            }

            var isValidIp = ignoreIpAddressValidation || IsValidIpAddress(host);
            var errorMessage = isValidIp
                ? null
                : "The host is not a valid IP address or the IP address is not a local IP address.";

            return (isValidIp, errorMessage);
        }

        public override string ToString()
        {
            return $"{nameof(host)}: {host}, {nameof(port)}: {port}, {nameof(region)}: {region}, {nameof(roomId)}: {roomId}, {nameof(worldId)}: {worldId}, {nameof(uniqueRoomId)}: {uniqueRoomId}, {nameof(schemaId)}: {schemaId}";
        }

        public string WorldIdString()
        {
            return worldId > 0 ? worldId.ToString() : "";
        }

        public static bool TryParse(string value, out EndpointData endpointData)
        {
            endpointData = new EndpointData();

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            bool success = ExtractParameter(value, nameof(host) + ":", out string extractedHost);
            endpointData.host = extractedHost.Trim();

            success &= ExtractParameter(value, nameof(port) + ":", out string extractedPort);
            success &= int.TryParse(extractedPort, out endpointData.port);

            success &= ExtractParameter(value, nameof(region) + ":", out string extractedRegion);
            endpointData.region = extractedRegion.Trim();

            success &= ExtractParameter(value, nameof(roomId) + ":", out string extractedRoomId);
            success &= ushort.TryParse(extractedRoomId, out endpointData.roomId);

            success &= ExtractParameter(value, nameof(worldId) + ":", out string extractedWorldId);
            success &= UInt64.TryParse(extractedWorldId, out endpointData.worldId);

            success &= ExtractParameter(value, nameof(schemaId) + ":", out string extractedSchemaId);
            endpointData.schemaId = extractedSchemaId.Trim();

            success &= ExtractParameter(value, nameof(uniqueRoomId) + ":", out string extractedUniqueRoomId);
            success &= ulong.TryParse(extractedUniqueRoomId, out endpointData.uniqueRoomId);

            return success;
        }

        private static bool TryGetHostAndPort(string value, out string host, out int port, out ushort roomId)
        {
            host = default;
            port = default;
            roomId = default;

            var split = value.Split(':');

            if (split.Length < 2)
            {
                return false;
            }

            string hostValue = split[0];
            string portString = split[1];

            if (!ushort.TryParse(portString, out ushort portValue))
            {
                return false;
            }

            if (split.Length == 3)
            {
                string roomIdString = split[2];
                if (!ushort.TryParse(roomIdString, out ushort roomIdValue))
                {
                    return false;
                }
                roomId = roomIdValue;
            }

            host = hostValue;
            port = portValue;
            return true;
        }

        private static bool ExtractParameter(string source, string parameter, out string value)
        {
            value = default;

            var split = source.Split(new string[] { parameter }, StringSplitOptions.None);

            if (split.Length < 2)
            {
                return false;
            }

            string result = split[1].Split(',')[0];

            value = result;
            return true;
        }

        private static bool IsValidIpAddress(string value)
        {
            return IPAddress.TryParse(value, out _) && GetAllLocalIPAddresses().Any(s => s.Equals(value));
        }

        private static string[] GetAllLocalIPAddresses()
        {
            var ipList = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .ToArray();
            return ipList;
        }

        private static bool IsValidDomain(string value)
        {
            try
            {
                bool hasScheme = value.IndexOf("://", StringComparison.Ordinal) >= 0;
                if (hasScheme)
                {
                    _ = new Uri(value);
                }
                else
                {
                    UriBuilder uriBuilder = new UriBuilder
                    {
                        Host = value
                    };
                    _ = uriBuilder.Uri;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
