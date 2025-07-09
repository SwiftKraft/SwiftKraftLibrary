namespace Coherence.Editor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using static Paths;

    /// <summary>
    /// Utility methods related to file and directory paths.
    /// </summary>
    internal static class PathUtils
    {
        /// <summary>
        /// Converts the given <paramref name="path"/> into a path relative to the project directory.
        /// <para>
        /// Examples:
        /// 'c:/Unity Projects/My Project/Assets/StreamingAssets' => 'Assets/StreamingAssets'.
        /// 'c:/Unity Projects/My Project/Library/PackageCache' => 'Library/PackageCache'.
        /// 'c:/Unity Projects/My Project/Packages/com.unity.test-runner' => 'Packages/com.unity.test-runner'.
        /// 'c:/file.exe' => 'c:/file.exe'.
        /// </para>
        /// </summary>
        /// <param name="path"> An absolute or a relative path to convert. </param>
        /// <returns> A path relative to the project directory. </returns>
        /// <exception cref="ArgumentNullException"> Thrown if a null <paramref name="path"/> argument is provided. </exception>
        [Pure]
        [return:NotNull]
        public static string GetRelativePath([DisallowNull] string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Length <= projectAbsolutePath.Length)
            {
                return path;
            }

            if (!AreEqual(path, projectAbsolutePath, projectAbsolutePath.Length))
            {
                return path;
            }

            return path.Substring(projectAbsolutePath.Length + 1);
        }

        /// <summary>
        /// Converts the given <paramref name="path"/> into an absolute path.
        /// <para>
        /// Examples:
        /// 'Assets/StreamingAssets' => 'c:/Unity Projects/My Project/Assets/StreamingAssets'.
        /// 'Library/PackageCache' => 'c:/Unity Projects/My Project/Library/PackageCache'.
        /// 'Packages/com.unity.test-runner' => 'c:/Unity Projects/My Project/Packages/com.unity.test-runner'.
        /// 'c:/file.exe' => 'c:/file.exe'.
        /// </para>
        /// </summary>
        /// <param name="path"> An absolute or a relative path to convert.
        /// </param>
        /// <returns> A path relative to the project directory. </returns>
        /// <exception cref="ArgumentNullException"> Thrown if a null <paramref name="path"/> argument is provided. </exception>
        public static string GetFullPath(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Length == 0)
            {
                return projectAbsolutePath;
            }

            if (AreEqual(path, projectAbsolutePath, projectAbsolutePath.Length))
            {
                return path;
            }

            if (path.Length >= 2 && path[1] is ':')
            {
                return path;
            }

            var fullPath = Path.GetFullPath(path);
            if (Path.DirectorySeparatorChar == '\\')
            {
                return path.Contains('/') ? fullPath.Replace('\\', '/') : fullPath;
            }

            return path.Contains('\\') ? fullPath.Replace('/', '\\') : fullPath;
        }

        /// <summary>
        /// Gets the path where the meta file for the asset or directory at the given <paramref name="path"/> would be located.
        /// <para>
        /// For example, <paramref name="path"/> 'Assets/StreamingAssets' would give the result 'Assets/StreamingAssets.meta'.
        /// </para>
        /// </summary>
        /// <param name="path"> An absolute or a relative path to an asset or a directory. </param>
        /// <returns>
        /// An absolute or relative path (same as the input <paramref name="path"/>) to where the meta file for the given asset or directory would be located.
        /// </returns>
        /// <exception cref="ArgumentNullException"> Thrown if a null <paramref name="path"/> argument is provided. </exception>
        [Pure]
        [return:NotNull]
        public static string GetMetaFilePath([DisallowNull] string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return Path.ChangeExtension(path, Path.GetExtension(path) + unityMetaFileExtension);
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="path"/> points to a location inside the 'Assets' folder.
        /// </summary>
        /// <param name="path"> An absolute or a relative path to an asset or a directory. </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/>  points to a location inside the 'Assets' folder;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool IsInsideAssetsFolder(string path)
        {
            if (path.Length < "Assets".Length)
            {
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            var fullPathLength = fullPath.Length;
            var assetsAbsolutePathLength = assetsAbsolutePath.Length;
            if (fullPathLength < assetsAbsolutePathLength)
            {
                return false;
            }

            if (!AreEqual(fullPath, assetsAbsolutePath, assetsAbsolutePathLength))
            {
                return false;
            }

            return fullPathLength == assetsAbsolutePathLength || fullPath[assetsAbsolutePathLength] is '/' or '\\';
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="path"/> points to a location inside the 'Assets' folder
        /// or a local or embedded package folder, and does not point to a hidden file or a location inside a hidden folder.
        /// <para>
        /// This can be used to determine if it's possible to use the <see cref="UnityEditor.AssetDatabase"/> class with the location.
        /// </para>
        /// <remarks>
        /// NOTE: This method does not currently detect absolute paths pointing to local package folders as being part of the asset database.
        /// To avoid the issue, provide a relative path ("Packages/...") instead.
        /// </remarks>
        /// </summary>
        /// <param name="path"> An absolute or a relative path (preferably the latter) to an asset or a directory. </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> points to a non-hidden location inside the asset database;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool IsPartOfAssetDatabase(string path) => (IsInsideAssetsFolder(path) || IsInsidePackageFolder(path)) && !IsHiddenAsset(path);

        /// <summary>
        /// Gets a value indicating whether the asset or directory at the given <paramref name="path"/> is hidden
        /// (ignored by the <see cref="UnityEditor.AssetDatabase"/>).
        /// </summary>
        /// <param name="path"> An absolute or a relative path to an asset or a directory. </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> contains file or folder names that would cause it be
        /// ignored by the <see cref="UnityEditor.AssetDatabase"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <seealso href="https://docs.unity3d.com/Manual/SpecialFolders.html"/>
        internal static bool IsHiddenAsset(string path)
        {
            while(!string.IsNullOrEmpty(path))
            {
                var fileOrDirectoryName = Path.GetFileName(path);

                // Files and folders which end with '~' are ignored.
                if (fileOrDirectoryName.EndsWith('~'))
                {
                    return true;
                }

                // Files with the extension ".tmp" are ignored.
                if (fileOrDirectoryName.EndsWith(".tmp"))
                {
                    return true;
                }

                // Files and folders named "cvs" are ignored.
                if (fileOrDirectoryName is "cvs")
                {
                    return true;
                }

                // Files and folders which start with . are ignored, except for those under 'StreamingAssets'.
                if (fileOrDirectoryName.StartsWith('.') && !path.Contains("StreamingAssets"))
                {
                    return true;
                }

                path = Path.GetDirectoryName(path);
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether <paramref name="path"/> points to a location inside
        /// an embedded package (located under the "Packages" folder) or a local package folder.
        /// <remarks>
        /// NOTE: This method does not currently recognize absolute paths pointing to local package folders.
        /// To avoid the issue, provide a relative path ("Packages/...") instead.
        /// </remarks>
        /// </summary>
        /// <param name="path"> An absolute or a relative path (preferably the latter) to an asset or a directory. </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/>  points to a package folder;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool IsInsidePackageFolder(string path)
        {
            if(path.Length == 0)
            {
                return false;
            }

            if (path.StartsWith("Packages", StringComparison.OrdinalIgnoreCase))
            {
                return path.Length >= "Packages/X".Length && path["Packages".Length] is '/' or '\\';
            }

            var fullPath = Path.GetFullPath(path);
            var fullPathLength = fullPath.Length;
            var packagesAbsolutePathLength = packagesAbsolutePath.Length;
            if (fullPathLength < packagesAbsolutePathLength)
            {
                return false;
            }

            if (!AreEqual(fullPath, packagesAbsolutePath, packagesAbsolutePathLength))
            {
                return false;
            }

            return fullPathLength == packagesAbsolutePathLength || fullPath[packagesAbsolutePathLength] is '/' or '\\';
        }

        /// <summary>
        /// Gets a value indicating whether the two paths are the same upto a certain number of characters.
        /// <para>
        /// Directory separator characters '/' and '\' are considered equal.
        /// </para>
        /// </summary>
        /// <param name="pathA"> First path to compare. </param>
        /// <param name="pathB"> Second path to compare. </param>
        /// <param name="charactersToCheck"> Total number of characters to test. </param>
        /// <returns>
        /// <see langword="true"/> if the first <paramref name="charactersToCheck"/> characters
        /// in <paramref name="pathA"/> and <paramref name="pathB"/> are the same (with
        /// directory separator characters '/' and '\' considered equal).
        /// <para>
        /// <see langword="false"/> if <paramref name="pathA"/> or <paramref name="pathB"/>
        /// contain fewer characters than <paramref name="charactersToCheck"/>.
        /// </para>
        /// </returns>
        private static bool AreEqual(string pathA, string pathB, int charactersToCheck)
        {
            if (pathA.Length < charactersToCheck || pathB.Length < charactersToCheck)
            {
                return false;
            }

            for (var i = 0; i < charactersToCheck; i++)
            {
                if (pathA[i].Equals(pathB[i]))
                {
                    continue;
                }

                if (pathA[i] is '/' or '\\' && pathB[i] is '/' or '\\')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
