namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using UnityEditor;

    /// <summary>
    /// Class responsible for knowing all replication server executable file locations.
    /// </summary>
    internal static class ReplicationServerBinaries
    {
        private const string nameWithoutExtension = "replication-server";
        private const string windowsExtension = ".exe";
        private const string nonWindowsExtension = "";

        private static readonly Dictionary<BuildTarget, string> namesByBuildTarget = new(4)
        {
            { BuildTarget.StandaloneOSX, ReplicationServerBinaries.nameWithoutExtension + ReplicationServerBinaries.nonWindowsExtension },
            { BuildTarget.StandaloneWindows, ReplicationServerBinaries.nameWithoutExtension + ReplicationServerBinaries.windowsExtension },
            { BuildTarget.StandaloneLinux64, ReplicationServerBinaries.nameWithoutExtension + ReplicationServerBinaries.nonWindowsExtension },
            { BuildTarget.StandaloneWindows64, ReplicationServerBinaries.nameWithoutExtension + ReplicationServerBinaries.windowsExtension },
        };

        /// <summary>
        /// Gets all platforms that support having a replication server executable bundled with them.
        /// </summary>
        public static IEnumerable<BuildTarget> GetSupportedPlatforms()
        {
            return ReplicationServerBinaries.namesByBuildTarget.Keys;
        }

        /// <summary>
        /// Gets a value indicating whether or not the given <paramref name="platform"/>
        /// supports having a replication server executable bundled with it.
        /// </summary>
        /// <param name="platform"> The platform to check. </param>
        /// <returns> <see langword="true"/> if platform is supported; otherwise, <see langword="false"/>. </returns>
        public static bool IsSupportedPlatform(BuildTarget platform)
        {
            return ReplicationServerBinaries.namesByBuildTarget.ContainsKey(platform);
        }

        /// <summary>
        /// Gets the name and extension (if any) for the replication server executable.
        /// </summary>
        /// <param name="platform"> The platform for which to get the name. </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"> Thrown if <paramref name="platform"/> is not supported. </exception>
        public static string GetName(BuildTarget platform)
        {
            return ReplicationServerBinaries.namesByBuildTarget.TryGetValue(platform, out var result)
                       ? result
                       : ReplicationServerBinaries.ThrowBuildTargetNotSupportedException(platform);
        }

        /// <summary>
        /// Gets the project-local path for the replication server executable under the streaming assets folder,
        /// including file name and extension (if any).
        /// </summary>
        /// <param name="platform"> The platform for which to get the path. </param>
        /// <exception cref="ArgumentException"> Thrown if <paramref name="platform"/> is not supported. </exception>
        public static AssetPath GetStreamingAssetsPath(BuildTarget platform)
        {
            return ReplicationServerBinaries.namesByBuildTarget.TryGetValue(platform, out var result)
                       ? Path.Combine(Paths.streamingAssetsPath, result)
                       : ReplicationServerBinaries.ThrowBuildTargetNotSupportedException(platform);
        }

        /// <summary>
        /// Gets the project-local path for the replication server executable under the tools folder,
        /// including file name and extension (if any).
        /// </summary>
        /// <param name="platform"> The platform for which to get the path. </param>
        /// <exception cref="ArgumentException"> Thrown if <paramref name="platform"/> is not supported. </exception>
        public static AssetPath GetToolsPath(BuildTarget platform)
        {
            return ReplicationServerBinaries.namesByBuildTarget.TryGetValue(platform, out var result)
                       ? Path.Combine(Paths.GetToolsPath(platform), result)
                       : ReplicationServerBinaries.ThrowBuildTargetNotSupportedException(platform);
        }

        /// <param name="platform"> The unsupported platform for which an invalid request was made. </param>
        /// <exception cref="ArgumentException"> Thrown always. </exception>
        [DoesNotReturn]
        private static dynamic ThrowBuildTargetNotSupportedException(BuildTarget platform)
        {
            throw new ArgumentException($"Build target does not support bundling of the replication server: {platform}");
        }
    }
}
