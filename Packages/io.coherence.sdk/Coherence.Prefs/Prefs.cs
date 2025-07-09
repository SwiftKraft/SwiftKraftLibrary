// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Prefs
{
    using Log;
    using Logger = Log.Logger;
#if UNITY
    using UnityEngine;
#endif

    public static class Prefs
    {
#if UNITY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPoolInUnity() => implementation = null;
#endif

        private static readonly Logger logger = Log.GetLogger(typeof(Prefs));

        private static IPrefsImplementation implementation;

        public static IPrefsImplementation Implementation
        {
            get => implementation ??= PrefsFactory.Create();
            set => implementation = value;
        }

        public static void Save()
        {
            logger.Trace(nameof(Save));
            Implementation.Save();
        }

        public static void DeleteAll()
        {
            logger.Trace(nameof(DeleteAll));
            Implementation.DeleteAll();
        }

        public static void DeleteKey(string key)
        {
            logger.Trace(nameof(DeleteKey), ("Key", key));
            Implementation.DeleteKey(key);
        }

        public static bool HasKey(string key)
        {
            logger.Trace(nameof(HasKey), ("Key", key));
            return Implementation.HasKey(key);
        }

        public static void SetFloat(string key, float value)
        {
            logger.Trace(nameof(SetFloat), ("Key", key), ("Value", value));
            Implementation.SetFloat(key, value);
        }

        public static float GetFloat(string key)
        {
            logger.Trace(nameof(GetFloat), ("Key", key));
            return Implementation.GetFloat(key);
        }

        public static float GetFloat(string key, float defaultValue)
        {
            logger.Trace(nameof(GetFloat), ("Key", key), ("DefaultValue", defaultValue));
            return Implementation.GetFloat(key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            logger.Trace(nameof(SetInt), ("Key", key), ("Value", value));
            Implementation.SetInt(key, value);
        }

        public static int GetInt(string key)
        {
            logger.Trace(nameof(GetInt), ("Key", key));
            return Implementation.GetInt(key);
        }

        public static int GetInt(string key, int defaultValue)
        {
            logger.Trace(nameof(GetInt), ("Key", key), ("DefaultValue", defaultValue));
            return Implementation.GetInt(key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            logger.Trace(nameof(SetString), ("Key", key));
            Implementation.SetString(key, value);
        }

        public static string GetString(string key)
        {
            logger.Trace(nameof(GetString), ("Key", key));
            return Implementation.GetString(key);
        }

        public static string GetString(string key, string defaultValue)
        {
            logger.Trace(nameof(GetString), ("Key", key), ("DefaultValue", defaultValue));
            return Implementation.GetString(key, defaultValue);
        }
    }
}
