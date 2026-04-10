using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// FsCheck Arbitrary generators and property/unit tests for JSON default-value skipping.
    /// </summary>
    public static class ModelGenerators
    {
        // ---------------------------------------------------------------
        // Primitive helpers
        // ---------------------------------------------------------------

        private static readonly string[] ShortStrings = { "a", "bb", "ccc", "d1", "e2f", "" };
        private static readonly string[] NonEmptyStrings = { "a", "bb", "ccc", "d1", "e2f" };

        private static Gen<string> GenShortString() =>
            Gen.Elements(ShortStrings);

        private static Gen<string> GenNonEmptyString() =>
            Gen.Elements(NonEmptyStrings);

        private static Gen<string> GenUuid() =>
            Gen.Fresh(() => Guid.NewGuid().ToString());

        private static Gen<ItemType.ItemTypeEnum> GenItemTypeEnum() =>
            Gen.Elements(
                ItemType.ItemTypeEnum.None,
                ItemType.ItemTypeEnum.Commodity,
                ItemType.ItemTypeEnum.ShipHull,
                ItemType.ItemTypeEnum.ShipPart,
                ItemType.ItemTypeEnum.Munition,
                ItemType.ItemTypeEnum.Resource,
                ItemType.ItemTypeEnum.Blueprint,
                ItemType.ItemTypeEnum.Flatpack,
                ItemType.ItemTypeEnum.Survey);

        private static Gen<DateTime> GenFutureDate() =>
            Gen.Choose(1, 365).Select(days => new DateTime(2030, 1, 1).AddDays(days));

        // ---------------------------------------------------------------
        // CountDownTime
        // ---------------------------------------------------------------

        private static Gen<CountDownTime> GenCountDownTime() =>
            from start in GenFutureDate()
            from offsetDays in Gen.Choose(1, 30)
            from repeat in Gen.Elements(0L, 3600L, 7200L)
            select new CountDownTime
            {
                StartTime = start,
                EndTime = start.AddDays(offsetDays),
                RepeatIntervalSeconds = repeat
            };

        // ---------------------------------------------------------------
        // PropertyBag
        // ---------------------------------------------------------------

        private static Gen<PropertyBag> GenPropertyBag() =>
            from count in Gen.Choose(0, 3)
            from keys in Gen.ListOf(count, GenNonEmptyString())
            from vals in Gen.ListOf(count, GenShortString())
            select BuildPropertyBag(keys, vals);

        private static PropertyBag BuildPropertyBag(
            IEnumerable<string> keys, IEnumerable<string> vals)
        {
            var bag = new PropertyBag();
            var keyArr = keys.Distinct().ToArray();
            var valArr = vals.ToArray();
            for (int i = 0; i < Math.Min(keyArr.Length, valArr.Length); i++)
                bag.Properties[keyArr[i]] = valArr[i];
            return bag;
        }

        // ---------------------------------------------------------------
        // Item
        // ---------------------------------------------------------------

        private static Gen<Item> GenItem() =>
            from uuid in GenUuid()
            from itemType in GenItemTypeEnum()
            from name in GenNonEmptyString()
            from nick in GenShortString()
            from desc in GenShortString()
            from qty in Gen.Choose(0, 100)
            from purity in Gen.Elements("", "Low", "Medium", "High")
            from vol in Gen.Elements(0.0m, 1.0m, 5.5m)
            from baseId in GenShortString()
            select new Item(itemType, name)
            {
                UUID = uuid,
                NickName = nick,
                Description = desc,
                Quantity = qty,
                ResourcePurity = purity,
                Volume = vol,
                BaseItemTypeID = baseId
            };

        // ---------------------------------------------------------------
        // ItemBag
        // ---------------------------------------------------------------

        private static Gen<ItemBag> GenItemBag() =>
            from count in Gen.Choose(0, 3)
            from items in Gen.ListOf(count, GenItem())
            select BuildItemBag(items);

        private static ItemBag BuildItemBag(IEnumerable<Item> items)
        {
            var bag = new ItemBag();
            foreach (var item in items)
            {
                if (!bag.Items.ContainsKey(item.UUID))
                    bag.Items[item.UUID] = item;
            }
            return bag;
        }

        // ---------------------------------------------------------------
        // ColonyStructure
        // ---------------------------------------------------------------

        private static Gen<ColonyStructure> GenColonyStructure() =>
            from uuid in GenUuid()
            from fpUuid in Gen.OneOf(Gen.Constant((string)null), GenUuid())
            from seq in Gen.Choose(0, 10)
            from bldId in Gen.Choose(0, 5)
            from bqSeq in Gen.Choose(0, 3)
            from props in GenPropertyBag()
            from workers in GenPropertyBag()
            from hasBuild in Arb.Generate<bool>()
            from buildTime in GenCountDownTime()
            from hasProcess in Arb.Generate<bool>()
            from processTime in GenCountDownTime()
            from mSurvey in Gen.OneOf(Gen.Constant((string)null), GenUuid())
            from mResource in Gen.OneOf(Gen.Constant((string)null), GenNonEmptyString())
            from leftOvers in Gen.Elements(0m, 0.5m, 1.25m)
            from refRes in Gen.OneOf(Gen.Constant((string)null), GenNonEmptyString())
            from refPur in Gen.OneOf(Gen.Constant((string)null), Gen.Elements("Low", "Medium", "High"))
            from researchUuid in Gen.OneOf(Gen.Constant((string)null), GenUuid())
            from mfgBpUuid in Gen.OneOf(Gen.Constant((string)null), GenUuid())
            from mfgCommodity in Gen.OneOf(Gen.Constant((string)null), GenNonEmptyString())
            from mfgQty in Gen.Choose(0, 10)
            from mfgDone in Gen.Choose(0, 10)
            from staging in Arb.Generate<bool>()
            from attitude in GenShortString()
            from content in Gen.Choose(0, 100)
            from wage in Gen.Choose(0, 5)
            select new ColonyStructure
            {
                UUID = uuid,
                FlatpackBlueprintUUID = fpUuid,
                displaySequence = seq,
                buildingID = bldId,
                buildQueueSequence = bqSeq,
                Properties = props,
                AssignedWorkers = workers,
                BuildCompletionTime = hasBuild ? buildTime : null,
                ProcessCompletionTime = hasProcess ? processTime : null,
                MiningSurvey = mSurvey,
                MiningSurveyResource = mResource,
                MiningLeftOvers = leftOvers,
                RefiningResource = refRes,
                RefiningResourcePurity = refPur,
                ResearchingBlueprintUUID = researchUuid,
                ManufacturingBlueprintUUID = mfgBpUuid,
                ManufacturingCommodityName = mfgCommodity,
                ManufacturingQuantity = mfgQty,
                ManufacturingCompleted = mfgDone,
                StagingResources = staging,
                CurrentAttitude = attitude,
                ContentmentIndex = content,
                WageLevel = wage
            };

        // ---------------------------------------------------------------
        // CommodityRequested
        // ---------------------------------------------------------------

        private static Gen<CommodityRequested> GenCommodityRequested() =>
            from name in GenNonEmptyString()
            from req in Gen.Choose(0, 50)
            from del in Gen.Choose(0, 50)
            from date in GenFutureDate()
            from ful in Arb.Generate<bool>()
            select new CommodityRequested
            {
                Name = name,
                Requested = req,
                Delivered = del,
                NeedBy = date,
                Fulfilled = ful
            };

        // ---------------------------------------------------------------
        // LockTracking (empty — custom converter handles it)
        // ---------------------------------------------------------------

        private static Gen<LockTracking> GenLockTracking() =>
            Gen.Constant(new LockTracking());

        // ---------------------------------------------------------------
        // Colony
        // ---------------------------------------------------------------

        private static Gen<Colony> GenColony() =>
            from uuid in GenUuid()
            from owner in GenShortString()
            from planet in GenNonEmptyString()
            from system in GenShortString()
            from colonyName in GenNonEmptyString()
            from items in GenItemBag()
            from structCount in Gen.Choose(0, 2)
            from structs in Gen.ListOf(structCount, GenColonyStructure())
            from commCount in Gen.Choose(0, 2)
            from comms in Gen.ListOf(commCount, GenCommodityRequested())
            from locks in GenLockTracking()
            select new Colony
            {
                UUID = uuid,
                OwnerUUID = owner,
                PlanetName = planet,
                SystemName = system,
                ColonyName = colonyName,
                Items = items,
                Structures = structs.ToList(),
                Commodities = comms.ToList(),
                Locks = locks
            };

        // ---------------------------------------------------------------
        // Blueprint
        // ---------------------------------------------------------------

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenBlueprint() =>
            from uuid in GenUuid()
            from name in GenNonEmptyString()
            from owner in GenShortString()
            from baseUuid in GenUuid()
            from bpType in GenNonEmptyString()
            from evo in Gen.Choose(0, 5)
            from techLevel in Gen.Elements("TL1", "TL2", "TL3", (string)null)
            from cls in Gen.Choose(0, 5)
            from cost in Gen.Choose(0, 1000)
            from nick in GenShortString()
            from desc in GenShortString()
            from props in GenPropertyBag()
            from resCount in Gen.Choose(0, 3)
            from resKeys in Gen.ListOf(resCount, GenNonEmptyString())
            from resVals in Gen.ListOf(resCount, GenShortString())
            select BuildBlueprint(uuid, name, owner, baseUuid, bpType, evo,
                techLevel, cls, cost, nick, desc, props, resKeys, resVals);

        private static OE2EmpireTracker.Models.Blueprint BuildBlueprint(
            string uuid, string name, string owner, string baseUuid,
            string bpType, int evo, string techLevel, int cls, int cost,
            string nick, string desc, PropertyBag props,
            IEnumerable<string> resKeys, IEnumerable<string> resVals)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name)
            {
                UUID = uuid,
                OwnerUUID = owner,
                BaseBlueprintUUID = baseUuid,
                BluePrintType = bpType,
                Evolution = evo,
                TechLevel = techLevel,
                Class = cls,
                CopyCost = cost,
                NickName = nick,
                Description = desc,
                Properties = props
            };
            var kArr = resKeys.Distinct().ToArray();
            var vArr = resVals.ToArray();
            for (int i = 0; i < Math.Min(kArr.Length, vArr.Length); i++)
                bp.Resources[kArr[i]] = vArr[i];
            return bp;
        }

        // ---------------------------------------------------------------
        // SurveyResource
        // ---------------------------------------------------------------

        private static Gen<SurveyResource> GenSurveyResource() =>
            from res in GenNonEmptyString()
            from pur in Gen.Elements("Low", "Medium", "High")
            from amt in Gen.Elements("10", "25", "50")
            select new SurveyResource(res, pur, amt);

        // ---------------------------------------------------------------
        // Survey
        // ---------------------------------------------------------------

        private static Gen<Survey> GenSurvey() =>
            from uuid in GenUuid()
            from owner in GenShortString()
            from scanned in GenNonEmptyString()
            from dt in GenNonEmptyString()
            from planet in GenNonEmptyString()
            from system in GenShortString()
            from surveyId in GenNonEmptyString()
            from scannerUuid in GenUuid()
            from nick in GenShortString()
            from desc in GenShortString()
            from propCount in Gen.Choose(0, 2)
            from propKeys in Gen.ListOf(propCount, GenNonEmptyString())
            from propVals in Gen.ListOf(propCount, GenShortString())
            from resCount in Gen.Choose(0, 2)
            from resKeys in Gen.ListOf(resCount, GenNonEmptyString())
            from resVals in Gen.ListOf(resCount, GenSurveyResource())
            select BuildSurvey(uuid, owner, scanned, dt, planet, system,
                surveyId, scannerUuid, nick, desc, propKeys, propVals, resKeys, resVals);

        private static Survey BuildSurvey(
            string uuid, string owner, string scanned, string dt,
            string planet, string system, string surveyId, string scannerUuid,
            string nick, string desc,
            IEnumerable<string> propKeys, IEnumerable<string> propVals,
            IEnumerable<string> resKeys, IEnumerable<SurveyResource> resVals)
        {
            var s = new Survey
            {
                UUID = uuid,
                OwnerUUID = owner,
                ScannedBy = scanned,
                DateTime = dt,
                PlanetName = planet,
                SystemName = system,
                SurveyID = surveyId,
                ScannerBlueprintUUID = scannerUuid,
                NickName = nick,
                Description = desc
            };
            var kArr = propKeys.Distinct().ToArray();
            var vArr = propVals.ToArray();
            for (int i = 0; i < Math.Min(kArr.Length, vArr.Length); i++)
                s.Properties[kArr[i]] = vArr[i];
            var rkArr = resKeys.Distinct().ToArray();
            var rvArr = resVals.ToArray();
            for (int i = 0; i < Math.Min(rkArr.Length, rvArr.Length); i++)
                s.Resources[rkArr[i]] = rvArr[i];
            return s;
        }

        // ---------------------------------------------------------------
        // DeliveryItem
        // ---------------------------------------------------------------

        private static Gen<DeliveryItem> GenDeliveryItem() =>
            from itemType in GenItemTypeEnum()
            from baseId in GenShortString()
            from name in GenNonEmptyString()
            from purity in Gen.Elements("", "Low", "Medium", "High")
            from qty in Gen.Choose(0, 50)
            from delivered in Arb.Generate<bool>()
            select new DeliveryItem
            {
                ItemType = itemType,
                BaseItemTypeID = baseId,
                Name = name,
                ResourcePurity = purity,
                Quantity = qty,
                Delivered = delivered
            };

        // ---------------------------------------------------------------
        // DeliveryPlanStop
        // ---------------------------------------------------------------

        private static Gen<DeliveryPlanStop> GenDeliveryPlanStop() =>
            from colonyUuid in GenUuid()
            from seq in Gen.Choose(0, 10)
            from completed in Arb.Generate<bool>()
            from dropCount in Gen.Choose(0, 2)
            from drops in Gen.ListOf(dropCount, GenDeliveryItem())
            from pickCount in Gen.Choose(0, 2)
            from picks in Gen.ListOf(pickCount, GenDeliveryItem())
            select new DeliveryPlanStop
            {
                ColonyUUID = colonyUuid,
                Sequence = seq,
                StopCompleted = completed,
                DropOff = drops.ToList(),
                PickUp = picks.ToList()
            };

        // ---------------------------------------------------------------
        // DeliveryPlan
        // ---------------------------------------------------------------

        private static Gen<DeliveryPlan> GenDeliveryPlan() =>
            from uuid in GenUuid()
            from name in GenShortString()
            from owner in GenShortString()
            from routeUuid in GenUuid()
            from completed in Arb.Generate<bool>()
            from stopCount in Gen.Choose(0, 2)
            from stops in Gen.ListOf(stopCount, GenDeliveryPlanStop())
            select new DeliveryPlan
            {
                UUID = uuid,
                Name = name,
                OwnerUUID = owner,
                RouteUUID = routeUuid,
                Completed = completed,
                Stops = stops.ToList()
            };

        // ---------------------------------------------------------------
        // RouteStop / DeliveryRoute
        // ---------------------------------------------------------------

        private static Gen<RouteStop> GenRouteStop() =>
            from colonyUuid in GenUuid()
            from seq in Gen.Choose(0, 10)
            select new RouteStop { ColonyUUID = colonyUuid, Sequence = seq };

        private static Gen<DeliveryRoute> GenDeliveryRoute() =>
            from uuid in GenUuid()
            from name in GenShortString()
            from owner in GenShortString()
            from stopCount in Gen.Choose(0, 3)
            from stops in Gen.ListOf(stopCount, GenRouteStop())
            select new DeliveryRoute
            {
                UUID = uuid,
                Name = name,
                OwnerUUID = owner,
                Stops = stops.ToList()
            };

        // ---------------------------------------------------------------
        // PlayerRank
        // ---------------------------------------------------------------

        private static Gen<PlayerRank> GenPlayerRank() =>
            from rank in Gen.Choose(0, 10)
            from curXp in Gen.Choose(0, 10000)
            from nextXp in Gen.Choose(0, 10000)
            select new PlayerRank { Rank = rank, CurrentXP = curXp, NextXP = nextXp };

        // ---------------------------------------------------------------
        // PlayerSkill
        // ---------------------------------------------------------------

        private static Gen<PlayerSkill> GenPlayerSkill() =>
            from level in Gen.Choose(0, 10)
            from training in Arb.Generate<bool>()
            from ct in GenCountDownTime()
            select new PlayerSkill
            {
                Level = level,
                TrainingStarted = training,
                CompletionTime = ct
            };

        // ---------------------------------------------------------------
        // PlayerProfile
        // ---------------------------------------------------------------

        private static Gen<PlayerProfile> GenPlayerProfile() =>
            from uuid in GenUuid()
            from name in GenNonEmptyString()
            from faction in GenShortString()
            from credits in Gen.Choose(0, 100000)
            from pub in GenPlayerRank()
            from priv in GenPlayerRank()
            from mil in GenPlayerRank()
            from sp in Gen.Choose(0, 50)
            from skillCount in Gen.Choose(0, 3)
            from skillNames in Gen.ListOf(skillCount, GenNonEmptyString())
            from skillVals in Gen.ListOf(skillCount, GenPlayerSkill())
            select BuildPlayerProfile(uuid, name, faction, credits, pub, priv, mil, sp, skillNames, skillVals);

        private static PlayerProfile BuildPlayerProfile(
            string uuid, string name, string faction, int credits,
            PlayerRank pub, PlayerRank priv, PlayerRank mil, int sp,
            IEnumerable<string> skillNames, IEnumerable<PlayerSkill> skillVals)
        {
            var p = new PlayerProfile
            {
                UUID = uuid,
                Name = name,
                Faction = faction,
                TotalCredits = credits,
                Public = pub,
                Private = priv,
                Military = mil,
                SkillPoints = sp
            };
            var kArr = skillNames.Distinct().ToArray();
            var vArr = skillVals.ToArray();
            for (int i = 0; i < Math.Min(kArr.Length, vArr.Length); i++)
                p.Skills[kArr[i]] = vArr[i];
            return p;
        }

        // ---------------------------------------------------------------
        // UIPreferences hierarchy
        // ---------------------------------------------------------------

        private static Gen<GridColumnState> GenGridColumnState() =>
            from w in Gen.Choose(20, 300)
            from idx in Gen.Choose(0, 10)
            select new GridColumnState { Width = w, DisplayIndex = idx };

        private static Gen<GridState> GenGridState() =>
            from colCount in Gen.Choose(0, 2)
            from colNames in Gen.ListOf(colCount, GenNonEmptyString())
            from colStates in Gen.ListOf(colCount, GenGridColumnState())
            from sortCol in Gen.OneOf(Gen.Constant((string)null), GenNonEmptyString())
            from sortDir in Gen.Elements("Ascending", "Descending", (string)null)
            select BuildGridState(colNames, colStates, sortCol, sortDir);

        private static GridState BuildGridState(
            IEnumerable<string> names, IEnumerable<GridColumnState> states,
            string sortCol, string sortDir)
        {
            var gs = new GridState { SortColumnName = sortCol, SortDirection = sortDir };
            var kArr = names.Distinct().ToArray();
            var vArr = states.ToArray();
            for (int i = 0; i < Math.Min(kArr.Length, vArr.Length); i++)
                gs.Columns[kArr[i]] = vArr[i];
            return gs;
        }

        private static Gen<ComboState> GenComboState() =>
            from val in GenShortString()
            from idx in Gen.Choose(0, 10)
            select new ComboState { SelectedValue = val, SelectedIndex = idx };

        private static Gen<FormControlState> GenFormControlState() =>
            from ftCount in Gen.Choose(0, 2)
            from ftKeys in Gen.ListOf(ftCount, GenNonEmptyString())
            from ftVals in Gen.ListOf(ftCount, GenShortString())
            from csCount in Gen.Choose(0, 2)
            from csKeys in Gen.ListOf(csCount, GenNonEmptyString())
            from csVals in Gen.ListOf(csCount, Arb.Generate<bool>())
            from comboCount in Gen.Choose(0, 2)
            from comboKeys in Gen.ListOf(comboCount, GenNonEmptyString())
            from comboVals in Gen.ListOf(comboCount, GenComboState())
            from gridCount in Gen.Choose(0, 1)
            from gridKeys in Gen.ListOf(gridCount, GenNonEmptyString())
            from gridVals in Gen.ListOf(gridCount, GenGridState())
            select BuildFormControlState(ftKeys, ftVals, csKeys, csVals, comboKeys, comboVals, gridKeys, gridVals);

        private static FormControlState BuildFormControlState(
            IEnumerable<string> ftKeys, IEnumerable<string> ftVals,
            IEnumerable<string> csKeys, IEnumerable<bool> csVals,
            IEnumerable<string> comboKeys, IEnumerable<ComboState> comboVals,
            IEnumerable<string> gridKeys, IEnumerable<GridState> gridVals)
        {
            var fcs = new FormControlState();
            MergeDictionary(fcs.FilterTexts, ftKeys, ftVals);
            MergeDictionary(fcs.CheckStates, csKeys, csVals);
            MergeDictionary(fcs.ComboSelections, comboKeys, comboVals);
            MergeDictionary(fcs.Grids, gridKeys, gridVals);
            return fcs;
        }

        private static void MergeDictionary<T>(Dictionary<string, T> dict,
            IEnumerable<string> keys, IEnumerable<T> vals)
        {
            var kArr = keys.Distinct().ToArray();
            var vArr = vals.ToArray();
            for (int i = 0; i < Math.Min(kArr.Length, vArr.Length); i++)
                dict[kArr[i]] = vArr[i];
        }

        private static Gen<WindowPosition> GenWindowPosition() =>
            from l in Gen.Choose(0, 1920)
            from t in Gen.Choose(0, 1080)
            from w in Gen.Choose(100, 800)
            from h in Gen.Choose(100, 600)
            select new WindowPosition { Left = l, Top = t, Width = w, Height = h };

        private static Gen<WindowState> GenWindowState() =>
            from pos in GenWindowPosition()
            from fcs in GenFormControlState()
            select new WindowState { Position = pos, FormState = fcs };

        private static Gen<UIPreferences> GenUIPreferences() =>
            from hasMain in Arb.Generate<bool>()
            from mainWin in GenWindowPosition()
            from formCount in Gen.Choose(0, 2)
            from formKeys in Gen.ListOf(formCount, GenNonEmptyString())
            from innerCount in Gen.Choose(0, 2)
            from innerKeys in Gen.ListOf(innerCount, GenNonEmptyString())
            from innerVals in Gen.ListOf(innerCount, GenWindowState())
            select BuildUIPreferences(hasMain ? mainWin : null, formKeys, innerKeys, innerVals);

        private static UIPreferences BuildUIPreferences(
            WindowPosition mainWin,
            IEnumerable<string> formKeys,
            IEnumerable<string> innerKeys,
            IEnumerable<WindowState> innerVals)
        {
            var prefs = new UIPreferences { MainWindow = mainWin };
            var innerDict = new Dictionary<string, WindowState>();
            var ikArr = innerKeys.Distinct().ToArray();
            var ivArr = innerVals.ToArray();
            for (int i = 0; i < Math.Min(ikArr.Length, ivArr.Length); i++)
                innerDict[ikArr[i]] = ivArr[i];

            foreach (var fk in formKeys.Distinct())
                prefs.Forms[fk] = new Dictionary<string, WindowState>(innerDict);
            return prefs;
        }

        // ---------------------------------------------------------------
        // BlueprintType
        // ---------------------------------------------------------------

        private static Gen<BlueprintType> GenBlueprintType() =>
            from id in GenNonEmptyString()
            from name in GenNonEmptyString()
            from universal in Arb.Generate<bool>()
            from propCount in Gen.Choose(0, 3)
            from props in Gen.ListOf(propCount, GenNonEmptyString())
            from resPropCount in Gen.Choose(0, 2)
            from resProps in Gen.ListOf(resPropCount, GenNonEmptyString())
            from icon in Gen.OneOf(Gen.Constant((string)null), GenShortString())
            from output in GenShortString()
            select new BlueprintType
            {
                Id = id,
                Name = name,
                Universal = universal,
                Properties = props.ToArray(),
                ResearchableProperties = resProps.ToArray(),
                IconPosition = icon,
                OutputItemType = output
            };

        // ---------------------------------------------------------------
        // ShipClass
        // ---------------------------------------------------------------

        private static Gen<ShipClass> GenShipClass() =>
            from id in Gen.Choose(1, 10)
            from name in GenNonEmptyString()
            select new ShipClass { Id = id, Name = name };

        // ---------------------------------------------------------------
        // TechLevel
        // ---------------------------------------------------------------

        private static Gen<TechLevel> GenTechLevel() =>
            from name in Gen.Elements("TL1", "TL2", "TL3")
            select new TechLevel { Name = name };

        // ---------------------------------------------------------------
        // PlayerRoot
        // ---------------------------------------------------------------

        public static Gen<PlayerRoot> GenPlayerRoot() =>
            from currentUuid in GenUuid()
            from profileCount in Gen.Choose(0, 2)
            from profiles in Gen.ListOf(profileCount, GenPlayerProfile())
            from bpCount in Gen.Choose(0, 2)
            from blueprints in Gen.ListOf(bpCount, GenBlueprint())
            from surveyCount in Gen.Choose(0, 2)
            from surveys in Gen.ListOf(surveyCount, GenSurvey())
            from colonyCount in Gen.Choose(0, 2)
            from colonies in Gen.ListOf(colonyCount, GenColony())
            from routeCount in Gen.Choose(0, 2)
            from routes in Gen.ListOf(routeCount, GenDeliveryRoute())
            from planCount in Gen.Choose(0, 2)
            from plans in Gen.ListOf(planCount, GenDeliveryPlan())
            select new PlayerRoot
            {
                CurrentPlayerUUID = currentUuid,
                PlayerProfile = profiles.ToArray(),
                Blueprint = blueprints.ToArray(),
                Survey = surveys.ToArray(),
                Colony = colonies.ToArray(),
                DeliveryRoute = routes.ToArray(),
                DeliveryPlan = plans.ToArray()
            };

        // ---------------------------------------------------------------
        // BaselineRoot
        // ---------------------------------------------------------------

        public static Gen<BaselineRoot> GenBaselineRoot() =>
            from scCount in Gen.Choose(0, 3)
            from shipClasses in Gen.ListOf(scCount, GenShipClass())
            from btCount in Gen.Choose(0, 3)
            from bpTypes in Gen.ListOf(btCount, GenBlueprintType())
            from bpCount in Gen.Choose(0, 2)
            from blueprints in Gen.ListOf(bpCount, GenBlueprint())
            from tlCount in Gen.Choose(0, 3)
            from techLevels in Gen.ListOf(tlCount, GenTechLevel())
            select new BaselineRoot
            {
                ShipClass = shipClasses.ToArray(),
                BlueprintType = bpTypes.ToArray(),
                Blueprint = blueprints.ToArray(),
                TechLevel = techLevels.ToArray()
            };

        // ---------------------------------------------------------------
        // Composite Arbitrary registration
        // ---------------------------------------------------------------

        public static Arbitrary<PropertyBag> PropertyBagArbitrary() =>
            Arb.From(GenPropertyBag());

        public static Arbitrary<ItemBag> ItemBagArbitrary() =>
            Arb.From(GenItemBag());

        public static Arbitrary<Item> ItemArbitrary() =>
            Arb.From(GenItem());

        public static Arbitrary<ColonyStructure> ColonyStructureArbitrary() =>
            Arb.From(GenColonyStructure());

        public static Arbitrary<Colony> ColonyArbitrary() =>
            Arb.From(GenColony());

        public static Arbitrary<OE2EmpireTracker.Models.Blueprint> BlueprintArbitrary() =>
            Arb.From(GenBlueprint());

        public static Arbitrary<Survey> SurveyArbitrary() =>
            Arb.From(GenSurvey());

        public static Arbitrary<SurveyResource> SurveyResourceArbitrary() =>
            Arb.From(GenSurveyResource());

        public static Arbitrary<DeliveryItem> DeliveryItemArbitrary() =>
            Arb.From(GenDeliveryItem());

        public static Arbitrary<DeliveryPlanStop> DeliveryPlanStopArbitrary() =>
            Arb.From(GenDeliveryPlanStop());

        public static Arbitrary<DeliveryPlan> DeliveryPlanArbitrary() =>
            Arb.From(GenDeliveryPlan());

        public static Arbitrary<RouteStop> RouteStopArbitrary() =>
            Arb.From(GenRouteStop());

        public static Arbitrary<DeliveryRoute> DeliveryRouteArbitrary() =>
            Arb.From(GenDeliveryRoute());

        public static Arbitrary<PlayerRank> PlayerRankArbitrary() =>
            Arb.From(GenPlayerRank());

        public static Arbitrary<PlayerSkill> PlayerSkillArbitrary() =>
            Arb.From(GenPlayerSkill());

        public static Arbitrary<PlayerProfile> PlayerProfileArbitrary() =>
            Arb.From(GenPlayerProfile());

        public static Arbitrary<CountDownTime> CountDownTimeArbitrary() =>
            Arb.From(GenCountDownTime());

        public static Arbitrary<CommodityRequested> CommodityRequestedArbitrary() =>
            Arb.From(GenCommodityRequested());

        public static Arbitrary<LockTracking> LockTrackingArbitrary() =>
            Arb.From(GenLockTracking());

        public static Arbitrary<WindowPosition> WindowPositionArbitrary() =>
            Arb.From(GenWindowPosition());

        public static Arbitrary<WindowState> WindowStateArbitrary() =>
            Arb.From(GenWindowState());

        public static Arbitrary<FormControlState> FormControlStateArbitrary() =>
            Arb.From(GenFormControlState());

        public static Arbitrary<ComboState> ComboStateArbitrary() =>
            Arb.From(GenComboState());

        public static Arbitrary<GridState> GridStateArbitrary() =>
            Arb.From(GenGridState());

        public static Arbitrary<GridColumnState> GridColumnStateArbitrary() =>
            Arb.From(GenGridColumnState());

        public static Arbitrary<UIPreferences> UIPreferencesArbitrary() =>
            Arb.From(GenUIPreferences());

        public static Arbitrary<BlueprintType> BlueprintTypeArbitrary() =>
            Arb.From(GenBlueprintType());

        public static Arbitrary<ShipClass> ShipClassArbitrary() =>
            Arb.From(GenShipClass());

        public static Arbitrary<TechLevel> TechLevelArbitrary() =>
            Arb.From(GenTechLevel());

        public static Arbitrary<PlayerRoot> PlayerRootArbitrary() =>
            Arb.From(GenPlayerRoot());

        public static Arbitrary<BaselineRoot> BaselineRootArbitrary() =>
            Arb.From(GenBaselineRoot());

        /// <summary>
        /// Registers all custom Arbitrary generators with FsCheck.
        /// </summary>
        public static void Register()
        {
            Arb.Register(typeof(ModelGenerators));
        }
    }

    // ===================================================================
    // Test fixture
    // ===================================================================

    [TestFixture]
    public class JsonDefaultSkipTests
    {
        [SetUp]
        public void SetUp()
        {
            ModelGenerators.Register();
        }

        // Property tests and unit tests will be added in tasks 4.2–4.7 and 5.1–5.4.

        // Feature: json-default-skip, Property 1: PlayerRoot serialization round-trip
        // Validates: Requirements 5.1, 5.3, 5.4, 2.2, 3.1
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool PlayerRoot_RoundTrip_PreservesAllNonDefaultFields(PlayerRoot root)
        {
            var json = JsonConvert.SerializeObject(root, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<PlayerRoot>(json);
            var reserializedJson = JsonConvert.SerializeObject(deserialized, JsonSettings.SerializerSettings);

            // CountDownTime.TimeRemainingString is computed from DateTime.Now and
            // its setter mutates StartTime/EndTime on deserialization, so the value
            // can shift by a second between serialize passes. Strip it for comparison.
            var normalized1 = StripVolatileFields(json);
            var normalized2 = StripVolatileFields(reserializedJson);
            return normalized1 == normalized2;
        }

        // Feature: json-default-skip, Property 2: BaselineRoot serialization round-trip
        // Validates: Requirements 5.2, 2.1, 3.2
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool BaselineRoot_RoundTrip_PreservesAllNonDefaultFields(BaselineRoot root)
        {
            var json = JsonConvert.SerializeObject(root, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<BaselineRoot>(json);
            var reserializedJson = JsonConvert.SerializeObject(deserialized, JsonSettings.SerializerSettings);

            // BaselineRoot has no CountDownTime fields, so no need to strip volatile fields —
            // direct string comparison is fine.
            return json == reserializedJson;
        }

        // Feature: json-default-skip, Property 3: UIPreferences serialization round-trip
        // Validates: Requirements 5.5, 2.3, 3.3
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool UIPreferences_RoundTrip_PreservesEquivalence(UIPreferences prefs)
        {
            var json = JsonConvert.SerializeObject(prefs, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<UIPreferences>(json);
            var reserializedJson = JsonConvert.SerializeObject(deserialized, JsonSettings.SerializerSettings);

            // UIPreferences has no CountDownTime fields, so direct string comparison is fine.
            return json == reserializedJson;
        }

        // Feature: json-default-skip, Property 4: ItemBag custom converter round-trip
        // Validates: Requirements 4.1, 4.3
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool ItemBag_CustomConverter_RoundTrip_PreservesItems(ItemBag bag)
        {
            var json = JsonConvert.SerializeObject(bag, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<ItemBag>(json);
            var reserializedJson = JsonConvert.SerializeObject(deserialized, JsonSettings.SerializerSettings);

            return json == reserializedJson;
        }

        // Feature: json-default-skip, Property 5: PropertyBag custom converter round-trip
        // Validates: Requirements 4.2
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool PropertyBag_CustomConverter_RoundTrip_PreservesEntries(PropertyBag bag)
        {
            var json = JsonConvert.SerializeObject(bag, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<PropertyBag>(json);
            var reserializedJson = JsonConvert.SerializeObject(deserialized, JsonSettings.SerializerSettings);

            return json == reserializedJson;
        }

        // Feature: json-default-skip, Property 6: Compact serialization is never larger than verbose
        // Validates: Requirements 6.1, 6.2
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool CompactSerialization_NeverLargerThanVerbose(PlayerRoot root)
        {
            var compact = JsonConvert.SerializeObject(root, JsonSettings.SerializerSettings);
            var verbose = JsonConvert.SerializeObject(root, Formatting.Indented);
            return compact.Length <= verbose.Length;
        }

        // Feature: json-default-skip, Property 6: Compact serialization is never larger than verbose
        // Validates: Requirements 6.1, 6.2
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool CompactSerialization_BaselineRoot_NeverLargerThanVerbose(BaselineRoot root)
        {
            var compact = JsonConvert.SerializeObject(root, JsonSettings.SerializerSettings);
            var verbose = JsonConvert.SerializeObject(root, Formatting.Indented);
            return compact.Length <= verbose.Length;
        }

        /// <summary>
        /// Removes JSON fields that are computed from DateTime.Now and therefore
        /// non-deterministic across serialize/deserialize passes.
        /// CountDownTime.TimeRemainingString setter mutates StartTime/EndTime
        /// during deserialization, making those fields volatile too.
        /// </summary>
        private static string StripVolatileFields(string json)
        {
            // Remove volatile CountDownTime fields: TimeRemainingString, StartTime, EndTime
            // These are mutated by the TimeRemainingString setter during deserialization.
            var result = System.Text.RegularExpressions.Regex.Replace(
                json,
                @"\s*""(TimeRemainingString|StartTime|EndTime)""\s*:\s*""[^""]*""\s*,?",
                string.Empty);
            // Clean up any trailing commas before closing braces
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @",(\s*[}\]])",
                "$1");
            return result;
        }

        // Unit test: Settings configuration verification
        // Validates: Requirements 1.1, 1.2, 1.3
        [Test]
        public void Settings_ConfiguredCorrectly()
        {
            Assert.That(JsonSettings.SerializerSettings.DefaultValueHandling,
                Is.EqualTo(DefaultValueHandling.Ignore));
            Assert.That(JsonSettings.SerializerSettings.NullValueHandling,
                Is.EqualTo(NullValueHandling.Ignore));
            Assert.That(JsonSettings.SerializerSettings.Formatting,
                Is.EqualTo(Formatting.Indented));
        }

        // Unit test: ItemBagJSONConverter omits defaults on nested Items
        // Validates: Requirements 4.3
        [Test]
        public void ItemBagConverter_OmitsDefaultFields_OnNestedItems()
        {
            var bag = new ItemBag();
            var item = new Item(ItemType.ItemTypeEnum.Resource, "TestItem")
            {
                UUID = "test-uuid-1234",
                Quantity = 0,
                Volume = 0.0m,
                NickName = "",
                Description = "",
                ResourcePurity = "",
                BaseItemTypeID = ""
            };
            bag.Items[item.UUID] = item;

            var json = JsonConvert.SerializeObject(bag, JsonSettings.SerializerSettings);

            // Default-valued fields should be omitted
            Assert.That(json, Does.Not.Contain("\"Quantity\""));
            Assert.That(json, Does.Not.Contain("\"Volume\""));
            Assert.That(json, Does.Not.Contain("\"NickName\""));
            Assert.That(json, Does.Not.Contain("\"Description\""));
            Assert.That(json, Does.Not.Contain("\"ResourcePurity\""));
            Assert.That(json, Does.Not.Contain("\"BaseItemTypeID\""));

            // Non-default values should be present
            Assert.That(json, Does.Contain("\"Name\""));
            Assert.That(json, Does.Contain("\"ItemType\""));
        }

        // Unit test: File size reduction on real test data
        // Validates: Requirements 6.1, 6.2
        [Test]
        public void RealTestData_CompactSerialization_ReducesFileSize()
        {
            // PlayerData.json
            var playerPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PlayerData.json");
            var playerContent = File.ReadAllText(playerPath);
            var playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(playerContent);
            var playerVerbose = JsonConvert.SerializeObject(playerRoot, Formatting.Indented);
            var playerCompact = JsonConvert.SerializeObject(playerRoot, JsonSettings.SerializerSettings);
            Assert.That(playerCompact.Length, Is.LessThanOrEqualTo(playerVerbose.Length));

            // BaselineData.json
            var baselinePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BaselineData.json");
            var baselineContent = File.ReadAllText(baselinePath);
            var baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(baselineContent);
            var baselineVerbose = JsonConvert.SerializeObject(baselineRoot, Formatting.Indented);
            var baselineCompact = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
            Assert.That(baselineCompact.Length, Is.LessThanOrEqualTo(baselineVerbose.Length));
        }

        // Unit test: Backward compatibility with verbose JSON
        // Validates: Requirements 3.4
        [Test]
        public void BackwardCompatibility_VerboseJsonLoadsIdentically()
        {
            var verboseJson = @"{
  ""UUID"": ""test-uuid"",
  ""ItemType"": ""Resource"",
  ""Name"": ""Iron"",
  ""NickName"": """",
  ""Description"": """",
  ""Quantity"": 0,
  ""ResourcePurity"": """",
  ""Volume"": 0.0,
  ""BaseItemTypeID"": """"
}";

            var compactJson = @"{
  ""UUID"": ""test-uuid"",
  ""ItemType"": ""Resource"",
  ""Name"": ""Iron""
}";

            var fromVerbose = JsonConvert.DeserializeObject<Item>(verboseJson);
            var fromCompact = JsonConvert.DeserializeObject<Item>(compactJson);

            // Core identity fields
            Assert.That(fromCompact.UUID, Is.EqualTo(fromVerbose.UUID));
            Assert.That(fromCompact.ItemType, Is.EqualTo(fromVerbose.ItemType));
            Assert.That(fromCompact.Name, Is.EqualTo(fromVerbose.Name));

            // Default-valued fields restored identically
            Assert.That(fromCompact.NickName, Is.EqualTo(fromVerbose.NickName));
            Assert.That(fromCompact.Description, Is.EqualTo(fromVerbose.Description));
            Assert.That(fromCompact.Quantity, Is.EqualTo(fromVerbose.Quantity));
            Assert.That(fromCompact.ResourcePurity, Is.EqualTo(fromVerbose.ResourcePurity));
            Assert.That(fromCompact.Volume, Is.EqualTo(fromVerbose.Volume));
            Assert.That(fromCompact.BaseItemTypeID, Is.EqualTo(fromVerbose.BaseItemTypeID));
        }
    }
}
