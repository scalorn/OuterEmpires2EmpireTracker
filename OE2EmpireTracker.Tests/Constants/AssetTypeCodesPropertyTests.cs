// <copyright file="AssetTypeCodesPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Reflection;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Constants
{
    /// <summary>
    /// Property-based tests for the AssetTypeCodes constants class.
    /// Feature: asset-type-constants
    /// </summary>
    [TestFixture]
    public class AssetTypeCodesPropertyTests
    {
        /// <summary>
        /// All public const string fields defined on AssetTypeCodes.
        /// </summary>
        private static readonly FieldInfo[] ConstantFields = typeof(AssetTypeCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToArray();

        /// <summary>
        /// Cargo constants that MapAssetTypeC maps to a non-None ItemTypeEnum.
        /// Excludes Deployable, CommodityL, Colony, Station, and Ship since
        /// MapAssetTypeC returns None for those (they are not in its mapping).
        /// </summary>
        private static readonly string[] CargoConstants = new[]
        {
            AssetTypeCodes.Blueprint,
            AssetTypeCodes.Crate,
            AssetTypeCodes.Survey,
            AssetTypeCodes.ShipPart,
            AssetTypeCodes.Flatpack,
            AssetTypeCodes.Workforce,
            AssetTypeCodes.Resource,
            AssetTypeCodes.Ammunition,
            AssetTypeCodes.Commodity,
            AssetTypeCodes.Share,
            AssetTypeCodes.ShipHull,
        };

        // ---------------------------------------------------------------
        // Property 1: All constants are trimmed
        // For any public const string field in AssetTypeCodes, the value
        // SHALL equal value.Trim().
        // **Validates: Requirements 1.4, 2.3**
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1: All constants are trimmed.
        /// Uses reflection to verify every public const string field in
        /// AssetTypeCodes has no leading or trailing whitespace.
        /// **Validates: Requirements 1.4, 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AllConstants_AreTrimmed()
        {
            var fieldIndexGen = Gen.Choose(0, ConstantFields.Length - 1);

            return Prop.ForAll(fieldIndexGen.ToArbitrary(), index =>
            {
                var field = ConstantFields[index];
                var value = (string)field.GetValue(null);

                return (value == value.Trim())
                    .Label($"Field '{field.Name}' has untrimmed value: \"{value}\"");
            });
        }

        // ---------------------------------------------------------------
        // Property 2: Trailing-space resilience in MapAssetTypeC
        // For each cargo AssetTypeCodes constant, appending 1-3 trailing
        // spaces and calling MapAssetTypeC returns the same result as
        // calling it with the trimmed value.
        // **Validates: Requirements 2.1, 2.2**
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: Trailing-space resilience in MapAssetTypeC.
        /// For each known cargo constant, appending 1-3 trailing spaces
        /// produces the same mapping result as the trimmed constant.
        /// **Validates: Requirements 2.1, 2.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MapAssetTypeC_TrailingSpaces_ReturnsSameResult()
        {
            var gen =
                from code in Gen.Elements(CargoConstants)
                from spaceCount in Gen.Choose(1, 3)
                select new { Code = code, Spaces = new string(' ', spaceCount) };

            return Prop.ForAll(gen.ToArbitrary(), input =>
            {
                var padded = input.Code + input.Spaces;
                var expected = AssetMergeService.MapAssetTypeC(input.Code);
                var actual = AssetMergeService.MapAssetTypeC(padded);

                return (actual == expected)
                    .Label($"MapAssetTypeC(\"{padded}\") = {actual}, expected {expected}");
            });
        }
    }
}
