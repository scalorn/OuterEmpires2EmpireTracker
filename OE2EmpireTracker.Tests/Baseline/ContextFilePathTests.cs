using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Services;
using System.IO;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class ContextFilePathTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
            PlayerContext.FilePath = @"..\..\PlayerData.json";
            EmpireContext.FilePath = @"..\..\BaselineData.json";
        }

        [Test]
        public void PlayerContext_DefaultFilePath_IsPlayerDataJson()
        {
            Assert.AreEqual(@"..\..\PlayerData.json", PlayerContext.FilePath);
        }

        [Test]
        public void EmpireContext_DefaultFilePath_IsBaselineDataJson()
        {
            Assert.AreEqual(@"..\..\BaselineData.json", EmpireContext.FilePath);
        }

        [Test]
        public void PlayerContext_FilePathCanBeOverriddenForTestSetup()
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, "{\"PlayerProfile\":[],\"Blueprint\":[],\"Survey\":[],\"Colony\":[]}");

            PlayerContext.FilePath = tempPath;
            PlayerContext.Reset();

            var instance = PlayerContext.getInstance();

            Assert.IsNotNull(instance);
            Assert.AreEqual(tempPath, PlayerContext.FilePath);
        }

        [Test]
        public void EmpireContext_FilePathCanBeOverriddenForTestSetup()
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, "{\"ShipClass\":[],\"BlueprintType\":[],\"TechLevel\":[]}");

            EmpireContext.FilePath = tempPath;
            EmpireContext.Reset();

            var instance = EmpireContext.getInstance();

            Assert.IsNotNull(instance);
            Assert.AreEqual(tempPath, EmpireContext.FilePath);
        }
    }
}
