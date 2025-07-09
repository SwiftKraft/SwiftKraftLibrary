// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Prefs
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public class DotnetPrefs : IPrefsImplementation
    {
        private struct PrefsObject
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? Int;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float? Float;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string String;

            public PrefsObject SetBool(bool value)
            {
                Int = value ? 1 : 0;
                Float = default;
                String = default;
                return this;
            }

            public PrefsObject SetInt(int value)
            {
                Int = value;
                Float = default;
                String = default;
                return this;
            }

            public PrefsObject SetFloat(float value)
            {
                Int = default;
                Float = value;
                String = default;
                return this;
            }

            public PrefsObject SetString(string value)
            {
                Int = default;
                Float = default;
                String = value;
                return this;
            }
        }

        private Dictionary<string, PrefsObject> prefsByKey = new();
        private readonly string prefsFilePath;

        // Used for unsafe but cheap encryption just to not expose prefs in plain text
        private static readonly byte[] xorTable =
        {
            227, 217, 95, 65, 177, 106, 197, 26, 137, 62, 30, 117, 151, 237, 147, 34, 17, 60, 246, 42, 24, 42, 6, 152,
            120, 74, 145, 98, 60, 33, 170, 190, 107, 152, 205, 57, 100, 15, 4, 138, 234, 183, 87, 35, 26, 129, 168, 168,
            48, 184, 122, 198, 228, 253, 55, 185, 210, 198, 201, 102, 216, 98, 222, 103, 148, 95, 10, 254, 32, 89, 189,
            1, 56, 107, 63, 96, 211, 227, 126, 247, 162, 233, 50, 131, 167, 228, 217, 110, 97, 19, 103, 157, 70, 158,
            177, 136, 216, 239, 143, 187, 119, 170, 136, 214, 251, 120, 142, 120, 189, 214, 230, 239, 72, 210, 196, 239,
            47, 91, 192, 175, 133, 155, 156, 12, 242, 31, 186, 153, 186, 183, 136, 227, 83, 195, 127, 247, 96, 91, 136,
            46, 86, 96, 177, 138, 45, 119, 85, 116, 218, 194, 88, 125, 46, 223, 53, 219, 13, 124, 227, 219, 189, 244,
            190, 5, 78, 2, 155, 133, 82, 204, 142, 212, 211, 146, 89, 36, 234, 17, 222, 123, 202, 70, 191, 248, 94, 11,
            180, 115, 141, 163, 234, 228, 119, 172, 112, 98, 162, 171, 60, 43, 219, 207, 33, 13, 51, 132, 169, 156, 134,
            16, 241, 252, 158, 104, 17, 119, 52, 198, 167, 66, 78, 40, 247, 174, 226, 170, 92, 38, 33, 247, 130, 63,
            212, 179, 160, 250, 36, 177, 196, 217, 97, 24, 237, 124, 178, 238, 70, 15, 155, 249, 45, 219, 3, 249, 221,
            179
        };

        public DotnetPrefs(string prefsFilePath = null)
        {
#if UNITY_EDITOR
            this.prefsFilePath =
                prefsFilePath ??
                Path.Combine(Environment.CurrentDirectory, "Prefs", "prefs.bin"); // Based on the Unity project path
#else
            this.prefsFilePath =
 prefsFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prefs", "prefs.bin"); // Based on the exec path
#endif
            Load();
        }

        public void Save()
        {
            if (!prefsByKey.Any())
            {
                File.Delete(prefsFilePath);
                return;
            }

            var prefsBinary = SerializePrefs();
            var tempFilePath = prefsFilePath + ".tmp";
            var backupFilePath = prefsFilePath + ".bak";

            using (var fileStream = File.Open(tempFilePath, FileMode.Create))
            {
                fileStream.Write(prefsBinary.Array, prefsBinary.Offset, prefsBinary.Count);
            }

            if (File.Exists(prefsFilePath))
            {
                File.Replace(tempFilePath, prefsFilePath, backupFilePath);
            }
            else
            {
                File.Move(tempFilePath, prefsFilePath);
            }
        }

        private void Load()
        {
            EnsurePrefsPathExists();

            if (!File.Exists(prefsFilePath))
            {
                return;
            }

            try
            {
                var prefsBinary = File.ReadAllBytes(prefsFilePath);
                var pbk = Deserialize(prefsBinary) ?? new Dictionary<string, PrefsObject>();
                prefsByKey = new Dictionary<string, PrefsObject>(pbk);
            }
            catch (JsonReaderException)
            {
                // We're ok with that.
            }
        }

        private void EnsurePrefsPathExists()
        {
            var dirPath = Path.GetDirectoryName(prefsFilePath);

            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public void DeleteAll()
        {
            prefsByKey.Clear();
        }

        public void DeleteKey(string key)
        {
            prefsByKey.Remove(key, out _);
        }

        public bool HasKey(string key)
        {
            return prefsByKey.TryGetValue(key, out _);
        }

        public void SetFloat(string key, float value)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            prefsByKey[key] = pref.SetFloat(value);
        }

        public float GetFloat(string key)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            return pref.Float.GetValueOrDefault();
        }

        public float GetFloat(string key, float defaultValue)
        {
            if (prefsByKey.TryGetValue(key, out PrefsObject pref) && pref.Float.HasValue)
            {
                return pref.Float.Value;
            }

            return defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            prefsByKey[key] = pref.SetBool(value);
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (prefsByKey.TryGetValue(key, out PrefsObject pref) && pref.Int.HasValue)
            {
                return pref.Int.Value != 0;
            }

            return defaultValue;
        }

        public void SetInt(string key, int value)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            prefsByKey[key] = pref.SetInt(value);
        }

        public int GetInt(string key)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            return pref.Int.GetValueOrDefault();
        }

        public int GetInt(string key, int defaultValue)
        {
            if (prefsByKey.TryGetValue(key, out PrefsObject pref) && pref.Int.HasValue)
            {
                return pref.Int.Value;
            }

            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            prefsByKey[key] = pref.SetString(value);
        }

        public string GetString(string key)
        {
            prefsByKey.TryGetValue(key, out PrefsObject pref);
            return pref.String;
        }

        public string GetString(string key, string defaultValue)
        {
            return prefsByKey.TryGetValue(key, out PrefsObject pref) && pref.String != null
                ? pref.String
                : defaultValue;
        }

        private ArraySegment<byte> SerializePrefs()
        {
            MemoryStream memoryStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memoryStream);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(writer, prefsByKey);
            writer.Flush();

            byte[] data = memoryStream.GetBuffer();
            for (int i = 0; i < memoryStream.Length; i++)
            {
                data[i] ^= xorTable[i % xorTable.Length];
            }

            return new ArraySegment<byte>(data, 0, (int)memoryStream.Length);
        }

        private Dictionary<string, PrefsObject> Deserialize(byte[] prefsBinary)
        {
            for (int i = 0; i < prefsBinary.Length; i++)
            {
                prefsBinary[i] ^= xorTable[i % xorTable.Length];
            }

            MemoryStream memoryStream = new MemoryStream(prefsBinary);
            StreamReader reader = new StreamReader(memoryStream);
            JsonSerializer serializer = new JsonSerializer();
            return (Dictionary<string, PrefsObject>)serializer.Deserialize(reader,
                typeof(Dictionary<string, PrefsObject>));
        }
    }
}
