using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Property-based and unit tests for Blueprint.OutputItemName normalization.
    /// Feature: structure-name-normalization
    /// </summary>
    [TestFixture]
    public class BlueprintOutputNameTests
    {
        // ── Shared generators ──

        private static readonly string[] FlatpackSuffixes = { " Flatpack", " FLATPACK", " flatpack", " FlatPack" };
        private static readonly string[] FlatpackTypes = { "Flatpacks/MiningRig", "Flatpacks/Refinery", "Flatpacks/ResearchLaboratory", "Flatpacks/Manufactory", "Flatpacks/CommodityFactory" };
        private static readonly string[] NonFlatpackTypes = { "Hulls/Corvette", "Weapons/Laser", "Components/Reactor", null };
        private static readonly string[] TechLevels = { null, "MilSpec", "CivSpec", "GovSpec" };
        private static readonly string[] BaseNames = { "Mining Rig", "Habitation Block", "Refinery", "Research Laboratory", "Administration Block", "Agridome", "X" };

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenFlatpackWithSuffix()
        {
            return Gen.Elements(BaseNames)
                .SelectMany(baseName => Gen.Elements(FlatpackSuffixes), (baseName, suffix) => new { baseName, suffix })
                .SelectMany(t => Gen.Elements(FlatpackTypes), (t, bpType) => new { t.baseName, t.suffix, bpType })
                .SelectMany(t => Gen.Choose(0, 5), (t, cls) => new { t.baseName, t.suffix, t.bpType, cls })
                .SelectMany(t => Gen.Choose(0, 5), (t, evo) => new { t.baseName, t.suffix, t.bpType, t.cls, evo })
                .SelectMany(t => Gen.Elements(TechLevels), (t, tech) => new { t.baseName, t.suffix, t.bpType, t.cls, t.evo, tech })
                .SelectMany(t => Gen.OneOf(Gen.Constant((string)null), Gen.Constant(""), Gen.Constant("MyNick")), (t, nick) => new { t.baseName, t.suffix, t.bpType, t.cls, t.evo, t.tech, nick })
                .SelectMany(t => Gen.OneOf(Gen.Constant((string)null), Gen.Constant(Guid.NewGuid().ToString())), (t, uuid) =>
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint();
                    bp.Name = t.baseName + t.suffix;
                    bp.BluePrintType = t.bpType;
                    bp.Class = t.cls;
                    bp.Evolution = t.evo;
                    bp.TechLevel = t.tech;
                    bp.NickName = t.nick ?? string.Empty;
                    bp.UUID = uuid;
                    return bp;
                });
        }

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenNonFlatpackOrNoSuffix()
        {
            var genNonFlatpack = Gen.Elements(BaseNames)
                .SelectMany(name => Gen.Elements(NonFlatpackTypes), (name, bpType) => new { name, bpType });

            var genFlatpackNoSuffix = Gen.Elements(BaseNames)
                .SelectMany(name => Gen.Elements(FlatpackTypes), (name, bpType) => new { name, bpType });

            return Gen.OneOf(genNonFlatpack, genFlatpackNoSuffix)
                .SelectMany(t => Gen.Choose(0, 5), (t, cls) => new { t.name, t.bpType, cls })
                .SelectMany(t => Gen.Choose(0, 5), (t, evo) => new { t.name, t.bpType, t.cls, evo })
                .SelectMany(t => Gen.Elements(TechLevels), (t, tech) => new { t.name, t.bpType, t.cls, t.evo, tech })
                .SelectMany(t => Gen.OneOf(Gen.Constant((string)null), Gen.Constant(""), Gen.Constant("MyNick")), (t, nick) => new { t.name, t.bpType, t.cls, t.evo, t.tech, nick })
                .SelectMany(t => Gen.OneOf(Gen.Constant((string)null), Gen.Constant(Guid.NewGuid().ToString())), (t, uuid) =>
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint();
                    bp.Name = t.name;
                    bp.BluePrintType = t.bpType;
                    bp.Class = t.cls;
                    bp.Evolution = t.evo;
                    bp.TechLevel = t.tech;
                    bp.NickName = t.nick ?? string.Empty;
                    bp.UUID = uuid;
                    return bp;
                });
        }

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenNullOrEmptyName()
        {
            return Gen.OneOf(Gen.Constant((string)null), Gen.Constant(""))
                .SelectMany(name => Gen.OneOf(Gen.Elements(FlatpackTypes), Gen.Elements(NonFlatpackTypes)), (name, bpType) => new { name, bpType })
                .Select(t =>
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint();
                    bp.Name = t.name;
                    bp.BluePrintType = t.bpType;
                    bp.UUID = Guid.NewGuid().ToString();
                    return bp;
                });
        }

        private static Gen<OE2EmpireTracker.Models.Blueprint> GenAnyBlueprint()
        {
            return Gen.Frequency(
                Tuple.Create(4, GenFlatpackWithSuffix()),
                Tuple.Create(3, GenNonFlatpackOrNoSuffix()),
                Tuple.Create(1, GenNullOrEmptyName())
            );
        }

        // ── Property 1: Suffix stripping round-trip ──

        /// <summary>
        /// Feature: structure-name-normalization, Property 1: Suffix stripping round-trip
        /// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SuffixStripping_RoundTrip_FlatpackWithSuffix()
        {
            return Prop.ForAll(
                GenFlatpackWithSuffix().ToArbitrary(),
                bp =>
                {
                    string originalSuffix = bp.Name.Substring(bp.Name.Length - " Flatpack".Length);
                    string reconstructed = bp.OutputItemName + originalSuffix;
                    return (reconstructed == bp.Name)
                        .Label($"Name='{bp.Name}', OutputItemName='{bp.OutputItemName}', reconstructed='{reconstructed}'");
                });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SuffixStripping_RoundTrip_NonFlatpackOrNoSuffix()
        {
            return Prop.ForAll(
                GenNonFlatpackOrNoSuffix().ToArbitrary(),
                bp =>
                {
                    return (bp.OutputItemName == bp.Name)
                        .Label($"Name='{bp.Name}', BluePrintType='{bp.BluePrintType}', OutputItemName='{bp.OutputItemName}'");
                });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SuffixStripping_RoundTrip_NullOrEmptyName()
        {
            return Prop.ForAll(
                GenNullOrEmptyName().ToArbitrary(),
                bp =>
                {
                    return (bp.OutputItemName == string.Empty)
                        .Label($"Name='{bp.Name ?? "(null)"}', OutputItemName='{bp.OutputItemName}'");
                });
        }

        // ── Property 2: ExtendedName uses OutputItemName ──

        /// <summary>
        /// Feature: structure-name-normalization, Property 2: ExtendedName uses OutputItemName
        /// **Validates: Requirements 2.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExtendedName_UsesFlatpackFullName()
        {
            var gen = GenFlatpackWithSuffix()
                .Where(bp => bp.UUID != null);

            return Prop.ForAll(
                gen.ToArbitrary(),
                bp =>
                {
                    var extended = bp.ExtendedName;
                    bool containsFullName = extended.Contains(bp.Name);
                    return containsFullName
                        .Label($"Name='{bp.Name}', ExtendedName='{extended}'");
                });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ExtendedName_UsesNameForNonFlatpack()
        {
            var gen = GenNonFlatpackOrNoSuffix()
                .Where(bp => bp.UUID != null && !string.IsNullOrEmpty(bp.Name));

            return Prop.ForAll(
                gen.ToArbitrary(),
                bp =>
                {
                    var extended = bp.ExtendedName;
                    return extended.Contains(bp.Name)
                        .Label($"Name='{bp.Name}', ExtendedName='{extended}'");
                });
        }

        // ── Property 3: BuildFlatpackLookup behavioral equivalence ──

        /// <summary>
        /// Feature: structure-name-normalization, Property 3: BuildFlatpackLookup behavioral equivalence
        /// **Validates: Requirements 4.1, 4.2, 4.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildFlatpackLookup_BehavioralEquivalence()
        {
            var genBlueprintList = Gen.ListOf(GenFlatpackWithSuffix());

            return Prop.ForAll(
                genBlueprintList.ToArbitrary(),
                blueprints =>
                {
                    EmpireContext.Reset();
                    var bpList = blueprints.ToList();

                    // OLD logic (inline suffix stripping)
                    var oldLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var bp in bpList)
                    {
                        if (bp.BluePrintType != null && bp.BluePrintType.IsFlatpack())
                        {
                            string designName = bp.Name;
                            if (designName != null && designName.EndsWith(" Flatpack", StringComparison.OrdinalIgnoreCase))
                            {
                                designName = designName.Substring(0, designName.Length - " Flatpack".Length);
                            }
                            if (!oldLookup.ContainsKey(designName))
                                oldLookup[designName] = bp.UUID;
                            if (!oldLookup.ContainsKey(bp.Name))
                                oldLookup[bp.Name] = bp.UUID;
                        }
                    }

                    // NEW logic (using OutputItemName)
                    var newLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var bp in bpList)
                    {
                        if (bp.BluePrintType != null && bp.BluePrintType.IsFlatpack())
                        {
                            string designName = bp.OutputItemName;
                            if (!newLookup.ContainsKey(designName))
                                newLookup[designName] = bp.UUID;
                            if (!newLookup.ContainsKey(bp.Name))
                                newLookup[bp.Name] = bp.UUID;
                        }
                    }

                    bool sameCount = oldLookup.Count == newLookup.Count;
                    bool sameKeys = !oldLookup.Keys.Except(newLookup.Keys, StringComparer.OrdinalIgnoreCase).Any()
                                 && !newLookup.Keys.Except(oldLookup.Keys, StringComparer.OrdinalIgnoreCase).Any();
                    bool sameValues = oldLookup.All(kv => newLookup.TryGetValue(kv.Key, out var val) && val == kv.Value);

                    return (sameCount && sameKeys && sameValues)
                        .Label($"oldCount={oldLookup.Count}, newCount={newLookup.Count}, sameKeys={sameKeys}, sameValues={sameValues}");
                });
        }

        // ── Property 4: Serialization round-trip preserves Name ──

        /// <summary>
        /// Feature: structure-name-normalization, Property 4: Serialization round-trip preserves Name
        /// **Validates: Requirements 1.5, 5.1, 5.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Serialization_RoundTrip_PreservesName()
        {
            return Prop.ForAll(
                GenAnyBlueprint().ToArbitrary(),
                bp =>
                {
                    string json = JsonConvert.SerializeObject(bp, JsonSettings.SerializerSettings);
                    var deserialized = JsonConvert.DeserializeObject<OE2EmpireTracker.Models.Blueprint>(json);

                    string expectedName = bp.Name ?? string.Empty;
                    string actualName = deserialized.Name ?? string.Empty;
                    bool namePreserved = expectedName == actualName;
                    bool noOutputItemNameKey = !json.Contains("\"OutputItemName\"");

                    return (namePreserved && noOutputItemNameKey)
                        .Label($"Name='{bp.Name ?? "(null)"}', deserialized='{deserialized.Name}', preserved={namePreserved}, noKey={noOutputItemNameKey}");
                });
        }

        // ── Property 5: Read-only invariant ──

        /// <summary>
        /// Feature: structure-name-normalization, Property 5: Read-only invariant
        /// **Validates: Requirements 5.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ReadOnly_Invariant()
        {
            return Prop.ForAll(
                GenAnyBlueprint().ToArbitrary(),
                bp =>
                {
                    string nameBefore = bp.Name;
                    string typeBefore = bp.BluePrintType;
                    int classBefore = bp.Class;
                    int evoBefore = bp.Evolution;
                    string techBefore = bp.TechLevel;
                    string nickBefore = bp.NickName;
                    string uuidBefore = bp.UUID;

                    var _ = bp.OutputItemName;
                    var __ = bp.ExtendedName;

                    bool nameOk = bp.Name == nameBefore;
                    bool typeOk = bp.BluePrintType == typeBefore;
                    bool classOk = bp.Class == classBefore;
                    bool evoOk = bp.Evolution == evoBefore;
                    bool techOk = bp.TechLevel == techBefore;
                    bool nickOk = bp.NickName == nickBefore;
                    bool uuidOk = bp.UUID == uuidBefore;

                    return (nameOk && typeOk && classOk && evoOk && techOk && nickOk && uuidOk)
                        .Label($"name={nameOk}, type={typeOk}, class={classOk}, evo={evoOk}, tech={techOk}, nick={nickOk}, uuid={uuidOk}");
                });
        }

        // ── Unit Tests (Task 4.6): Specific examples and edge cases ──

        [Test]
        public void OutputItemName_MiningRigFlatpack_ReturnsMiningRig()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Mining Rig Flatpack") { BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo("Mining Rig"));
        }

        [Test]
        public void OutputItemName_HabitationBlockFlatpack_ReturnsHabitationBlock()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Habitation Block Flatpack") { BluePrintType = "Flatpacks/CommodityFactory" };
            Assert.That(bp.OutputItemName, Is.EqualTo("Habitation Block"));
        }

        [Test]
        public void OutputItemName_NonFlatpack_ReturnsNameUnchanged()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Corvette Hull") { BluePrintType = "Hulls/Corvette" };
            Assert.That(bp.OutputItemName, Is.EqualTo("Corvette Hull"));
        }

        [Test]
        public void OutputItemName_NameIsJustFlatpackSuffix_ReturnsEmptyString()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(" Flatpack") { BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo(""));
        }

        [Test]
        public void OutputItemName_FlatpackWithoutLeadingSpace_NotStripped()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Flatpack") { BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo("Flatpack"));
        }

        [Test]
        public void OutputItemName_NullName_ReturnsEmptyString()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint() { Name = null, BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo(""));
        }

        [Test]
        public void OutputItemName_EmptyName_ReturnsEmptyString()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("") { BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo(""));
        }

        [Test]
        public void OutputItemName_CaseInsensitiveSuffix_Stripped()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Mining Rig FLATPACK") { BluePrintType = "Flatpacks/MiningRig" };
            Assert.That(bp.OutputItemName, Is.EqualTo("Mining Rig"));
        }

        [Test]
        public void ExtendedName_FlatpackBlueprint_ContainsFullName()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Mining Rig Flatpack")
            {
                BluePrintType = "Flatpacks/MiningRig",
                UUID = Guid.NewGuid().ToString()
            };
            Assert.That(bp.ExtendedName, Does.Contain("Mining Rig Flatpack"));
        }
    }
}
