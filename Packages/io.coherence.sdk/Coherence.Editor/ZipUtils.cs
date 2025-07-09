// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Diagnostics;
    using System.IO;
    using UnityEditor;

    internal static class ZipUtils
    {
        private static string Get7zPath()
        {
#if UNITY_EDITOR_WIN
            string fileName = "7z.exe";
#else
            string fileName = "7za";
#endif
            string path = EditorApplication.applicationContentsPath + "/Tools/" + fileName;

            return !File.Exists(path) ? throw new FileNotFoundException("Could not find " + path) : path;
        }

        internal static void Unzip(string zipPath, string destPath)
        {
            string zipper = Get7zPath();
            string args = $"x -y -o\"{destPath}\" \"{zipPath}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = zipper,
                Arguments = args,
                RedirectStandardError = true
            };

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new IOException($"Failed to unzip:\n{zipper} {args}\n{process.StandardError.ReadToEnd()}");
            }
        }

        internal static void Zip(string zipPath, string destFile, bool uploadAll = true)
        {
            string zipper = Get7zPath();
            string args = $"a -tzip \"{destFile}\" -y \"{(uploadAll ? Path.Combine(zipPath, "*") : zipPath)}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = zipper,
                Arguments = args,
                RedirectStandardError = true,
            };

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new IOException($"Failed to zip:\n{zipper} {args}\n{process.StandardError.ReadToEnd()}");
            }
        }
    }
}
