// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using UnityEditor;

    /// <summary>
    /// Represents a project-relative path to a potential asset (file or folder) in the asset database.
    /// </summary>
    internal sealed record AssetPath
    {
        private readonly string localPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPath"/> object.
        /// </summary>
        /// <param name="path">
        /// An absolute or project-relative  path to a potential asset (file or folder) in the asset database.
        ///  For example, 'c:/Unity Projects/My Project/Assets/StreamingAssets' would get converted into 'Assets/StreamingAssets'.
        /// <para>
        /// <example> 'c:/Unity Projects/My Project/Assets/StreamingAssets' </example>
        /// <example> 'Assets/StreamingAssets' </example>
        /// </para>
        /// <remarks>
        /// The <see cref="IsPartOfAssetDatabase"/> and <see cref="IsInsidePackageFolder"/> methods work most reliably
        /// when a relative path is provided.
        /// </remarks>
        /// </param>
        /// <exception cref="ArgumentNullException"> Thrown if a <see langword="null"/> path argument is provided. </exception>
        public AssetPath([DisallowNull] string path) => localPath = PathUtils.GetRelativePath(path);

        /// <returns> <see langword="true"/> if a file exists at this path; otherwise, <see langword="false"/>. </returns>
        public bool HasFile() => File.Exists(localPath);

        /// <returns> <see langword="true"/> if a directory exists at this path; otherwise, <see langword="false"/>. </returns>
        public bool HasFolder() => Directory.Exists(localPath);

        /// <summary>
        /// Gets a value indicating whether this path points to a location inside the 'Assets' folder
        /// or a local or embedded package folder, and does not point to a hidden file or a location inside a hidden folder.
        /// <para>
        /// This can be used to determine if it's possible to use the <see cref="UnityEditor.AssetDatabase"/> class with the location.
        /// </para>
        /// <remarks>
        /// NOTE: This method does not currently detect absolute paths pointing to local package folders as being part of the asset database.
        /// To avoid the issue, construct the <see cref="AssetPath"/> using a relative path ("Packages/...") instead.
        /// </remarks>
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this path points to a non-hidden location inside the asset database;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsPartOfAssetDatabase() => PathUtils.IsPartOfAssetDatabase(localPath);

        /// <summary>
        /// Gets a value indicating whether this path points to a location inside the 'Assets' folder.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this path points to a location inside the 'Assets' folder; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsInsideAssetsFolder() => PathUtils.IsInsideAssetsFolder(localPath);

        /// <summary>
        /// Gets a value indicating whether this path points to a location inside
        /// an embedded package (located under the "Packages" folder) or a local package folder.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this path points to a location inside a package folder; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsInsidePackageFolder() => PathUtils.IsInsidePackageFolder(localPath);

        /// <summary>
        /// Check whether an asset exists at the given path in the <see cref="AssetDatabase"/>.
        /// </summary>
        public bool ContainsAsset()
        {
            if (localPath is not { Length : > 0 } path)
            {
                return false;
            }

            if(path[^1] is '/' or '\\')
            {
                path = localPath.Substring(0, localPath.Length - 1);
            }

#if UNITY_6000_0_OR_NEWER
            return AssetDatabase.AssetPathExists(path);
#else
            return AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets) is { Length: > 0 };
#endif
        }

        /// <summary>
        /// Gets the absolute (full) representation of this path.
        /// </summary>
        public string ToFullPath() => PathUtils.GetFullPath(localPath);

        public override string ToString() => "\"" + localPath + "\"";
        public static implicit operator string(AssetPath assetPath) => assetPath?.localPath;
        public static implicit operator AssetPath(string fullOrLocalPath) => fullOrLocalPath is null ? null : new AssetPath(fullOrLocalPath);
    }
}
