using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for BlueprintService.
    /// Feature: BL-108 Blueprint Immutable Data Model
    /// Validates: Update round-trip, Create round-trip, Delete removes blueprint.
    /// </summary>
    [TestFixture]
    public class BlueprintServicePropertyTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private BlueprintService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            empireContext = EmpireContext.GetInstance();
            service = new BlueprintService(playerContext, empireContext);
        }

        // -----------------------------------------------------------------------
        // Shared generators
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

        private static Gen<BlueprintUpdateRequest> UpdateRequestGen()
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
                   select new BlueprintUpdateRequest
                   {
                       Name = name,
                       NickName = nickName,
                       Description = description,
                       BluePrintType = bpType,
                       Evolution = evolution,
                       TechLevel = techLevel,
                       Class = cls,
                       CopyCost = copyCost,
                       BaseBlueprintUUID = baseBpUuid,
                       Properties = properties,
                       Resources = resources,
                   };
        }

        private static Gen<BlueprintCreateRequest> CreateRequestGen()
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
                   select new BlueprintCreateRequest
                   {
                       Name = name,
                       NickName = nickName,
                       Description = description,
                       BluePrintType = bpType,
                       Evolution = evolution,
                       TechLevel = techLevel,
                       Class = cls,
                       CopyCost = copyCost,
                       BaseBlueprintUUID = baseBpUuid,
                       Properties = properties,
                       Resources = resources,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.Update round-trip
        // Feature: BL-108, Property 4: Service.Update round-trip
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var ec = EmpireContext.GetInstance();
                var svc = new BlueprintService(ctx, ec);

                var seed = new OE2EmpireTracker.Models.Blueprint { UUID = Guid.NewGuid().ToString(), Name = "Seed" };
                ctx.AddBlueprint(seed);

                var result = svc.Update(seed.UUID, request);

                if (result.Name != request.Name) return false;
                if (result.NickName != request.NickName) return false;
                if (result.Description != request.Description) return false;
                if (result.BluePrintType != request.BluePrintType) return false;
                if (result.Evolution != request.Evolution) return false;
                if (result.TechLevel != request.TechLevel) return false;
                if (result.Class != request.Class) return false;
                if (result.CopyCost != request.CopyCost) return false;
                if (result.BaseBlueprintUUID != request.BaseBlueprintUUID) return false;

                // Properties
                if (request.Properties != null)
                {
                    if (result.Properties.Count != request.Properties.Count) return false;
                    foreach (var kvp in request.Properties)
                    {
                        if (!result.Properties.GetString(kvp.Key, null, out string val)) return false;
                        if (val != kvp.Value) return false;
                    }
                }

                // Resources
                if (request.Resources != null)
                {
                    if (result.Resources.Count != request.Resources.Count) return false;
                    foreach (var kvp in request.Resources)
                    {
                        if (!result.Resources.TryGetValue(kvp.Key, out string val)) return false;
                        if (val != kvp.Value) return false;
                    }
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Create round-trip
        // Feature: BL-108, Property 5: Service.Create round-trip
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var ec = EmpireContext.GetInstance();
                var svc = new BlueprintService(ctx, ec);

                var result = svc.Create(request, false);

                if (string.IsNullOrEmpty(result.UUID)) return false;
                if (result.Name != request.Name) return false;
                if (result.NickName != request.NickName) return false;
                if (result.Description != request.Description) return false;
                if (result.BluePrintType != request.BluePrintType) return false;
                if (result.Evolution != request.Evolution) return false;
                if (result.TechLevel != request.TechLevel) return false;
                if (result.Class != request.Class) return false;
                if (result.CopyCost != request.CopyCost) return false;
                if (result.BaseBlueprintUUID != request.BaseBlueprintUUID) return false;

                // Properties
                if (request.Properties != null)
                {
                    if (result.Properties.Count != request.Properties.Count) return false;
                    foreach (var kvp in request.Properties)
                    {
                        if (!result.Properties.GetString(kvp.Key, null, out string val)) return false;
                        if (val != kvp.Value) return false;
                    }
                }

                // Resources
                if (request.Resources != null)
                {
                    if (result.Resources.Count != request.Resources.Count) return false;
                    foreach (var kvp in request.Resources)
                    {
                        if (!result.Resources.TryGetValue(kvp.Key, out string val)) return false;
                        if (val != kvp.Value) return false;
                    }
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.Delete removes blueprint
        // Feature: BL-108, Property 6: Service.Delete removes blueprint
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesBlueprint()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var ec = EmpireContext.GetInstance();
                var svc = new BlueprintService(ctx, ec);

                var created = svc.Create(request, false);
                string uuid = created.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutableBlueprint(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Blueprint still exists after Delete");
            });
        }
    }
}
