// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeUtils
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Settings for the <see cref="ThreadResumer"/> class.
    /// </summary>
    [Serializable]
    public class ThreadResumerSettings
    {
        internal static bool SteamDetected;

        /// <summary>
        /// Should the <see cref="ThreadResumer"/> run?
        /// </summary>
        public bool Enabled = SteamDetected;

        /// <summary>
        /// How often to conduct a search for suspended threads (in milliseconds).
        /// </summary>
        public uint SearchIntervalMs = 50;

        /// <summary>
        /// If searching for suspended threads takes longer than this threshold, log a warning.
        /// </summary>
        /// <remarks>0 means the warning is disabled.</remarks>
        public uint LongSearchWarnThresholdMs = 50;

        /// <summary>
        /// Should the <see cref="ThreadResumer"/> log when a suspended thread is found?
        /// </summary>
        public bool WarnOnSuspension = true;

#if UNITY_EDITOR
        static ThreadResumerSettings()
        {
            DetectSteam();
        }
#endif

        private static void DetectSteam()
        {
            try
            {
                var hasSteamSampleAssembly = TryLoadAssembly("SteamSample") != null;
                if (hasSteamSampleAssembly)
                {
                    SteamDetected = true;
                    return;
                }

                if (HasFacePunchSteamworks())
                {
                    SteamDetected = true;
                    return;
                }

                if (HasSteamWorksDotNet())
                {
                    SteamDetected = true;
                    return;
                }

                var mainAssembly = TryLoadAssembly("Assembly-CSharp");
                if (mainAssembly == null)
                {
                    return;
                }

                // Perhaps the steam code was copied, check in the main assembly
                var hasSteamTypeInMainAsm = AssemblyHasSteamManagerType(mainAssembly);
                if (hasSteamTypeInMainAsm)
                {
                    SteamDetected = true;
                    return;
                }

                // Perhaps it was copied to other assembly, check all referenced assemblies
                var referencedAssemblies = mainAssembly.GetReferencedAssemblies();
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (AssemblyHasSteamManagerType(Assembly.Load(referencedAssembly)))
                    {
                        SteamDetected = true;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.Log($"(coherence) Failed to check for Steam: {exception}");
            }
        }

        private static bool HasSteamWorksDotNet()
        {
            return TryLoadAssembly("com.rlabrecque.steamworks.net") != null;
        }

        private static bool HasFacePunchSteamworks()
        {
            var hasSteamWorks = TryLoadAssembly("Facepunch.Steamworks") != null;

            HasWindowsFacePunchSteamworks(ref hasSteamWorks);
            HasMacFacePunchSteamworks(ref hasSteamWorks);
            HasLinuxFacePunchSteamworks(ref hasSteamWorks);

            return hasSteamWorks;

            [Conditional("UNITY_EDITOR_WIN")]
            static void HasWindowsFacePunchSteamworks(ref bool hasSteamWorks)
            {
                hasSteamWorks |= TryLoadAssembly("Facepunch.Steamworks.Win32") != null
                                || TryLoadAssembly("Facepunch.Steamworks.Win64") != null;
            }

            [Conditional("UNITY_EDITOR_OSX")]
            static void HasMacFacePunchSteamworks(ref bool hasSteamWorks)
            {
                hasSteamWorks |= TryLoadAssembly("Facepunch.Steamworks.Posix") != null;
            }

            [Conditional("UNITY_EDITOR_LINUX")]
            static void HasLinuxFacePunchSteamworks(ref bool hasSteamWorks)
            {
                hasSteamWorks |= TryLoadAssembly("Facepunch.Steamworks.Posix") != null
                                || TryLoadAssembly("Facepunch.Steamworks.Posix.Linux") != null;
            }
        }

        private static Assembly TryLoadAssembly(string name)
        {
            try { return Assembly.Load(name); }
            catch { return null; }
        }

        private static bool AssemblyHasSteamManagerType(Assembly assembly)
        {
            return assembly.GetType("SteamManager") != null
                   || assembly.GetType("SteamSample.SteamManager") != null;
        }
    }
}
