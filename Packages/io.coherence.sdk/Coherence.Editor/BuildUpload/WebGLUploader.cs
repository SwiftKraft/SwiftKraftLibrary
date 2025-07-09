// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Portal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    internal class WebGLUploader : BuildUploader
    {
        protected override string DialogTitle => "Batch upload?";
        protected override string Message => "Contents in the following path will be uploaded one by one. Are you sure?\n\n";
        protected override string OkButton => "Batch upload";

        struct FileToUpload
        {
            public string fullName;
            public long fileSize;
            public UploadURL uploadURL;
        }

        private List<FileToUpload> filesToUpload;

        public WebGLUploader()
        {
            filesToUpload = new List<FileToUpload>();
        }

        // Recursive and maintain file and folder name and structure.
        static bool GatherStreamingAssets(DirectoryInfo dir, string path, ICollection<FileToUpload> filesToUpload)
        {
            if (!dir.Exists)
            {
                return true;
            }

            FileInfo[] streamingFiles = dir.GetFiles("*.*");
            foreach (FileInfo f in streamingFiles)
            {
                var uurl = UploadURL.GetWebGLFile(f.Length, Uri.EscapeDataString($"{path}{f.Name}"), true);
                if (uurl == null)
                {
                    return false;
                }

                filesToUpload.Add(new FileToUpload { fullName = f.FullName, fileSize = f.Length, uploadURL = uurl});
            }

            DirectoryInfo[] subDirs = dir.GetDirectories("*.*");
            foreach (var subDir in subDirs)
            {
                if (!GatherStreamingAssets(subDir, $"{path}{subDir.Name}/", filesToUpload))
                {
                    return false;
                }
            }

            return true;
        }

        // Non-recursive. Rename files to 'game'.
        static bool GatherGameAssets(DirectoryInfo dir, ICollection<FileToUpload> filesToUpload)
        {
            FileInfo[] buildFiles = dir.GetFiles("*.*");
            var gameName = GetGameName(buildFiles);
            foreach (FileInfo f in buildFiles)
            {
                var uurl = UploadURL.GetWebGLFile(f.Length, f.Name.Replace(gameName, "game"), false);
                if (uurl == null)
                {
                    return false;
                }
                filesToUpload.Add(new FileToUpload { fileSize = f.Length, fullName = f.FullName, uploadURL = uurl});
            }

            return true;
        }

        // For WebGL builds - get the game's name so we can safely change it to `game` for our system.
        // We have to make an assumtion here that there will be a file called [gamename].loader.js
        // based on: https://docs.unity3d.com/Manual/webgl-templates.html#build_configuration
        static string GetGameName(FileInfo[] buildFiles)
        {
            var name = "";
            foreach (FileInfo f in buildFiles)
            {
                if (f.Name.Contains(".loader.js"))
                {
                    var split = f.Name.Split(new string[] { ".loader.js" }, StringSplitOptions.None);
                    name = split[0];
                    break;
                }
            }

            return name;
        }

        internal override bool Upload(AvailablePlatforms platform, string buildPath)
        {
            OnUploadStart("webgl");

            filesToUpload.Clear();

            DirectoryInfo buildDir = new DirectoryInfo(Path.Combine(buildPath, "Build"));

            if (!GatherGameAssets(buildDir, filesToUpload))
            {
                return false;
            }

            DirectoryInfo streamingDir = new DirectoryInfo(Path.Combine(buildPath, "StreamingAssets"));

            if (!GatherStreamingAssets(streamingDir, "", filesToUpload))
            {
                return false;
            }

            Debug.Log($"Uploading {filesToUpload.Count} files...");
            for (int i = 0; i < filesToUpload.Count; i++)
            {
                FileToUpload f = filesToUpload[i];
                if (!f.uploadURL.Upload(f.fullName, f.fileSize, $"Uploading file {i + 1}/{filesToUpload.Count}..."))
                {
                    return false;
                }
            }

            // Register the successful upload
            if (!UploadURL.RegisterBuild("webgl", ""))
            {
                return false;
            }

            OnUploadEnd("webgl");

            return true;
        }
    }
}
