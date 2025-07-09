// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    using Targets;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public static class Log
    {
        public enum FilterMode
        {
            Include,
            Exclude
        }

        internal static FilterMode SourceFilterMode => GetSettings().FilterMode;
        internal static string[] SourceFilters => GetSettings().GetSourceFilter();

        private static readonly object lockObject = new();
        private static bool didParseCLIArgs;
        private static bool didCheckForLevelDefines;

        private static ILogTarget consoleTarget;
        private static FileTarget fileTarget;
        private static Settings settings;
        private static List<ILogTarget> baseTargets = new();

#if UNITY_5_3_OR_NEWER && UNITY_EDITOR
        private static string SettingsSavePath => "Library/coherence/logSettings.json";
        private static Func<Type, Logger> LoggerSource = (source) => new UnityLogger(source, baseTargets);
#else
        private static Func<Type, Logger> LoggerSource = (source) => new Logger(source, null, baseTargets);
#endif

        public static Settings GetSettings()
        {
            if (settings != null)
            {
                return settings;
            }

            Init();
            return settings;
        }

        private static void Init()
        {
            lock (lockObject)
            {
                InitSettings();
                ParseCLIArgs();
                InitConsoleTarget();
                InitFileTarget();
                CheckForLevelDefines();
            }
        }

        private static void InitSettings()
        {
            if (settings != null)
            {
                return;
            }

#if UNITY_5_3_OR_NEWER && UNITY_EDITOR
            settings = Settings.Load(SettingsSavePath);
#else
            settings = new Settings()
            {
                // It will be filtered based on preproc symbols anyway
                LogLevel = LogLevel.Trace,
            };
#endif

            settings.OnSaved += () =>
            {
                InitConsoleTarget();
                InitFileTarget();
            };
        }

        private static void InitFileTarget()
        {
            if (settings.LogToFile)
            {
                if (fileTarget == null)
                {
                    fileTarget = new FileTarget(settings.LogFilePath);
                    baseTargets.Add(fileTarget);
                }

                fileTarget.Level = settings.FileLogLevel;
            }
            else
            {
                if (fileTarget != null)
                {
                    baseTargets.Remove(fileTarget);
                    fileTarget = null;
                }
            }
        }

        private static void InitConsoleTarget()
        {
            if (consoleTarget == null)
            {
#if UNITY_5_3_OR_NEWER
                consoleTarget = new UnityConsoleTarget();
#else
                consoleTarget = new ConsoleTarget();
#endif
                baseTargets.Add(consoleTarget);
            }

            consoleTarget.Level = settings.LogLevel;

#if UNITY_5_3_OR_NEWER
            if (consoleTarget is UnityConsoleTarget unityConsole)
            {
                unityConsole.LogStackTrace = settings.LogStackTrace;
            }
#endif
        }

        private static void ParseCLIArgs()
        {
            if (didParseCLIArgs)
            {
                return;
            }

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--coherence-log-file-path")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new Exception("Missing value of --coherence-log-file-path argument.");
                    }

                    var filePath = args[i + 1];

                    settings.LogToFile = true;
                    settings.LogFilePath = filePath;
                }

                if (args[i] == "--coherence-file-log-level")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new Exception("Missing value of --coherence-file-log-level argument.");
                    }

                    if (!Enum.TryParse(args[i + 1], ignoreCase: true, out LogLevel level))
                    {
                        throw new ArgumentException($"Value {args[i + 1]} is not a valid enum value.");
                    }

                    settings.FileLogLevel = level;
                }
            }

            didParseCLIArgs = true;
        }

        private static void CheckForLevelDefines()
        {
            if (didCheckForLevelDefines)
            {
                return;
            }

            didCheckForLevelDefines = true;

            if (settings.LogLevel <= LogLevel.Trace)
            {
                CheckForTraceDefine();
            }

            if (settings.LogLevel <= LogLevel.Debug)
            {
                CheckForDebugDefine();
            }
        }

        private static void CheckForTraceDefine()
        {
            var hasTraceDefine = false;
            HasTraceDefine(ref hasTraceDefine);

            if (!hasTraceDefine) {
                GetLogger(typeof(Log)).Warning(Warning.MissingTraceDefine);
            }
        }

        private static void CheckForDebugDefine()
        {
            var hasDebugDefine = false;
            HasDebugDefine(ref hasDebugDefine);

            if (!hasDebugDefine)
            {
                GetLogger(typeof(Log)).Warning(Warning.MissingDebugDefine);
            }
        }

        [Conditional(LogConditionals.Trace)]
        private static void HasTraceDefine(ref bool has)
        {
            has = true;
        }

        [Conditional(LogConditionals.Debug)]
        private static void HasDebugDefine(ref bool has)
        {
            has = true;
        }

        // Logger context is used to differentiate between different instances
        // of loggers that log the same Source Types.  For example, Unity scene
        // objects that use a logger set the context of the logger to be the scene
        // they are in.
        public static Logger GetLogger<TSource>(object context = null)
        {
            return GetLogger(typeof(TSource), context);
        }

        public static Logger GetLogger(Type source, object context = null)
        {
            Init();

            var logger = LoggerSource(source);
            logger.Context = context;

            return logger;
        }
    }
}
