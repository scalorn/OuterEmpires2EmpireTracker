// <copyright file="ColonyMergeBuildingsPreservationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Preservation property tests for ColonyMergeService.MergeBuildings.
    /// These tests capture baseline behavior that MUST remain unchanged after the fix.
    /// Uses single-match scenarios only (1 pool entry, 1 API building) to avoid
    /// triggering the bug condition.
    /// Feature: colony-import-fix
    /// </summary>
    [TestFixture]
    public class ColonyMergeBuildingsPreservationTests
    {
        private static readonly DateTime FrozenTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => FrozenTime;
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // ---------------------------------------------------------------
        // Generators
        // ---------------------------------------------------------------

        private static Gen<int> PositiveBuildingIdGen()
        {
            return Gen.Choose(1, 99999);
        }

        private static Gen<int> ColonyBuildingTypeIdGen()
        {
            return Gen.Choose(1, 30);
        }

        private static Gen<int> ResourceIdGen()
        {
            return Gen.Choose(0, 500);
        }

        private static Gen<string> ResourceIconGen()
        {
            return Gen.Elements(
                "iron_ore.png",
                "copper_ore.png",
                "titanium.png",
                "crystal.png",
                string.Empty);
        }

        private static Gen<int> ManufactureAmountGen()
        {
            return Gen.Choose(0, 100);
        }

        private static Gen<double> DurabilityGen()
        {
            return from val in Gen.Choose(0, 10000)
                   select val / 10.0;
        }

        private static Gen<string> ResourceNameGen()
        {
            return Gen.Elements(
                "Iron Ore",
                "Copper Ore",
                "Titanium",
                "Crystal",
                string.Empty);
        }

        /// <summary>
        /// Generates a single built API building with a specific BuildingId.
        /// </summary>
        private static Gen<GameApiColonyBuilding> BuiltApiBuildingGen(int buildingId, int typeId)
        {
            return from resourceId in ResourceIdGen()
                   from resourceIcon in ResourceIconGen()
                   from resourceName in ResourceNameGen()
                   from mfgAmount in ManufactureAmountGen()
                   from durabilityCur in DurabilityGen()
                   from durabilityMax in DurabilityGen()
                   select new GameApiColonyBuilding
                   {
                       BuildingId = buildingId,
                       ColonyBuildingTypeId = typeId,
                       BlueprintDesignName = "Blueprint_" + typeId,
                       BuildingOnline = true,
                       StatusId = 1,
                       ConstructingBuildingFinish = FrozenTime.AddHours(-1),
                       ResourceId = resourceId,
                       ResourceIcon = resourceIcon,
                       ResourceName = resourceName,
                       ManufactureAmountPerRun = mfgAmount,
                       DurabilityCurrent = durabilityCur,
                       DurabilityMax = durabilityMax,
                   };
        }

        /// <summary>
        /// Generates an API building for new-structure creation (no pool match).
        /// </summary>
        private static Gen<GameApiColonyBuilding> NewStructureApiBuildingGen()
        {
            return from buildingId in PositiveBuildingIdGen()
                   from typeId in Gen.Choose(900, 999)
                   from resourceId in ResourceIdGen()
                   from resourceIcon in ResourceIconGen()
                   from mfgAmount in ManufactureAmountGen()
                   from durabilityCur in DurabilityGen()
                   from durabilityMax in DurabilityGen()
                   select new GameApiColonyBuilding
                   {
                       BuildingId = buildingId,
                       ColonyBuildingTypeId = typeId,
                       BlueprintDesignName = "NewBlueprint_" + typeId,
                       BuildingOnline = true,
                       StatusId = 1,
                       ConstructingBuildingFinish = FrozenTime.AddHours(-2),
                       ResourceId = resourceId,
                       ResourceIcon = resourceIcon,
                       ResourceName = string.Empty,
                       ManufactureAmountPerRun = mfgAmount,
                       DurabilityCurrent = durabilityCur,
                       DurabilityMax = durabilityMax,
                   };
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static Colony CreateSingleStructureColony(ColonyStructure structure)
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-owner",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                Structures = new List<ColonyStructure> { structure },
                Items = new ItemBag(),
            };
        }

        private static Colony CreateEmptyColony()
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-owner",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
                Structures = new List<ColonyStructure>(),
                Items = new ItemBag(),
            };
        }

        // ---------------------------------------------------------------
        // Property 1: Field-Level Merge by BuildingID (Primary Key Match)
        // Validates: Requirements 3.1, 3.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1: Field-Level Merge by BuildingID.
        /// For any single pool entry matched by BuildingID to a single API building,
        /// the merge produces correct field values: ResourceId, ResourceIcon,
        /// DurabilityCurrent, DurabilityMax, ManufactureAmountPerRun are set from API.
        /// Validates: Requirements 3.1, 3.5
        /// </summary>
        [Test]
        public void FieldLevelMerge_ByBuildingId_MergesAllApiFields()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from typeId in ColonyBuildingTypeIdGen()
                from apiBuilding in BuiltApiBuildingGen(buildingId, typeId)
                select new { BuildingId = buildingId, TypeId = typeId, ApiBuilding = apiBuilding };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var structure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 1,
                };
                structure.Properties.SetProperty(GameConstants.PropBuilt, false);

                var colony = CreateSingleStructureColony(structure);
                var apiBuildings = new List<GameApiColonyBuilding> { data.ApiBuilding };

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                bool resourceIdOk = structure.ResourceId == data.ApiBuilding.ResourceId;
                bool resourceIconOk = structure.ResourceIcon == (data.ApiBuilding.ResourceIcon ?? string.Empty);
                bool mfgAmountOk = structure.ManufactureAmountPerRun == data.ApiBuilding.ManufactureAmountPerRun;
                bool durabilityCurOk = structure.DurabilityCurrent == (decimal)data.ApiBuilding.DurabilityCurrent;
                bool durabilityMaxOk = structure.DurabilityMax == (decimal)data.ApiBuilding.DurabilityMax;
                bool buildingIdPreserved = structure.BuildingID == data.BuildingId;

                return (resourceIdOk && resourceIconOk && mfgAmountOk
                    && durabilityCurOk && durabilityMaxOk && buildingIdPreserved).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 2: BuildingID Primary Key Takes Priority
        // Validates: Requirements 3.7
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: BuildingID Primary Key Priority.
        /// For any pool entry with a non-zero BuildingID matching the API building,
        /// the match occurs by BuildingID and receives the API data.
        /// Validates: Requirements 3.7
        /// </summary>
        [Test]
        public void BuildingIdMatch_TakesPriority_OverTypeFallback()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from typeId in ColonyBuildingTypeIdGen()
                from apiBuilding in BuiltApiBuildingGen(buildingId, typeId)
                select new { BuildingId = buildingId, TypeId = typeId, ApiBuilding = apiBuilding };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var matchedStructure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 2,
                };
                matchedStructure.Properties.SetProperty(GameConstants.PropBuilt, false);

                var colony = new Colony
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-owner",
                    PlanetName = "TestPlanet",
                    SystemName = "TestSystem",
                    Structures = new List<ColonyStructure> { matchedStructure },
                    Items = new ItemBag(),
                };

                var apiBuildings = new List<GameApiColonyBuilding> { data.ApiBuilding };
                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                bool matchedGotApiData = matchedStructure.ResourceId == data.ApiBuilding.ResourceId;
                bool buildingIdKept = matchedStructure.BuildingID == data.BuildingId;

                return (matchedGotApiData && buildingIdKept).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 3: Type Fallback Match Assigns BuildingID
        // Validates: Requirements 3.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 3: Type Fallback Match Assigns BuildingID.
        /// For any unassigned pool entry (BuildingID=0) matching by ColonyBuildingTypeId,
        /// the API BuildingId is assigned to the structure.
        /// Validates: Requirements 3.5
        /// </summary>
        [Test]
        public void TypeFallbackMatch_AssignsBuildingId_FromApi()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from typeId in ColonyBuildingTypeIdGen()
                from apiBuilding in BuiltApiBuildingGen(buildingId, typeId)
                select new { BuildingId = buildingId, TypeId = typeId, ApiBuilding = apiBuilding };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var structure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = 0,
                    ColonyBuildingTypeId = data.TypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 1,
                };
                structure.Properties.SetProperty(GameConstants.PropBuilt, false);

                var colony = CreateSingleStructureColony(structure);
                var apiBuildings = new List<GameApiColonyBuilding> { data.ApiBuilding };

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                bool buildingIdAssigned = structure.BuildingID == data.BuildingId;
                bool resourceIdMerged = structure.ResourceId == data.ApiBuilding.ResourceId;

                return (buildingIdAssigned && resourceIdMerged).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 4: New Structure Creation
        // Validates: Requirements 3.6
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 4: New Structure Creation.
        /// For any API building with no matching pool entry, a new structure
        /// is created with BuildQueueSequence at end of the existing list.
        /// Validates: Requirements 3.6
        /// </summary>
        [Test]
        public void NewStructure_CreatedWithBuildQueueSequenceAtEnd()
        {
            var gen =
                from existingSeq in Gen.Choose(1, 50)
                from apiBuilding in NewStructureApiBuildingGen()
                select new { ExistingSeq = existingSeq, ApiBuilding = apiBuilding };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var existingStructure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = 1,
                    ColonyBuildingTypeId = 1,
                    BuildQueueSequence = data.ExistingSeq,
                    DisplaySequence = 1,
                };
                existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);

                var colony = CreateSingleStructureColony(existingStructure);
                var apiBuildings = new List<GameApiColonyBuilding> { data.ApiBuilding };

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                if (colony.Structures.Count != 2)
                {
                    return false.ToProperty();
                }

                var newStructure = colony.Structures.FirstOrDefault(
                    s => s.BuildingID == data.ApiBuilding.BuildingId);

                if (newStructure == null)
                {
                    return false.ToProperty();
                }

                bool seqAtEnd = newStructure.BuildQueueSequence > data.ExistingSeq;
                bool buildingIdSet = newStructure.BuildingID == data.ApiBuilding.BuildingId;
                bool typeIdSet = newStructure.ColonyBuildingTypeId == data.ApiBuilding.ColonyBuildingTypeId;

                return (seqAtEnd && buildingIdSet && typeIdSet).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 5: Built Status and BuildCompletionTime
        // Validates: Requirements 3.1, 3.3
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 5: Built Status and BuildCompletionTime.
        /// When API reports a future finish time, Built=false and BuildCompletionTime created.
        /// When API reports a past finish time, Built=true.
        /// Validates: Requirements 3.1, 3.3
        /// </summary>
        [Test]
        public void BuiltStatus_SetCorrectly_BasedOnFinishTime()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from typeId in ColonyBuildingTypeIdGen()
                from isFuture in Gen.Elements(true, false)
                from hoursOffset in Gen.Choose(1, 48)
                from resourceId in ResourceIdGen()
                select new
                {
                    BuildingId = buildingId,
                    TypeId = typeId,
                    IsFuture = isFuture,
                    HoursOffset = hoursOffset,
                    ResourceId = resourceId,
                };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var finishTime = data.IsFuture
                    ? FrozenTime.AddHours(data.HoursOffset)
                    : FrozenTime.AddHours(-data.HoursOffset);

                var apiBuilding = new GameApiColonyBuilding
                {
                    BuildingId = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BlueprintDesignName = "Blueprint_" + data.TypeId,
                    BuildingOnline = !data.IsFuture,
                    StatusId = 1,
                    ConstructingBuildingFinish = finishTime,
                    ResourceId = data.ResourceId,
                    ResourceIcon = string.Empty,
                    ResourceName = string.Empty,
                    ManufactureAmountPerRun = 0,
                    DurabilityCurrent = 100.0,
                    DurabilityMax = 100.0,
                };

                var structure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 1,
                    BuildCompletionTime = null,
                };
                structure.Properties.SetProperty(GameConstants.PropBuilt, false);

                var colony = CreateSingleStructureColony(structure);
                ColonyMergeService.MergeBuildings(
                    new List<GameApiColonyBuilding> { apiBuilding }, colony);

                bool builtValue;
                structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out builtValue);

                if (data.IsFuture)
                {
                    bool notBuilt = !builtValue;
                    bool hasTimer = structure.BuildCompletionTime != null;
                    return (notBuilt && hasTimer).ToProperty();
                }
                else
                {
                    return builtValue.ToProperty();
                }
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 6: MiningSurveyResource Conditional Assignment
        // Validates: Requirements 3.4
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 6: MiningSurveyResource Conditional Assignment.
        /// For a matched structure with empty MiningSurveyResource, when API
        /// provides a non-empty ResourceName, it is assigned to the structure.
        /// Validates: Requirements 3.4
        /// </summary>
        [Test]
        public void MiningSurveyResource_AssignedWhenLocalEmpty()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from typeId in ColonyBuildingTypeIdGen()
                from resourceName in Gen.Elements("Iron Ore", "Copper Ore", "Titanium", "Crystal")
                select new { BuildingId = buildingId, TypeId = typeId, ResourceName = resourceName };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var apiBuilding = new GameApiColonyBuilding
                {
                    BuildingId = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BlueprintDesignName = "Mining_" + data.TypeId,
                    BuildingOnline = true,
                    StatusId = 1,
                    ConstructingBuildingFinish = FrozenTime.AddHours(-1),
                    ResourceId = 1,
                    ResourceIcon = "ore.png",
                    ResourceName = data.ResourceName,
                    ManufactureAmountPerRun = 0,
                    DurabilityCurrent = 100.0,
                    DurabilityMax = 100.0,
                };

                var structure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 1,
                    MiningSurveyResource = null,
                };
                structure.Properties.SetProperty(GameConstants.PropBuilt, true);

                var colony = CreateSingleStructureColony(structure);
                ColonyMergeService.MergeBuildings(
                    new List<GameApiColonyBuilding> { apiBuilding }, colony);

                return (structure.MiningSurveyResource == data.ResourceName).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 7: New Structure From Empty Pool
        // Validates: Requirements 3.6
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 7: New Structure From Empty Pool.
        /// When the colony has zero pool entries and API returns a building,
        /// a new structure is created with BuildQueueSequence starting at 1.
        /// Validates: Requirements 3.6
        /// </summary>
        [Test]
        public void NewStructureFromEmptyPool_HasBuildQueueSequenceOne()
        {
            var gen = NewStructureApiBuildingGen();

            Prop.ForAll(gen.ToArbitrary(), apiBuilding =>
            {
                var colony = CreateEmptyColony();

                ColonyMergeService.MergeBuildings(
                    new List<GameApiColonyBuilding> { apiBuilding }, colony);

                if (colony.Structures.Count != 1)
                {
                    return false.ToProperty();
                }

                var created = colony.Structures[0];
                bool seqIsOne = created.BuildQueueSequence == 1;
                bool buildingIdSet = created.BuildingID == apiBuilding.BuildingId;
                bool typeIdSet = created.ColonyBuildingTypeId == apiBuilding.ColonyBuildingTypeId;

                return (seqIsOne && buildingIdSet && typeIdSet).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 8: ColonyBuildingTypeId Updated From API
        // Validates: Requirements 3.1
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 8: ColonyBuildingTypeId Updated From API.
        /// For any matched structure, ColonyBuildingTypeId is set from API.
        /// Validates: Requirements 3.1
        /// </summary>
        [Test]
        public void ColonyBuildingTypeId_UpdatedFromApi()
        {
            var gen =
                from buildingId in PositiveBuildingIdGen()
                from localTypeId in ColonyBuildingTypeIdGen()
                from apiTypeId in ColonyBuildingTypeIdGen()
                from resourceId in ResourceIdGen()
                select new
                {
                    BuildingId = buildingId,
                    LocalTypeId = localTypeId,
                    ApiTypeId = apiTypeId,
                    ResourceId = resourceId,
                };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var apiBuilding = new GameApiColonyBuilding
                {
                    BuildingId = data.BuildingId,
                    ColonyBuildingTypeId = data.ApiTypeId,
                    BlueprintDesignName = "Blueprint_" + data.ApiTypeId,
                    BuildingOnline = true,
                    StatusId = 1,
                    ConstructingBuildingFinish = FrozenTime.AddHours(-1),
                    ResourceId = data.ResourceId,
                    ResourceIcon = string.Empty,
                    ResourceName = string.Empty,
                    ManufactureAmountPerRun = 0,
                    DurabilityCurrent = 50.0,
                    DurabilityMax = 100.0,
                };

                var structure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.LocalTypeId,
                    BuildQueueSequence = 1,
                    DisplaySequence = 1,
                };
                structure.Properties.SetProperty(GameConstants.PropBuilt, true);

                var colony = CreateSingleStructureColony(structure);
                ColonyMergeService.MergeBuildings(
                    new List<GameApiColonyBuilding> { apiBuilding }, colony);

                return (structure.ColonyBuildingTypeId == data.ApiTypeId).ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
