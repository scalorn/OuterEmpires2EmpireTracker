using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using System.Collections.Generic;
using System.Linq;

using BP = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.ViewModels
{
    [TestFixture]
    public class BaseBlueprintCandidatesTests
    {
        private PlayerContext playerContext;
        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;

        [SetUp]
        public void SetUp()
        {
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            EmpireContext.Reset();
            var ec = EmpireContext.GetInstance();
            PlayerContext.Reset();
            playerContext = PlayerContext.GetInstance();
            EmpireContext.PlayerContext = playerContext;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        private BP MakeBlueprint(string name, string type, int cls, string techLevel, int evolution, string uuid = null)
        {
            return new BP
            {
                UUID = uuid ?? System.Guid.NewGuid().ToString(),
                Name = name,
                BluePrintType = type,
                Class = cls,
                TechLevel = techLevel,
                Evolution = evolution,
                OwnerUUID = playerContext.CurrentPlayerUUID ?? "test-owner"
            };
        }

        private BlueprintViewModel CreateViewModel(BP current)
        {
            return new BlueprintViewModel(current, playerContext);
        }

        // -----------------------------------------------------------------------
        // Filtering
        // -----------------------------------------------------------------------

        [Test]
        public void FiltersByBluePrintType()
        {
            var match = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var noMatch = MakeBlueprint("Laser", "Hull", 3, "LL", 0);
            playerContext.BlueprintList.Add(match);
            playerContext.BlueprintList.Add(noMatch);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 1);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo(match.UUID));
        }

        [Test]
        public void FiltersByName_ExactMatch_CaseInsensitive()
        {
            var match = MakeBlueprint("Pulse Laser", "Weapon", 3, "LL", 0);
            var noMatch = MakeBlueprint("Beam Laser", "Weapon", 3, "LL", 0);
            playerContext.BlueprintList.Add(match);
            playerContext.BlueprintList.Add(noMatch);

            var current = MakeBlueprint("pulse laser", "Weapon", 3, "LL", 1);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo(match.UUID));
        }

        [Test]
        public void FiltersByClass()
        {
            var match = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var noMatch = MakeBlueprint("Laser", "Weapon", 5, "LL", 0);
            playerContext.BlueprintList.Add(match);
            playerContext.BlueprintList.Add(noMatch);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 1);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo(match.UUID));
        }

        [Test]
        public void FiltersByTechLevel_CaseInsensitive()
        {
            var match = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var noMatch = MakeBlueprint("Laser", "Weapon", 3, "Milspec", 0);
            playerContext.BlueprintList.Add(match);
            playerContext.BlueprintList.Add(noMatch);

            var current = MakeBlueprint("Laser", "Weapon", 3, "ll", 1);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo(match.UUID));
        }

        [Test]
        public void OnlyShowsLowerEvolutions()
        {
            var evo0 = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var evo1 = MakeBlueprint("Laser", "Weapon", 3, "LL", 1);
            var evo2 = MakeBlueprint("Laser", "Weapon", 3, "LL", 2);
            var evo3 = MakeBlueprint("Laser", "Weapon", 3, "LL", 3);
            playerContext.BlueprintList.Add(evo0);
            playerContext.BlueprintList.Add(evo1);
            playerContext.BlueprintList.Add(evo2);
            playerContext.BlueprintList.Add(evo3);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 2);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(b => b.Evolution), Is.EquivalentTo(new[] { 0, 1 }));
        }

        [Test]
        public void ExcludesCurrentBlueprint()
        {
            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 1, "current-uuid");
            var sameUuid = MakeBlueprint("Laser", "Weapon", 3, "LL", 0, "current-uuid");
            playerContext.BlueprintList.Add(sameUuid);

            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Is.Empty);
        }

        // -----------------------------------------------------------------------
        // Ordering
        // -----------------------------------------------------------------------

        [Test]
        public void OrderedByEvolutionDescending()
        {
            var evo0 = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var evo1 = MakeBlueprint("Laser", "Weapon", 3, "LL", 1);
            var evo2 = MakeBlueprint("Laser", "Weapon", 3, "LL", 2);
            playerContext.BlueprintList.Add(evo0);
            playerContext.BlueprintList.Add(evo1);
            playerContext.BlueprintList.Add(evo2);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 4);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0].Evolution, Is.EqualTo(2));
            Assert.That(result[1].Evolution, Is.EqualTo(1));
            Assert.That(result[2].Evolution, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Text filter
        // -----------------------------------------------------------------------

        [Test]
        public void TextFilterAppliesOnTopOfMatching()
        {
            var bp1 = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            bp1.NickName = "Alpha";
            var bp2 = MakeBlueprint("Laser", "Weapon", 3, "LL", 1);
            bp2.NickName = "Beta";
            playerContext.BlueprintList.Add(bp1);
            playerContext.BlueprintList.Add(bp2);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 3);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates("Alpha");

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].NickName, Is.EqualTo("Alpha"));
        }

        // -----------------------------------------------------------------------
        // Edge cases
        // -----------------------------------------------------------------------

        [Test]
        public void Evolution0_ReturnsEmpty()
        {
            var other = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            playerContext.BlueprintList.Add(other);

            var current = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void EmptyCurrentData_ReturnsEmpty()
        {
            var other = MakeBlueprint("Laser", "Weapon", 3, "LL", 0);
            playerContext.BlueprintList.Add(other);

            var current = new BP { UUID = "new-bp" };
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SkipsClassFilter_WhenClassIsZero()
        {
            var match = MakeBlueprint("Scanner", "SystemObjectScanner", 0, null, 0);
            var alsoMatch = MakeBlueprint("Scanner", "SystemObjectScanner", 5, null, 0);
            playerContext.BlueprintList.Add(match);
            playerContext.BlueprintList.Add(alsoMatch);

            var current = MakeBlueprint("Scanner", "SystemObjectScanner", 0, null, 1);
            var vm = CreateViewModel(current);

            var result = vm.GetBaseBlueprintCandidates();

            Assert.That(result, Has.Count.EqualTo(2));
        }
    }
}
