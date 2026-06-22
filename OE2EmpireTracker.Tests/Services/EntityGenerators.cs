using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// FsCheck generators for complex entity types used in round-trip property tests.
    /// </summary>
    public static class EntityGenerators
    {
        private static readonly string[] BlueprintTypeValues = new[]
        {
            BlueprintTypes.MiningRig,
            BlueprintTypes.Refinery,
            BlueprintTypes.ResearchLaboratory,
            BlueprintTypes.Manufactory,
            BlueprintTypes.ColonyCommandCentre,
            BlueprintTypes.OreHopper,
            BlueprintTypes.Shield,
            BlueprintTypes.NavComp,
        };

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<string> UuidGen()
        {
            return Gen.Fresh(() => Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Generates a ColonyStructure with key serializable fields populated.
        /// </summary>
        public static Gen<ColonyStructure> GenColonyStructure()
        {
            return from uuid in UuidGen()
                   from flatpackUuid in UuidGen()
                   from displaySeq in Gen.Choose(0, 50)
                   from buildingId in Gen.Choose(0, 100)
                   from buildQueueSeq in Gen.Choose(1, 50)
                   from durCurrent in Gen.Choose(0, 100)
                   from durMax in Gen.Choose(50, 100)
                   select new ColonyStructure
                   {
                       UUID = uuid,
                       FlatpackBlueprintUUID = flatpackUuid,
                       DisplaySequence = displaySeq,
                       BuildingID = buildingId,
                       BuildQueueSequence = buildQueueSeq,
                       DurabilityCurrent = durCurrent,
                       DurabilityMax = durMax,
                   };
        }

        /// <summary>
        /// Generates a Colony with enough fields populated for round-trip serialization testing.
        /// ColonyLock is runtime-only and excluded.
        /// </summary>
        public static Gen<Colony> GenColony()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from colonyName in SafeStringGen()
                   from planetName in SafeStringGen()
                   from systemName in SafeStringGen()
                   from colonyId in Gen.Choose(1, 9999)
                   from systemId in Gen.Choose(1, 500)
                   from colonySize in Gen.Choose(1, 20)
                   from structCount in Gen.Choose(0, 3)
                   from structures in Gen.ListOf(structCount, GenColonyStructure())
                   select new Colony
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       ColonyName = colonyName,
                       PlanetName = planetName,
                       SystemName = systemName,
                       ColonyId = colonyId,
                       SystemId = systemId,
                       ColonySize = colonySize,
                       Structures = structures.ToList(),
                   };
        }

        /// <summary>
        /// Generates a Blueprint with key fields for round-trip testing.
        /// Includes Properties dictionary and BuildItems-equivalent Resources.
        /// </summary>
        public static Gen<global::OE2EmpireTracker.Models.Blueprint> GenBlueprint()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from name in SafeStringGen()
                   from bpType in Gen.Elements(BlueprintTypeValues)
                   from techLevel in SafeStringGen()
                   from classVal in Gen.Choose(0, 5)
                   from evolution in Gen.Choose(0, 10)
                   from copyCost in Gen.Choose(0, 5000)
                   from propCount in Gen.Choose(0, 3)
                   from propKeys in Gen.ListOf(propCount, SafeStringGen())
                   from propValues in Gen.ListOf(propCount, SafeStringGen())
                   from resCount in Gen.Choose(0, 3)
                   from resKeys in Gen.ListOf(resCount, SafeStringGen())
                   from resValues in Gen.ListOf(resCount, SafeStringGen())
                   select BuildBlueprint(
                       uuid, ownerUuid, name, bpType, techLevel,
                       classVal, evolution, copyCost,
                       propKeys.ToList(), propValues.ToList(),
                       resKeys.ToList(), resValues.ToList());
        }

        private static global::OE2EmpireTracker.Models.Blueprint BuildBlueprint(
            string uuid,
            string ownerUuid,
            string name,
            string bpType,
            string techLevel,
            int classVal,
            int evolution,
            int copyCost,
            List<string> propKeys,
            List<string> propValues,
            List<string> resKeys,
            List<string> resValues)
        {
            var bp = new global::OE2EmpireTracker.Models.Blueprint(name)
            {
                UUID = uuid,
                OwnerUUID = ownerUuid,
                BluePrintType = bpType,
                TechLevel = techLevel,
                Class = classVal,
                Evolution = evolution,
                CopyCost = copyCost,
            };

            for (int i = 0; i < propKeys.Count; i++)
            {
                bp.Properties.SetProperty(propKeys[i], propValues[i]);
            }

            for (int i = 0; i < resKeys.Count; i++)
            {
                bp.Resources[resKeys[i]] = resValues[i];
            }

            return bp;
        }

        /// <summary>
        /// Creates an Arbitrary for Colony from the generator.
        /// Register via Arb.Register in test setup.
        /// </summary>
        public static Arbitrary<Colony> ArbColony()
        {
            return Arb.From(GenColony());
        }

        /// <summary>
        /// Creates an Arbitrary for Blueprint from the generator.
        /// Register via Arb.Register in test setup.
        /// </summary>
        public static Arbitrary<global::OE2EmpireTracker.Models.Blueprint> ArbBlueprint()
        {
            return Arb.From(GenBlueprint());
        }
    }
}
