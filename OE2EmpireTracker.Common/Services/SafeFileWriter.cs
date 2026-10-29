using System;
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
                string dir = Path.GetDirectoryName(tempPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(tempPath, content);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Replace(tempPath, fullPath, backupPath);
                    }
                    catch (IOException ex)
                    {
                        ReplaceByMove(fullPath, tempPath, backupPath, ex);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ReplaceByMove(fullPath, tempPath, backupPath, ex);
                    }
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }

                Log.Debug("Safe write completed: {0}", fullPath);
            }
        }

        /// <summary>
        /// Fallback for when File.Replace fails. This is known to happen on
        /// cloud-synced folders, when antivirus holds a handle on the target,
        /// and on some network or virtual volumes.
        /// <para>
        /// Two moves preserve the atomicity guarantee this class exists to provide:
        /// the target is never left truncated, and a recoverable copy exists at every
        /// point. Deliberately does NOT fall back to File.WriteAllText, which truncates
        /// before writing and would silently reintroduce the corruption window.
        /// </para>
        /// </summary>
        private static void ReplaceByMove(string fullPath, string tempPath, string backupPath, Exception cause)
        {
            Log.Warn(cause, "File.Replace failed for {0}; falling back to move-based replace.", fullPath);

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            File.Move(fullPath, backupPath);
            File.Move(tempPath, fullPath);
        }
    }
}
