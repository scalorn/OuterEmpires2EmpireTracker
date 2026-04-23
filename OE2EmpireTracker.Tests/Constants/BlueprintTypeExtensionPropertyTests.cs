using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Tests.Constants
{
    [TestFixture]
    public class BlueprintTypeExtensionPropertyTests
    {
        private static readonly string Prefix = BlueprintTypes.CommodityFactoryPrefix;

        #region Generators

        /// <summary>
        /// Generates a non-empty trailing string to append after the prefix.
        /// </summary>
        private static Gen<string> TrailingContentGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        /// <summary>
        /// Generates strings that do NOT start with the CommodityFactory prefix.
        /// Includes null, empty, whitespace, other Flatpacks/ types, and random strings.
        /// </summary>
        private static Gen<string> NonCommodityFactoryGen()
        {
            return Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty),
                Gen.Constant("   "),
                Gen.Elements(
                    "Flatpacks/MiningRig",
                    "Flatpacks/Refinery",
                    "Flatpacks/ResearchLaboratory",
                    "Flatpacks/Manufactory",
                    "OreHopper",
                    "Reactor",
                    "Hull"),
                // Bare prefix with no trailing content
                Gen.Constant(BlueprintTypes.CommodityFactoryPrefix),
                // Random strings that don't start with the prefix
                Arb.Generate<NonNull<string>>()
                    .Select(s => s.Get)
                    .Where(s => !s.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                             || s.Length <= Prefix.Length)
           );
        }

        #endregion

        #region Property 2: IsCommodityFactory consistency

        /// <summary>
        /// Property 2: IsCommodityFactory consistency.
        /// For any string that starts with "Flatpacks/CommodityFactory/" and has content
        /// after the prefix, IsCommodityFactory() should return true.
        /// **Validates: Requirements 5.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property IsCommodityFactory_TrueForPrefixWithTrailingContent()
        {
            var gen = TrailingContentGen().Select(suffix => Prefix + suffix);

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                return input.IsCommodityFactory()
                    .Label($"Expected true for \"{input}\"");
            });
        }

        /// <summary>
        /// Property 2 (negative): IsCommodityFactory consistency.
        /// For any string that does not start with "Flatpacks/CommodityFactory/" (including
        /// null, empty, and other Flatpacks/ types), or is the bare prefix with no trailing
        /// content, IsCommodityFactory() should return false.
        /// **Validates: Requirements 5.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property IsCommodityFactory_FalseForNonCommodityFactory()
        {
            return Prop.ForAll(NonCommodityFactoryGen().ToArbitrary(), input =>
            {
                return (!input.IsCommodityFactory())
                    .Label($"Expected false for \"{input ?? "(null)"}\"");
            });
        }

        #endregion
    }
}
