// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Utility methods for manipulating files and folders in the asset database.
    /// <para>
    /// All methods make sure that project relative paths are being used, and will handle updating metafiles properly.
    /// </para>
    /// </summary>
    internal static class AssetUtils
    {
        /// <summary>
        /// Deletes the file located at the given path, as well its .meta file, if one exists.
        /// </summary>
        /// <param name="filePath"> Path to the file to delete. </param>
        /// <returns>
        /// <see langword="true"/> if the file and any metadata it had have been successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool DeleteFile([DisallowNull] AssetPath filePath)
        {
            if(!filePath.HasFile())
            {
                return false;
            }

            if(AssetDatabase.DeleteAsset(filePath))
            {
                return true;
            }

            if(!filePath.IsPartOfAssetDatabase())
            {
                Debug.LogWarning($"Will not delete file at path '{filePath}', because it is not part of the asset database. Use File.Delete instead to remove files located outside the project folder.");
                return false;
            }

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.DeleteAsset(filePath);
        }

        /// <summary>
        /// Creates a prefab from the given <see paramref="instance"/> at the specified <see paramref="assetPath"/>,
        /// and connects it to be an instance of the created prefab.
        /// <remarks>
        /// If a file already exists at the given <see cref="assetPath"/>, then a number suffix will be added to the
        /// filename, to avoid overwriting the existing file.
        /// <para>
        /// The created prefab asset will not be forced to be imported synchronously; use <see cref="ImportAsset"/> to
        /// do this if needed.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <param name="instance">
        /// GameObject from which to create the prefab, and which should be made into an instance of the created prefab.
        /// </param>
        /// <param name="assetPath">
        /// Path for the prefab to create.
        /// </param>
        /// <param name="interactionMode">Indicates whether the user should be prompted for confirmation or performed automatically.</param>
        /// <returns> GameObject The root GameObject of the saved Prefab Asset, if available. </returns>
        [return: MaybeNull]
        public static GameObject CreatePrefab(GameObject instance, ref AssetPath assetPath, InteractionMode interactionMode)
        {
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, assetPath, interactionMode);
            assetPath = AssetDatabase.GetAssetPath(prefab);
            return prefab;
        }

        public static AssetPath GenerateUniqueAssetPath(AssetPath assetPath)
        {
            AssetPath uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            while(uniquePath.HasFile())
            {
                ImportAsset(uniquePath, ImportAssetOptions.ForceSynchronousImport);
                uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            return uniquePath;
        }

        /// <summary>
        /// Deletes the directory located at the given path, as well its .meta file, if one exists.
        /// </summary>
        /// <param name="folderPath"> Path to the folder to delete. </param>
        /// <returns>
        /// <see langword="true"/> if the folder and any metadata it had have been successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool DeleteFolder([DisallowNull] AssetPath folderPath) => folderPath.HasFolder() && AssetDatabase.DeleteAsset(folderPath);

        /// <summary>
        /// Deletes the directory located at the given path, if there are no files or folders inside of it.
        /// </summary>
        /// <param name="folderPath"> Path to the directory to delete if it is empty. </param>
        /// <returns>
        /// <see langword="true"/> if the folder and any metadata it had have been successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool DeleteFolderIfEmpty([DisallowNull] AssetPath folderPath)
        {
            if (!folderPath.HasFolder())
            {
                return false;
            }

            var directoryInfo = new DirectoryInfo(folderPath);

            var isEmpty = !directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Any()
                          && !directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories).Any();

            return isEmpty && AssetDatabase.DeleteAsset(folderPath);
        }

        /// <summary>
        /// Imports the asset at the given <paramref name="assetPath"/>.
        /// <para>
        /// If no asset exists at the given path, then logs a warning.
        /// </para>
        /// </summary>
        /// <param name="assetPath"> Path to the asset to import. </param>
        /// <param name="importAssetOptions"> (Optional) Asset importing options to use. </param>
        public static void ImportAsset(AssetPath assetPath, ImportAssetOptions importAssetOptions = ImportAssetOptions.Default) => AssetDatabase.ImportAsset(assetPath, importAssetOptions);

        /// <summary>
        /// Creates all folders and subfolders in the specified path unless they already exist.
        /// </summary>
        /// <param name="folderPath"> Path to the folder to create. </param>
        /// <returns>
        /// <see langword="true"/> if the folder was successfully created or it already existed; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool CreateFolder(AssetPath folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            string parentFolderPath;
            Stack<string> parentFoldersToCreate = new();
            for (parentFolderPath = Path.GetDirectoryName(folderPath); parentFolderPath is not null && !AssetDatabase.IsValidFolder(parentFolderPath); parentFolderPath = Path.GetDirectoryName(parentFolderPath))
            {
                parentFoldersToCreate.Push(parentFolderPath);
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                string newFolderName;
                foreach (var create in parentFoldersToCreate)
                {
                    parentFolderPath = Path.GetDirectoryName(create);
                    newFolderName = Path.GetFileName(create);
                    if (AssetDatabase.CreateFolder(parentFolderPath, newFolderName).Length == 0)
                    {
                        return false;
                    }
                }

                parentFolderPath = Path.GetDirectoryName(folderPath);
                newFolderName = Path.GetFileName(folderPath);
                return AssetDatabase.CreateFolder(parentFolderPath, newFolderName).Length > 0;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Duplicates the file from <paramref name="sourcePath"/> to <paramref name="destinationPath"/>.
        /// <para>
        /// If there is an existing file at the destination filepath, it will get overwritten.
        /// </para>
        /// <para>
        /// If destination folder does not exist, it will be created.
        /// </para>
        /// <para>
        /// If destination is a project relative path, the file will be imported using the specified options.
        /// </para>
        /// </summary>
        /// <param name="sourcePath"> Source filepath. </param>
        /// <param name="destinationPath"> Destination filepath. </param>
        /// <param name="importAssetOptions"> (Optional) Asset importing options to use for the created copy. </param>
        /// <returns> <see langword="true"/> if the file was copied successful; otherwise, <see langword="true"/>. </returns>
        public static bool CopyFile(AssetPath sourcePath, AssetPath destinationPath, ImportAssetOptions importAssetOptions = ImportAssetOptions.Default)
        {
            var destinationIsPartOfAssetDatabase = destinationPath.IsPartOfAssetDatabase();

            if (Path.GetDirectoryName(destinationPath) is { } destinationFolder)
            {
                if (destinationIsPartOfAssetDatabase)
                {
                    CreateFolder(destinationFolder);
                }
                else
                {
                    Directory.CreateDirectory(destinationFolder);
                }
            }

            if (!destinationIsPartOfAssetDatabase || !sourcePath.ContainsAsset())
            {
                File.Copy(sourcePath, destinationPath, true);

                if (destinationIsPartOfAssetDatabase)
                {
                    ImportAsset(destinationPath, importAssetOptions);
                }

                return true;
            }

            var success = AssetDatabase.CopyAsset(sourcePath, destinationPath);
            if (importAssetOptions != ImportAssetOptions.Default && success)
            {
                ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
            }

            return success;
        }

        /// <summary>
        /// Move a file from the source path to the destination path.
        /// </summary>
        /// <param name="source">Source file's relative path from the project root.</param>
        /// <param name="destination">Destination file's relative path from the project root.</param>
        /// <returns>A <see cref="String"/> containing an error if the file could not be moved, or an empty string if
        /// the move operation was successful.</returns>
        public static string MoveFile(string source, string destination)
        {
            var result = AssetDatabase.ValidateMoveAsset(source, destination);
            if (string.IsNullOrEmpty(result))
            {
                AssetDatabase.MoveAsset(source, destination);
                return "";
            }

            return result;
        }
    }
}
