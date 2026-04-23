using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ResourcesOnlyImportPropertyTests
    {
        /// <summary>
        /// Feature: blueprint-form-fixes, Property 1: IsResourcesOnlyImport classification
        ///
        /// For any Blueprint object with arbitrary Resources (0 or more entries),
        /// BluePrintType (null, empty, or non-empty), Class (0 or positive),
        /// and TechLevel (null, empty, or non-empty), IsResourcesOnlyImport shall
        /// return true if and only if Resources.Count > 0 AND BluePrintType is null
        /// or empty AND Class == 0 AND TechLevel is null or empty.
        ///
        /// **Validates: Requirements 1.1, 1.2, 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IsResourcesOnlyImportClassification()
        {
            var gen = from resources in ResourcesDictGen()
                      from bpType in NullableStringGen()
                      from cls in ZeroOrPositiveGen()
                      from techLevel in NullableStringGen()
                      select new
                      {
                          Resources = resources,
                          BluePrintType = bpType,
                          Class = cls,
                          TechLevel = techLevel
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var bp = new BpModel();
                bp.Resources = data.Resources;
                bp.BluePrintType = data.BluePrintType;
                bp.Class = data.Class;
                bp.TechLevel = data.TechLevel;

                bool result = MarketBlueprintImporter.IsResourcesOnlyImport(bp);

                bool hasResources = data.Resources != null && data.Resources.Count > 0;
                bool missingType = string.IsNullOrEmpty(data.BluePrintType);
                bool missingClass = data.Class == 0;
                bool missingTechLevel = string.IsNullOrEmpty(data.TechLevel);
                bool expected = hasResources && missingType && missingClass && missingTechLevel;

                return (result == expected)
                    .Label($"Expected {expected} but got {result}. " +
                           $"Resources.Count={data.Resources?.Count ?? 0}, " +
                           $"BluePrintType='{data.BluePrintType}', " +
                           $"Class={data.Class}, TechLevel='{data.TechLevel}'");
            });
        }

        /// <summary>
        /// Feature: blueprint-form-fixes, Property 2: MergeResourcesOnly replaces resources and preserves all other fields
        ///
        /// For any target Blueprint with a non-empty UUID, arbitrary scalar fields,
        /// arbitrary Properties (including protected keys), and for any incoming Blueprint
        /// with arbitrary Resources and Properties, after calling MergeResourcesOnly:
        /// - target.Resources equals incoming.Resources
        /// - All scalar fields on target are unchanged
        /// - Protected property keys retain their pre-call values if they existed before
        /// - Non-protected property keys from incoming are present in target.Properties
        ///
        /// **Validates: Requirements 2.1, 2.2, 3.1, 3.2, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeResourcesOnlyReplacesResourcesAndPreservesFields()
        {
            var gen = from uuid in NonEmptyAlphaStringGen()
                      from ownerUuid in NonEmptyAlphaStringGen()
                      from name in NonEmptyAlphaStringGen()
                      from nickName in NonEmptyAlphaStringGen()
                      from description in NonEmptyAlphaStringGen()
                      from baseBpUuid in NonEmptyAlphaStringGen()
                      from bpType in NonEmptyAlphaStringGen()
                      from cls in Gen.Choose(1, 10)
                      from techLevel in NonEmptyAlphaStringGen()
                      from evolution in Gen.Choose(0, 10)
                      from copyCost in Gen.Choose(0, 10000)
                      from mfgRunTime in NonEmptyAlphaStringGen()
                      from powerReq in NonEmptyAlphaStringGen()
                      from targetPropCount in Gen.Choose(0, 3)
                      from incomingResources in ResourcesDictGen()
                      from incomingPropCount in Gen.Choose(0, 3)
                      from incomingHasMfg in Arb.Default.Bool().Generator
                      from incomingHasPwr in Arb.Default.Bool().Generator
                      select new
                      {
                          UUID = uuid,
                          OwnerUUID = ownerUuid,
                          Name = name,
                          NickName = nickName,
                          Description = description,
                          BaseBlueprintUUID = baseBpUuid,
                          BluePrintType = bpType,
                          Class = cls,
                          TechLevel = techLevel,
                          Evolution = evolution,
                          CopyCost = copyCost,
                          MfgRunTime = mfgRunTime,
                          PowerReq = powerReq,
                          TargetPropCount = targetPropCount,
                          IncomingResources = incomingResources,
                          IncomingPropCount = incomingPropCount,
                          IncomingHasMfg = incomingHasMfg,
                          IncomingHasPwr = incomingHasPwr
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Build target blueprint with all scalar fields and protected properties
                var target = new BpModel();
                target.UUID = data.UUID;
                target.OwnerUUID = data.OwnerUUID;
                target.Name = data.Name;
                target.NickName = data.NickName;
                target.Description = data.Description;
                target.BaseBlueprintUUID = data.BaseBlueprintUUID;
                target.BluePrintType = data.BluePrintType;
                target.Class = data.Class;
                target.TechLevel = data.TechLevel;
                target.Evolution = data.Evolution;
                target.CopyCost = data.CopyCost;
                target.Properties = new PropertyBag();
                target.Properties.SetProperty("Manufacture Run Time", data.MfgRunTime);
                target.Properties.SetProperty("Power Required", data.PowerReq);
                for (int i = 0; i < data.TargetPropCount; i++)
                {
                    target.Properties.SetProperty("TargetProp" + i, "tval" + i);
                }

                // Build incoming blueprint with resources and properties
                var incoming = new BpModel();
                incoming.Resources = data.IncomingResources;
                incoming.Properties = new PropertyBag();
                for (int i = 0; i < data.IncomingPropCount; i++)
                {
                    incoming.Properties.SetProperty("IncomingProp" + i, "ival" + i);
                }

                if (data.IncomingHasMfg)
                {
                    incoming.Properties.SetProperty("Manufacture Run Time", "incoming_mfg");
                }

                if (data.IncomingHasPwr)
                {
                    incoming.Properties.SetProperty("Power Required", "incoming_pwr");
                }

                // Snapshot expected resource content
                var expectedResources = new Dictionary<string, string>(data.IncomingResources);

                // Act
                MarketBlueprintImporter.MergeResourcesOnly(target, incoming);

                // Assert: resources replaced
                var resMatch = (target.Resources.Count == expectedResources.Count
                    && expectedResources.All(kvp =>
                        target.Resources.ContainsKey(kvp.Key) && target.Resources[kvp.Key] == kvp.Value))
                    .Label("Resources do not match incoming");

                // Assert: scalar fields unchanged
                var uuidOk = (target.UUID == data.UUID).Label("UUID changed");
                var ownerOk = (target.OwnerUUID == data.OwnerUUID).Label("OwnerUUID changed");
                var nameOk = (target.Name == data.Name).Label("Name changed");
                var nickOk = (target.NickName == data.NickName).Label("NickName changed");
                var descOk = (target.Description == data.Description).Label("Description changed");
                var baseOk = (target.BaseBlueprintUUID == data.BaseBlueprintUUID).Label("BaseBlueprintUUID changed");
                var typeOk = (target.BluePrintType == data.BluePrintType).Label("BluePrintType changed");
                var classOk = (target.Class == data.Class).Label("Class changed");
                var tlOk = (target.TechLevel == data.TechLevel).Label("TechLevel changed");
                var evoOk = (target.Evolution == data.Evolution).Label("Evolution changed");
                var costOk = (target.CopyCost == data.CopyCost).Label("CopyCost changed");

                // Assert: protected properties preserved (original values, not incoming)
                string mfgVal;
                target.Properties.GetString("Manufacture Run Time", null, out mfgVal);
                var mfgOk = (mfgVal == data.MfgRunTime)
                    .Label($"Manufacture Run Time: expected '{data.MfgRunTime}', got '{mfgVal}'");

                string pwrVal;
                target.Properties.GetString("Power Required", null, out pwrVal);
                var pwrOk = (pwrVal == data.PowerReq)
                    .Label($"Power Required: expected '{data.PowerReq}', got '{pwrVal}'");

                // Assert: non-protected incoming properties present
                var incomingPropsOk = true;
                for (int i = 0; i < data.IncomingPropCount; i++)
                {
                    if (!target.Properties.ContainsKey("IncomingProp" + i))
                    {
                        incomingPropsOk = false;
                        break;
                    }
                }

                var propsPresent = incomingPropsOk
                    .Label("Not all non-protected incoming properties are present");

                return resMatch
                    .And(uuidOk).And(ownerOk).And(nameOk).And(nickOk).And(descOk)
                    .And(baseOk).And(typeOk).And(classOk).And(tlOk).And(evoOk).And(costOk)
                    .And(mfgOk).And(pwrOk).And(propsPresent);
            });
        }

        private static Gen<string> NonEmptyAlphaStringGen()
        {
            return Gen.Elements(
                "Alpha",
                "Beta",
                "Gamma",
                "Delta",
                "Hull",
                "Shield",
                "Reactor",
                "Drive",
                "Weapon",
                "Cargo",
                "Nav",
                "Fuel",
                "Thruster",
                "Laser");
        }

        /// <summary>
        /// Generates null, empty string, or a non-empty string.
        /// </summary>
        private static Gen<string> NullableStringGen()
        {
            return Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty),
                NonEmptyAlphaStringGen());
        }

        /// <summary>
        /// Generates 0 or a positive int (1--10).
        /// </summary>
        private static Gen<int> ZeroOrPositiveGen()
        {
            return Gen.OneOf(
                Gen.Constant(0),
                Gen.Choose(1, 10));
        }

        /// <summary>
        /// Generates a Resources dictionary with 0--5 entries.
        /// </summary>
        private static Gen<Dictionary<string, string>> ResourcesDictGen()
        {
            return from count in Gen.Choose(0, 5)
                   from keys in Gen.ListOf(count, NonEmptyAlphaStringGen())
                   from vals in Gen.ListOf(count, Gen.Choose(1, 9999).Select(v => v.ToString()))
                   select keys.Zip(vals, (k, v) => new { k, v })
                              .GroupBy(x => x.k)
                              .ToDictionary(g => g.Key, g => g.First().v);
        }
    }
}
