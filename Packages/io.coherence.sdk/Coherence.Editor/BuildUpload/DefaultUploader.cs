// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    internal class DefaultUploader : BuildUploader
    {
        internal override bool Upload(AvailablePlatforms platform, string buildPath)
        {
            string path = GetZipTempPath();
            string platformAsString = platform.ToString().ToLowerInvariant();

            OnUploadStart(platformAsString);

            try
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayProgressBar("Game", "Compressing game build path...", 1f);
                }
                
                File.Delete(path);
                ZipUtils.Zip(buildPath, path);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            try
            {
                long size = new FileInfo(path).Length;

                // request a valid upload endpoint
                var uurl = Portal.UploadURL.GetGame(size, platformAsString);
                if (uurl == null)
                {
                    return false;
                }

                // upload the game (zipfile)
                if (!uurl.Upload(path, size))
                {
                    return false;
                }

                _ = Portal.UploadURL.RegisterBuild(platformAsString, "");

                OnUploadEnd(platformAsString);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            return true;
        }
    }
}
