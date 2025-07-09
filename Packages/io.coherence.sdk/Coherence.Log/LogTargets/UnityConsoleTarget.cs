// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER

namespace Coherence.Log.Targets
{
    using System;
    using System.Text;
    using System.Threading;
    using UnityEngine;
    using Logger = Coherence.Log.Logger;
    using Object = UnityEngine.Object;

    public class UnityConsoleTarget : ILogTarget
    {
        public LogLevel Level { get; set; }
        public bool LogStackTrace
        {
            get => logOptions == LogOption.None;
            set => logOptions = value ? LogOption.None : LogOption.NoStacktrace;
        }

        private LogOption logOptions;

        private static readonly ThreadLocal<StringBuilder> StringBuilderCache = new(() => new StringBuilder());

        public void Log(LogLevel level, string message, (string key, object value)[] args, Logger logger)
        {
            var logBuilder = StringBuilderCache.Value;
            logBuilder.Clear();

            LogFormatter.AppendTimestamp(logBuilder);
            LogFormatter.AppendPrefix(logBuilder, logger.UseWatermark, logger.Source);
            logBuilder.Append(": ");
            logBuilder.Append(message).Append(" ");
            logger.AppendPrefixArgs(logBuilder);

            Object context = null;
            if (logger is UnityLogger unityLogger)
            {
                context = unityLogger.GetUnityLogContext();
            }

            if (args.Length > 0 && args[0].value is Object unityObject)
            {
                context = unityObject;
                LogFormatter.AppendArgs(logBuilder,
                    new ArraySegment<(string key, object value)>(args, 1, args.Length - 1));
            }
            else
            {
                LogFormatter.AppendArgs(logBuilder, args);
            }

            var result = logBuilder.ToString();
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    if (context != null)
                    {
                        Debug.LogFormat(LogType.Log, logOptions, context, "{0}", result);
                    }
                    else
                    {
                        Debug.LogFormat(LogType.Log, logOptions, null, "{0}", result);
                    }

                    break;
                case LogLevel.Warning:
                    if (context != null)
                    {
                        Debug.LogWarning(result, context);
                    }
                    else
                    {
                        Debug.LogWarning(result);
                    }

                    break;
                case LogLevel.Error:
                    if (context != null)
                    {
                        Debug.LogError(result, context);
                    }
                    else
                    {
                        Debug.LogError(result);
                    }

                    break;
                default: throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public void Dispose()
        {
        }
    }
}

#endif
