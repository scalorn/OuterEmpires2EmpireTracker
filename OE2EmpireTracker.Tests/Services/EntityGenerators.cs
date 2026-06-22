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

        /// <summary>
        /// Generates a SurveyResource with key fields populated.
        /// </summary>
        public static Gen<SurveyResource> GenSurveyResource()
        {
            return from resource in GenHelpers.GenNonEmptyString
                   from purity in Gen.Elements("High", "Medium", "Low", "Refined")
                   from amount in Gen.Choose(1, 5000).Select(a => a.ToString())
                   select new SurveyResource(resource, purity, amount);
        }

        /// <summary>
        /// Generates a Survey with key fields for round-trip testing.
        /// </summary>
        public static Gen<Survey> GenSurvey()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from planetName in GenHelpers.GenNonEmptyString
                   from systemName in GenHelpers.GenNonEmptyString
                   from surveyType in Gen.Elements(SurveyType.Planet, SurveyType.Asteroid)
                   from resCount in Gen.Choose(0, 4)
                   from resKeys in Gen.ListOf(resCount, GenHelpers.GenNonEmptyString)
                   from resValues in Gen.ListOf(resCount, GenSurveyResource())
                   select BuildSurvey(uuid, ownerUuid, planetName, systemName, surveyType, resKeys.ToList(), resValues.ToList());
        }

        private static Survey BuildSurvey(
            string uuid,
            string ownerUuid,
            string planetName,
            string systemName,
            SurveyType surveyType,
            List<string> resKeys,
            List<SurveyResource> resValues)
        {
            var survey = new Survey
            {
                UUID = uuid,
                OwnerUUID = ownerUuid,
                PlanetName = planetName,
                SystemName = systemName,
                SurveyType = surveyType,
            };

            for (int i = 0; i < resKeys.Count; i++)
            {
                survey.Resources[resKeys[i]] = resValues[i];
            }

            return survey;
        }

        /// <summary>
        /// Generates a PlayerRank with reasonable field values.
        /// </summary>
        public static Gen<PlayerRank> GenPlayerRank()
        {
            return from rank in Gen.Choose(0, 20)
                   from xp in Gen.Choose(0, 100000).Select(v => (long)v)
                   from xpNext in Gen.Choose(1, 100000).Select(v => (long)v)
                   from rankName in GenHelpers.GenNonEmptyString
                   select new PlayerRank
                   {
                       Rank = rank,
                       CurrentXp = xp,
                       XpToNextLevel = xpNext,
                       RankName = rankName,
                   };
        }

        /// <summary>
        /// Generates a PlayerProfile with key fields for round-trip testing.
        /// </summary>
        public static Gen<PlayerProfile> GenPlayerProfile()
        {
            return from uuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from faction in GenHelpers.GenNonEmptyString
                   from credits in GenHelpers.GenPositiveDecimal
                   from skillPoints in Gen.Choose(0, 500)
                   from publicRank in GenPlayerRank()
                   from privateRank in GenPlayerRank()
                   from militaryRank in GenPlayerRank()
                   from skillCount in Gen.Choose(0, 3)
                   from skillNames in Gen.ListOf(skillCount, GenHelpers.GenNonEmptyString)
                   from skillLevels in Gen.ListOf(skillCount, Gen.Choose(0, 10))
                   select BuildPlayerProfile(
                       uuid, name, faction, credits, skillPoints,
                       publicRank, privateRank, militaryRank,
                       skillNames.ToList(), skillLevels.ToList());
        }

        private static PlayerProfile BuildPlayerProfile(
            string uuid,
            string name,
            string faction,
            decimal credits,
            int skillPoints,
            PlayerRank publicRank,
            PlayerRank privateRank,
            PlayerRank militaryRank,
            List<string> skillNames,
            List<int> skillLevels)
        {
            var profile = new PlayerProfile
            {
                UUID = uuid,
                Name = name,
                Faction = faction,
                TotalCredits = credits,
                SkillPoints = skillPoints,
                Public = publicRank,
                Private = privateRank,
                Military = militaryRank,
            };

            for (int i = 0; i < skillNames.Count; i++)
            {
                profile.Skills[skillNames[i]] = new PlayerSkill { Level = skillLevels[i] };
            }

            return profile;
        }

        /// <summary>
        /// Generates a RouteStop with key fields populated.
        /// </summary>
        public static Gen<RouteStop> GenRouteStop()
        {
            return from colonyUuid in GenHelpers.GenUUID
                   from sequence in Gen.Choose(0, 20)
                   from destType in Gen.Elements(
                       DestinationType.Colony,
                       DestinationType.Station,
                       DestinationType.Asteroid,
                       DestinationType.Ship)
                   from destUuid in GenHelpers.GenUUID
                   from purpose in Gen.Elements(
                       RouteStopPurpose.Cargo,
                       RouteStopPurpose.Refuel,
                       RouteStopPurpose.CargoAndRefuel)
                   from fuel in GenHelpers.GenPositiveDecimal
                   select new RouteStop
                   {
                       ColonyUUID = colonyUuid,
                       Sequence = sequence,
                       DestinationType = destType,
                       DestinationUUID = destUuid,
                       Purpose = purpose,
                       FuelEstimate = fuel,
                   };
        }

        /// <summary>
        /// Generates a DeliveryRoute with key fields for round-trip testing.
        /// </summary>
        public static Gen<DeliveryRoute> GenDeliveryRoute()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from stopCount in Gen.Choose(0, 4)
                   from stops in Gen.ListOf(stopCount, GenRouteStop())
                   select new DeliveryRoute
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       Stops = stops.ToList(),
                   };
        }

        /// <summary>
        /// Generates a DeliveryItem with key fields populated.
        /// </summary>
        public static Gen<DeliveryItem> GenDeliveryItem()
        {
            return from name in GenHelpers.GenNonEmptyString
                   from baseId in GenHelpers.GenNonEmptyString
                   from purity in Gen.Elements(string.Empty, "High", "Medium", "Low")
                   from qty in Gen.Choose(1, 500)
                   select new DeliveryItem
                   {
                       Name = name,
                       BaseItemTypeID = baseId,
                       ResourcePurity = purity,
                       Quantity = qty,
                   };
        }

        /// <summary>
        /// Generates a DeliveryPlanStop with nested item lists.
        /// </summary>
        public static Gen<DeliveryPlanStop> GenDeliveryPlanStop()
        {
            return from colonyUuid in GenHelpers.GenUUID
                   from sequence in Gen.Choose(0, 20)
                   from destType in Gen.Elements(
                       DestinationType.Colony,
                       DestinationType.Station,
                       DestinationType.Asteroid,
                       DestinationType.Ship)
                   from destUuid in GenHelpers.GenUUID
                   from completed in Arb.Generate<bool>()
                   from dropCount in Gen.Choose(0, 3)
                   from dropItems in Gen.ListOf(dropCount, GenDeliveryItem())
                   from pickCount in Gen.Choose(0, 3)
                   from pickItems in Gen.ListOf(pickCount, GenDeliveryItem())
                   select new DeliveryPlanStop
                   {
                       ColonyUUID = colonyUuid,
                       Sequence = sequence,
                       DestinationType = destType,
                       DestinationUUID = destUuid,
                       StopCompleted = completed,
                       DropOff = dropItems.ToList(),
                       PickUp = pickItems.ToList(),
                   };
        }

        /// <summary>
        /// Generates a DeliveryPlan with key fields for round-trip testing.
        /// </summary>
        public static Gen<DeliveryPlan> GenDeliveryPlan()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from routeUuid in GenHelpers.GenUUID
                   from shipUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from completed in Arb.Generate<bool>()
                   from stopCount in Gen.Choose(0, 3)
                   from stops in Gen.ListOf(stopCount, GenDeliveryPlanStop())
                   select new DeliveryPlan
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       RouteUUID = routeUuid,
                       ShipUUID = shipUuid,
                       Name = name,
                       Completed = completed,
                       Stops = stops.ToList(),
                   };
        }

        /// <summary>
        /// Creates an Arbitrary for Survey from the generator.
        /// </summary>
        public static Arbitrary<Survey> ArbSurvey()
        {
            return Arb.From(GenSurvey());
        }

        /// <summary>
        /// Creates an Arbitrary for PlayerProfile from the generator.
        /// </summary>
        public static Arbitrary<PlayerProfile> ArbPlayerProfile()
        {
            return Arb.From(GenPlayerProfile());
        }

        /// <summary>
        /// Creates an Arbitrary for DeliveryRoute from the generator.
        /// </summary>
        public static Arbitrary<DeliveryRoute> ArbDeliveryRoute()
        {
            return Arb.From(GenDeliveryRoute());
        }

        /// <summary>
        /// Creates an Arbitrary for DeliveryPlan from the generator.
        /// </summary>
        public static Arbitrary<DeliveryPlan> ArbDeliveryPlan()
        {
            return Arb.From(GenDeliveryPlan());
        }

        /// <summary>
        /// Generates a ShipComponentSlot with serializable fields.
        /// </summary>
        public static Gen<ShipComponentSlot> GenShipComponentSlot()
        {
            return from slotType in SafeStringGen()
                   from slotIndex in Gen.Choose(0, 10)
                   from blueprintUuid in UuidGen()
                   from currentHp in Gen.Choose(0, 100)
                   from maxHp in Gen.Choose(50, 100)
                   from maxRepair in GenHelpers.GenDecimal
                   select new ShipComponentSlot
                   {
                       SlotType = slotType,
                       SlotIndex = slotIndex,
                       BlueprintUUID = blueprintUuid,
                       CurrentHP = currentHp,
                       MaxHP = maxHp,
                       MaxRepairPercent = maxRepair,
                   };
        }

        /// <summary>
        /// Generates a Ship with serializable fields populated for round-trip testing.
        /// </summary>
        public static Gen<Ship> GenShip()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from name in SafeStringGen()
                   from templateUuid in UuidGen()
                   from hullBpUuid in UuidGen()
                   from locationType in Gen.Elements(
                       DestinationType.Colony,
                       DestinationType.Station,
                       DestinationType.Asteroid,
                       DestinationType.Ship)
                   from locationUuid in UuidGen()
                   from hullCurrent in Gen.Choose(0, 100)
                   from hullMax in Gen.Choose(50, 100)
                   from hullRepair in GenHelpers.GenDecimal
                   from compCount in Gen.Choose(0, 3)
                   from components in Gen.ListOf(compCount, GenShipComponentSlot())
                   select new Ship
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       TemplateUUID = templateUuid,
                       HullBlueprintUUID = hullBpUuid,
                       LocationType = locationType,
                       LocationUUID = locationUuid,
                       HullCurrentHP = hullCurrent,
                       HullMaxHP = hullMax,
                       HullMaxRepairPercent = hullRepair,
                       Components = components.ToList(),
                       Cargo = new ItemBag(),
                       Hopper = new ItemBag(),
                   };
        }

        /// <summary>
        /// Generates a ShipTemplate with serializable fields populated.
        /// </summary>
        public static Gen<ShipTemplate> GenShipTemplate()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from name in SafeStringGen()
                   from hullBpUuid in UuidGen()
                   from compCount in Gen.Choose(0, 3)
                   from components in Gen.ListOf(compCount, GenShipComponentSlot())
                   select new ShipTemplate
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       HullBlueprintUUID = hullBpUuid,
                       Components = components.ToList(),
                   };
        }

        /// <summary>
        /// Generates a Station with serializable fields and nested SystemUUID references.
        /// </summary>
        public static Gen<Station> GenStation()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from name in SafeStringGen()
                   from stationType in Gen.Elements(
                       StationType.Outpost,
                       StationType.Station,
                       StationType.Starbase)
                   from ownership in Gen.Elements(
                       StationOwnership.Government,
                       StationOwnership.PlayerOwned)
                   from systemName in SafeStringGen()
                   from systemId in Gen.Choose(1, 500)
                   from stationBpUuid in UuidGen()
                   from hullCurrent in Gen.Choose(0, 100)
                   from hullMax in Gen.Choose(50, 100)
                   from hullRepair in GenHelpers.GenDecimal
                   from compCount in Gen.Choose(0, 3)
                   from components in Gen.ListOf(compCount, GenShipComponentSlot())
                   select new Station
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       StationType = stationType,
                       Ownership = ownership,
                       SystemName = systemName,
                       SystemId = systemId,
                       StationBlueprintUUID = stationBpUuid,
                       HullCurrentHP = hullCurrent,
                       HullMaxHP = hullMax,
                       HullMaxRepairPercent = hullRepair,
                       Components = components.ToList(),
                       Holds = new Dictionary<string, ItemBag>(),
                       MunitionsHold = new ItemBag(),
                   };
        }

        /// <summary>
        /// Generates a MarketListing with serializable fields.
        /// Uses GenDecimal for PricePerUnit.
        /// </summary>
        public static Gen<MarketListing> GenMarketListing()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from stationUuid in UuidGen()
                   from itemType in Gen.Elements(
                       ItemType.ItemTypeEnum.Commodity,
                       ItemType.ItemTypeEnum.Resource,
                       ItemType.ItemTypeEnum.ShipPart,
                       ItemType.ItemTypeEnum.Munition)
                   from itemName in SafeStringGen()
                   from quantity in Gen.Choose(1, 10000)
                   from pricePerUnit in GenHelpers.GenDecimal
                   from currentHp in Gen.Choose(0, 100)
                   from maxHp in Gen.Choose(50, 100)
                   from maxRepair in GenHelpers.GenDecimal
                   select new MarketListing
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       StationUUID = stationUuid,
                       ItemType = itemType,
                       ItemName = itemName,
                       Quantity = quantity,
                       PricePerUnit = pricePerUnit,
                       CurrentHP = currentHp,
                       MaxHP = maxHp,
                       MaxRepairPercent = maxRepair,
                   };
        }

        /// <summary>
        /// Generates a MarketTransaction with serializable fields.
        /// Uses GenDecimal for PricePerUnit, TotalPrice, and MaxRepairPercent.
        /// </summary>
        public static Gen<MarketTransaction> GenMarketTransaction()
        {
            return from uuid in UuidGen()
                   from ownerUuid in UuidGen()
                   from transType in Gen.Elements(
                       TransactionType.Buy,
                       TransactionType.Sell)
                   from itemType in Gen.Elements(
                       ItemType.ItemTypeEnum.Commodity,
                       ItemType.ItemTypeEnum.Resource,
                       ItemType.ItemTypeEnum.ShipPart,
                       ItemType.ItemTypeEnum.Munition)
                   from itemName in SafeStringGen()
                   from quantity in Gen.Choose(1, 10000)
                   from pricePerUnit in GenHelpers.GenDecimal
                   from totalPrice in GenHelpers.GenDecimal
                   from stationUuid in UuidGen()
                   from counterparty in SafeStringGen()
                   from timestamp in SafeStringGen()
                   from currentHp in Gen.Choose(0, 100)
                   from maxHp in Gen.Choose(50, 100)
                   from maxRepair in GenHelpers.GenDecimal
                   select new MarketTransaction
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       TransactionType = transType,
                       ItemType = itemType,
                       ItemName = itemName,
                       Quantity = quantity,
                       PricePerUnit = pricePerUnit,
                       TotalPrice = totalPrice,
                       StationUUID = stationUuid,
                       Counterparty = counterparty,
                       Timestamp = timestamp,
                       CurrentHP = currentHp,
                       MaxHP = maxHp,
                       MaxRepairPercent = maxRepair,
                   };
        }

        /// <summary>
        /// Creates an Arbitrary for Ship.
        /// </summary>
        public static Arbitrary<Ship> ArbShip()
        {
            return Arb.From(GenShip());
        }

        /// <summary>
        /// Creates an Arbitrary for ShipTemplate.
        /// </summary>
        public static Arbitrary<ShipTemplate> ArbShipTemplate()
        {
            return Arb.From(GenShipTemplate());
        }

        /// <summary>
        /// Creates an Arbitrary for Station.
        /// </summary>
        public static Arbitrary<Station> ArbStation()
        {
            return Arb.From(GenStation());
        }

        /// <summary>
        /// Creates an Arbitrary for MarketListing.
        /// </summary>
        public static Arbitrary<MarketListing> ArbMarketListing()
        {
            return Arb.From(GenMarketListing());
        }

        /// <summary>
        /// Creates an Arbitrary for MarketTransaction.
        /// </summary>
        public static Arbitrary<MarketTransaction> ArbMarketTransaction()
        {
            return Arb.From(GenMarketTransaction());
        }

        /// <summary>
        /// Generates a PricingPlan with key fields for round-trip testing.
        /// </summary>
        public static Gen<PricingPlan> GenPricingPlan()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from description in GenHelpers.GenNonEmptyString
                   from fixedCost in GenHelpers.GenPositiveDecimal
                   from hourlyCost in GenHelpers.GenPositiveDecimal
                   from priceCount in Gen.Choose(0, 3)
                   from priceKeys in Gen.ListOf(priceCount, GenHelpers.GenNonEmptyString)
                   from priceValues in Gen.ListOf(priceCount, GenHelpers.GenPositiveDecimal)
                   select BuildPricingPlan(
                       uuid, ownerUuid, name, description,
                       fixedCost, hourlyCost,
                       priceKeys.ToList(), priceValues.ToList());
        }

        private static PricingPlan BuildPricingPlan(
            string uuid,
            string ownerUuid,
            string name,
            string description,
            decimal fixedCost,
            decimal hourlyCost,
            List<string> priceKeys,
            List<decimal> priceValues)
        {
            var plan = new PricingPlan
            {
                UUID = uuid,
                OwnerUUID = ownerUuid,
                Name = name,
                Description = description,
                FixedCostPerItem = fixedCost,
                HourlyCostRate = hourlyCost,
            };

            for (int i = 0; i < priceKeys.Count; i++)
            {
                plan.ResourcePrices[priceKeys[i]] = priceValues[i];
            }

            return plan;
        }

        /// <summary>
        /// Generates a BuildItem with key fields populated.
        /// </summary>
        public static Gen<BuildItem> GenBuildItem()
        {
            return from uuid in GenHelpers.GenUUID
                   from itemType in Gen.Elements(
                       BuildItemType.Manufactory,
                       BuildItemType.Commodity,
                       BuildItemType.ShipTemplate,
                       BuildItemType.Mining,
                       BuildItemType.Refining,
                       BuildItemType.Research)
                   from status in Gen.Elements(
                       BuildItemStatus.Staged,
                       BuildItemStatus.Delivering,
                       BuildItemStatus.Ready,
                       BuildItemStatus.InProgress,
                       BuildItemStatus.Completed)
                   from itemName in GenHelpers.GenNonEmptyString
                   from quantity in Gen.Choose(1, 100)
                   select new BuildItem
                   {
                       UUID = uuid,
                       ItemType = itemType,
                       Status = status,
                       ItemName = itemName,
                       Quantity = quantity,
                   };
        }

        /// <summary>
        /// Generates a BuildPlan with key fields for round-trip testing.
        /// </summary>
        public static Gen<BuildPlan> GenBuildPlan()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from description in GenHelpers.GenNonEmptyString
                   from isActive in Arb.Generate<bool>()
                   from itemCount in Gen.Choose(0, 3)
                   from items in Gen.ListOf(itemCount, GenBuildItem())
                   select new BuildPlan
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       Description = description,
                       IsActive = isActive,
                       Items = items.ToList(),
                   };
        }

        /// <summary>
        /// Generates a StockTarget with key fields populated.
        /// </summary>
        public static Gen<StockTarget> GenStockTarget()
        {
            return from uuid in GenHelpers.GenUUID
                   from itemType in Gen.Elements(
                       ItemType.ItemTypeEnum.Commodity,
                       ItemType.ItemTypeEnum.Resource,
                       ItemType.ItemTypeEnum.ShipPart,
                       ItemType.ItemTypeEnum.Munition)
                   from itemName in GenHelpers.GenNonEmptyString
                   from targetQty in Gen.Choose(1, 10000)
                   from critThreshold in Gen.Choose(0, 500)
                   from scope in Gen.Elements(
                       StockTargetScope.EmpireWide,
                       StockTargetScope.Colony,
                       StockTargetScope.Station,
                       StockTargetScope.Market,
                       StockTargetScope.StationPlusMarket)
                   select new StockTarget
                   {
                       UUID = uuid,
                       ItemType = itemType,
                       ItemName = itemName,
                       TargetQuantity = targetQty,
                       CriticalThreshold = critThreshold,
                       Scope = scope,
                   };
        }

        /// <summary>
        /// Generates a StockPlan with key fields for round-trip testing.
        /// </summary>
        public static Gen<StockPlan> GenStockPlan()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from isActive in Arb.Generate<bool>()
                   from targetCount in Gen.Choose(0, 3)
                   from targets in Gen.ListOf(targetCount, GenStockTarget())
                   select new StockPlan
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       IsActive = isActive,
                       Targets = targets.ToList(),
                   };
        }

        /// <summary>
        /// Generates a StockProfileEntry with key fields populated.
        /// </summary>
        public static Gen<StockProfileEntry> GenStockProfileEntry()
        {
            return from groupId in GenHelpers.GenNonEmptyString
                   from stockPlanUuid in GenHelpers.GenUUID
                   select new StockProfileEntry
                   {
                       GroupID = groupId,
                       StockPlanUUID = stockPlanUuid,
                   };
        }

        /// <summary>
        /// Generates a StockProfile with key fields for round-trip testing.
        /// </summary>
        public static Gen<StockProfile> GenStockProfile()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from isActive in Arb.Generate<bool>()
                   from entryCount in Gen.Choose(0, 3)
                   from entries in Gen.ListOf(entryCount, GenStockProfileEntry())
                   select new StockProfile
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       IsActive = isActive,
                       Entries = entries.ToList(),
                   };
        }

        /// <summary>
        /// Generates a SupplyChainStage with key fields populated.
        /// </summary>
        public static Gen<SupplyChainStage> GenSupplyChainStage()
        {
            return from sequence in Gen.Choose(0, 10)
                   from stageType in Gen.Elements(
                       SupplyChainStageType.Mine,
                       SupplyChainStageType.AsteroidMine,
                       SupplyChainStageType.PickUp,
                       SupplyChainStageType.Refine,
                       SupplyChainStageType.Deliver,
                       SupplyChainStageType.Research)
                   from locationType in Gen.Elements(
                       DestinationType.Colony,
                       DestinationType.Station,
                       DestinationType.Asteroid,
                       DestinationType.Ship)
                   from locationUuid in GenHelpers.GenUUID
                   from resourceName in GenHelpers.GenNonEmptyString
                   from threshold in Gen.Choose(0, 5000)
                   from rate in GenHelpers.GenPositiveDecimal
                   select new SupplyChainStage
                   {
                       Sequence = sequence,
                       StageType = stageType,
                       LocationType = locationType,
                       LocationUUID = locationUuid,
                       ResourceName = resourceName,
                       AccumulationThreshold = threshold,
                       ProductionRatePerHour = rate,
                   };
        }

        /// <summary>
        /// Generates a SupplyChain with key fields for round-trip testing.
        /// </summary>
        public static Gen<SupplyChain> GenSupplyChain()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from name in GenHelpers.GenNonEmptyString
                   from isActive in Arb.Generate<bool>()
                   from stageCount in Gen.Choose(0, 3)
                   from stages in Gen.ListOf(stageCount, GenSupplyChainStage())
                   select new SupplyChain
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       Name = name,
                       IsActive = isActive,
                       Stages = stages.ToList(),
                   };
        }

        /// <summary>
        /// Generates a WarehouseOverflowRule with key fields for round-trip testing.
        /// </summary>
        public static Gen<WarehouseOverflowRule> GenWarehouseOverflowRule()
        {
            return from uuid in GenHelpers.GenUUID
                   from ownerUuid in GenHelpers.GenUUID
                   from isActive in Arb.Generate<bool>()
                   from colonyUuid in GenHelpers.GenUUID
                   from resourceName in GenHelpers.GenNonEmptyString
                   from resourcePurity in Gen.Elements("High", "Medium", "Low", "Refined")
                   from ruleType in Gen.Elements(
                       OverflowRuleType.SpecificResource,
                       OverflowRuleType.TotalWarehouse)
                   from threshold in GenHelpers.GenPositiveDecimal
                   from destType in Gen.Elements(
                       DestinationType.Colony,
                       DestinationType.Station,
                       DestinationType.Asteroid,
                       DestinationType.Ship)
                   from destUuid in GenHelpers.GenUUID
                   select new WarehouseOverflowRule
                   {
                       UUID = uuid,
                       OwnerUUID = ownerUuid,
                       IsActive = isActive,
                       ColonyUUID = colonyUuid,
                       ResourceName = resourceName,
                       ResourcePurity = resourcePurity,
                       RuleType = ruleType,
                       TriggerThreshold = threshold,
                       DestinationType = destType,
                       DestinationUUID = destUuid,
                   };
        }

        /// <summary>
        /// Creates an Arbitrary for PricingPlan.
        /// </summary>
        public static Arbitrary<PricingPlan> ArbPricingPlan()
        {
            return Arb.From(GenPricingPlan());
        }

        /// <summary>
        /// Creates an Arbitrary for BuildPlan.
        /// </summary>
        public static Arbitrary<BuildPlan> ArbBuildPlan()
        {
            return Arb.From(GenBuildPlan());
        }

        /// <summary>
        /// Creates an Arbitrary for StockPlan.
        /// </summary>
        public static Arbitrary<StockPlan> ArbStockPlan()
        {
            return Arb.From(GenStockPlan());
        }

        /// <summary>
        /// Creates an Arbitrary for StockProfile.
        /// </summary>
        public static Arbitrary<StockProfile> ArbStockProfile()
        {
            return Arb.From(GenStockProfile());
        }

        /// <summary>
        /// Creates an Arbitrary for SupplyChain.
        /// </summary>
        public static Arbitrary<SupplyChain> ArbSupplyChain()
        {
            return Arb.From(GenSupplyChain());
        }

        /// <summary>
        /// Creates an Arbitrary for WarehouseOverflowRule.
        /// </summary>
        public static Arbitrary<WarehouseOverflowRule> ArbWarehouseOverflowRule()
        {
            return Arb.From(GenWarehouseOverflowRule());
        }
    }
}
