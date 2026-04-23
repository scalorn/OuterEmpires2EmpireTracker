using System.IO;
using NLog;

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

            // Retry up to 3 times with short delays for file locking conflicts
            // (e.g., background processor and UI thread writing simultaneously)
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Write to temp file first -- if this fails, the original is untouched
                    File.WriteAllText(tempPath, content);

                    if (File.Exists(fullPath))
                    {
                        // Atomically replace the target with the temp file,
                        // moving the old target to the backup path
                        File.Replace(tempPath, fullPath, backupPath);
                    }
                    else
                    {
                        // First save -- target doesn't exist yet, just move the temp file
                        File.Move(tempPath, fullPath);
                    }

                    Log.Debug("Safe write completed: {0}", fullPath);
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    Log.Warn("Safe write attempt {0}/{1} failed for {2}: {3}", attempt, maxRetries, fullPath, ex.Message);
                    System.Threading.Thread.Sleep(retryDelayMs * attempt);
                }
            }
        }
    }
}
