// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using Log;
    using Log.Targets;

    public class TestLogger : Logger
    {
        private Dictionary<LogLevel, uint> logLevels;
        private Dictionary<Warning, uint> logWarnings;
        private Dictionary<Error, uint> logErrors;

        public TestLogger(Type source = null, LogLevel level = LogLevel.Warning) :
            this(source,
                new Dictionary<LogLevel, uint>(),
                new Dictionary<Warning, uint>(),
                new Dictionary<Error, uint>(),
                new ILogTarget[]
            {
                new ConsoleTarget() { Level = level }
            })
        { }

        private TestLogger(Type source,
            Dictionary<LogLevel, uint> logLevels,
            Dictionary<Warning, uint> logWarnings,
            Dictionary<Error, uint> logErrors,
            IEnumerable<ILogTarget> logTargets) :
            base(source, null, logTargets)
        {
            this.logLevels = logLevels;
            this.logWarnings = logWarnings;
            this.logErrors = logErrors;
        }

        public override Logger With<TSource>()
        {
            return With(typeof(TSource));
        }

        public override Logger With(Type source)
        {
            var newLogger = new TestLogger(source, logLevels, logWarnings, logErrors, LogTargets)
            {
                WithLogger = this,
                Context = Context,
                UseWatermark = UseWatermark
            };

            return newLogger;
        }

        public override void Trace(string log, params (string key, object value)[] args)
        {
            base.Trace(log, args);

            AddLog(LogLevel.Trace);
        }

        public override void Debug(string log, params (string key, object value)[] args)
        {
            base.Debug(log, args);

            AddLog(LogLevel.Debug);
        }

        public override void Info(string log, params (string key, object value)[] args)
        {
            base.Info(log, args);

            AddLog(LogLevel.Info);
        }

        public override void Warning(Warning id, params (string key, object value)[] args)
        {
            base.Warning(id, args);

            AddLog(LogLevel.Warning);
            AddWarning(id);
        }

        public override void Warning(Warning id, string msg, params (string key, object value)[] args)
        {
            base.Warning(id, msg, args);

            AddLog(LogLevel.Warning);
            AddWarning(id);
        }

        public override void Error(Error id, params (string key, object value)[] args)
        {
            base.Error(id, args);

            AddLog(LogLevel.Error);
            AddError(id);
        }

        public override void Error(Error id, string msg, params (string key, object value)[] args)
        {
            base.Error(id, msg, args);

            AddLog(LogLevel.Error);
            AddError(id);
        }

        public uint GetLogLevelCount(LogLevel level)
        {
            if (!logLevels.TryGetValue(level, out var count))
            {
                return 0;
            }

            return count;
        }

        public uint GetCountForWarningID(Warning id)
        {
            logWarnings.Remove(id, out var count);
            DecrementLog(LogLevel.Warning, count);

            return count;
        }

        public uint GetCountForErrorID(Error id)
        {
            logErrors.Remove(id, out var count);
            DecrementLog(LogLevel.Error, count);

            return count;
        }

        private void AddLog(LogLevel level)
        {
            logLevels.TryAdd(level, 0);
            logLevels[level]++;
        }

        private void DecrementLog(LogLevel level, uint count)
        {
            if (logLevels.TryGetValue(level, out var logLevelCount))
            {
                logLevelCount -= count;
                if (count == 0)
                {
                    logLevels.Remove(level);
                }
                else
                {
                    logLevels[level] = logLevelCount;
                }
            }
        }

        private void AddWarning(Warning id)
        {
            logWarnings.TryAdd(id, 0);
            logWarnings[id]++;
        }

        private void AddError(Error id)
        {
            logErrors.TryAdd(id, 0);
            logErrors[id]++;
        }
    }
}

