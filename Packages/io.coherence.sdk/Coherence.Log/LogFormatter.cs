// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class LogFormatter
    {
        public static StringBuilder AppendLevel(StringBuilder logBuilder, LogLevel level, bool noTrailingSpace = false)
        {
            logBuilder.Append("[")
                .Append(level)
                .Append("]");

            if (noTrailingSpace)
            {
                return logBuilder;
            }

            return logBuilder.Append(" ");
        }

        public static StringBuilder AppendPrefix(StringBuilder logBuilder, bool useWatermark, Type source = null)
        {
            if (useWatermark)
            {
                logBuilder.Append("(coherence)");
                logBuilder.Append(" ");
            }

            AppendSource(logBuilder, source);

            return logBuilder;
        }

        public static StringBuilder AppendSource(StringBuilder logBuilder, Type source)
        {
            if (source == null)
            {
                return logBuilder;
            }

            return logBuilder.Append(source.Name);
        }

        public static StringBuilder AppendTimestamp(StringBuilder logBuilder, bool noTrailingSpace = false)
        {
            logBuilder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));

            if (noTrailingSpace)
            {
                return logBuilder;
            }

            return logBuilder.Append(" ");
        }

        public static StringBuilder AppendArgs(StringBuilder logBuilder, ICollection<(string key, object value)> args, bool useTab = true)
        {
            if (args.Count <= 0)
            {
                return logBuilder;
            }

            logBuilder.Append(useTab ? '\t' : ' ');

            foreach ((string key, object value) in args)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }
                logBuilder.Append(key)
                    .Append("=")
                    .Append(value)
                    .Append(" ");
            }

            logBuilder.Remove(logBuilder.Length - 1, 1);

            return logBuilder;
        }
    }
}
