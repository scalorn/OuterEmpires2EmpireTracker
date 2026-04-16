using System.IO;
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
    }
}
