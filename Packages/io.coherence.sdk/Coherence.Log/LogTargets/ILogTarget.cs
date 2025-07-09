// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log.Targets
{
    using System;

    public interface ILogTarget : IDisposable
    {
        LogLevel Level { get; set; }

        void Log(LogLevel level, string message, (string key, object value)[] args, Logger logger);
    }
}
