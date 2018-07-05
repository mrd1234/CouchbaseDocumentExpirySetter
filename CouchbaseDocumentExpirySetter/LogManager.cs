namespace CouchbaseDocumentExpirySetter
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;

    public class LogManager
    {
        private const int MaxEntries = 200;
        private readonly object _locker = new object();
        
        private string LogPath { get; }
        private List<string> LogEntries { get; } = new List<string>();

        public LogManager(string logFileFullPath)
        {
            LogPath = logFileFullPath;
            ValidateLogPath();
        }

        public void Log(string documentId, TimeSpan ttl, string expectedExpiryDateTime)
        {
            lock (_locker)
            {
                LogEntries.Add($"Document id {documentId} ttl set to {ttl.TotalMinutes} minute(s) and will {(ttl == TimeSpan.Zero ? "never expire" : $"expire approx {expectedExpiryDateTime}")}.");

                if (LogEntries.Count < MaxEntries) return;

                Flush();
            }
        }

        public void Flush()
        {
            lock (_locker)
            {
                if (!LogEntries.Any()) return;

                File.AppendAllLines(LogPath, LogEntries);
                LogEntries.Clear();
            }
        }

        public void ValidateLogPath()
        {
            if (string.IsNullOrEmpty(LogPath)) throw new ArgumentException(nameof(LogPath));

            var folder = Path.GetDirectoryName(LogPath);
            if (string.IsNullOrEmpty(folder)) throw new ApplicationException($"Unable to extract directory name from {LogPath}");

            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new ApplicationException($"Could not create directory {folder}");
                }
            }

            if (File.Exists(LogPath))
            {
                try
                {
                    File.Delete(LogPath);
                }
                catch (Exception)
                {
                    throw new ApplicationException($"Log file {LogPath} exists but cannot be deleted");
                }
            }

            try
            {
                File.AppendText(LogPath).Write("");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Unable to write to log file {LogPath}: {ex.Message}");
            }
        }
    }
}
