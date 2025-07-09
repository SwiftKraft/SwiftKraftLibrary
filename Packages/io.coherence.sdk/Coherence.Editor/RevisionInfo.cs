// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Log;
    using UnityEditor;
    using UnityEngine;

    internal static class RevisionInfo
    {
        private static readonly Coherence.Log.Logger Logger = Log.GetLogger(typeof(RevisionInfo));

        internal static void GetAndSaveHash()
        {
            var versionInfo = AssetDatabase.LoadAssetAtPath<VersionInfo>(Paths.versionInfoPath);

            if (!TryGetHash(out var hash))
            {
                hash = null;
            }

            if (versionInfo.SdkRevisionHash != hash)
            {
                versionInfo.SdkRevisionHash = hash;

                EditorUtility.SetDirty(versionInfo);
                AssetDatabase.SaveAssetIfDirty(versionInfo);
            }
        }

        private static bool TryGetHash(out string revisionHash)
        {
            revisionHash = null;

            var sdkPath = GetCoherencePackageLocalPath();
            if (sdkPath == null)
            {
                return false;
            }

            var result = ProcessUtil.RunProcess("git", $"-C \"{sdkPath}\" rev-parse --short HEAD", out var output, out var errors);

            if (result != 0)
            {
                Logger.Error(Error.EditorCodegenSelectorSchemaOverride,
                    $"Failed to get current revision hash. \n Output: {output} \n Errors: {errors}");
                return false;
            }

            revisionHash = output.TrimEnd('\n');

            return true;
        }

        private static string GetCoherencePackageLocalPath()
        {
            var coherencePackage = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(Paths.packageRootPath);

            if (coherencePackage == null)
            {
                Logger.Error(Error.EditorRevisionInfoMissingPackage);
                return null;
            }

            if (coherencePackage.source != UnityEditor.PackageManager.PackageSource.Local)
            {
                return null;
            }

            return coherencePackage.resolvedPath;
        }
    }
}
