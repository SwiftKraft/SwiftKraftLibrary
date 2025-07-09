// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Compilation;

    internal class BuildPostprocessor : IPostprocessBuildWithReport
    {
        private class BuildEventProperties : Analytics.BaseProperties
        {
            public string build_target;
            public bool development;
            public bool headless;
            public double build_time;
        }

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var properties = new BuildEventProperties
            {
                build_target = report.summary.platform.ToString(),
                development = (report.summary.options & BuildOptions.Development) != 0,
                headless = BuildPostprocessor.IsHeadless(report),
                build_time = report.summary.totalTime.TotalSeconds,
            };

            Analytics.Capture(new Analytics.Event<BuildEventProperties>(Analytics.Events.Build, properties));

            if (BakeUtil.BakeOnBuild)
            {
                // make sure changes to baked files are picked up by Unity
                // after the build is completed
                // NOTE triggered even when no changes to generated code
                EditorApplication.delayCall += BuildPostprocessor.Refresh;
            }

            BuildPostprocessor.DeleteCombinedSchema();

            BuildPostprocessor.DeleteRsBinary(report.summary.platform);

            BuildPostprocessor.DeleteStreamingAssetsIfEmpty();

            ManagedCodeStrippingUtils.DeleteLinkXmlFileUnderAssetsFolder();
        }

        [InitializeOnLoadMethod]
        internal static void CleanUpTempFiles()
        {
            BuildPostprocessor.DeleteCombinedSchema();

            foreach (var supportedBuildTarget in ReplicationServerBinaries.GetSupportedPlatforms())
            {
                BuildPostprocessor.DeleteRsBinary(supportedBuildTarget);
            }

            BuildPostprocessor.DeleteStreamingAssetsIfEmpty();
        }

        private static void DeleteRsBinary(BuildTarget platform)
        {
            if (ProjectSettings.instance.RSBundlingEnabled && ReplicationServerBinaries.IsSupportedPlatform(platform))
            {
                ReplicationServerBundler.DeleteRsFromStreamingAssets(platform);
            }
        }

        internal static void DeleteStreamingAssetsIfEmpty()
        {
            AssetUtils.DeleteFolderIfEmpty(Paths.streamingAssetsPath);
        }

        private static void DeleteCombinedSchema()
        {
            AssetUtils.DeleteFile(Paths.streamingAssetsCombinedSchemaPath);
        }

        private static bool IsHeadless(BuildReport report)
        {
            try
            {
                switch (report.summary.platform)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    case BuildTarget.StandaloneOSX:
                    case BuildTarget.StandaloneLinux64:
                        return report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void Refresh()
        {
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
