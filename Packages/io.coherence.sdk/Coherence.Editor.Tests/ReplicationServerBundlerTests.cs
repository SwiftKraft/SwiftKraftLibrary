// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Coherence.Tests;
    using Log;
    using NUnit.Framework;
    using UnityEditor;

    public class ReplicationServerBundlerTests : CoherenceTest
    {
        [Test]
        public void DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets_ForAllSupportedPlatforms()
        {
            var supportedPlatforms = ReplicationServerBinaries.GetSupportedPlatforms();
            var skippedPlatforms = new List<BuildTarget>();
            foreach (var buildTarget in supportedPlatforms)
            {
                var skipped = !DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets(buildTarget);
                if (skipped)
                {
                    skippedPlatforms.Add(buildTarget);
                }
            }

            if (skippedPlatforms.Any())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("DeleteRsFromStreamingAssets test was skipped for the following platforms:");
                foreach (var buildTarget in skippedPlatforms)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(buildTarget);
                    stringBuilder.Append(" (binary not found at ");
                    stringBuilder.Append(ReplicationServerBinaries.GetToolsPath(buildTarget));
                    stringBuilder.Append(").");
                }

                stringBuilder.Append("\n\nThis is not unusual; with dev testing it's common to install only OS-related bundle for development.");
                Assert.Ignore(stringBuilder.ToString());
            }
        }

        [Test]
        public void BundleWithStreamingAssets_Should_Fail_If_ReplicationServer_Binary_Does_Not_Exist()
        {
            var supportedPlatforms = ReplicationServerBinaries.GetSupportedPlatforms();

            foreach (var platform in supportedPlatforms)
            {
                ReplicationServerBundler.Logger = logger;
                var sourcePath = ReplicationServerBinaries.GetToolsPath(platform);
                string originalPath = null;
                string temporaryPath = null;
                if (sourcePath.HasFile())
                {
                    originalPath = sourcePath;
                    temporaryPath = originalPath + "_temp";

                    if (File.Exists(temporaryPath))
                    {
                        // a previous run must have crashed, remove it.
                        File.Delete(temporaryPath);
                    }

                    File.Move(originalPath, temporaryPath);
                }

                var loggerWas = ReplicationServerBundler.Logger;
                bool success;

                try
                {
                    success = ReplicationServerBundler.BundleWithStreamingAssets(platform);
                }
                finally
                {
                    ReplicationServerBundler.Logger = loggerWas;

                    if (temporaryPath is not null)
                    {
                        File.Move(temporaryPath, originalPath);
                    }
                }

                Assert.That(logger.GetCountForErrorID(Error.EditorRSBundlerMissingRS), Is.EqualTo(1));
                Assert.That(success, Is.False);
            }
        }

        [Test]
        public void BundleWithStreamingAssets_Should_Work_ForAllSupportedPlatforms()
        {
            foreach (var platform in ReplicationServerBinaries.GetSupportedPlatforms())
            {
                var sourcePath = ReplicationServerBinaries.GetToolsPath(platform);
                var destinationPath = ReplicationServerBinaries.GetStreamingAssetsPath(platform);

                string tempFileCreated = null;
                string tempDirectoryCreated = null;
                try
                {
                    if (!sourcePath.HasFile())
                    {
                        var directoryPath = Path.GetDirectoryName(sourcePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                            tempDirectoryCreated = directoryPath;
                        }

                        File.WriteAllText(sourcePath, "test");
                        tempFileCreated = sourcePath;
                    }

                    var success = ReplicationServerBundler.BundleWithStreamingAssets(platform);
                    Assert.That(destinationPath.HasFile(), Is.True, "Failed to bundle replication server from {0} for platform {1}.", sourcePath, platform);
                    Assert.That(success, Is.True, "Failed to bundle replication server from {0} for platform {1}.", sourcePath, platform);
                    Assert.That(logger.GetCountForErrorID(Error.EditorRSBundlerMissingRS), Is.Zero);
                    Assert.That(logger.GetCountForErrorID(Error.EditorRSBundlerException), Is.Zero);
                }
                finally
                {
                    if (tempFileCreated is not null)
                    {
                        File.Delete(tempFileCreated);
                    }

                    if (tempDirectoryCreated is not null)
                    {
                        Directory.Delete(tempDirectoryCreated);
                    }

                    AssetUtils.DeleteFolderIfEmpty(Paths.streamingAssetsPath);
                }
            }
        }

        /// <returns>
        /// true if replication server file for the platform was found, and was able to perform the test; otherwise, false.
        /// </returns>
        private bool DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets(BuildTarget buildTarget)
        {
            var sourcePath = ReplicationServerBinaries.GetToolsPath(buildTarget);
            if (!sourcePath.HasFile())
            {
                return false;
            }

            var destinationPath = ReplicationServerBinaries.GetStreamingAssetsPath(buildTarget);
            AssetPath metaFilePath = PathUtils.GetMetaFilePath(destinationPath);
            ReplicationServerBundler.BundleWithStreamingAssets(buildTarget);

            var fileExisted = destinationPath.HasFile();
            var metaFileExisted = metaFilePath.HasFile();

            var wasDeleted = ReplicationServerBundler.DeleteRsFromStreamingAssets(buildTarget);

            Assert.IsTrue(fileExisted, "BundleWithStreamingAssets failed for platform {0} at {1}.", buildTarget, destinationPath);
            Assert.IsTrue(metaFileExisted, "BundleWithStreamingAssets failed to create .meta file synchronously for platform {0} at {1}.", buildTarget, metaFilePath);
            Assert.IsTrue(wasDeleted, "DeleteRsFromStreamingAssets failed for platform {0} at {1}.", buildTarget, destinationPath);
            Assert.IsFalse(destinationPath.HasFile(), "DeleteRsFromStreamingAssets failed to delete executable file for platform {0} at {1}.", buildTarget, destinationPath, destinationPath.ToFullPath());
            Assert.IsFalse(metaFilePath.HasFile(), "DeleteRsFromStreamingAssets failed to delete .meta file for platform {0} at {1}.", buildTarget, destinationPath, destinationPath.ToFullPath());

            AssetUtils.DeleteFolderIfEmpty(Paths.streamingAssetsPath);
            return true;
        }
    }
}
