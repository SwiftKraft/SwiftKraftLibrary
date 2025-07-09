// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using System.Text.RegularExpressions;

    [System.Serializable]
    internal class VersionData
    {
        private GUIContent label;
        private int selected;
        public SerializedProperty prop;
        public bool populated;

        private string productID;

        public GUIContent[] versions = new GUIContent[] { };
        Portal.ReleaseList releases;

        public VersionData(GUIContent label, string productID, SerializedProperty prop)
        {
            this.productID = productID;

            this.label = label;
            this.prop = prop;

            string currentVersion = prop.stringValue;
            if (string.IsNullOrEmpty(currentVersion))
            {
                currentVersion = "None";
            }

            versions = new GUIContent[] { new GUIContent(currentVersion) };
        }

        public void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(!populated);
            EditorGUI.BeginChangeCheck();
            int v = EditorGUILayout.Popup(label, selected, versions);
            if (EditorGUI.EndChangeCheck())
            {
                selected = v;
                _ = DownloadAssets();
                prop.serializedObject.Update();
                prop.stringValue = selected == 0 ? string.Empty : versions[v].text;
                if (prop.serializedObject.ApplyModifiedProperties())
                {
                    foreach (var target in prop.serializedObject.targetObjects)
                    {
                        AssetDatabase.SaveAssetIfDirty(target);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        public bool FetchReleases()
        {
            try
            {
                releases = Portal.ReleaseList.Get(productID);
                LoadReleases(releases);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private void LoadReleases(Portal.ReleaseList releases)
        {
            versions = new GUIContent[1 + releases.releases.Length];
            versions[0] = EditorGUIUtility.TrTextContent("None");
            selected = 0;

            for (int i = 0; i < releases.releases.Length; i++)
            {
                Portal.Release r = releases.releases[i];
                versions[1 + i] = EditorGUIUtility.TrTextContent(r.version, r.published_at);
                if (r.version == prop.stringValue)
                {
                    selected = 1 + i;
                }
            }
            populated = true;
        }

        private bool DownloadAssets()
        {
            // TODO download assets to temp folder, then if every download is OK, move them to proper folders

            if (selected == 0)
            {
                return true;
            }

            var version = releases.releases[selected - 1].version;
            var selectedRelease = Portal.Release.Get(productID, version);
            bool hasErrors = false;
            for (int i = 0; i < selectedRelease.assets.Length; i++)
            {
                Portal.Asset asset = selectedRelease.assets[i];

                try
                {
                    if (asset.Download(out byte[] data))
                    {
                        Regex rx = new Regex(@"(?<name>[^_]+)_(?<platform>[^_]+)_(?<arch>[^_.]+)\w*\.?(?<ext>\w*).zip");
                        MatchCollection matches = rx.Matches(asset.name);
                        GroupCollection groups = matches[0].Groups;
                        string name = groups["name"].Value;
                        string platform = groups["platform"].Value;
                        string arch = groups["arch"].Value;
                        string ext = groups["ext"].Value;
                        string fileName = ext == "exe" ? name + ".exe" : name;

                        string outputFolderName = platform;

                        if (platform.ToLower() == "linux" &&
                            arch.ToLower() == "arm64")
                        {
                            outputFolderName = "linux-arm64";
                        }

                        _ = Directory.CreateDirectory($"{Paths.toolsPath}/{outputFolderName}");
                        string zipFile = Path.GetFullPath($"{Paths.toolsPath}/{outputFolderName}/{fileName}.zip");
                        string finalPath = Path.GetFullPath($"{Paths.toolsPath}/{outputFolderName}/{fileName}");
                        File.WriteAllBytes(zipFile, data);

                        string decompressPath = Path.GetFullPath($"{Paths.toolsPath}/{outputFolderName}");
                        ZipUtils.Unzip(zipFile, decompressPath);
                        File.Delete(zipFile);

                        string originalName = name + "_" + platform + "_" + arch;
                        originalName = ext == "exe" ? originalName + ".exe" : originalName;
                        string originalPath = Path.GetFullPath($"{Paths.toolsPath}/{outputFolderName}/{originalName}");
                        File.Delete(finalPath);
                        File.Move(originalPath, finalPath);

                        ProcessUtil.FixUnixPermissions(finalPath);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    hasErrors = true;
                }
            }
            return hasErrors;
        }
    }
}
