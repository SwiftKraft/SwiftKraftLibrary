// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Prefs
{
#if UNITY
    using UnityEngine;
#endif

    public static class PrefsFactory
    {
        public static IPrefsImplementation Create()
        {
#if UNITY
            switch (Application.platform)
            {
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                    return new UnityPrefs();
                case RuntimePlatform.Switch:
                    return new DummyPrefs();
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                    {
                        var path = "Library/coherence/prefs.bin";
                        return new DotnetPrefs(path);
                    }
                default:
                    {
                        var path = System.IO.Path.Combine(Application.persistentDataPath, "prefs.bin");
                        return new DotnetPrefs(path); 
                    }
            }
#else
            return new DotnetPrefs();
#endif
        }
    }
}

