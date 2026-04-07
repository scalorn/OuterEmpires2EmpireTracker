# Implementation Plan: Mass Blueprint Importer

## Overview

Add a bulk market import capability to the Blueprint form. An "Import Market" button reads market HTML from the clipboard, parses all expanded blueprint listings, extracts seller name and TechLevel, deduplicates against existing data, routes to global or player storage, creates or updates records, and displays a results summary.

## Tasks

- [x] 1. Extend BlueprintScanner.ProcessMarketHtml with seller name and TechLevel extraction
  - [x] 1.1 Add `MarketBlueprint` wrapper class to `BlueprintScanner.cs`
    - Add `MarketBlueprint` class with `Blueprint` and `SellerName` properties
    - Change `ProcessMarketHtml` return type from `List<Blueprint>` to `List<MarketBlueprint>`
    - Extract seller name from `<span class="ui_text_light_grey">` inside the name div
    - Extract TechLevel from name parentheses if matching known value (Hi-Tech, Junker, MilSpec, Rugged, Service, Standard)
    - Strip TechLevel from Name, set `bp.TechLevel`
    - Update existing tests to use `MarketBlueprint` wrapper
    - _Requirements: 4_

  - [x] 1.2 Write tests for seller name and TechLevel extraction
    - Test: blueprint with "(MilSpec)" in name → TechLevel = "MilSpec", name stripped
    - Test: blueprint with "(Rugged)" → TechLevel = "Rugged"
    - Test: blueprint without parentheses → TechLevel = null, name unchanged
    - Test: blueprint with non-TechLevel parentheses (e.g. "(Ev0)") → TechLevel = null
    - Test: seller name "Government" extracted correctly
    - Test: seller name for player-sold blueprint extracted correctly
    - Test: no seller span → empty seller name
    - _Requirements: 4, 5_

- [x] 2. Checkpoint — Ensure parser changes build and all tests pass

- [x] 3. Create MarketBlueprintImporter service with import logic
  - [x] 3.1 Create `OE2EmpireTracker/Services/MarketBlueprintImporter.cs`
    - Add `ImportResult`, `ImportResultEntry`, `ImportAction` classes/enum
    - Implement `Import(List<MarketBlueprint>, PlayerContext, EmpireContext)` method
    - Skip unexpanded listings (zero properties, no BluePrintType)
    - Route by seller: "Government" → globalBlueprintList, else → blueprintList
    - Skip player-routed if no current player selected
    - Dedup by Name + Evolution + BluePrintType + Class + TechLevel
    - Update existing: overwrite properties/resources, preserve protected fields (UUID, OwnerUUID, NickName, CopyCost, Manufacture Run Time, Power Required, Description)
    - Create new: generate UUID, set OwnerUUID for player blueprints
    - Log each action via NLog
    - Persist: call writeContext() on EmpireContext/PlayerContext as needed
    - Return ImportResult
    - Add `<Compile Include>` to csproj
    - _Requirements: 2, 3, 5, 6, 7, 8, 9_

  - [x] 3.2 Write tests for MarketBlueprintImporter
    - Test: create new global blueprint (Government seller)
    - Test: create new player blueprint (non-Government seller)
    - Test: skip unexpanded listing (zero properties)
    - Test: skip player blueprint when no current player
    - Test: update existing blueprint on duplicate key match
    - Test: protected fields preserved on update
    - Test: import result counts match expectations
    - Test: routing correctness (Government → global, other → player)
    - **Property 1: Dedup key uniqueness**
    - **Property 2: Routing correctness**
    - **Property 3: Protected field preservation**
    - **Property 4: Import result completeness**
    - _Requirements: 2, 3, 5, 6, 7, 8_

- [x] 4. Checkpoint — Ensure importer service builds and tests pass

- [x] 5. Add idempotency integration tests
  - [x] 5.1 Write idempotency tests using real MarketSample HTML files
    - Test: import MarketSampleReactor.html once → verify blueprints created with correct count
    - Test: import same file 3 times → verify no duplicate blueprints, all "Updated" on 2nd/3rd
    - Test: after 3 imports, each blueprint's property count unchanged from first import
    - Test: after 3 imports, each blueprint's resource count unchanged from first import
    - Test: import MarketSampleAllWeaponTypes.html 2 times → verify weapon blueprints not duplicated
    - **Property 5: TechLevel extraction** (verified via real HTML names)
    - **Property 6: Import idempotency**
    - _Requirements: 5, 6_

- [x] 6. Checkpoint — Ensure idempotency tests pass

- [ ] 7. Add Import Market button to FormBlueprint UI
  - [x] 7.1 Modify `FormBlueprint.Designer.cs` to add `cmdImportMarket` button
    - Declare `private System.Windows.Forms.Button cmdImportMarket;`
    - Add to `flpCommands.Controls` before `cmdImport`
    - Set `Text = "Import Market"`, appropriate size
    - _Requirements: 1_

  - [-] 7.2 Modify `FormBlueprint.cs` to wire up Import Market click handler
    - Wire `cmdImportMarket.Click` handler
    - Read clipboard HTML, check for valid HTML content
    - Call `BlueprintScanner.ProcessMarketHtml()`
    - Call `MarketBlueprintImporter.Import()`
    - Display results summary via MessageBox
    - Fire `PlayerContext.OnBlueprintDataChanged()` if player blueprints changed
    - Refresh form after import
    - _Requirements: 1, 10, 11_

- [~] 8. Final checkpoint — Ensure full solution builds and all tests pass

## Notes

- No FsCheck — all tests use plain NUnit
- Build with: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Test with: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
- Old-style csproj requires explicit `<Compile Include>` entries for new `.cs` files
- MarketSample HTML test files are already in TestData/ and csproj
- The `ProcessMarketHtml` return type change will require updating existing market tests
