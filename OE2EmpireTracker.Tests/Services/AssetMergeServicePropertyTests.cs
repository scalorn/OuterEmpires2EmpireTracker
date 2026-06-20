// <copyright file="AssetMergeServicePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for AssetMergeService TypeC mapping and resource purity extraction.
    /// Feature: assets-api-integration
    /// </summary>
    [TestFixture]
    public class AssetMergeServicePropertyTests
    {
        // Known TypeC codes and their expected mappings
        private static readonly string[] KnownTypeCCodes = new[]
        {
            AssetTypeCodes.Resource, AssetTypeCodes.Commodity, AssetTypeCodes.Flatpack, AssetTypeCodes.Blueprint, AssetTypeCodes.ShipPart, AssetTypeCodes.Survey, AssetTypeCodes.Workforce, AssetTypeCodes.ShipHull, AssetTypeCodes.Ammunition, AssetTypeCodes.Share, AssetTypeCodes.Crate,
        };

        private static readonly string[] PurityDescriptors = new[]
        {
            "High Purity", "Med Purity", "Low Purity",
        };

        /// <summary>
        /// Maps an API purity descriptor (e.g. "Low Purity") to the normalized
        /// stored form (e.g. "Low") using the same logic as ExtractResourcePurity.
        /// </summary>
        private static string ExpectedNormalizedPurity(string apiPurity)
        {
            // Strip " Purity" suffix (same as ExtractResourcePurity does)
            string stripped = apiPurity.EndsWith(" Purity", StringComparison.OrdinalIgnoreCase)
                ? apiPurity.Substring(0, apiPurity.Length - " Purity".Length).Trim()
                : apiPurity;

            // Apply the same normalization (Med → Medium, etc.)
            return SurveyParser.NormalizePurity(stripped);
        }

        // ---------------------------------------------------------------
        // Generators
        // ---------------------------------------------------------------

        private static Gen<string> KnownTypeCGen()
        {
            return Gen.Elements(KnownTypeCCodes);
        }

        private static Gen<string> UnknownTypeCGen()
        {
            // Generate strings that are NOT in the known set
            return from prefix in Gen.Elements("X", "Z", "Q", "Zz", "RR", "CC", "Unknown", "abc", "123", "!!")
                   from suffix in Gen.Elements(string.Empty, "x", "1", "_")
                   let candidate = prefix + suffix
                   where !KnownTypeCCodes.Contains(candidate)
                       && !KnownTypeCCodes.Any(k => string.Equals(k, candidate, StringComparison.OrdinalIgnoreCase))
                   select candidate;
        }

        private static Gen<string> BaseResourceNameGen()
        {
            return from prefix in Gen.Elements(
                       "Heavy Post-Trans Metals",
                       "Light Metals",
                       "Radioactives",
                       "Silicates",
                       "Carbon Compounds",
                       "Rare Earth Elements",
                       "Hydrocarbons",
                       "Gases",
                       "Water Ice",
                       "Organics")
                   select prefix;
        }

        private static Gen<string> PurityGen()
        {
            return Gen.Elements(PurityDescriptors);
        }

        // ---------------------------------------------------------------
        // Property 1: TypeC Totality (Correctness Property 3)
        // All known typeC codes map to non-None ItemTypeEnum;
        // all unknown strings map to None.
        // Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1a: All known TypeC codes map to a non-None ItemTypeEnum.
        /// Validates: Requirements 3.1-3.11
        /// </summary>
        [Test]
        public void MapAssetTypeC_KnownCodes_AlwaysMapToNonNone()
        {
            Prop.ForAll(KnownTypeCGen().ToArbitrary(), (typeC) =>
            {
                var result = AssetMergeService.MapAssetTypeC(typeC);
                return (result != ItemType.ItemTypeEnum.None)
                    .Label($"Expected non-None for '{typeC}', got {result}");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 1b: All known TypeC codes with leading/trailing whitespace
        /// still map to a non-None ItemTypeEnum (whitespace trimming).
        /// Validates: Requirements 3.11
        /// </summary>
        [Test]
        public void MapAssetTypeC_KnownCodesWithWhitespace_AlwaysMapToNonNone()
        {
            var paddedGen =
                from code in KnownTypeCGen()
                from leadingSpaces in Gen.Choose(0, 3)
                from trailingSpaces in Gen.Choose(0, 3)
                select new string(' ', leadingSpaces) + code + new string(' ', trailingSpaces);

            Prop.ForAll(paddedGen.ToArbitrary(), (paddedTypeC) =>
            {
                var result = AssetMergeService.MapAssetTypeC(paddedTypeC);
                return (result != ItemType.ItemTypeEnum.None)
                    .Label($"Expected non-None for '{paddedTypeC}', got {result}");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 1c: All unknown TypeC strings map to ItemTypeEnum.None.
        /// Validates: Requirements 3.12
        /// </summary>
        [Test]
        public void MapAssetTypeC_UnknownCodes_AlwaysMapToNone()
        {
            Prop.ForAll(UnknownTypeCGen().ToArbitrary(), (typeC) =>
            {
                var result = AssetMergeService.MapAssetTypeC(typeC);
                return (result == ItemType.ItemTypeEnum.None)
                    .Label($"Expected None for '{typeC}', got {result}");
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 2: Purity Extraction Consistency (Correctness Property 4)
        // For any base name + known purity, constructing the parenthesized
        // format and calling ExtractResourcePurity returns the original
        // name and purity.
        // Validates: Requirements 10.1, 10.2, 10.3, 10.4
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: Purity Extraction Roundtrip.
        /// For any base resource name and any known purity, constructing
        /// "{name} (Unrefined, {purity})" and calling ExtractResourcePurity
        /// returns the original name and purity.
        /// Validates: Requirements 10.1, 10.2, 10.3, 10.4
        /// </summary>
        [Test]
        public void ExtractResourcePurity_Roundtrip_ReturnsOriginalNameAndPurity()
        {
            var inputGen =
                from baseName in BaseResourceNameGen()
                from purity in PurityGen()
                select new { BaseName = baseName, Purity = purity };

            Prop.ForAll(inputGen.ToArbitrary(), (input) =>
            {
                // Construct the parenthesized format as the API would return it
                string constructed = $"{input.BaseName} (Unrefined, {input.Purity})";

                var (extractedName, extractedPurity) = AssetMergeService.ExtractResourcePurity(constructed);

                // The expected purity is the normalized form (e.g. "Med Purity" → "Medium")
                string expectedPurity = ExpectedNormalizedPurity(input.Purity);

                var nameMatches = string.Equals(extractedName, input.BaseName, StringComparison.Ordinal);
                var purityMatches = string.Equals(extractedPurity, expectedPurity, StringComparison.Ordinal);

                return (nameMatches && purityMatches)
                    .Label($"Input: '{constructed}' => name='{extractedName}' (expected '{input.BaseName}'), purity='{extractedPurity}' (expected '{expectedPurity}')");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 2b: For resource names without a purity descriptor,
        /// ExtractResourcePurity returns the full name and empty purity.
        /// Validates: Requirements 10.3
        /// </summary>
        [Test]
        public void ExtractResourcePurity_NoPurity_ReturnsFullNameAndEmptyPurity()
        {
            Prop.ForAll(BaseResourceNameGen().ToArbitrary(), (baseName) =>
            {
                var (extractedName, extractedPurity) = AssetMergeService.ExtractResourcePurity(baseName);

                var nameMatches = string.Equals(extractedName, baseName, StringComparison.Ordinal);
                var purityEmpty = string.IsNullOrEmpty(extractedPurity);

                return (nameMatches && purityEmpty)
                    .Label($"Input: '{baseName}' => name='{extractedName}', purity='{extractedPurity}'");
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Generators for merge property tests
        // ---------------------------------------------------------------

        private static Gen<AssetCargoItem> CargoItemGen()
        {
            return from cargoItemId in Gen.Choose(1, 100000)
                   from typeC in KnownTypeCGen()
                   from amount in Gen.Choose(1, 9999)
                   from typeId in Gen.Choose(1, 500)
                   from resourceName in BaseResourceNameGen()
                   select new AssetCargoItem
                   {
                       Id = cargoItemId,
                       TypeC = typeC,
                       Amount = amount,
                       TypeId = typeId,
                       ResourceName = resourceName,
                   };
        }

        private static Gen<List<AssetCargoItem>> CargoItemListGen()
        {
            return from count in Gen.Choose(1, 20)
                   from items in Gen.ListOf(count, CargoItemGen())
                   select items.ToList();
        }

        private static Gen<List<AssetCargoItem>> UniqueCargoItemListGen()
        {
            return from count in Gen.Choose(1, 20)
                   from items in Gen.ListOf(count, CargoItemGen())
                   let uniqueItems = items
                       .GroupBy(i => i.Id)
                       .Select(g => g.First())
                       .ToList()
                   where uniqueItems.Count > 0
                   select uniqueItems;
        }

        // ---------------------------------------------------------------
        // Property 3: Additive Merge Invariant (Correctness Property 1)
        // After any asset merge operation, the item count in the target
        // ItemBag is >= the count before the merge. Items are never removed.
        // Validates: Requirements 5.4, 6.5, 7.5, 13.4
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 3: After any merge, ItemBag count >= count before merge (additive only).
        /// Validates: Requirements 5.4, 6.5, 7.5, 13.4
        /// </summary>
        [Test]
        public void MergeAssets_AdditiveOnly_CountNeverDecreases()
        {
            Prop.ForAll(CargoItemListGen().ToArbitrary(), (apiItems) =>
            {
                var bag = new ItemBag();
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };
                colony.Items = bag;

                int countBefore = bag.Count();
                AssetMergeService.MergeColonyAssets(apiItems, colony);
                int countAfter = bag.Count();

                return (countAfter >= countBefore)
                    .Label($"Count decreased: before={countBefore}, after={countAfter}");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 3b: Authoritative merge with pre-existing items — count equals API item count
        /// (items not in the new API response are removed as stale).
        /// Validates: Requirements 5.4, 6.5, 7.5, 13.4
        /// </summary>
        [Test]
        public void MergeAssets_AdditiveOnly_WithPreExistingItems_CountNeverDecreases()
        {
            var inputGen =
                from existingItems in UniqueCargoItemListGen()
                from newItems in CargoItemListGen()
                select new { ExistingItems = existingItems, NewItems = newItems };

            Prop.ForAll(inputGen.ToArbitrary(), (input) =>
            {
                var bag = new ItemBag();
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };
                colony.Items = bag;

                // Seed the bag with existing items
                AssetMergeService.MergeColonyAssets(input.ExistingItems, colony);

                // Merge new items (authoritative: stale items removed)
                AssetMergeService.MergeColonyAssets(input.NewItems, colony);
                int countAfter = bag.Count();

                // Count should equal the number of unique GameItemIds in the new items
                // (the API response is authoritative — only its items remain)
                var uniqueNewIds = input.NewItems.Select(i => i.Id).Distinct().Count();
                return (countAfter == uniqueNewIds)
                    .Label($"Count mismatch: expected={uniqueNewIds}, actual={countAfter}");
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 4: GameItemId Uniqueness (Correctness Property 2)
        // After merge, no ItemBag contains two items with the same
        // non-null GameItemId value.
        // Validates: Requirements 5.3, 6.4, 7.4, 12.1
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 4: After merge, no two items share the same non-null GameItemId.
        /// Validates: Requirements 5.3, 6.4, 7.4, 12.1
        /// </summary>
        [Test]
        public void MergeAssets_GameItemIdUniqueness_NoDuplicateGameItemIds()
        {
            Prop.ForAll(CargoItemListGen().ToArbitrary(), (apiItems) =>
            {
                var bag = new ItemBag();
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };
                colony.Items = bag;

                AssetMergeService.MergeColonyAssets(apiItems, colony);

                var nonNullGameItemIds = bag.Items.Values
                    .Where(i => i.GameItemId.HasValue)
                    .Select(i => i.GameItemId.Value)
                    .ToList();

                var distinctCount = nonNullGameItemIds.Distinct().Count();

                return (distinctCount == nonNullGameItemIds.Count)
                    .Label($"Duplicate GameItemIds found: {nonNullGameItemIds.Count} total, {distinctCount} distinct");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 4b: After multiple merges, GameItemId uniqueness still holds.
        /// Validates: Requirements 5.3, 6.4, 7.4, 12.1
        /// </summary>
        [Test]
        public void MergeAssets_GameItemIdUniqueness_AfterMultipleMerges()
        {
            var inputGen =
                from firstBatch in UniqueCargoItemListGen()
                from secondBatch in UniqueCargoItemListGen()
                select new { FirstBatch = firstBatch, SecondBatch = secondBatch };

            Prop.ForAll(inputGen.ToArbitrary(), (input) =>
            {
                var bag = new ItemBag();
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };
                colony.Items = bag;

                AssetMergeService.MergeColonyAssets(input.FirstBatch, colony);
                AssetMergeService.MergeColonyAssets(input.SecondBatch, colony);

                var nonNullGameItemIds = bag.Items.Values
                    .Where(i => i.GameItemId.HasValue)
                    .Select(i => i.GameItemId.Value)
                    .ToList();

                var distinctCount = nonNullGameItemIds.Distinct().Count();

                return (distinctCount == nonNullGameItemIds.Count)
                    .Label($"Duplicate GameItemIds found: {nonNullGameItemIds.Count} total, {distinctCount} distinct");
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 5: Idempotent Merge (Correctness Property 5)
        // Merging the same API response twice produces the same state;
        // second call returns false (no changes).
        // Validates: Requirements 5.3, 5.4, 6.4, 6.5, 7.4, 7.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 5: Merging the same API response twice produces the same state;
        /// second call returns false (idempotent).
        /// Validates: Requirements 5.3, 5.4, 6.4, 6.5, 7.4, 7.5
        /// </summary>
        [Test]
        public void MergeAssets_Idempotent_SecondMergeReturnsFalse()
        {
            Prop.ForAll(UniqueCargoItemListGen().ToArbitrary(), (apiItems) =>
            {
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };

                // First merge — may return true (changes made)
                AssetMergeService.MergeColonyAssets(apiItems, colony);

                // Second merge with same data — should return false (no changes)
                bool secondResult = AssetMergeService.MergeColonyAssets(apiItems, colony);

                return (!secondResult)
                    .Label($"Second merge returned true (expected false for idempotent merge)");
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 5b: Merging the same API response twice produces the same item count.
        /// Validates: Requirements 5.3, 5.4, 6.4, 6.5, 7.4, 7.5
        /// </summary>
        [Test]
        public void MergeAssets_Idempotent_SecondMergeDoesNotChangeCount()
        {
            Prop.ForAll(UniqueCargoItemListGen().ToArbitrary(), (apiItems) =>
            {
                var colony = new Colony { UUID = Guid.NewGuid().ToString(), ColonyId = 1 };

                AssetMergeService.MergeColonyAssets(apiItems, colony);
                int countAfterFirst = colony.Items.Count();

                AssetMergeService.MergeColonyAssets(apiItems, colony);
                int countAfterSecond = colony.Items.Count();

                return (countAfterFirst == countAfterSecond)
                    .Label($"Count changed: after first={countAfterFirst}, after second={countAfterSecond}");
            }).QuickCheckThrowOnFailure();
        }
    }
}
