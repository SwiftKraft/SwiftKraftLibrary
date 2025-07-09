// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Build
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Log;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.Build;

    public static class ScriptingSymbolsChanger
    {
        private const string StoredScriptingSymbolsKey = "io.coherence.storedscriptingsymbols";

        public static bool ChangeScriptingSymbols(NamedBuildTarget target, bool checkBatchMode)
        {
            var scriptingSymbols = GetScriptingSymbols(target);

            if (scriptingSymbols.Contains("COHERENCE_SIMULATOR"))
            {
                return false;
            }

            if (checkBatchMode && Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    "Running Editor in Batch Mode, but no scripting symbol COHERENCE_SIMULATOR has been found," +
                    " make sure you run the method PrepareHeadlessBuild first.");
            }

            SessionState.SetString(StoredScriptingSymbolsKey, string.Join(";", scriptingSymbols));

            var newSymbols = new List<string>(scriptingSymbols)
            {
                "COHERENCE_SIMULATOR",
            };

            var logSettings = Log.GetSettings();
            if (logSettings.LogLevel <= LogLevel.Debug && !scriptingSymbols.Contains(LogConditionals.Debug))
            {
                newSymbols.Add(LogConditionals.Debug);
            }

            PlayerSettings.SetScriptingDefineSymbols(target, newSymbols.ToArray());

            return true;
        }

        public static void RestoreScriptingSymbols(NamedBuildTarget target)
        {
            var symbols = SessionState.GetString(StoredScriptingSymbolsKey, "")
                .Split(";", StringSplitOptions.RemoveEmptyEntries);

            PlayerSettings.SetScriptingDefineSymbols(target, symbols);
        }

        private static string[] GetScriptingSymbols(NamedBuildTarget target)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out var scriptingSymbols);
            return scriptingSymbols;
        }
    }
}

