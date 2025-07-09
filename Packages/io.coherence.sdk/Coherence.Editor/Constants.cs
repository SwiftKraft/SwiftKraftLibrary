// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;

    public static class Constants
    {
        // https://en.wikipedia.org/wiki/Registered_port
        public const int minPort = 1024;
        public const int maxPort = 49151;
        public const int defaultWorldUDPPort = 32001;
        public const int defaultWorldWebPort = 32002;

        public const int defaultRoomsUDPPort = 42001;
        public const int defaultRoomsWebPort = 42002;

        public const int minFrequency = 1;
        public const int maxFrequency = 256;
        public const int defaultSendFrequency = 20;
        public const int defaultRecvFrequency = 60;

        public const string defaultRSLogFilePath = "Logs/rs_logs.txt";

        public const int defaultPersistenceSaveRateInSeconds = 30;
        public static readonly TimeSpan localRoomsCleanupTime = TimeSpan.FromMinutes(2);

        public const string schemaExtension = "schema";
        public const string schemaAssetIdentifier = "schema";

        public const string defineSymbolPrefix = "COHERENCE_SCHEMA_DEFINE_";

        public const string watchdogTriggeredKey = "Coherence.Watchdog.Triggered";
        public const string bindingsAdvancedModeKey = "Coherence.BindingsWindow.AdvancedMode";
    }
}
