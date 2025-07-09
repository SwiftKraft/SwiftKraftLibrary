// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log.Targets
{
    using System.IO;

    public class FileTarget : ILogTarget
    {
        private static readonly object threadlock = new object();

        private bool disposed;
        private FileStream file;
        private StreamWriter writer;

        public LogLevel Level { get; set; }

        public FileTarget(string filePath)
        {
            // make sure that the target directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(file);
        }

        public void Log(LogLevel level, string message, (string key, object value)[] args, Logger logger)
        {
            if (disposed)
            {
                return;
            }

            var json = JsonLogFormatter.Format(level, message, args, logger.Source);

            lock (threadlock)
            {
                writer.Write(json);
            }
        }

        public void Dispose()
        {
            writer?.Flush();
            writer?.Close();
            file?.Close();

            writer = null;
            file = null;
            disposed = true;
        }
    }
}
