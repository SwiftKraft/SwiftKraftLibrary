// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEditor;
    using Object = UnityEngine.Object;

    public class AssetUtilsTests : CoherenceTest
    {
        #region IOUtils.DeleteFile

        [Test]
        public void DeleteFile_ShouldDelete_FileWithAnExtension()
        {
            using var rootFolder = MakeTempFolder();
            var file = CreateFile(rootFolder, "File.txt");

            var result = AssetUtils.DeleteFile(file);

            Assert.IsTrue(result);
            Assert.IsTrue(file.Existed);
            Assert.IsTrue(file.MetaFileExisted);
            Assert.IsFalse(file.Exists);
            Assert.IsFalse(file.MetaFileExists);
        }

        [Test]
        public void DeleteFile_ShouldDelete_FileWithNoExtension()
        {
            using var rootFolder = MakeTempFolder();
            var file = CreateFile(rootFolder, "File");

            var result = AssetUtils.DeleteFile(file);

            Assert.IsTrue(result);
            Assert.IsTrue(file.Existed);
            Assert.IsTrue(file.MetaFileExisted);
            Assert.IsFalse(file.Exists);
            Assert.IsFalse(file.MetaFileExists);
        }

        [Test]
        public void DeleteFile_ShouldNotDelete_FolderWithDotInItsName()
        {
            using var rootFolder = MakeTempFolder();
            var folder = CreateFolder(rootFolder, "Folder.txt");

            var result = AssetUtils.DeleteFile(folder);

            Assert.IsFalse(result);
            Assert.IsTrue(folder.Existed);
            Assert.IsTrue(folder.MetaFileExisted);
            Assert.IsTrue(folder.Exists);
            Assert.IsTrue(folder.MetaFileExists);
        }

        #endregion

        #region IOUtils.DeleteFolder

        [Test]
        public void DeleteFolder_ShouldNotDelete_FileWithNoExtension()
        {
            using var rootFolder = MakeTempFolder();
            var file = CreateFile(rootFolder, "File");

            var result = AssetUtils.DeleteFolder(file);

            Assert.IsFalse(result);
            Assert.IsTrue(file.Existed);
            Assert.IsTrue(file.MetaFileExisted);
            Assert.IsTrue(file.Exists);
            Assert.IsTrue(file.MetaFileExists);
        }

        [Test]
        public void DeleteFolder_ShouldDelete_FolderContainingFileAndSubfolder()
        {
            using var rootFolder = MakeTempFolder();
            var folder = CreateFolder(rootFolder, "Folder");
            var file = CreateFile(rootFolder, "File");

            var result = AssetUtils.DeleteFolder(rootFolder);

            Assert.IsTrue(folder.Existed);
            Assert.IsTrue(file.Existed);
            Assert.IsTrue(result);
            Assert.IsTrue(rootFolder.Existed);
            Assert.IsTrue(rootFolder.MetaFileExisted);
            Assert.IsFalse(rootFolder.Exists);
            Assert.IsFalse(rootFolder.MetaFileExists);
        }

        #endregion

        #region IOUtils.DeleteFolderIfEmpty

        [Test]
        public void DeleteFolderIfEmpty_ShouldDelete_EmptyFolder()
        {
            using var rootFolder = MakeTempFolder();

            var result = AssetUtils.DeleteFolderIfEmpty(rootFolder);

            Assert.IsTrue(result);
            Assert.IsTrue(rootFolder.Existed);
            Assert.IsTrue(rootFolder.MetaFileExisted);
            Assert.IsFalse(rootFolder.Exists);
            Assert.IsFalse(rootFolder.MetaFileExists);
        }

        [Test]
        public void DeleteFolderIfEmpty_ShouldNotDelete_FolderContainingAFile()
        {
            using var rootFolder = MakeTempFolder();
            var file = CreateFile(rootFolder, "File");

            var result = AssetUtils.DeleteFolderIfEmpty(rootFolder);

            Assert.IsTrue(file.Existed);
            Assert.IsFalse(result);
            Assert.IsTrue(rootFolder.Exists);
            Assert.IsTrue(rootFolder.MetaFileExists);
        }

        [Test]
        public void DeleteFolderIfEmpty_ShouldNotDelete_FolderContainingASubfolder()
        {
            using var rootFolder = MakeTempFolder();
            var folder = CreateFolder(rootFolder, "Folder");

            var result = AssetUtils.DeleteFolderIfEmpty(rootFolder);

            Assert.IsTrue(folder.Existed);
            Assert.IsFalse(result);
            Assert.IsTrue(rootFolder.Exists);
            Assert.IsTrue(rootFolder.MetaFileExists);
        }

        #endregion

        #region IOUtils.ImportAsset

        [Test]
        public void ImportAsset_ShouldImport_Folder()
        {
            using var rootFolder = MakeTempFolder();
            var folderPath = Path.Combine(rootFolder, "Folder");
            Directory.CreateDirectory(folderPath);
            var assetAtPathBeforeImport = AssetDatabase.LoadAssetAtPath<Object>(folderPath);

            AssetUtils.ImportAsset(folderPath);

            Assert.IsFalse(assetAtPathBeforeImport);
            var assetAtPathAfterImport = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            Assert.IsTrue(assetAtPathAfterImport);
        }

        [Test]
        public void ImportAsset_ShouldImport_File()
        {
            using var rootFolder = MakeTempFolder();
            var filePath = Path.Combine(rootFolder, "File");
            File.Create(filePath).Close();
            var assetAtPathBeforeImport = AssetDatabase.LoadAssetAtPath<Object>(filePath);

            AssetUtils.ImportAsset(filePath);

            Assert.IsFalse(assetAtPathBeforeImport);
            var assetAtPathAfterImport = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            Assert.IsTrue(assetAtPathAfterImport);
        }

        #endregion

        #region IOUtils.CreateFolder

        [Test]
        public void CreateFolder_Should_CreateFolderAndMetaFileImmediately()
        {
            using var rootFolder = MakeTempFolder();
            var folderPath = Path.Combine(rootFolder, "Folder");
            var existed = Directory.Exists(folderPath);

            var result = AssetUtils.CreateFolder(folderPath);

            Assert.IsTrue(result);
            Assert.IsFalse(existed);
            Assert.IsTrue(Directory.Exists(folderPath));
            var metafilePath = PathUtils.GetMetaFilePath(folderPath);
            Assert.IsTrue(File.Exists(metafilePath));
        }

        [Test]
        public void CreateFolder_ShouldReturnTrue_IfFolderAlreadyExists()
        {
            using var rootFolder = MakeTempFolder();
            var folder = CreateFolder(rootFolder, "Folder");
            var existed = Directory.Exists(folder);

            var result = AssetUtils.CreateFolder(folder);

            Assert.IsTrue(result);
            Assert.IsTrue(existed);
            Assert.IsTrue(folder.Exists);
        }

        [Test]
        public void CreateFolder_ShouldBeAbleToHandle_CreatingMultipleNestedFolders()
        {
            using var rootFolder = MakeTempFolder();
            var middleFolder = Path.Combine(rootFolder, "Middle");
            var leafFolder = Path.Combine(middleFolder, "Leaf");
            var existed = Directory.Exists(middleFolder);

            var result = AssetUtils.CreateFolder(leafFolder);

            Assert.IsTrue(result);
            Assert.IsFalse(existed);
            Assert.IsTrue(Directory.Exists(leafFolder));
        }

        #endregion

        #region HelperMethods

        public override void OneTimeTearDown()
        {
            AssetUtils.DeleteFolderIfEmpty(Paths.streamingAssetsPath);
            base.OneTimeTearDown();
        }

        private TemporaryFile CreateFile(TemporaryFolder parentFolder, string filename)
        {
            var path = Path.Combine(parentFolder, filename);
            File.Create(path).Close();
            AssetUtils.ImportAsset(path);
            return new TemporaryFile(path);
        }

        private TemporaryFolder CreateFolder(TemporaryFolder parentFolder, string filename)
        {
            return CreateFolder(Path.Combine(parentFolder, filename));
        }

        private TemporaryFolder CreateFolder(string path)
        {
            AssetUtils.CreateFolder(path);
            var result = new TemporaryFolder(path);
            return result;
        }

        private TemporaryFolder MakeTempFolder([CallerMemberName] string folderName = null)
        {
            var path = Path.Combine(Paths.streamingAssetsPath, "[TEMP] " + folderName);
            return CreateFolder(path);
        }

        private readonly struct TemporaryFile : IDisposable
        {
            private readonly string path;
            public bool Existed { get; }
            public bool MetaFileExisted { get; }
            public bool Exists => File.Exists(path);
            public bool MetaFileExists => File.Exists(PathUtils.GetMetaFilePath(path));

            public TemporaryFile(string path)
            {
                this.path = path;
                Existed = File.Exists(path);
                MetaFileExisted = File.Exists(PathUtils.GetMetaFilePath(path));
            }

            public void Dispose()
            {
                AssetUtils.DeleteFile(path);
            }

            public static implicit operator string(TemporaryFile file)
            {
                return file.path;
            }

            public static implicit operator AssetPath(TemporaryFile file)
            {
                return new AssetPath(file.path);
            }
        }

        private readonly struct TemporaryFolder : IDisposable
        {
            private readonly string path;
            public bool Existed { get; }
            public bool MetaFileExisted { get; }
            public bool Exists => Directory.Exists(path);
            public bool MetaFileExists => File.Exists(PathUtils.GetMetaFilePath(path));

            public TemporaryFolder(string path)
            {
                this.path = path;
                Existed = Directory.Exists(path);
                MetaFileExisted = File.Exists(PathUtils.GetMetaFilePath(path));
            }

            public void Dispose()
            {
                AssetUtils.DeleteFolder(path);
            }

            public static implicit operator string(TemporaryFolder folder)
            {
                return folder.path;
            }

            public static implicit operator AssetPath(TemporaryFolder file)
            {
                return new AssetPath(file.path);
            }
        }

        #endregion
    }
}
