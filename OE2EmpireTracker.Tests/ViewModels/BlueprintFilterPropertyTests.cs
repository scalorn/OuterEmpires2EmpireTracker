using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using FormBP = OE2EmpireTracker.FormBlueprint;
using BP = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.ViewModels
{
    [TestFixture]
    public class BlueprintFilterPropertyTests
    {
        private PlayerContext _playerContext;
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
            _playerContext = PlayerContext.GetInstance();
            EmpireContext.PlayerContext = _playerContext;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        #region Helpers

        private static readonly string[] TypePool = { "Reactor", "Hull", "Weapon", "Shield", "MainDrive" };
        private static readonly string[] TechPool = { "LL", "Milspec", "Hi-Tech", "Junker" };

        private static BP MakeBp(string name, string type, int cls, string techLevel, int evolution, string ownerUUID)
        {
            return new BP(name)
            {
                UUID = Guid.NewGuid().ToString(),
                BluePrintType = type,
                Class = cls,
                TechLevel = techLevel,
                Evolution = evolution,
                OwnerUUID = ownerUUID
            };
        }

        /// <summary>
        /// Determines whether a blueprint satisfies all active filter constraints.
        /// This is the oracle -- a simple, independent re-implementation of the expected logic.
        /// </summary>
        private static bool SatisfiesAll(BP bp, string nameFilter, BlueprintFilterCriteria criteria)
        {
            // Text filter: matches ExtendedName or BluePrintType (case-insensitive)
            if (!string.IsNullOrEmpty(nameFilter))
            {
                bool matchesExtended = bp.ExtendedName != null
                    && bp.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesType = !string.IsNullOrEmpty(bp.BluePrintType)
                    && bp.BluePrintType.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesExtended && !matchesType)
                    return false;
            }

            if (criteria != null)
            {
                if (criteria.BlueprintTypeId != null && bp.BluePrintType != criteria.BlueprintTypeId)
                    return false;
                if (criteria.ShipClassId.HasValue && bp.Class != criteria.ShipClassId.Value)
                    return false;
                if (criteria.TechLevelName != null && bp.TechLevel != criteria.TechLevelName)
                    return false;
                if (criteria.Evolution.HasValue)
                {
                    if (criteria.EvolutionAndAbove)
                    {
                        if (bp.Evolution < criteria.Evolution.Value)
                            return false;
                    }
                    else
                    {
                        if (bp.Evolution != criteria.Evolution.Value)
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Property 1: Title bar format correctness

        /// <summary>
        /// Feature: blueprint-form-filters, Property 1: Title bar format correctness
        ///
        /// For any pair of non-negative integers (globalCount, playerCount),
        /// FormatTitleBar returns "Blueprints - Global: {globalCount} Player: {playerCount}".
        ///
        /// **Validates: Requirements 1.1, 1.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TitleBar_Format_Correctness()
        {
            var nonNegInt = Gen.Choose(0, int.MaxValue);

            var inputGen = from g in nonNegInt
                           from p in nonNegInt
                           select new { Global = g, Player = p };

            return Prop.ForAll(inputGen.ToArbitrary(), data =>
            {
                var result = FormBP.FormatTitleBar(data.Global, data.Player);
                var expected = $"Blueprints - Global: {data.Global} Player: {data.Player}";
                return (result == expected)
                    .Label($"Expected: \"{expected}\" but got: \"{result}\"");
            });
        }

        #endregion

        #region Property 2: Combined filter AND semantics

        /// <summary>
        /// Feature: blueprint-form-filters, Property 2: Combined filter AND semantics
        ///
        /// For any list of blueprints, any text filter string, and any BlueprintFilterCriteria
        /// (each field independently null or set), every blueprint returned by
        /// GetFilteredBlueprints(nameFilter, criteria) satisfies ALL active constraints,
        /// and no qualifying blueprint is excluded from the result.
        ///
        /// **Validates: Requirements 3.2, 4.2, 5.2, 6.2, 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CombinedFilter_AND_Semantics()
        {
            var nameGen = Gen.Elements("Laser", "Drive", "Plate", "Scanner", "Coupler");
            var typeGen = Gen.Elements(TypePool);
            var techGen = Gen.Elements(TechPool);
            var classGen = Gen.Choose(1, 5);
            var evoGen = Gen.Choose(0, 5);
            var ownerGen = Gen.Elements("player-1", "global");

            // Generate a list of blueprints
            var bpGen = from name in nameGen
                        from type in typeGen
                        from cls in classGen
                        from tech in techGen
                        from evo in evoGen
                        from owner in ownerGen
                        select MakeBp(name, type, cls, tech, evo, owner);

            var bpListGen = Gen.ListOf(bpGen);

            // Generate a text filter: null, empty, or a short substring from the pools
            var textFilterGen = Gen.Frequency(
                Tuple.Create(2, Gen.Constant((string)null)),
                Tuple.Create(1, Gen.Constant(string.Empty)),
                Tuple.Create(3, Gen.Elements("Laser", "Drive", "Reactor", "LL", "C3", "Ev")));

            // Generate criteria with each field independently null or set
            var nullableTypeGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant((string)null)),
                Tuple.Create(2, Gen.Elements(TypePool)));
            var nullableClassGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant((int?)null)),
                Tuple.Create(2, Gen.Choose(1, 5).Select(x => (int?)x)));
            var nullableTechGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant((string)null)),
                Tuple.Create(2, Gen.Elements(TechPool)));
            var nullableEvoGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant((int?)null)),
                Tuple.Create(2, Gen.Choose(0, 5).Select(x => (int?)x)));

            var criteriaGen = Gen.Frequency(
                Tuple.Create(1, Gen.Constant((BlueprintFilterCriteria)null)),
                Tuple.Create(4, from bpType in nullableTypeGen
                                from shipClass in nullableClassGen
                                from tech in nullableTechGen
                                from evo in nullableEvoGen
                                from evoAndAbove in Gen.Elements(true, false)
                                select new BlueprintFilterCriteria
                                {
                                    BlueprintTypeId = bpType,
                                    ShipClassId = shipClass,
                                    TechLevelName = tech,
                                    Evolution = evo,
                                    EvolutionAndAbove = evo.HasValue && evoAndAbove
                                }));

            var inputGen = from bps in bpListGen
                           from text in textFilterGen
                           from criteria in criteriaGen
                           select new { Blueprints = bps.ToList(), TextFilter = text, Criteria = criteria };

            return Prop.ForAll(inputGen.ToArbitrary(), data =>
            {
                // Clear and populate context lists
                _playerContext.BlueprintList.Clear();
                var ec = EmpireContext.GetInstance();
                ec.GlobalBlueprintList.Clear();

                foreach (var bp in data.Blueprints)
                {
                    if (bp.OwnerUUID == "global")
                    {
                        bp.OwnerUUID = string.Empty;
                        ec.GlobalBlueprintList.Add(bp);
                    }
                    else
                    {
                        _playerContext.CurrentPlayerUUID = "player-1";
                        _playerContext.BlueprintList.Add(bp);
                    }
                }

                var vm = new BlueprintViewModel(new BP("dummy"), _playerContext);
                var result = vm.GetFilteredBlueprints(data.TextFilter, data.Criteria);

                // Build the expected set: all blueprints that would be in the merged list
                // and satisfy all constraints
                var merged = new List<BP>(_playerContext.GetCurrentPlayerBlueprints());
                if (ec.GlobalBlueprintList != null)
                    merged.AddRange(ec.GlobalBlueprintList);

                var expectedUUIDs = new HashSet<string>(
                    merged.Where(b => SatisfiesAll(b, data.TextFilter, data.Criteria))
                          .Select(b => b.UUID));

                var resultUUIDs = new HashSet<string>(result.Select(b => b.UUID));

                // Soundness: every result satisfies all constraints
                bool allResultsSatisfy = result.All(b => SatisfiesAll(b, data.TextFilter, data.Criteria));

                // Completeness: every qualifying blueprint appears in the result
                bool allExpectedPresent = expectedUUIDs.IsSubsetOf(resultUUIDs);

                return allResultsSatisfy
                    .Label($"Soundness: all {result.Count} results satisfy constraints")
                    .And(allExpectedPresent)
                    .Label($"Completeness: expected {expectedUUIDs.Count} blueprints, got {resultUUIDs.Count}");
            });
        }

        #endregion
    }
}
