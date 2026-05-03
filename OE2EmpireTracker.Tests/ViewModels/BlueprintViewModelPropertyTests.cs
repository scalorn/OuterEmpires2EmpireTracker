using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for BlueprintViewModel edit buffer.
    /// Feature: BL-108 Blueprint Immutable Data Model
    /// Validates: LoadFrom round-trip, IsDirty false after LoadFrom, IsDirty detects changes.
    /// </summary>
    [TestFixture]
    public class BlueprintViewModelPropertyTests
    {
        private PlayerContext playerContext;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
        }

        // -----------------------------------------------------------------------
        // Shared generator: builds a random Blueprint with all fields populated
        // -----------------------------------------------------------------------

        private static readonly string[] BlueprintTypes = { "Reactor", "Hull", "Weapon", "Shield", "MainDrive" };
        private static readonly string[] TechLevels = { "Hi-Tech", "Junker", "MilSpec", "Standard" };

        private static Gen<string> SafeStringGen()
        {
            return Gen.OneOf(
                Gen.Constant(string.Empty),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));
        }

        private static Gen<string> NullableStringGen()
        {
            return Gen.OneOf(
                Gen.Constant((string)null),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));
        }

        private static Gen<Dictionary<string, string>> StringDictGen()
        {
            return from count in Gen.Choose(0, 5)
                   from keys in Gen.ArrayOf(count, Arb.From<NonEmptyString>().Generator.Select(s => s.Get))
                   from values in Gen.ArrayOf(count, Arb.From<NonEmptyString>().Generator.Select(s => s.Get))
                   let distinctKeys = keys.Distinct().ToArray()
                   select distinctKeys.Zip(values, (k, v) => new { k, v })
                       .ToDictionary(x => x.k, x => x.v);
        }

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenBlueprint()
        {
            return from name in SafeStringGen()
                   from nickName in SafeStringGen()
                   from description in SafeStringGen()
                   from bpType in Gen.Elements(BlueprintTypes)
                   from evolution in Gen.Choose(0, 15)
                   from techLevel in Gen.OneOf(Gen.Constant((string)null), Gen.Elements(TechLevels))
                   from cls in Gen.Choose(0, 10)
                   from copyCost in Gen.Choose(0, 1000)
                   from baseBpUuid in NullableStringGen()
                   from properties in StringDictGen()
                   from resources in StringDictGen()
                   select BuildBlueprint(
                       name, nickName, description, bpType, evolution,
                       techLevel, cls, copyCost, baseBpUuid, properties, resources);
        }

        private static OE2EmpireTracker.Models.Blueprint BuildBlueprint(
            string name,
            string nickName,
            string description,
            string bpType,
            int evolution,
            string techLevel,
            int cls,
            int copyCost,
            string baseBpUuid,
            Dictionary<string, string> properties,
            Dictionary<string, string> resources)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                NickName = nickName,
                Description = description,
                BluePrintType = bpType,
                Evolution = evolution,
                TechLevel = techLevel,
                Class = cls,
                CopyCost = copyCost,
                BaseBlueprintUUID = baseBpUuid,
            };

            bp.Properties = new PropertyBag();
            foreach (var kvp in properties)
            {
                bp.Properties.SetProperty(kvp.Key, kvp.Value);
            }

            bp.Resources = new Dictionary<string, string>(resources);
            return bp;
        }

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all fields
        // Feature: BL-108, Property 1: LoadFrom round-trip preserves all fields
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(GenBlueprint()), blueprint =>
            {
                var ro = new ReadOnlyBlueprint(blueprint);
                var vm = new BlueprintViewModel(playerContext);
                vm.LoadFrom(ro);

                // Scalar fields
                if (vm.Name != ro.Name) return false;
                if (vm.NickName != ro.NickName) return false;
                if (vm.Description != ro.Description) return false;
                if (vm.BluePrintType != ro.BluePrintType) return false;
                if (vm.Evolution != ro.Evolution) return false;
                if (vm.TechLevel != ro.TechLevel) return false;
                if (vm.Class != ro.Class) return false;
                if (vm.CopyCost != ro.CopyCost) return false;
                if (vm.BaseBlueprintUUID != ro.BaseBlueprintUUID) return false;
                if (vm.UUID != ro.UUID) return false;

                // Properties dictionary
                if (vm.PropertyCount != ro.Properties.Count) return false;
                foreach (var key in vm.PropertyKeys)
                {
                    vm.GetProperty(key, null, out string vmVal);
                    ro.Properties.GetString(key, null, out string roVal);
                    if (vmVal != roVal) return false;
                }

                // Resources dictionary
                if (vm.ResourceCount != ro.Resources.Count) return false;
                foreach (var kvp in vm.Resources)
                {
                    if (!ro.Resources.TryGetValue(kvp.Key, out string roVal)) return false;
                    if (kvp.Value != roVal) return false;
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: IsDirty is false immediately after LoadFrom
        // Feature: BL-108, Property 2: IsDirty is false immediately after LoadFrom
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(Arb.From(GenBlueprint()), blueprint =>
            {
                var ro = new ReadOnlyBlueprint(blueprint);
                var vm = new BlueprintViewModel(playerContext);
                vm.LoadFrom(ro);
                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects any single field change
        // Feature: BL-108, Property 3: IsDirty detects any single field change
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsAnySingleFieldChange()
        {
            // 9 scalar fields + 1 property change + 1 resource change = 11 cases
            var fieldIndexGen = Gen.Choose(0, 10);

            return Prop.ForAll(
                Arb.From(GenBlueprint()),
                Arb.From(fieldIndexGen),
                (blueprint, fieldIndex) =>
                {
                    var ro = new ReadOnlyBlueprint(blueprint);
                    var vm = new BlueprintViewModel(playerContext);
                    vm.LoadFrom(ro);

                    string changedField = MutateSingleField(vm, ro, fieldIndex);

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing " + changedField);
                });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string MutateSingleField(
            BlueprintViewModel vm, ReadOnlyBlueprint ro, int fieldIndex)
        {
            switch (fieldIndex % 11)
            {
                case 0:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name";
                case 1:
                    vm.NickName = (ro.NickName ?? string.Empty) + "X";
                    return "NickName";
                case 2:
                    vm.Description = (ro.Description ?? string.Empty) + "X";
                    return "Description";
                case 3:
                    vm.BluePrintType = (ro.BluePrintType ?? string.Empty) + "X";
                    return "BluePrintType";
                case 4:
                    vm.Evolution = ro.Evolution + 1;
                    return "Evolution";
                case 5:
                    vm.TechLevel = (ro.TechLevel ?? string.Empty) + "X";
                    return "TechLevel";
                case 6:
                    vm.Class = ro.Class + 1;
                    return "Class";
                case 7:
                    vm.CopyCost = ro.CopyCost + 1;
                    return "CopyCost";
                case 8:
                    vm.BaseBlueprintUUID = (ro.BaseBlueprintUUID ?? string.Empty) + "X";
                    return "BaseBlueprintUUID";
                case 9:
                    // Add a new property to make properties differ
                    vm.SetProperty("__test_key__", "dirty");
                    return "Properties";
                case 10:
                    // Add a new resource to make resources differ
                    vm.SetResource("__test_resource__", "999");
                    return "Resources";
                default:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name(default)";
            }
        }
    }
}
