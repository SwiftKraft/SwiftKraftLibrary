// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using UnityEngine;

    internal static class ProcessUtil
    {
        public static Process RunOutsideTerminal(string executable, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executable, arguments)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true,
            };

            if (process.Start())
            {
                process.BeginOutputReadLine();
            }

            return process;
        }

        public static string CommandFromExecutableAndArguments(string executable, string arguments) => $"\"{executable}\" {arguments}";

        public static Process RunInTerminal(string command)
        {
            var projectPath = $"\\\"{Path.GetDirectoryName(Application.dataPath)}\\\"";
            return RunInTerminal(command, projectPath);
        }

        public static Process RunInTerminal(string command, string projectPath)
        {
#if UNITY_EDITOR_OSX
            var script = $"{command}".Trim();
            script = script.Replace("\"", "\\\"");
            script = $@"
                tell application ""Terminal""
                    if it is not running then
                        reopen
                    end if
                    activate
                    do script ""cd {projectPath}; {script}""
                end tell
            ";

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                }
            };

            p.Start();

            p.StandardInput.WriteLine(script);
            p.StandardInput.Close();
            return p;
#elif UNITY_EDITOR_WIN
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = projectPath,
                    FileName = "cmd",
                    Arguments = $@"/k ""{command}""",
                },
            };

            p.Start();
            return p;

#elif UNITY_EDITOR_LINUX
            var script = $@"-e ""{command}""".Trim();
            try
            {
                var p = Process.Start("x-terminal-emulator", script);
                if (p == null || p.HasExited)
                {
                    throw new System.Exception();
                }

                return p;
            }
            catch(System.Exception)
            {
            }

            try
            {
                var p = Process.Start("xterm", script);
                if (p == null || p.HasExited)
                {
                    throw new System.Exception();
                }

                return p;
            }
            catch (System.Exception)
            {
            }

            UnityEngine.Debug.LogError("Cannot start terminal. Please make sure you have xterm installed.");
            return null;
#else
            throw new System.PlatformNotSupportedException();
#endif
        }

        /// <summary>
        /// Runs the specified process and waits for it to exit. Its output and errors are
        /// returned as well as the exit code from the process.
        /// See: https://stackoverflow.com/questions/4291912/process-start-how-to-get-the-output
        /// Note that if any deadlocks occur, read the above thread (cubrman's response).
        /// </summary>
        public static int RunProcess(string application, string arguments, out string output, out string errors, int waitTimeMs = 5000)
        {
            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = application,
                Arguments = arguments
            };

            // Use the following event to read both output and errors output.
            var outputBuilder = new StringBuilder();
            var errorsBuilder = new StringBuilder();
            process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
            process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

            // Start the process and wait for it to exit.
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(waitTimeMs);

            output = outputBuilder.ToString().TrimEnd();
            errors = errorsBuilder.ToString().TrimEnd();
            return process.ExitCode;
        }

        /// <summary>
        /// Sets permissions to the given path on Unix systems.
        /// </summary>
        /// <remarks>
        /// Executes <c>chmod 755</c> on the given path.
        /// No effect on Windows.
        /// </remarks>
        [Conditional("UNITY_EDITOR_OSX")]
        [Conditional("UNITY_EDITOR_LINUX")]
        public static void FixUnixPermissions(string path)
        {
            _ = Process.Start("chmod", $"755 {path}");
        }
    }
}
