// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log.Targets
{
    using System;
    using System.Text;

    public class ConsoleTarget : ILogTarget
    {
        public LogLevel Level { get; set; }

        private static readonly object locker = new object();

        public void Log(LogLevel level, string message, (string key, object value)[] args, Logger logger)
        {
            var logBuilder = new StringBuilder();

            string result = logger.BuildDefaultLog(level, message, logBuilder, args);

            if (level >= LogLevel.Error)
            {
                lock (locker)
                {
                    Console.Error.WriteLine(result);
                }
            }
            else
            {
                lock (locker)
                {
                    Console.WriteLine(result);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
