// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    public class Settings
    {
        internal const string defaultLogFilePath = "Logs/player_logs.txt";
        internal const LogLevel defaultFileLogLevel = LogLevel.Debug;

        internal event Action OnSaved;

#if UNITY_5_3_OR_NEWER
        [JsonProperty("editorLoglevel")]
        public LogLevel EditorLogLevel = LogLevel.Info;
#endif

        [JsonProperty("loglevel")]
        public LogLevel LogLevel = LogLevel.Info;

        [JsonProperty("filtermode")]
        public Log.FilterMode FilterMode = Log.FilterMode.Include;

        [JsonProperty("logStackTrace")]
        public bool LogStackTrace;

        [JsonProperty("sourcefilters")]
        public string SourceFilters = string.Empty;

        [JsonProperty("logToFile")]
        public bool LogToFile;

        [JsonProperty("logFilePath", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(defaultLogFilePath)]
        public string LogFilePath = defaultLogFilePath;

        [JsonProperty("fileLogLevel", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(defaultFileLogLevel)]
        public LogLevel FileLogLevel = defaultFileLogLevel;

        [NonSerialized]
        private string[] processedSourceFilters;

        [NonSerialized]
        private string savePath;

        public Settings() { }

        public Settings(string savePath)
        {
            this.savePath = savePath;
        }

        internal string[] GetSourceFilter()
        {
            if (string.IsNullOrEmpty(SourceFilters))
            {
                return null;
            }

            return processedSourceFilters;
        }

        public static Settings Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            try
            {
                if (!File.Exists(path))
                {
                    var freshSettings = new Settings(path);
                    freshSettings.Save();
                    return freshSettings;
                }

                var json = File.ReadAllText(path);
                var settings = Deserialize(json);
                settings.savePath = path;

                settings.ProcessSourceFilters();

                return settings;
            }
            catch (Exception e)
            {
                LogError($"Failed to read log settings from {path}: {e.Message}");
                return new Settings(path);
            }
        }

        public void Save()
        {
            ProcessSourceFilters();

            if (string.IsNullOrEmpty(savePath))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(savePath));
            }

            try
            {
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(savePath, Serialize(this));
            }
            catch (Exception e)
            {
                LogError($"Failed to write log settings to {savePath}: {e.Message}");
            }

            OnSaved?.Invoke();
        }

        private void ProcessSourceFilters()
        {
            processedSourceFilters = SourceFilters.Split(',').Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// Custom serialization to avoid using global settings.
        /// </summary>
        private static string Serialize(Settings settings)
        {
            var jsonSerializer = JsonSerializer.Create(null);
            var stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);

            using (var jsonTextWriter = new JsonTextWriter(stringWriter))
            {
                jsonTextWriter.Formatting = jsonSerializer.Formatting;
                jsonSerializer.Serialize(jsonTextWriter, settings, null);
            }

            return stringWriter.ToString();
        }

        /// <summary>
        /// Custom serialization to avoid using global settings.
        /// </summary>
        private static Settings Deserialize(string json)
        {
            var jsonSerializer = JsonSerializer.Create(null);
            using var reader = new JsonTextReader(new StringReader(json));
            return (Settings)jsonSerializer.Deserialize(reader, typeof(Settings));
        }

        private static void LogError(string message)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning(message);
#else
            Console.Error.WriteLine(message);
#endif
        }
    }
}
