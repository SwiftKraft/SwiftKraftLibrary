namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Log;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Utility class that can be used to generate a zip file containing information that could be useful when debugging coherence.
    /// </summary>
    /// <seealso cref="CoherenceMainMenu"/>
    internal sealed class BugReportHelper
    {
        private const string ZipFileNameWithExtension = ZipFileNameWithoutExtension + "." + ZipFileExtension;
        private const string ZipFileNameWithoutExtension = "Diagnostics";
        private const string ZipFileExtension = "zip";

        /// <summary>
        /// Generates a zip file containing coherence related information, and instructs the user on how to share it
        /// with coherence using a series of popup dialogs.
        /// </summary>
        public static void DisplayReportBugDialogs()
        {
            var logger = Log.GetLogger<BugReportHelper>();

            if (!EditorUtility.DisplayDialog(ZipFileNameWithExtension, "Do you want to generate a zip file containing coherence-related information gathered from your project, which you can share with support?", "Generate", "No thanks"))
            {
                DisplayReportBugDialog("You can submit a bug report using coherence Community's Bug Reports page.\n\nYou can also seek assistance in the #help channel of the coherence Discord server.");
                return;
            }

            var requestedOutputPath = EditorUtility.SaveFilePanel("Export Diagnostics Info", "Assets", ZipFileNameWithExtension, ZipFileExtension);
            if (string.IsNullOrEmpty(requestedOutputPath))
            {
                return;
            }

            var includeEditorLog = EditorUtility.DisplayDialog("Editor.log", "Include Editor.log file in the bundle?\n\nIt contains stack traces for all logged messages and errors.", "Include", "Don't Include");
            var result = CreateZipFile(requestedOutputPath, includeEditorLog, InteractionMode.UserAction);

            foreach (var exception in result.Exceptions)
            {
                logger.Warning(Warning.EditorBugReportDisplayException, exception.ToString());
            }

            switch (result.Type)
            {
                case CreateZipFileResultType.Succeeded:
                    logger.Info($"File containing diagnostics information created at:\n{PathUtils.GetFullPath(result.CreatedFilePath)}.");
                    EditorUtility.RevealInFinder(result.CreatedFilePath);
                    DisplayReportBugDialog($"Diagnostics file was successfully generated at {result.CreatedFilePath}.\n\nYou can share the file with a member of the coherence team on the coherence Discord server.\n\nYou can also submit a bug report using coherence Community's Bug Reports page.");
                    break;
                case CreateZipFileResultType.Failed:
                    logger.Warning(Warning.EditorBugReportCreateFile);
                    return;
            }

            void DisplayReportBugDialog(string reportBugDialogText)
            {
                const int ok = 0;
                const int alt = 2;
                switch (EditorUtility.DisplayDialogComplex("Report Bug", reportBugDialogText, "Discord", "Cancel", "Bug Reports"))
                {
                    case ok:
                        logger.Info("Opening Discord...");
                        Application.OpenURL(Urls.Discord.HelpChannel);
                        break;
                    case alt:
                        logger.Info("Opening Bug Reports page...");
                        Application.OpenURL(Urls.Community.BugReports);
                        break;
                }
            }
        }

        /// <summary>
        /// Generates a zip file containing coherence related information.
        /// </summary>
        public static CreateZipFileResult CreateZipFile(string outputPath, bool includeEditorLog, InteractionMode interactionMode)
            => CreateZipFile(outputPath, GetEntries(includeEditorLog), interactionMode);

        /// <summary>
        /// Generates a zip file containing the given contents.
        /// </summary>
        private static CreateZipFileResult CreateZipFile(string outputPath, IReadOnlyList<Entry> contents, InteractionMode interactionMode)
        {
            if (string.IsNullOrEmpty(outputPath) || Path.GetDirectoryName(outputPath) is not { } outputDirectory || !Directory.Exists(outputDirectory))
            {
                return CreateZipFileResult.Failed(Enumerable.Repeat(new DirectoryNotFoundException($"The provided output path {outputPath} was invalid."), 1));
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var exceptions = new List<Exception>(0);
            using (var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                if (!AddEntries(archive, contents))
                {
                    return CreateZipFileResult.Canceled();
                }
            }

            return File.Exists(outputPath)
                ? CreateZipFileResult.Succeeded(exceptions, outputPath)
                : CreateZipFileResult.Failed(exceptions);

            bool AddEntries(ZipArchive archive, IReadOnlyList<Entry> entries)
            {
                for (int i = 0, count = entries.Count; i < count; i++)
                {
                    if (interactionMode == InteractionMode.UserAction)
                    {
                        var path = entries[i];
                        var progress = (float)i / count;
                        if (EditorUtility.DisplayCancelableProgressBar("Gathering Files", $"Bundling file '{path}' ({i + 1}/{count})", progress))
                        {
                            return false;
                        }
                    }

                    try
                    {
                        AddEntry(archive, entries[i]);
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(exception);
                    }
                    finally
                    {
                        if (interactionMode == InteractionMode.UserAction)
                        {
                            EditorUtility.ClearProgressBar();
                        }
                    }
                }

                return true;
            }

            void AddEntry(ZipArchive archive, Entry entry)
            {
                if (File.Exists(entry.Path))
                {
                    CreateEntryFromFile(archive, entry);
                    return;
                }

                if (Directory.Exists(entry.Path))
                {
                    foreach (var nestedFilePath in Directory.GetFiles(entry.Path, "*", SearchOption.AllDirectories))
                    {
                        CreateEntryFromFile(archive, new(nestedFilePath, entry.Name + Path.GetFileName(nestedFilePath), false));
                    }

                    return;
                }

                if (!entry.IsOptional)
                {
                    exceptions.Add(new FileNotFoundException($"No file or folder found at path '{entry.Path}'."));
                }
            }

            void CreateEntryFromFile(ZipArchive archive, Entry entry, bool isTemporaryCopy = false)
            {
                try
                {
                    archive.CreateEntryFromFile(entry.Path, entry.Name);
                }
                // If the file in question is currently being used by another process, it can result in a sharing violation exception.
                catch (IOException exception)
                {
                    if (isTemporaryCopy)
                    {
                        exceptions.Add(new IOException($"Failed to include file '{entry.Path}'.", exception));
                        return;
                    }

                    var temporaryCopyPath = FileUtil.GetUniqueTempPathInProject();
                    try
                    {
                        File.Copy(entry.Path, temporaryCopyPath);
                        CreateEntryFromFile(archive, new(temporaryCopyPath, entry.Name, false));
                    }
                    finally
                    {
                        if (File.Exists(temporaryCopyPath))
                        {
                            File.Delete(temporaryCopyPath);
                        }
                    }
                }
            }
        }

        private static IReadOnlyList<Entry> GetEntries(bool includeEditorLog)
        {
            var entries = new List<Entry>(6);

            Add(Paths.libraryRootPath, Paths.libraryRootPath + "/", false);
            var runtimeSettingsPath = AssetDatabase.GetAssetPath(RuntimeSettings.Instance);
            Add(runtimeSettingsPath, Path.GetFileName(runtimeSettingsPath), false);

            if (includeEditorLog)
            {
                Add(Paths.CurrentEditorLogFileAbsolutePath, Path.GetFileName(Paths.CurrentEditorLogFileAbsolutePath), false);
                Add(Paths.PreviousEditorLogFileAbsolutePath, Path.GetFileName(Paths.PreviousEditorLogFileAbsolutePath), true);
            }

            return entries;

            void Add(string path, string name, bool isOptional) => entries.Add(new(path, name, isOptional));
        }

        /// <summary>
        /// Represents an entry that should be included in the Diagnostics.zip file.
        /// </summary>
        private readonly struct Entry
        {
            public string Path { get; }
            public string Name { get; }
            public bool IsOptional { get; }

            public Entry(string path, string name, bool isOptional)
            {
                Path = path;
                Name = name;
                IsOptional = isOptional;
            }
        }

        public enum CreateZipFileResultType
        {
            Failed,
            Canceled,
            Succeeded
        }

        /// <summary>
        /// Represents the result of <see cref="CreateZipFile"/>.
        /// </summary>
        public sealed class CreateZipFileResult : IDisposable
        {
            public CreateZipFileResultType Type { get; }
            public IEnumerable<Exception> Exceptions { get; }
            public string CreatedFilePath { get; }

            private CreateZipFileResult(CreateZipFileResultType type, IEnumerable<Exception> exceptions, string zipFilePath)
            {
                Type = type;
                Exceptions = exceptions;
                CreatedFilePath = zipFilePath;
            }

            public static CreateZipFileResult Succeeded(IEnumerable<Exception> exceptions, string zipFilePath) => new(CreateZipFileResultType.Succeeded, exceptions, zipFilePath);
            public static CreateZipFileResult Failed(IEnumerable<Exception> exceptions) => new(CreateZipFileResultType.Failed, exceptions, "");
            public static CreateZipFileResult Canceled() => new(CreateZipFileResultType.Canceled, Enumerable.Empty<Exception>(), "");

            public void DeleteCreatedFile()
            {
                if (File.Exists(CreatedFilePath))
                {
                    File.Delete(CreatedFilePath);
                }
            }

            void IDisposable.Dispose() => DeleteCreatedFile();
        }
    }
}
