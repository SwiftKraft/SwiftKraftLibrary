// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal static class AssetMover
    {
        internal static bool MoveAssetPathIfNeeded(Type providerType, Object obj)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);

            var isResources = assetPath.Contains("/Resources/");

            var lastIndex = assetPath.LastIndexOf("/") + 1;

            var fileName = assetPath.Remove(0, lastIndex);

            var newPath = string.Empty;

            if (!isResources && providerType == typeof(ResourcesProvider))
            {
                newPath = $"Assets/Resources/{fileName}";
            }

            if (isResources && providerType != typeof(ResourcesProvider))
            {
                newPath = assetPath.Replace("/Resources/", "/Resources_moved/");
            }

            if (!string.IsNullOrEmpty(newPath))
            {
                bool fileAlreadyExists = File.Exists(newPath);

                var message = GetBaseMessage(providerType);

                if (fileAlreadyExists)
                {
                    message += $"Tried to move the asset to {newPath}. But an asset with the same name already exists there.";

                    EditorUtility.DisplayDialog("Failed to change Object Provider", message, "Ok");

                    return false;
                }

                message += $"We will move the asset to: \n{newPath}";

                if (EditorUtility.DisplayDialog("Move Asset", message, "Yes", "No"))
                {
                    var directoryName = Path.GetDirectoryName(newPath);
                    if (directoryName != null && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                        AssetDatabase.Refresh();
                    }

                    var result = AssetDatabase.MoveAsset(assetPath, newPath);

                    if (!string.IsNullOrEmpty(result))
                    {
                        Debug.LogError("Error moving asset: " + result);
                        return false;
                    }

                    return true;
                }

                return false;
            }

            return true;
        }

        private static string GetBaseMessage(Type providerType)
        {
            return $"Changing to {INetworkObjectDrawer.typeDisplayNames[providerType.FullName].text} requires moving the asset {(providerType == typeof(ResourcesProvider) ? "to" : "from" )} the Resources folder.\n\n";
        }
    }
}
