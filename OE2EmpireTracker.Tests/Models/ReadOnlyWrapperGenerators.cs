using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Custom FsCheck Arbitrary generators for all entity types used by
    /// the readonly-data-wrappers property tests.
    /// </summary>
    public static class ReadOnlyWrapperGenerators
    {
        private static readonly string[] SampleNames =
        {
            "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet"
        };

        private static readonly string[] SampleDescriptions =
        {
            "A test description", "Another description", "Short desc", "Detailed description text", "Placeholder"
        };

        private static readonly string[] SamplePurities =
        {
            "Low", "Medium", "High", "Refined"
        };

        private static readonly string[] SampleResources =
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Trans-Metals", "Metallics"
        };

        private static readonly string[] SampleTechLevels =
        {
            "LL", "ML", "HL"
        };

        private static readonly string[] SampleBlueprintTypes =
        {
            "Mining Rig", "Refinery", "Manufactory", "Research Laboratory", "Habitation", "Warehouse", "Power Plant"
        };

        private static readonly string[] SampleSlotTypes =
        {
            "Reactor", "Main Drive", "Thrusters", "Nav Comp", "Shields", "Hull Plating", "Weapon", "Mining Laser"
        };

        private static readonly string[] SamplePropKeys =
        {
            "Power", "Workers", "Capacity", "Rate", "Built", "Online"
        };

        private static readonly string[] SamplePropValues =
        {
            "100", "True", "50", "1.5", "False", "200"
        };

        // --- Primitive generators ---

        public static Gen<string> GenUUID() => Gen.Fresh(() => Guid.NewGuid().ToString());

        public static Gen<string> GenName() => Gen.Elements(SampleNames);

        public static Gen<T> GenEnum<T>() where T : struct =>
            Gen.Elements(Enum.GetValues(typeof(T)).Cast<T>().ToArray());

        public static Gen<List<T>> GenSmallList<T>(Gen<T> elementGen, int maxCount = 5) =>
            from count in Gen.Choose(0, maxCount)
            from items in Gen.ListOf(count, elementGen)
            select items.ToList();

        public static Gen<T> GenNullable<T>(Gen<T> gen) where T : class =>
            Gen.OneOf(Gen.Constant<T>(null), gen);

        // --- Utility container generators ---

        public static Gen<PropertyBag> GenPropertyBag() =>
            from count in Gen.Choose(0, 5)
            from keys in Gen.ListOf(count, Gen.Elements(SamplePropKeys))
            from vals in Gen.ListOf(count, Gen.Elements(SamplePropValues))
            select MakePropertyBag(keys.ToList(), vals.ToList());

        private static PropertyBag MakePropertyBag(List<string> keys, List<string> vals)
        {
            var bag = new PropertyBag();
            for (int i = 0; i < keys.Count; i++)
            {
                bag.SetProperty(keys[i], vals[i]);
            }

            return bag;
        }

        public static Gen<ItemBag> GenItemBag() =>
            from items in GenSmallList(GenItem(), 5)
            select MakeItemBag(items);

        private static ItemBag MakeItemBag(List<Item> items)
        {
            var bag = new ItemBag();
            foreach (var item in items)
            {
                bag.AddItem(item);
            }

            return bag;
        }

        public static Gen<LockTracking> GenLockTracking() =>
            from count in Gen.Choose(0, 3)
            from processIds in Gen.ListOf(count, GenUUID())
            from itemTypes in Gen.ListOf(count, GenEnum<ItemType.ItemTypeEnum>())
            from quantities in Gen.ListOf(count, Gen.Choose(1, 100))
            select MakeLockTracking(processIds.ToList(), itemTypes.ToList(), quantities.ToList());

        private static LockTracking MakeLockTracking(
            List<string> processIds, List<ItemType.ItemTypeEnum> itemTypes, List<int> quantities)
        {
            var tracking = new LockTracking();
            for (int i = 0; i < processIds.Count; i++)
            {
                tracking.LockItem(processIds[i], itemTypes[i], processIds[i], quantities[i]);
            }

            return tracking;
        }

        public static Gen<CountDownTime> GenCountDownTime() =>
            from elapsed in Gen.Choose(0, 86400)
            from remaining in Gen.Choose(0, 3600)
            from repeating in Gen.Elements(true, false)
            select MakeCountDownTime(elapsed, remaining, repeating);

        private static CountDownTime MakeCountDownTime(int elapsed, int remaining, bool repeating)
        {
            var cdt = new CountDownTime();
            cdt.StartTime = DateTime.UtcNow.AddSeconds(-elapsed);
            cdt.EndTime = DateTime.UtcNow.AddSeconds(remaining);
            cdt.RepeatIntervalSeconds = repeating ? (long)Math.Max(60, remaining) : 0;
            return cdt;
        }

        // --- Core entity generators ---

        public static Gen<Item> GenItem() =>
            from uuid in GenUUID()
            from itemType in GenEnum<ItemType.ItemTypeEnum>()
            from name in GenName()
            from qty in Gen.Choose(0, 1000)
            from vol in Gen.Choose(0, 10000)
            from nick in GenName()
            from purity in Gen.Elements(SamplePurities)
            from hp in Gen.Choose(0, 1000)
            from maxHp in Gen.Choose(0, 1000)
            from repair in Gen.Choose(0, 10000)
            select MakeItem(uuid, itemType, name, qty, vol, nick, purity, hp, maxHp, repair);

        private static Item MakeItem(
            string uuid, ItemType.ItemTypeEnum itemType, string name, int qty, int vol,
            string nick, string purity, int hp, int maxHp, int repair)
        {
            var item = new Item(itemType, name);
            item.UUID = uuid;
            item.BaseItemTypeID = name;
            item.Quantity = qty;
            item.Volume = vol / 100m;
            item.NickName = nick;
            item.ResourcePurity = purity;
            item.CurrentHP = hp;
            item.MaxHP = maxHp;
            item.MaxRepairPercent = repair / 100m;
            return item;
        }

        public static Gen<OE2EmpireTracker.Models.Blueprint> GenBlueprint() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from bpType in Gen.Elements(SampleBlueprintTypes)
            from evo in Gen.Choose(0, 10)
            from tech in Gen.Elements(SampleTechLevels)
            from cls in Gen.Choose(0, 5)
            from cost in Gen.Choose(0, 500)
            from props in GenPropertyBag()
            from desc in Gen.Elements(SampleDescriptions)
            select MakeBlueprint(uuid, name, owner, bpType, evo, tech, cls, cost, props, desc);

        private static OE2EmpireTracker.Models.Blueprint MakeBlueprint(
            string uuid, string name, string owner, string bpType, int evo,
            string tech, int cls, int cost, PropertyBag props, string desc)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name);
            bp.UUID = uuid;
            bp.OwnerUUID = owner;
            bp.BluePrintType = bpType;
            bp.Evolution = evo;
            bp.TechLevel = tech;
            bp.Class = cls;
            bp.CopyCost = cost;
            bp.Properties = props;
            bp.Description = desc;
            bp.NickName = name;
            bp.BaseBlueprintUUID = uuid;
            return bp;
        }

        public static Gen<SurveyResource> GenSurveyResource() =>
            from resource in Gen.Elements(SampleResources)
            from purity in Gen.Elements(SamplePurities)
            from amount in Gen.Choose(0, 1000)
            select new SurveyResource(resource, purity, amount.ToString());

        public static Gen<Survey> GenSurvey() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from planet in GenName()
            from system in GenName()
            from surveyId in GenName()
            from resources in GenSmallList(GenSurveyResource(), 5)
            from surveyType in GenEnum<SurveyType>()
            from asteroidUuid in GenUUID()
            select MakeSurvey(uuid, name, owner, planet, system, surveyId, resources, surveyType, asteroidUuid);

        private static Survey MakeSurvey(
            string uuid, string name, string owner, string planet, string system,
            string surveyId, List<SurveyResource> resources, SurveyType surveyType, string asteroidUuid)
        {
            var survey = new Survey(name);
            survey.UUID = uuid;
            survey.OwnerUUID = owner;
            survey.PlanetName = planet;
            survey.SystemName = system;
            survey.SurveyID = surveyId;
            survey.SurveyType = surveyType;
            survey.AsteroidUUID = asteroidUuid;
            foreach (var sr in resources)
            {
                survey.Resources[Guid.NewGuid().ToString()] = sr;
            }

            return survey;
        }

        public static Gen<ColonyStructureStatus> GenColonyStructureStatus() =>
            from pp in Gen.Choose(0, 10000)
            from pr in Gen.Choose(0, 10000)
            from hp in Gen.Choose(0, 10000)
            from hr in Gen.Choose(0, 10000)
            from fp in Gen.Choose(0, 10000)
            from fr in Gen.Choose(0, 10000)
            from ep in Gen.Choose(0, 10000)
            from er in Gen.Choose(0, 10000)
            from wc in Gen.Choose(0, 10000)
            from wr in Gen.Choose(0, 10000)
            select new ColonyStructureStatus
            {
                PowerProvided = pp / 100m,
                PowerRequired = pr / 100m,
                HabitationProvision = hp / 100m,
                HabitationRequired = hr / 100m,
                FoodProvision = fp / 100m,
                FoodRequired = fr / 100m,
                EntertainmentProvided = ep / 100m,
                EntertainmentRequired = er / 100m,
                WarehouseCapacity = wc / 100m,
                WarehouseRequired = wr / 100m
            };

        public static Gen<ColonyStructure> GenColonyStructure() =>
            from uuid in GenUUID()
            from props in GenPropertyBag()
            from workers in GenPropertyBag()
            from buildTime in GenNullable(GenCountDownTime())
            from processTime in GenNullable(GenCountDownTime())
            from seq in Gen.Choose(0, 100)
            from buildId in Gen.Choose(0, 50)
            from bqSeq in Gen.Choose(0, 20)
            from staging in Gen.Elements(true, false)
            from mfgQty in Gen.Choose(0, 1000)
            select MakeColonyStructure(uuid, props, workers, buildTime, processTime, seq, buildId, bqSeq, staging, mfgQty);

        private static ColonyStructure MakeColonyStructure(
            string uuid, PropertyBag props, PropertyBag workers,
            CountDownTime buildTime, CountDownTime processTime,
            int seq, int buildId, int bqSeq, bool staging, int mfgQty)
        {
            var cs = new ColonyStructure();
            cs.UUID = uuid;
            cs.Properties = props;
            cs.AssignedWorkers = workers;
            cs.BuildCompletionTime = buildTime;
            cs.ProcessCompletionTime = processTime;
            cs.DisplaySequence = seq;
            cs.BuildingID = buildId;
            cs.BuildQueueSequence = bqSeq;
            cs.StagingResources = staging;
            cs.ManufacturingQuantity = mfgQty;
            return cs;
        }

        public static Gen<CommodityRequested> GenCommodityRequested() =>
            from name in GenName()
            from requested in Gen.Choose(0, 1000)
            from delivered in Gen.Choose(0, 1000)
            from fulfilled in Gen.Elements(true, false)
            select new CommodityRequested
            {
                Name = name,
                Requested = requested,
                Delivered = delivered,
                NeedBy = DateTime.UtcNow.AddDays(requested % 30),
                Fulfilled = fulfilled
            };

        public static Gen<Colony> GenColony() =>
            from uuid in GenUUID()
            from owner in GenUUID()
            from colonyName in GenName()
            from planet in GenName()
            from system in GenName()
            from items in GenItemBag()
            from structures in GenSmallList(GenColonyStructure(), 5)
            from commodities in GenSmallList(GenCommodityRequested(), 3)
            from locks in GenLockTracking()
            select MakeColony(uuid, owner, colonyName, planet, system, items, structures, commodities, locks);

        private static Colony MakeColony(
            string uuid, string owner, string colonyName, string planet, string system,
            ItemBag items, List<ColonyStructure> structures, List<CommodityRequested> commodities, LockTracking locks)
        {
            var colony = new Colony();
            colony.UUID = uuid;
            colony.OwnerUUID = owner;
            colony.ColonyName = colonyName;
            colony.PlanetName = planet;
            colony.SystemName = system;
            colony.Items = items;
            colony.Structures = structures;
            colony.Commodities = commodities;
            colony.Locks = locks;
            return colony;
        }

        public static Gen<PlayerRank> GenPlayerRank() =>
            from rank in Gen.Choose(0, 50)
            from currentXp in Gen.Choose(0, 100000)
            from nextXp in Gen.Choose(0, 100000)
            from title in GenName()
            select new PlayerRank { Rank = rank, CurrentXP = (long)currentXp, NextXP = (long)nextXp, Title = title };

        public static Gen<PlayerSkill> GenPlayerSkill() =>
            from level in Gen.Choose(0, 10)
            select new PlayerSkill { Level = level };

        public static Gen<PlayerProfile> GenPlayerProfile() =>
            from uuid in GenUUID()
            from name in GenName()
            from faction in GenName()
            from factionUuid in GenUUID()
            from credits in Gen.Choose(0, 10000)
            from pubRank in GenPlayerRank()
            from privRank in GenPlayerRank()
            from milRank in GenPlayerRank()
            from skillPts in Gen.Choose(0, 100)
            from citizenId in GenName()
            select MakePlayerProfile(uuid, name, faction, factionUuid, credits, pubRank, privRank, milRank, skillPts, citizenId);

        private static PlayerProfile MakePlayerProfile(
            string uuid, string name, string faction, string factionUuid, int credits,
            PlayerRank pubRank, PlayerRank privRank, PlayerRank milRank, int skillPts, string citizenId)
        {
            var profile = new PlayerProfile();
            profile.UUID = uuid;
            profile.Name = name;
            profile.Faction = faction;
            profile.FactionUUID = factionUuid;
            profile.TotalCredits = credits / 100m;
            profile.Public = pubRank;
            profile.Private = privRank;
            profile.Military = milRank;
            profile.SkillPoints = skillPts;
            profile.CitizenId = citizenId;
            profile.Skills["Human Resources"] = new PlayerSkill { Level = skillPts % 11 };
            profile.Skills["Foreman"] = new PlayerSkill { Level = (skillPts + 3) % 11 };
            return profile;
        }

        public static Gen<RouteStop> GenRouteStop() =>
            from colonyUuid in GenUUID()
            from seq in Gen.Choose(0, 20)
            from destType in GenEnum<DestinationType>()
            from destUuid in GenUUID()
            from purpose in GenEnum<RouteStopPurpose>()
            from fuel in Gen.Choose(0, 10000)
            select new RouteStop
            {
                ColonyUUID = colonyUuid, Sequence = seq, DestinationType = destType,
                DestinationUUID = destUuid, Purpose = purpose, FuelEstimate = fuel / 100m
            };

        public static Gen<DeliveryRoute> GenDeliveryRoute() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from stops in GenSmallList(GenRouteStop(), 5)
            select new DeliveryRoute { UUID = uuid, Name = name, OwnerUUID = owner, Stops = stops };

        public static Gen<DeliveryItem> GenDeliveryItem() =>
            from itemType in GenEnum<ItemType.ItemTypeEnum>()
            from baseId in GenName()
            from name in GenName()
            from purity in Gen.Elements(SamplePurities)
            from qty in Gen.Choose(0, 1000)
            from delivered in Gen.Elements(true, false)
            select new DeliveryItem
            {
                ItemType = itemType, BaseItemTypeID = baseId, Name = name,
                ResourcePurity = purity, Quantity = qty, Delivered = delivered
            };

        public static Gen<DeliveryPlanStop> GenDeliveryPlanStop() =>
            from colonyUuid in GenUUID()
            from seq in Gen.Choose(0, 20)
            from completed in Gen.Elements(true, false)
            from dropOff in GenSmallList(GenDeliveryItem(), 3)
            from pickUp in GenSmallList(GenDeliveryItem(), 3)
            from destType in GenEnum<DestinationType>()
            from destUuid in GenUUID()
            select new DeliveryPlanStop
            {
                ColonyUUID = colonyUuid, Sequence = seq, StopCompleted = completed,
                DropOff = dropOff, PickUp = pickUp, DestinationType = destType, DestinationUUID = destUuid
            };

        public static Gen<DeliveryPlan> GenDeliveryPlan() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from routeUuid in GenUUID()
            from shipUuid in GenUUID()
            from completed in Gen.Elements(true, false)
            from stops in GenSmallList(GenDeliveryPlanStop(), 5)
            select new DeliveryPlan
            {
                UUID = uuid, Name = name, OwnerUUID = owner,
                RouteUUID = routeUuid, ShipUUID = shipUuid, Completed = completed, Stops = stops
            };

        public static Gen<BuildItem> GenBuildItem() =>
            from uuid in GenUUID()
            from itemType in GenEnum<BuildItemType>()
            from status in GenEnum<BuildItemStatus>()
            from bpUuid in GenUUID()
            from itemName in GenName()
            from commodityName in GenName()
            from shipTemplateUuid in GenUUID()
            from qty in Gen.Choose(0, 1000)
            from buildLocType in GenEnum<DestinationType>()
            from buildLocUuid in GenUUID()
            select MakeBuildItem(uuid, itemType, status, bpUuid, itemName, commodityName, shipTemplateUuid, qty, buildLocType, buildLocUuid);

        private static BuildItem MakeBuildItem(
            string uuid, BuildItemType itemType, BuildItemStatus status, string bpUuid,
            string itemName, string commodityName, string shipTemplateUuid, int qty,
            DestinationType buildLocType, string buildLocUuid)
        {
            return new BuildItem
            {
                UUID = uuid, ItemType = itemType, Status = status, BlueprintUUID = bpUuid,
                ItemName = itemName, CommodityName = commodityName, ShipTemplateUUID = shipTemplateUuid,
                Quantity = qty, BuildLocationType = buildLocType, BuildLocationUUID = buildLocUuid
            };
        }

        public static Gen<BuildPlan> GenBuildPlan() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from desc in Gen.Elements(SampleDescriptions)
            from deliveryPlanUuid in GenUUID()
            from isActive in Gen.Elements(true, false)
            from items in GenSmallList(GenBuildItem(), 5)
            select new BuildPlan
            {
                UUID = uuid, Name = name, OwnerUUID = owner, Description = desc,
                DeliveryPlanUUID = deliveryPlanUuid, IsActive = isActive, Items = items
            };

        public static Gen<ShipComponentSlot> GenShipComponentSlot() =>
            from slotType in Gen.Elements(SampleSlotTypes)
            from slotIndex in Gen.Choose(0, 10)
            from bpUuid in GenUUID()
            from hp in Gen.Choose(0, 1000)
            from maxHp in Gen.Choose(0, 1000)
            from repair in Gen.Choose(0, 10000)
            select new ShipComponentSlot
            {
                SlotType = slotType, SlotIndex = slotIndex, BlueprintUUID = bpUuid,
                CurrentHP = hp, MaxHP = maxHp, MaxRepairPercent = repair / 100m
            };

        public static Gen<ShipTemplate> GenShipTemplate() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from hullBpUuid in GenUUID()
            from components in GenSmallList(GenShipComponentSlot(), 5)
            select new ShipTemplate
            {
                UUID = uuid, Name = name, OwnerUUID = owner,
                HullBlueprintUUID = hullBpUuid, Components = components
            };

        public static Gen<Ship> GenShip() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from templateUuid in GenUUID()
            from hullBpUuid in GenUUID()
            from components in GenSmallList(GenShipComponentSlot(), 5)
            from locType in GenEnum<DestinationType>()
            from locUuid in GenUUID()
            from cargo in GenItemBag()
            from hopper in GenItemBag()
            select MakeShip(uuid, name, owner, templateUuid, hullBpUuid, components, locType, locUuid, cargo, hopper);

        private static Ship MakeShip(
            string uuid, string name, string owner, string templateUuid, string hullBpUuid,
            List<ShipComponentSlot> components, DestinationType locType, string locUuid,
            ItemBag cargo, ItemBag hopper)
        {
            return new Ship
            {
                UUID = uuid, Name = name, OwnerUUID = owner, TemplateUUID = templateUuid,
                HullBlueprintUUID = hullBpUuid, Components = components, LocationType = locType,
                LocationUUID = locUuid, Cargo = cargo, Hopper = hopper
            };
        }

        public static Gen<Station> GenStation() =>
            from uuid in GenUUID()
            from name in GenName()
            from stationType in GenEnum<StationType>()
            from ownership in GenEnum<StationOwnership>()
            from owner in GenUUID()
            from components in GenSmallList(GenShipComponentSlot(), 3)
            from stationBpUuid in GenUUID()
            from munitions in GenItemBag()
            from hp in Gen.Choose(0, 1000)
            from maxHp in Gen.Choose(0, 1000)
            select new Station
            {
                UUID = uuid, Name = name, StationType = stationType, Ownership = ownership,
                OwnerUUID = owner, Components = components, StationBlueprintUUID = stationBpUuid,
                MunitionsHold = munitions, HullCurrentHP = hp, HullMaxHP = maxHp
            };

        public static Gen<MarketListing> GenMarketListing() =>
            from uuid in GenUUID()
            from owner in GenUUID()
            from stationUuid in GenUUID()
            from itemType in GenEnum<ItemType.ItemTypeEnum>()
            from refId in GenName()
            from itemName in GenName()
            from qty in Gen.Choose(0, 1000)
            from price in Gen.Choose(0, 10000)
            from hp in Gen.Choose(0, 1000)
            from maxHp in Gen.Choose(0, 1000)
            select new MarketListing
            {
                UUID = uuid, OwnerUUID = owner, StationUUID = stationUuid, ItemType = itemType,
                ItemReferenceID = refId, ItemName = itemName, Quantity = qty, PricePerUnit = price / 100m,
                CurrentHP = hp, MaxHP = maxHp
            };

        public static Gen<MarketTransaction> GenMarketTransaction() =>
            from uuid in GenUUID()
            from owner in GenUUID()
            from txType in GenEnum<TransactionType>()
            from itemType in GenEnum<ItemType.ItemTypeEnum>()
            from refId in GenName()
            from itemName in GenName()
            from qty in Gen.Choose(0, 1000)
            from price in Gen.Choose(0, 10000)
            from total in Gen.Choose(0, 10000)
            from counterparty in GenName()
            select MakeMarketTransaction(uuid, owner, txType, itemType, refId, itemName, qty, price, total, counterparty);

        private static MarketTransaction MakeMarketTransaction(
            string uuid, string owner, TransactionType txType, ItemType.ItemTypeEnum itemType,
            string refId, string itemName, int qty, int price, int total, string counterparty)
        {
            return new MarketTransaction
            {
                UUID = uuid, OwnerUUID = owner, TransactionType = txType, ItemType = itemType,
                ItemReferenceID = refId, ItemName = itemName, Quantity = qty,
                PricePerUnit = price / 100m, TotalPrice = total / 100m, Counterparty = counterparty
            };
        }

        public static Gen<StockTarget> GenStockTarget() =>
            from uuid in GenUUID()
            from itemType in GenEnum<ItemType.ItemTypeEnum>()
            from refId in GenName()
            from itemName in GenName()
            from shipTemplateUuid in GenUUID()
            from targetQty in Gen.Choose(0, 1000)
            from critical in Gen.Choose(0, 1000)
            from scope in GenEnum<StockTargetScope>()
            from locUuid in GenUUID()
            select new StockTarget
            {
                UUID = uuid, ItemType = itemType, ItemReferenceID = refId, ItemName = itemName,
                ShipTemplateUUID = shipTemplateUuid, TargetQuantity = targetQty,
                CriticalThreshold = critical, Scope = scope, LocationUUID = locUuid
            };

        public static Gen<StockPlan> GenStockPlan() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from replenishUuid in GenUUID()
            from isActive in Gen.Elements(true, false)
            from targets in GenSmallList(GenStockTarget(), 5)
            select new StockPlan
            {
                UUID = uuid, Name = name, OwnerUUID = owner,
                ReplenishmentBuildPlanUUID = replenishUuid, IsActive = isActive, Targets = targets
            };

        public static Gen<StockProfileEntry> GenStockProfileEntry() =>
            from groupId in GenName()
            from stockPlanUuid in GenUUID()
            select new StockProfileEntry { GroupID = groupId, StockPlanUUID = stockPlanUuid };

        public static Gen<StockProfile> GenStockProfile() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from isActive in Gen.Elements(true, false)
            from entries in GenSmallList(GenStockProfileEntry(), 5)
            select new StockProfile
            {
                UUID = uuid, Name = name, OwnerUUID = owner, IsActive = isActive, Entries = entries
            };

        public static Gen<SupplyChainStage> GenSupplyChainStage() =>
            from seq in Gen.Choose(0, 20)
            from stageType in GenEnum<SupplyChainStageType>()
            from locType in GenEnum<DestinationType>()
            from locUuid in GenUUID()
            from resource in Gen.Elements(SampleResources)
            from purity in Gen.Elements(SamplePurities)
            from threshold in Gen.Choose(0, 1000)
            from rate in Gen.Choose(0, 10000)
            from routeUuid in GenUUID()
            select new SupplyChainStage
            {
                Sequence = seq, StageType = stageType, LocationType = locType, LocationUUID = locUuid,
                ResourceName = resource, ResourcePurity = purity, AccumulationThreshold = threshold,
                ProductionRatePerHour = rate / 100m, DeliveryRouteUUID = routeUuid
            };

        public static Gen<SupplyChain> GenSupplyChain() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from isActive in Gen.Elements(true, false)
            from stages in GenSmallList(GenSupplyChainStage(), 5)
            select new SupplyChain
            {
                UUID = uuid, Name = name, OwnerUUID = owner, IsActive = isActive, Stages = stages
            };

        public static Gen<WarehouseOverflowRule> GenWarehouseOverflowRule() =>
            from uuid in GenUUID()
            from owner in GenUUID()
            from isActive in Gen.Elements(true, false)
            from colonyUuid in GenUUID()
            from resource in Gen.Elements(SampleResources)
            from purity in Gen.Elements(SamplePurities)
            from threshold in Gen.Choose(0, 1000)
            from destType in GenEnum<DestinationType>()
            from destUuid in GenUUID()
            from routeUuid in GenUUID()
            select new WarehouseOverflowRule
            {
                UUID = uuid, OwnerUUID = owner, IsActive = isActive, ColonyUUID = colonyUuid,
                ResourceName = resource, ResourcePurity = purity, TriggerThreshold = threshold,
                DestinationType = destType, DestinationUUID = destUuid, DeliveryRouteUUID = routeUuid
            };

        public static Gen<Faction> GenFaction() =>
            from uuid in GenUUID()
            from name in GenName()
            from desc in Gen.Elements(SampleDescriptions)
            select new Faction { UUID = uuid, Name = name, Description = desc };

        public static Gen<ExternalCharacter> GenExternalCharacter() =>
            from uuid in GenUUID()
            from name in GenName()
            from factionUuid in GenUUID()
            select new ExternalCharacter { UUID = uuid, Name = name, FactionUUID = factionUuid };

        public static Gen<AsteroidReserve> GenAsteroidReserve() =>
            from resource in Gen.Elements(SampleResources)
            from purity in Gen.Elements(SamplePurities)
            from maxReserve in Gen.Choose(0, 1000)
            from currentReserve in Gen.Choose(0, 1000)
            from resetTimestamp in GenName()
            select new AsteroidReserve
            {
                ResourceName = resource, Purity = purity, MaxReserve = maxReserve,
                CurrentReserve = currentReserve, ResetTimestamp = resetTimestamp
            };

        public static Gen<Asteroid> GenAsteroid() =>
            from uuid in GenUUID()
            from name in GenName()
            from system in GenName()
            from reserves in GenSmallList(GenAsteroidReserve(), 5)
            select new Asteroid { UUID = uuid, Name = name, SystemName = system, Reserves = reserves };

        public static Gen<PricingPlan> GenPricingPlan() =>
            from uuid in GenUUID()
            from name in GenName()
            from owner in GenUUID()
            from desc in Gen.Elements(SampleDescriptions)
            from fixedCost in Gen.Choose(0, 10000)
            from hourlyRate in Gen.Choose(0, 10000)
            select MakePricingPlan(uuid, name, owner, desc, fixedCost, hourlyRate);

        private static PricingPlan MakePricingPlan(
            string uuid, string name, string owner, string desc, int fixedCost, int hourlyRate)
        {
            var plan = new PricingPlan();
            plan.UUID = uuid;
            plan.Name = name;
            plan.OwnerUUID = owner;
            plan.Description = desc;
            plan.FixedCostPerItem = fixedCost / 100m;
            plan.HourlyCostRate = hourlyRate / 100m;
            plan.ResourcePrices["Alkali Metals|Refined"] = fixedCost / 100m;
            plan.ResourcePrices["Noble Gases|High"] = hourlyRate / 100m;
            return plan;
        }

        public static Gen<Commodity> GenCommodity() =>
            from industry in GenEnum<CommodityIndustry.CommodityIndustryEnum>()
            from commodityGroup in GenEnum<CommodityGroup.CommodityGroupEnum>()
            from id in GenName()
            from name in GenName()
            select MakeCommodity(industry, commodityGroup, id, name);

        private static Commodity MakeCommodity(
            CommodityIndustry.CommodityIndustryEnum industry,
            CommodityGroup.CommodityGroupEnum commodityGroup, string id, string name)
        {
            var commodity = new Commodity();
            commodity.CommodityIndustry = industry;
            commodity.CommodityGroup = commodityGroup;
            commodity.ID = id;
            commodity.Name = name;
            commodity.ConstructionResources = new Dictionary<string, string>
            {
                { "Alkali Metals", "2" },
                { "Noble Gases", "1" }
            };
            return commodity;
        }

        /// <summary>
        /// FsCheck Arbitrary class that registers all entity generators.
        /// Use with [Property(Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })].
        /// </summary>
        public class ReadOnlyWrapperArbitraries
        {
            public static Arbitrary<PropertyBag> PropertyBagArb() => Arb.From(GenPropertyBag());

            public static Arbitrary<ItemBag> ItemBagArb() => Arb.From(GenItemBag());

            public static Arbitrary<LockTracking> LockTrackingArb() => Arb.From(GenLockTracking());

            public static Arbitrary<CountDownTime> CountDownTimeArb() => Arb.From(GenCountDownTime());

            public static Arbitrary<Item> ItemArb() => Arb.From(GenItem());

            public static Arbitrary<OE2EmpireTracker.Models.Blueprint> BlueprintArb() => Arb.From(GenBlueprint());

            public static Arbitrary<SurveyResource> SurveyResourceArb() => Arb.From(GenSurveyResource());

            public static Arbitrary<Survey> SurveyArb() => Arb.From(GenSurvey());

            public static Arbitrary<ColonyStructureStatus> ColonyStructureStatusArb() => Arb.From(GenColonyStructureStatus());

            public static Arbitrary<ColonyStructure> ColonyStructureArb() => Arb.From(GenColonyStructure());

            public static Arbitrary<CommodityRequested> CommodityRequestedArb() => Arb.From(GenCommodityRequested());

            public static Arbitrary<Colony> ColonyArb() => Arb.From(GenColony());

            public static Arbitrary<PlayerRank> PlayerRankArb() => Arb.From(GenPlayerRank());

            public static Arbitrary<PlayerSkill> PlayerSkillArb() => Arb.From(GenPlayerSkill());

            public static Arbitrary<PlayerProfile> PlayerProfileArb() => Arb.From(GenPlayerProfile());

            public static Arbitrary<RouteStop> RouteStopArb() => Arb.From(GenRouteStop());

            public static Arbitrary<DeliveryRoute> DeliveryRouteArb() => Arb.From(GenDeliveryRoute());

            public static Arbitrary<DeliveryItem> DeliveryItemArb() => Arb.From(GenDeliveryItem());

            public static Arbitrary<DeliveryPlanStop> DeliveryPlanStopArb() => Arb.From(GenDeliveryPlanStop());

            public static Arbitrary<DeliveryPlan> DeliveryPlanArb() => Arb.From(GenDeliveryPlan());

            public static Arbitrary<BuildItem> BuildItemArb() => Arb.From(GenBuildItem());

            public static Arbitrary<BuildPlan> BuildPlanArb() => Arb.From(GenBuildPlan());

            public static Arbitrary<ShipComponentSlot> ShipComponentSlotArb() => Arb.From(GenShipComponentSlot());

            public static Arbitrary<ShipTemplate> ShipTemplateArb() => Arb.From(GenShipTemplate());

            public static Arbitrary<Ship> ShipArb() => Arb.From(GenShip());

            public static Arbitrary<Station> StationArb() => Arb.From(GenStation());

            public static Arbitrary<MarketListing> MarketListingArb() => Arb.From(GenMarketListing());

            public static Arbitrary<MarketTransaction> MarketTransactionArb() => Arb.From(GenMarketTransaction());

            public static Arbitrary<StockTarget> StockTargetArb() => Arb.From(GenStockTarget());

            public static Arbitrary<StockPlan> StockPlanArb() => Arb.From(GenStockPlan());

            public static Arbitrary<StockProfileEntry> StockProfileEntryArb() => Arb.From(GenStockProfileEntry());

            public static Arbitrary<StockProfile> StockProfileArb() => Arb.From(GenStockProfile());

            public static Arbitrary<SupplyChainStage> SupplyChainStageArb() => Arb.From(GenSupplyChainStage());

            public static Arbitrary<SupplyChain> SupplyChainArb() => Arb.From(GenSupplyChain());

            public static Arbitrary<WarehouseOverflowRule> WarehouseOverflowRuleArb() => Arb.From(GenWarehouseOverflowRule());

            public static Arbitrary<Faction> FactionArb() => Arb.From(GenFaction());

            public static Arbitrary<ExternalCharacter> ExternalCharacterArb() => Arb.From(GenExternalCharacter());

            public static Arbitrary<AsteroidReserve> AsteroidReserveArb() => Arb.From(GenAsteroidReserve());

            public static Arbitrary<Asteroid> AsteroidArb() => Arb.From(GenAsteroid());

            public static Arbitrary<PricingPlan> PricingPlanArb() => Arb.From(GenPricingPlan());

            public static Arbitrary<Commodity> CommodityArb() => Arb.From(GenCommodity());
        }
    }
}
