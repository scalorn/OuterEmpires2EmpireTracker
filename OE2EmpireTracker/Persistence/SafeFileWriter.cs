using System.IO;
using NLog;

namespace OE2EmpireTracker.Persistence
{
    /// <summary>
    /// Writes files using a temp-then-replace strategy to prevent data loss
    /// if the process crashes or is interrupted during a write.
    /// Thread-safe: a lock prevents concurrent writes from colliding on the temp file.
    /// </summary>
    public static class SafeFileWriter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object WriteLock = new object();

        /// <summary>
        /// Writes content to a file safely. The content is first written to a
        /// temporary file, then atomically swapped into place via File.Replace.
        /// The previous version is kept as a .bak file for one-deep recovery.
        /// </summary>
        public static void WriteAllText(string filePath, string content)
        {
            string fullPath = Path.GetFullPath(filePath);
            string tempPath = fullPath + ".tmp";
            string backupPath = fullPath + ".bak";

            lock (WriteLock)
            {
                File.WriteAllText(tempPath, content);

                if (File.Exists(fullPath))
                {
                    File.Replace(tempPath, fullPath, backupPath);
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }

                Log.Debug("Safe write completed: {0}", fullPath);
            }
        }
    }
}
