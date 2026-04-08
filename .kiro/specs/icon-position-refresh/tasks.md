# Implementation Plan: Icon Position Refresh

## Overview

Update the blueprint market import system to handle game sprite sheet changes. Build a repeatable IconPositionExtractor test utility, split CommodityFactory into per-industry BlueprintType entries, add OreHopper as a new type, and update all CommodityFactory references across the codebase.

## Tasks

- [x] 1. Update BlueprintTypes constants and add IsCommodityFactory helper
  - [x] 1.1 In `BlueprintTypes.cs`, replace `CommodityFactory` constant with `CommodityFactoryPrefix = "Flatpacks/CommodityFactory/"` and add `OreHopper = "OreHopper"` constant. Add `IsCommodityFactory()` extension method to `BlueprintTypeExtensions` that checks `StartsWith(CommodityFactoryPrefix)`. Keep the old `CommodityFactory` constant temporarily marked `[Obsolete]` to find all references.
    - _Requirements: 4.5, 5.6_

  - [x] 1.2 Write unit tests for `IsCommodityFactory()` in `OE2EmpireTracker.Tests/Constants/BlueprintTypeExtensionTests.cs` — null returns false, empty returns false, exact prefix without trailing content returns false, valid variants return true, other Flatpacks/ types return false, case insensitive. Add `<Compile Include>` to test csproj.
    - _Requirements: 5.6_

  - [x] 1.3 Write FsCheck property test for `IsCommodityFactory()` consistency (Property 2 from design). Add to `OE2EmpireTracker.Tests/Constants/BlueprintTypeExtensionPropertyTests.cs`.
    - _Property 2: IsCommodityFactory consistency_
    - _Requirements: 5.6_

- [x] 2. Update all CommodityFactory references across the codebase
  - [x] 2.1 Update `ColonyParser.cs` — replace all `== BlueprintTypes.CommodityFactory` checks with `.IsCommodityFactory()`. Verify with getDiagnostics.
    - _Requirements: 5.7_

  - [x] 2.2 Update `Colony.cs` (ProcessCommodityFactory and LockCommodityFactoryResources) — replace CommodityFactory checks with `.IsCommodityFactory()`.
    - _Requirements: 5.7_

  - [x] 2.3 Update `ColonyStatusCalculator.cs` — replace CommodityFactory checks with `.IsCommodityFactory()`.
    - _Requirements: 5.7_

  - [x] 2.4 Update `ColonyStructure.cs` (form) — replace CommodityFactory checks with `.IsCommodityFactory()`.
    - _Requirements: 5.7_

  - [x] 2.5 Update `ColonyActivityCollector.cs` and `ColonyInactivityCollector.cs` — replace CommodityFactory checks with `.IsCommodityFactory()`.
    - _Requirements: 5.7_

  - [x] 2.6 Update `DeliveryPlanViewModel.cs` — replace CommodityFactory checks with `.IsCommodityFactory()`.
    - _Requirements: 5.7_

  - [x] 2.7 Remove the `[Obsolete]` `CommodityFactory` constant once all references are updated. Verify no remaining references with grep.
    - _Requirements: 5.6_

- [x] 3. Checkpoint — build and run all existing tests
  - Ensure all existing tests pass after the CommodityFactory refactor. Ask the user if questions arise.

- [x] 4. Split CommodityFactory BaselineData entries
  - [x] 4.1 In both `OE2EmpireTracker/BaselineData.json` and `OE2EmpireTracker.Tests/TestData/BaselineData.json`, replace the single `Flatpacks/CommodityFactory` BlueprintType entry with 14 per-industry entries using the `CommodityIndustryEnum` as the master list. Id pattern: `Flatpacks/CommodityFactory/{IndustryName}`. Each entry gets `Universal: true`, `OutputItemType: "Flatpack"`, and the Properties array from the old single entry as defaults. Set `IconPosition: null` for all (the extractor will fill these in later).
    - _Requirements: 5.2, 5.3, 5.4_

  - [x] 4.2 In both BaselineData.json files, update existing global blueprint records that have `BluePrintType: "Flatpacks/CommodityFactory"` to reference their per-industry type based on their `Commodity Industry` property value.
    - _Requirements: 5.4, 5.8_

- [x] 5. Checkpoint — build and run all existing tests
  - Ensure all existing tests pass after the BaselineData split. Ask the user if questions arise.

- [x] 6. Add MarketSampleOreHopper.html to test csproj
  - [x] 6.1 Add `<Content Include="TestData\MarketSampleOreHopper.html"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` to `OE2EmpireTracker.Tests.csproj`.
    - _Requirements: 7.5_

- [x] 7. Build the IconPositionExtractor test utility
  - [x] 7.1 Create `OE2EmpireTracker.Tests/Services/IconPositionExtractorTests.cs` with the `ExtractedIcon` DTO class and `ExtractIconsFromAllSamples()` helper that parses every `MarketSample*.html` file using `BlueprintScanner.ProcessMarketHtml()` and extracts `_IconPosition` from each parsed blueprint. Add `<Compile Include>` to test csproj.
    - _Requirements: 1.1, 1.2, 1.5_

  - [x] 7.2 Add `CompareAndUpdateBaselineData()` helper that loads BaselineData.json as JObject, compares extracted icon positions against BlueprintType entries, updates changed positions, and adds new BlueprintType entries for unknown icons. Log all changes via TestContext.WriteLine.
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 7.3 Add `EnsureCommodityFactoryEntries()` helper that ensures all 14 per-industry entries exist in BaselineData by checking against `CommodityIndustryEnum`. Creates missing entries with null IconPosition and default properties.
    - _Requirements: 5.2, 5.9_

  - [x] 7.4 Add `WriteBothBaselineFiles()` helper that writes the updated JObject to both `OE2EmpireTracker/BaselineData.json` and `OE2EmpireTracker.Tests/TestData/BaselineData.json`.
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 7.5 Add `ProduceCoverageGapReport()` helper that compares all BlueprintType Ids in BaselineData against extracted icons and logs uncovered types. Distinguish between types with stale IconPosition vs types with null IconPosition.
    - _Requirements: 9.1, 9.2, 9.3, 9.5_

  - [x] 7.6 Wire up the main `ExtractAndUpdateIconPositions()` test method that calls all helpers in sequence: extract → ensure commodity entries → compare & update → write files → produce gap report.
    - _Requirements: 1.5, 8.1, 8.3_

- [x] 8. Run the IconPositionExtractor to populate BaselineData
  - [x] 8.1 Execute the `ExtractAndUpdateIconPositions` test to populate all icon positions from the refreshed MarketSample HTML files. Verify the OreHopper entry is created with correct icon position and properties. Verify per-industry CommodityFactory entries get their icon positions filled in where samples exist.
    - _Requirements: 1.6, 2.2, 2.3, 3.5, 4.1, 4.2, 4.3_

- [x] 9. Checkpoint — build and run all tests
  - Ensure all tests pass after the extractor has updated BaselineData. Ask the user if questions arise.

- [x] 10. Add OreHopper constant and integration tests
  - [x] 10.1 Verify `BlueprintTypes.OreHopper` constant matches the Id created by the extractor. Update if needed.
    - _Requirements: 4.5_

  - [x] 10.2 Add integration test: parse `MarketSampleOreHopper.html`, verify at least one blueprint with BluePrintType "OreHopper" and populated properties.
    - _Requirements: 7.1, 7.2_

  - [x] 10.3 Add integration test: import OreHopper twice, verify idempotency (second import updates, no duplicates).
    - _Requirements: 7.4_

  - [x] 10.4 Add integration test: parse `MarketSampleAllFlatpacks.html`, verify commodity industry listings resolve to per-industry BlueprintType entries.
    - _Requirements: 7.3_

  - [x] 10.5 Add integration test: import AllFlatpacks twice, verify idempotency.
    - _Requirements: 7.4_

- [x] 11. Coverage gap report and backlog integration
  - [x] 11.1 Review the coverage gap report output from the extractor run. For any BlueprintTypes with no HTML coverage, add backlog items to `spec/BACKLOG.md` following the `BL-NNN` format under a "MarketSample Coverage Gaps" section.
    - _Requirements: 10.1, 10.2, 10.5_

  - [x] 11.2 For any CommodityIndustry variants missing from MarketSample coverage, add a grouped backlog item listing the missing variants.
    - _Requirements: 10.4_

- [x] 12. Final checkpoint — build and run all tests
  - Ensure all tests pass. Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- New `.cs` files require a `<Compile Include="..."/>` entry in the corresponding `.csproj`
- Use `getDiagnostics` for compile checks; use `vstest.console` for test execution (not `dotnet test`)
- The IconPositionExtractor writes to source-tree paths, not output directory paths — it modifies the actual BaselineData.json files
