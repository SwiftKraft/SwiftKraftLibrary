// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using UnityEngine;
    using Utils;
    using Logger = Log.Logger;
    using Transport;

    public static class SimulatorUtility
    {
        private const string ArgumentPrefix = "--coherence";
        private static readonly Logger Logger = Log.Log.GetLogger(typeof(SimulatorUtility));
        private static readonly string[] Args = Environment.GetCommandLineArgs();

        static SimulatorUtility()
        {
            for (var i = 0; i < Args.Length; i++)
            {
                var argument = Args[i];
                if (!argument.StartsWith(ArgumentPrefix))
                {
                    continue;
                }

                try
                {
                    AddArgument(argument, Args.Length > i + 1 ? Args[i + 1] : null);
                }
                catch (Exception exception)
                {
                    Logger.Error(Log.Error.SimulatorUtilityFailedToAddArgument,
                        ("faultyArg", argument),
                        ("exception", exception));
                }
            }
        }

        public enum Type
        {
            Undefined = 0,
            World = 1,
            Rooms = 2,
        }

        public const string LocalRegionParameter = "local";
        public const string SimulatorTypeRoomsParameter = "rooms";
        public const string SimulatorTypeWorldParameter = "world";
        internal const string AuthTokenKeyword = "--coherence-auth-token";

        private static readonly Dictionary<string, string> ArgumentsDict = new();

        private static bool wantsToBehaveAsSimulator;

        public static Type SimulatorType
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                var idx = Array.IndexOf(args, "--coherence-simulator-type");
                if (idx == -1 || idx == args.Length - 1)
                {
                    return Type.Undefined;
                }

                var typeStr = args[idx + 1];
                return typeStr switch
                {
                    SimulatorTypeWorldParameter => Type.World,
                    SimulatorTypeRoomsParameter => Type.Rooms,
                    _ => Type.Undefined,
                };
            }
        }

        public static string Region => GetArgument("--coherence-region") ?? /*obsolete*/GetArgument("--coherence-play-region");
        public static string Ip => GetArgument("--coherence-ip");
        public static int Port => int.TryParse(GetArgument("--coherence-port"), out var port) ? port : 0;
        public static int RoomId => ushort.TryParse(GetArgument("--coherence-room-id"), out var id) ? id : -1;
        public static ulong UniqueRoomId => ulong.TryParse(GetArgument("--coherence-unique-room-id"), out var id) ? id : 0;
        public static ulong WorldId => ulong.TryParse(GetArgument("--coherence-world-id"), out var id) ? id : 0;
        public static int HttpServerPort => int.TryParse(GetArgument("--coherence-http-server-port"), out var port) ? port : -1;
        public static string AuthToken => GetArgument(AuthTokenKeyword);

        /// <summary>
        /// Gets the private data payload for the simulator.
        /// </summary>
        /// <remarks>
        /// For room simulators, this is the private data provided by the player when creating a room.
        /// For world simulators, the data is configured when creating the world on the customer dashboard.
        /// </remarks>
        public static string Payload
        {
            get
            {
                var payload = GetArgument("--coherence-payload");
                if (string.IsNullOrEmpty(payload))
                {
                    return string.Empty;
                }

                var data = Convert.FromBase64String(payload);
                return Encoding.UTF8.GetString(data);
            }
        }

        /// <summary>
        /// Gets a value indicating whether every cloud service instance should use the same shared AuthClient and RequestFactory instances.
        /// <para>
        /// This will be True if an <see cref="AuthToken">authentication token</see> has been provided, and should cause
        /// CloudCredentialsFactory.ForSimulator to return the same shared AuthClient and RequestFactory instances with every request.
        /// </para>
        /// </summary>
        internal static bool UseSharedCloudCredentials =>
#if UNITY_5_3_OR_NEWER || UNITY
            !string.IsNullOrEmpty(AuthToken);
#else
            false;
#endif

        public static bool IsCloudSimulator => !string.IsNullOrEmpty(AuthToken) && Region != LocalRegionParameter;

        public static new string ToString()
        {
            var tokenString = string.IsNullOrEmpty(AuthToken) ? "null" : "not null";
            return $"Type:{SimulatorType} Region:{Region} IP:{Ip} Port:{Port} RoomId:{RoomId} UniqueRoomId:{UniqueRoomId} WorldId:{WorldId} HttpsServerPort:{HttpServerPort} AuthToken:{tokenString}";
        }

        public static List<string> RoomTags
        {
            get
            {
                var tags = new List<string>();

                var encodedS = GetArgument("--coherence-room-tags");
                if (string.IsNullOrEmpty(encodedS))
                {
                    return tags;
                }

                var data = Convert.FromBase64String(encodedS);
                var s = Encoding.UTF8.GetString(data);

                if (string.IsNullOrEmpty(s))
                {
                    return tags;
                }

                var arr = s.Split(' ');
                foreach (var t in arr)
                {
                    tags.Add(t);
                }

                return tags;
            }
        }

        public static Dictionary<string, string> RoomKV
        {
            get
            {
                var kvBase64 = GetArgument("--coherence-room-kv-json");
                if (string.IsNullOrEmpty(kvBase64))
                {
                    return new Dictionary<string, string>();
                }

                var kvJsonBytes = Convert.FromBase64String(kvBase64);
                var kvJsonString = Encoding.UTF8.GetString(kvJsonBytes);
                return CoherenceJson.DeserializeObject<Dictionary<string, string>>(kvJsonString);
            }
        }

        private static bool HasSimulatorCommandLineParameter => ArgumentsDict.ContainsKey("--coherence-simulation-server") || ArgumentsDict.ContainsKey("--coherence-simulator");

        public static bool IsInvokedAsSimulator => wantsToBehaveAsSimulator || HasSimulatorCommandLineParameter;
        public static bool IsInvokedInCommandLine => Application.isBatchMode;

        public static bool IsSimulator
        {
            get =>
#if COHERENCE_SIMULATOR
            true;
#else
            IsInvokedAsSimulator;
#endif

#if COHERENCE_SIMULATOR
            set
            {
            }
#else
            set => wantsToBehaveAsSimulator = value;
#endif
        }

        /// <summary>
        /// Cloud-hosted simulators have UDP transport enforced. The reason being, they should never have
        /// any issues with UDP and TCP is blocked on free-tier cloud services.
        /// </summary>
        internal static TransportType EnsureCorrectCloudSimulatorTransport(Logger logger, TransportType transportType)
        {
            if (transportType != TransportType.UDPOnly)
            {
                logger.Info($"Transport type was set to {transportType}, but cloud-hosted simulators support only UDP transport. " +
                            "Defaulting to UDP transport.");
                return TransportType.UDPOnly;
            }

            return transportType;
        }

        /// <summary>
        /// Allows updating the simulator utility with command line arguments at runtime.
        /// Use this to set arguments when running simulators from the editor since the setup of the utility
        /// reads args from the commandline otherwise.
        /// </summary>
        /// <param name="arg"> string name of the arg like --coherence-ip</param>
        /// <param name="val"> string value fo the arg like 127.0.0.1</param>
        public static void AddArgument(string arg, string val) => ArgumentsDict.Add(arg, val);

        internal static void SetArgument(string keyword, string value) => ArgumentsDict.Add(keyword, value);

        internal static bool RemoveArgument(string keyword) => ArgumentsDict.Remove(keyword);

        internal static string GetArgument(string arg) => ArgumentsDict.TryGetValue(arg, out string value) ? value : null;
    }
}
