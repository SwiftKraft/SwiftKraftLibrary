// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Log;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.PackageManager;
    using Logger = Log.Logger;

    [InitializeOnLoad]
    internal class Watchdog
    {
        private static IEnumerable<CompilerMessage> bakedCodeErrors;

        private const string LogEntriesInternalAPI =
            "UnityEditor.LogEntries, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        private const string LogEntryInternalAPI =
            "UnityEditor.LogEntry, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        private static object editorCompilationSingletonInstance;
        private static MethodInfo getCompileMessagesMethod;


        private static readonly Logger logger = Log.GetLogger<Watchdog>();

        public static bool Triggered
        {
            get => SessionState.GetBool(Constants.watchdogTriggeredKey, false);
            set => SessionState.SetBool(Constants.watchdogTriggeredKey, value);
        }

        public static bool BakedCodeInAssetsHasCompilationErrors => bakedCodeErrors?.Any() ?? false;

        static Watchdog()
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            EditorApplication.delayCall += TryOpenDiagnosis;

            if (EditorUtility.scriptCompilationFailed)
            {
                GetCompilationErrorsInBakedCodeViaReflectionWithFallback(out bool bakedCodeInAssetsHasCompilationErrors);

                if (bakedCodeInAssetsHasCompilationErrors)
                {
                    RunWatchdogForAssetsStrategy();
                }
            }

            CompilationPipeline.compilationStarted += _ =>
            {
                bakedCodeErrors = null;
            };

            CompilationPipeline.assemblyCompilationFinished += ProcessBatchModeCompileFinish;
            Events.registeringPackages += DeleteBakedCodeBeforeUpdatingCoherence;
        }

        // TODO Maybe return bool?
        public static void GetCompilationErrorsInBakedCodeViaReflectionWithFallback(
            out bool bakedCodeInAssetsHasCompilationErrors)
        {
            bakedCodeInAssetsHasCompilationErrors = BakedCodeInAssetsHasCompilationErrors;

            if (!EditorUtility.scriptCompilationFailed)
            {
                return;
            }

            if (bakedCodeInAssetsHasCompilationErrors)
            {
                return;
            }

            try
            {
                var logEntriesType = Type.GetType(LogEntriesInternalAPI);
                var logEntryType = Type.GetType(LogEntryInternalAPI);

                var startGettingEntriesMethod =
                    logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Public | BindingFlags.Static);

                var logEntriesCount = (int)startGettingEntriesMethod.Invoke(null, null);

                var getEntryInternal =
                    logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);

                var bakedCodeAssetsErrors = new List<CompilerMessage>();

                var log = Activator.CreateInstance(logEntryType);

                for (var i = 0; i < logEntriesCount; i++)
                {
                    var result = (bool)getEntryInternal.Invoke(null, new[]
                    {
                        i,
                        log,
                    });

                    if (!result)
                    {
                        continue;
                    }

                    var msgField = logEntryType.GetField("message");
                    var msgCasted = msgField.GetValue(log) as string;

                    var compilerErrorRegex =
                        new Regex(
                            @"(?<filename>[^:]*)\((?<line>\d+),(?<column>\d+)\):\s*(?<type>warning|error)\s*(?<message>.*)",
                            RegexOptions.ExplicitCapture | RegexOptions.Compiled);
                    var matches = compilerErrorRegex.Matches(msgCasted);

                    foreach (Match match in matches)
                    {
                        var publicCompilerMessage = new CompilerMessage
                        {
                            type = (CompilerMessageType)Enum.Parse(typeof(CompilerMessageType),
                                match.Groups["type"].Value, true),
                            message = match.Groups["message"].Value,
                            column = int.Parse(match.Groups["column"].Value),
                            file = match.Groups["filename"].Value,
                            line = int.Parse(match.Groups["line"].Value)
                        };

                        if (IsMessageBakedCodeAssetCompilationError(publicCompilerMessage.file))
                        {
                            bakedCodeAssetsErrors.Add(publicCompilerMessage);
                            bakedCodeInAssetsHasCompilationErrors = true;
                        }
                    }

                    bakedCodeErrors = bakedCodeAssetsErrors;
                }

                var endMethod =
                    logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Public | BindingFlags.Static);

                endMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                logger.Debug($"Failed to get compiler messages via reflection: {e.Message} \n\n {e.StackTrace}");
            }
        }

        private static void DeleteBakedCodeBeforeUpdatingCoherence(PackageRegistrationEventArgs args)
        {
            foreach (var package in args.changedFrom)
            {
                if (!package.name.Equals(Paths.packageId))
                {
                    continue;
                }

                CodeGenSelector.Clear(warn: false);
            }
        }

        private static void ProcessBatchModeCompileFinish(string dllPath, CompilerMessage[] compilerMessages)
        {
            bakedCodeErrors = compilerMessages.Where(m =>
                m.type == CompilerMessageType.Error &&
                IsMessageBakedCodeAssetCompilationError(m.message));

            RunWatchdogForAssetsStrategy();
        }

        private static void RunWatchdogForAssetsStrategy()
        {
            if (bakedCodeErrors.Any())
            {
                EditorApplication.delayCall -= Fixup;
                EditorApplication.delayCall += Fixup;
            }
        }

        private static bool IsMessageBakedCodeAssetCompilationError(string m)
        {
            return m.Replace("\\", "/").StartsWith(ProjectSettings.instance.GetSchemaBakeFolderPath());
        }

        private static void TryOpenDiagnosis()
        {
            if (Triggered)
            {
                Triggered = false;
                CoherenceMainMenu.OpenNetworkedPrefabs();
            }
        }

        private static void Fixup()
        {
            // to avoid infinite compile error loop
            CompilationPipeline.assemblyCompilationFinished -= ProcessBatchModeCompileFinish;

            if (EditorUtility.DisplayDialog("coherence watchdog",
                    "Detected compilation errors on baked scripts. coherence can delete the baked folder and diagnose prefabs that might need fixing.",
                    "Delete Baked Scripts and Diagnose", "Do Nothing"))
            {
                if (CodeGenSelector.Clear(warn: false))
                {
                    SessionState.SetBool(Constants.watchdogTriggeredKey, true);
                    CompilationPipeline.RequestScriptCompilation();
                }
            }
        }
    }
}
