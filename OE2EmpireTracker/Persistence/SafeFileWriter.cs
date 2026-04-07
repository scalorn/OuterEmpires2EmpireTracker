using NLog;
using System.IO;

namespace OE2EmpireTracker.Persistence
{
    /// <summary>
    /// Writes files using a temp-then-replace strategy to prevent data loss
    /// if the process crashes or is interrupted during a write.
    /// </summary>
    public static class SafeFileWriter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

            // Write to temp file first — if this fails, the original is untouched
            File.WriteAllText(tempPath, content);

            if (File.Exists(fullPath))
            {
                // Atomically replace the target with the temp file,
                // moving the old target to the backup path
                File.Replace(tempPath, fullPath, backupPath);
            }
            else
            {
                // First save — target doesn't exist yet, just move the temp file
                File.Move(tempPath, fullPath);
            }

            Log.Debug("Safe write completed: {0}", fullPath);
        }
    }
}
