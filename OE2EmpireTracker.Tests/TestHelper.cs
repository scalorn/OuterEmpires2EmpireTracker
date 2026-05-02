using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests
{
    /// <summary>
    /// Shared test infrastructure for setting up context singletons.
    /// Points EmpireContext and PlayerContext at the dedicated test copies
    /// of BaselineData.json and PlayerData.json in TestData/.
    /// </summary>
    public static class TestHelper
    {
        private static string _cachedBaselineJson;
        private static string _cachedPlayerJson;

        /// <summary>
        /// Returns the full path to a file in the TestData/ folder.
        /// </summary>
        public static string TestDataPath(string filename)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", filename);
        }

        /// <summary>
        /// Points EmpireContext.FilePath at the test copy of BaselineData.json.
        /// Call this before EmpireContext.GetInstance().
        /// </summary>
        public static void SetEmpireFilePath()
        {
            EmpireContext.FilePath = TestDataPath("BaselineData.json");
        }

        /// <summary>
        /// Points PlayerContext.FilePath at the test copy of PlayerData.json.
        /// Call this before PlayerContext.GetInstance().
        /// </summary>
        public static void SetPlayerFilePath()
        {
            PlayerContext.FilePath = TestDataPath("PlayerData.json");
        }

        /// <summary>
        /// Sets both file paths to the test copies and suppresses migration UI.
        /// </summary>
        public static void SetAllFilePaths()
        {
            MigrationRunner.SuppressUI = true;
            SetEmpireFilePath();
            SetPlayerFilePath();
        }

        /// <summary>
        /// Resets both singletons and initializes them from cached, deep-copied
        /// root objects. Replaces the Reset() + SetAllFilePaths() + GetInstance()
        /// pattern with zero disk I/O.
        /// </summary>
        public static void ResetWithCachedData()
        {
            MigrationRunner.SuppressUI = true;
            EnsureCachePopulated();

            EmpireContext.Reset();
            var baselineCopy = DeepCopyBaseline();
            var playerCopy = DeepCopyPlayer();
            new EmpireContext(baselineCopy, playerCopy);
        }

        /// <summary>
        /// Deserializes and caches the test JSON files on first call.
        /// Subsequent calls return immediately.
        /// </summary>
        private static void EnsureCachePopulated()
        {
            if (_cachedBaselineJson == null)
            {
                string baselinePath = TestDataPath("BaselineData.json");
                _cachedBaselineJson = File.ReadAllText(baselinePath);
            }

            if (_cachedPlayerJson == null)
            {
                string playerPath = TestDataPath("PlayerData.json");
                _cachedPlayerJson = File.ReadAllText(playerPath);
            }
        }

        /// <summary>
        /// Returns a deep copy of the cached BaselineRoot via JSON round-trip.
        /// </summary>
        private static BaselineRoot DeepCopyBaseline()
        {
            return JsonConvert.DeserializeObject<BaselineRoot>(_cachedBaselineJson);
        }

        /// <summary>
        /// Returns a deep copy of the cached PlayerRoot via JSON round-trip.
        /// </summary>
        private static PlayerRoot DeepCopyPlayer()
        {
            return JsonConvert.DeserializeObject<PlayerRoot>(_cachedPlayerJson);
        }
    }
}
