// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
#if UNITY_2023_1_OR_NEWER
    using UnityEditor.Build;
#endif
    using UnityEngine;
    using Paths = Coherence.Editor.Paths;

    [Serializable]
    public class OptimizeForSize
    {
        static OptimizeForSize()
        {
            SetBatchingForPlatform = (SetBatchingForPlatformFunction)Delegate.CreateDelegate(typeof(SetBatchingForPlatformFunction), null, typeof(PlayerSettings).GetMethod("SetBatchingForPlatform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            GetBatchingForPlatform = (GetBatchingForPlatformFunction)Delegate.CreateDelegate(typeof(GetBatchingForPlatformFunction), null, typeof(PlayerSettings).GetMethod("GetBatchingForPlatform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        private delegate void SetBatchingForPlatformFunction(BuildTarget platform, int staticBatching, int dynamicBatching);
        private delegate void GetBatchingForPlatformFunction(BuildTarget platform, out int staticBatching, out int dynamicBatching);

        private static readonly SetBatchingForPlatformFunction SetBatchingForPlatform;
        private static readonly GetBatchingForPlatformFunction GetBatchingForPlatform;
        private int staticBatching;
        private int dynamicBatching;

        private readonly List<string> replacedAssets = new List<string>();
        private readonly List<KeyValuePair<ModelImporter, ModelImporterMeshCompression>> meshCompression = new List<KeyValuePair<ModelImporter, ModelImporterMeshCompression>>();

        [SerializeField]
        private bool stripAssets;

        [SerializeField]
        private bool backupAssets;

        [SerializeField]
        private bool compressMeshes;

        [SerializeField]
        private bool disableStaticBatching;

        public void Optimize()
        {
            if (stripAssets)
            {
                ReplaceAssetsWithDummies();
            }

            if (compressMeshes)
            {
                CompressMeshes();
            }

            if (disableStaticBatching)
            {
                DisableStaticBatching();
            }
        }

        public void Restore()
        {
            if (stripAssets)
            {
                RestoreAssetsWithDummies();
            }

            if (backupAssets)
            {
                var path = GetProjectPath() + Paths.assetBackupPath;
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            if (compressMeshes)
            {
                DecompressMeshes();
            }

            if (disableStaticBatching)
            {
                RestoreStaticBatching();
            }
        }

        private void DisableStaticBatching()
        {
            GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out staticBatching, out dynamicBatching);
            SetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, 0, dynamicBatching);
        }

        private void RestoreStaticBatching()
        {
            SetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, staticBatching, dynamicBatching);
        }

        private void CompressMeshes()
        {
            meshCompression.Clear();
            var guids = AssetDatabase.FindAssets("a:assets t:Model");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer)
                {
                    continue;
                }

                meshCompression.Add(new KeyValuePair<ModelImporter, ModelImporterMeshCompression>(importer, importer.meshCompression));
                importer.meshCompression = ModelImporterMeshCompression.High;
                EditorUtility.SetDirty(importer);
            }

            AssetDatabase.Refresh();
        }

        private void DecompressMeshes()
        {
            foreach (var pair in meshCompression)
            {
                var importer = pair.Key;
                var compression = pair.Value;

                importer.meshCompression = compression;
                EditorUtility.SetDirty(importer);
            }

            meshCompression.Clear();
            AssetDatabase.Refresh();
        }

        private void ReplaceAssetsWithDummies()
        {
            var backupPath = GetProjectPath() + Paths.assetBackupPath + "/";
            replacedAssets.Clear();

            var skip = new List<string>();

#if UNITY_2023_1_OR_NEWER
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
#else
            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
#endif
            foreach (Texture2D icon in icons)
            {
                skip.Add(Path.GetFullPath(AssetDatabase.GetAssetPath(icon)));
            }

#if UNITY_2023_1_OR_NEWER
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var activeBuildTargetName = NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);
            icons = PlayerSettings.GetIcons(activeBuildTargetName, IconKind.Any);
#else
            icons = PlayerSettings.GetIconsForTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif
            foreach (Texture2D icon in icons)
            {
                skip.Add(Path.GetFullPath(AssetDatabase.GetAssetPath(icon)));
            }

            var dummiesPath = Path.GetFullPath(Paths.dummiesPath);
            foreach (var path in Directory.EnumerateFiles(GetProjectPath() + "Assets/", "*", SearchOption.AllDirectories))
            {
                if (skip.Contains(path))
                {
                    continue;
                }

                var p = path.Substring(GetProjectPath().Length);

                var ext = Path.GetExtension(p);
                var dummyPath = dummiesPath + "/bin" + ext;

                // if we have a dummy for this extension
                if (File.Exists(dummyPath))
                {
                    // backup original file
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(backupPath + p));
                    FileUtil.ReplaceFile(GetProjectPath() + p, backupPath + p);

                    // replace with dummy
                    FileUtil.ReplaceFile(dummyPath, GetProjectPath() + p);

                    replacedAssets.Add(p);
                }
            }
        }

        private void RestoreAssetsWithDummies()
        {
            var backupPath = GetProjectPath() + Paths.assetBackupPath + "/";
            foreach (var p in replacedAssets)
            {
                FileUtil.ReplaceFile(backupPath + p, GetProjectPath() + p);
            }

            replacedAssets.Clear();
        }

        private string GetProjectPath()
        {
            return Application.dataPath.Substring(0, Application.dataPath.Length - 6);
        }
    }
}
