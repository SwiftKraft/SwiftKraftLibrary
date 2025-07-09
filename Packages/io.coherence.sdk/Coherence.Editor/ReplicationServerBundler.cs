namespace Coherence.Editor
{
    using System;
    using Log;
    using UnityEditor;
    using Logger = Log.Logger;

    public static class ReplicationServerBundler
    {
        internal static Logger Logger = Log.GetLogger(typeof(ReplicationServerBundler));

        /// <summary>
        /// Copies the replication server binary to the Streaming Assets folder.
        /// </summary>
        /// <returns>True if the bundling was successful.</returns>
        public static bool BundleWithStreamingAssets(BuildTarget platform)
        {
            return CopyReplicationServerTo(platform);
        }

        /// <summary>
        /// Deletes the replication server binary from the Streaming Assets folder.
        /// </summary>
        public static bool DeleteRsFromStreamingAssets(BuildTarget platform)
        {
            var path = ReplicationServerBinaries.GetStreamingAssetsPath(platform);
            return AssetUtils.DeleteFile(path);
        }

        private static bool CopyReplicationServerTo(BuildTarget platform)
        {
            try
            {
                var sourcePath = ReplicationServerBinaries.GetToolsPath(platform);
                if (!sourcePath.HasFile())
                {
                    Logger.Error(Error.EditorRSBundlerMissingRS,
                        $"Can't find replication server at '{sourcePath}', won't be able to bundle it with build for platform {platform}.");
                    return false;
                }

                var destinationPath = ReplicationServerBinaries.GetStreamingAssetsPath(platform);

                if (AssetUtils.CopyFile(sourcePath, destinationPath, ImportAssetOptions.ForceSynchronousImport))
                {
                    Logger.Info($"Successfully bundled replication server with '{platform}' build.",
                                                         ("destinationPath", destinationPath));
                }
                else
                {
                    Logger.Error(Error.EditorRSBundlerException,
                        $"Can't bundle replication server for build that targets '{platform}'.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(Error.EditorRSBundlerException,
                    $"Can't bundle replication server for build that targets '{platform}'.",
                    ("Exception", e));
                return false;
            }

            return true;
        }
    }
}
