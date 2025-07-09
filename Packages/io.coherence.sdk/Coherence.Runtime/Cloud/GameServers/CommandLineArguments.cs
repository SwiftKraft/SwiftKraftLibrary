// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Log;

    public class CommandLineArguments
    {
        public Dictionary<string, string> Args { get; private set; }

        public ushort GamePort { get; private set; }
        public ushort ApiPort { get; private set; }
        public string AuthToken { get; private set; }
        public string PlayApiEndpoint { get; private set; }
        public string Region { get; private set; }
        public string StateFile { get; private set; }
        public string Id { get; private set; }
        public string Tag { get; private set; }
        public Dictionary<string, string> KV  { get; private set; }

        private readonly Logger logger = new();

        public CommandLineArguments(string[] args = null)
        {
            args ??= Environment.GetCommandLineArgs();
            Args = GetArgumentsDictionary(args);

            Args.TryGetValue("--coherence-game-port", out var gamePortStr);
            ushort.TryParse(gamePortStr, out var gamePort);
            GamePort = gamePort;
            Args.TryGetValue("--coherence-api-port", out var apiPortStr);
            ushort.TryParse(apiPortStr, out var apiPort);
            ApiPort = apiPort;
            Args.TryGetValue("--coherence-play-api-endpoint", out var playApiEndpoint);
            PlayApiEndpoint = playApiEndpoint;
            Args.TryGetValue("--coherence-region", out var region);
            Region = region;
            Args.TryGetValue("--coherence-auth-token", out var authToken);
            AuthToken = authToken;
            Args.TryGetValue("--coherence-state-file", out var stateFile);
            StateFile = stateFile;
            Args.TryGetValue("--coherence-id", out var id);
            Id = id;
            Args.TryGetValue("--coherence-tag", out var tag);
            Tag = tag;
            try
            {
                if (!Args.TryGetValue("--coherence-kv", out var kvBase64))
                {
                    return;
                }

                var kvJsonBytes = Convert.FromBase64String(kvBase64);
                var kvJsonString = Encoding.UTF8.GetString(kvJsonBytes);
                KV = Utils.CoherenceJson.DeserializeObject<Dictionary<string, string>>(kvJsonString);
            }
            catch (Exception ex)
            {
                logger.Error(Error.RuntimeCloudCLIFailedToParseKV, ("exception:", ex.Message));
            }
        }

        private static Dictionary<string, string> GetArgumentsDictionary(IReadOnlyList<string> args)
        {
            Dictionary<string, string> argsDict = new();
            for (var i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                if (!arg.StartsWith("-"))
                {
                    continue;
                }
                var value = i < args.Count - 1 ? args[i + 1] : null;
                value = (value?.StartsWith("-") ?? false) ? null : value;
                argsDict.Add(arg, value);
            }
            return argsDict;
        }
    }
}
