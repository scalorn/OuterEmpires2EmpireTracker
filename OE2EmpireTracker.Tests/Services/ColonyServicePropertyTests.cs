using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for ColonyService.
    /// Feature: bl-109-colony-readonly, Properties 4, 5, 6, 7, 8 from the design document.
    /// </summary>
    [TestFixture]
    public class ColonyServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random Colony with scalar fields populated
        // (reuses the same pattern as ColonyViewModelPropertyTests)
        // -----------------------------------------------------------------------

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<Colony> ValidColonyGen()
        {
            return from planetName in SafeStringGen()
                   from colonyName in SafeStringGen()
                   from systemName in SafeStringGen()
                   select BuildColony(planetName, colonyName, systemName);
        }

        private static Colony BuildColony(
            string planetName,
            string colonyName,
            string systemName)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = planetName,
                ColonyName = colonyName,
                SystemName = systemName,
            };

            return colony;
        }

        private static Gen<ColonyUpdateRequest> UpdateRequestGen()
        {
            return from planetName in SafeStringGen()
                   from colonyName in SafeStringGen()
                   from systemName in SafeStringGen()
                   select new ColonyUpdateRequest
                   {
                       PlanetName = planetName,
                       ColonyName = colonyName,
                       SystemName = systemName,
                   };
        }

        private static Gen<ColonyCreateRequest> CreateRequestGen()
        {
            return from planetName in SafeStringGen()
                   from colonyName in SafeStringGen()
                   from systemName in SafeStringGen()
                   select new ColonyCreateRequest
                   {
                       PlanetName = planetName,
                       ColonyName = colonyName,
                       SystemName = systemName,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.Update Round-Trip
        // Feature: bl-109-colony-readonly, Property 4: Service.Update Round-Trip
        // **Validates: Requirements 13.4, 13.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllScalarFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ColonyService(ctx);

                var seed = new Colony
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    PlanetName = "Seed",
                    ColonyName = "SeedColony",
                    SystemName = "SeedSystem",
                };
                ctx.AddColony(seed);

                var result = svc.Update(seed.UUID, request);

                bool planetMatch = result.PlanetName == request.PlanetName;
                bool colonyMatch = result.ColonyName == request.ColonyName;
                bool systemMatch = result.SystemName == request.SystemName;

                return (planetMatch && colonyMatch && systemMatch)
                    .Label(
                        "planet=" + planetMatch
                        + ", colony=" + colonyMatch
                        + ", system=" + systemMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Create Round-Trip
        // Feature: bl-109-colony-readonly, Property 5: Service.Create Round-Trip
        // **Validates: Requirements 14.2, 14.4, 14.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllScalarFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ColonyService(ctx);

                var result = svc.Create(request);

                if (string.IsNullOrEmpty(result.UUID))
                {
                    return false.Label("UUID was empty");
                }

                bool planetMatch = result.PlanetName == request.PlanetName;
                bool colonyMatch = result.ColonyName == request.ColonyName;
                bool systemMatch = result.SystemName == request.SystemName;

                return (planetMatch && colonyMatch && systemMatch)
                    .Label(
                        "planet=" + planetMatch
                        + ", colony=" + colonyMatch
                        + ", system=" + systemMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.Delete Removes Colony
        // Feature: bl-109-colony-readonly, Property 6: Service.Delete Removes Colony
        // **Validates: Requirements 15.1, 15.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesColony()
        {
            return Prop.ForAll(ValidColonyGen().ToArbitrary(), colony =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ColonyService(ctx);

                ctx.AddColony(colony);
                string uuid = colony.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutableColony(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Colony still exists after Delete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 7: Service.AddStructure Increases Structure Count
        // Feature: bl-109-colony-readonly, Property 7
        // **Validates: Requirements 17.3, 17.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property AddStructure_IncreasesStructureCount()
        {
            return Prop.ForAll(
                ValidColonyGen().ToArbitrary(),
                SafeStringGen().ToArbitrary(),
                (colony, flatpackUUID) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ColonyService(ctx);

                ctx.AddColony(colony);
                int countBefore = colony.Structures.Count;

                svc.AddStructure(colony.UUID, flatpackUUID);

                int countAfter = colony.Structures.Count;
                bool countIncreased = countAfter == countBefore + 1;

                var lastStructure = colony.Structures.Last();
                bool flatpackMatch = lastStructure.FlatpackBlueprintUUID == flatpackUUID;

                return (countIncreased && flatpackMatch)
                    .Label(
                        "countIncreased=" + countIncreased
                        + ", flatpackMatch=" + flatpackMatch
                        + " (before=" + countBefore + ", after=" + countAfter + ")");
            });
        }

        // -----------------------------------------------------------------------
        // Property 8: Service.RemoveStructure Decreases Structure Count
        // Feature: bl-109-colony-readonly, Property 8
        // **Validates: Requirements 18.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property RemoveStructure_DecreasesStructureCount()
        {
            return Prop.ForAll(
                ValidColonyGen().ToArbitrary(),
                SafeStringGen().ToArbitrary(),
                (colony, flatpackUUID) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new ColonyService(ctx);

                ctx.AddColony(colony);

                // Add a structure first so we have one to remove
                svc.AddStructure(colony.UUID, flatpackUUID);
                int countBefore = colony.Structures.Count;
                string structureUUID = colony.Structures.Last().UUID;

                svc.RemoveStructure(colony.UUID, structureUUID);

                int countAfter = colony.Structures.Count;
                bool countDecreased = countAfter == countBefore - 1;
                bool structureGone = !colony.Structures.Any(
                    s => s.UUID == structureUUID);

                return (countDecreased && structureGone)
                    .Label(
                        "countDecreased=" + countDecreased
                        + ", structureGone=" + structureGone
                        + " (before=" + countBefore + ", after=" + countAfter + ")");
            });
        }
    }
}