# Implementation Plan: Baseline Data Stability

## Overview

Stabilize BaselineData.json for multi-user distribution: deterministic UUIDs for global blueprints, versioned migration framework, idempotent rename table, externalize game data from code to JSON, and replace double with decimal for game data numeric types. All property tests use FsCheck 2.16.6 with `[FsCheck.NUnit.Property(MaxTest = 100)]`.

## Tasks

- [-] 1. Create DeterministicUUID and RemapUUID utilities
  - [-] 1.1 Create `OE2EmpireTracker/Services/Migration/DeterministicUUID.cs` with UUID v5 generation
    - Namespace UUID: `e0058083-0f64-b398-ed53-762f7d8b8eb2`
    - `Generate(string name, int evolution, string blueprintType, int cls, string techLevel)` → string UUID
    - `Generate(Blueprint bp)` → string UUID (convenience overload)
    - Private `GenerateV5(Guid namespaceId, string name)` implementing RFC 4122 UUID v5
    - Add `<Compile Include="Services\Migration\DeterministicUUID.cs" />` to csproj
    - _Requirements: 1.1, 1.3, 1.4_

  - [~] 1.2 Create `OE2EmpireTracker/Services/Migration/RemapUUID.cs` with generic reference walker
    - `Remap(EmpireContext ec, PlayerContext pc, string oldUUID, string newUUID)` — walks all 5 reference fields
    - Add `<Compile Include="Services\Migration\RemapUUID.cs" />` to csproj
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [~] 1.3 Write property test: UUID v5 determinism (Property 1)
    - Create `OE2EmpireTracker.Tests/Services/Migration/DeterministicUUIDPropertyTests.cs`
    - Generate random dedup key fields, verify same inputs produce same UUID, different inputs produce different UUIDs
    - Add `<Compile Include>` to test csproj
    - _Requirements: 1.1, 1.3, 1.4_

  - [~] 1.4 Write property test: RemapUUID completeness (Property 2)
    - Create `OE2EmpireTracker.Tests/Services/Migration/RemapUUIDPropertyTests.cs`
    - Generate random data model state with UUID references, call Remap, verify no old UUID remains
    - Add `<Compile Include>` to test csproj
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

- [~] 2. Checkpoint — Ensure all tests pass

- [ ] 3. Create migration framework
  - [~] 3.1 Add `DataVersion` field to `BaselineRoot` and `PlayerRoot`
    - Default value 0, serialized to JSON
    - Add `DataVersion` property to `EmpireContext` and `PlayerContext`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [~] 3.2 Create `OE2EmpireTracker/Services/Migration/RenameTable.cs` with idempotent rename entries
    - `RenameEntry` class with OldName, NewName, Evolution, BluePrintType, Class, TechLevel
    - `Apply(EmpireContext, PlayerContext)` — processes all entries, computes old/new hashes, remaps
    - Empty initial list (no renames yet)
    - Add `<Compile Include>` to csproj
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [~] 3.3 Create `OE2EmpireTracker/Services/Migration/MigrationRunner.cs` with sequential executor
    - `CurrentVersion` constant
    - `Run(EmpireContext, PlayerContext)` — applies renames, runs versioned migrations, applies renames again
    - Dictionary of version → migration action
    - Add `<Compile Include>` to csproj
    - _Requirements: 3.1, 3.2, 3.3, 3.6_

  - [~] 3.4 Create `OE2EmpireTracker/Services/Migration/Migrations/Migration001_DeterministicUUIDs.cs`
    - Scans global blueprints, computes deterministic UUID, stores LegacyUUID, calls RemapUUID
    - Add `<Compile Include>` to csproj
    - _Requirements: 3.4, 3.5, 6.1, 6.2_

  - [~] 3.5 Add `LegacyUUID` field to `Blueprint` model
    - Nullable string, default null, serialized to JSON
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [~] 3.6 Wire `MigrationRunner.Run` into app startup (after loading both contexts)
    - Call in `EmpireContext.loadContext` or `MainWindow` after both contexts are loaded
    - Persist both contexts if any changes were made
    - _Requirements: 3.6, 11.1, 11.2, 11.3, 11.4_

  - [~] 3.7 Write property test: Rename idempotency (Property 3)
    - Generate random rename entries and data model state, apply twice, verify same result
    - _Requirements: 5.6_

  - [~] 3.8 Write property test: Migration version gating (Property 4)
    - Generate random DataVersion values, verify correct migrations run
    - _Requirements: 3.1, 3.2, 3.3_

  - [~] 3.9 Write property test: LegacyUUID preservation (Property 5)
    - Generate random blueprints, run migration, verify LegacyUUID equals original UUID and doesn't change on re-save
    - _Requirements: 6.2, 6.3_

- [~] 4. Checkpoint — Ensure all tests pass

- [ ] 5. Update blueprint creation to use deterministic UUIDs
  - [~] 5.1 Update `MarketBlueprintImporter.Import` to use `DeterministicUUID.Generate` for global blueprints (Evo 0)
    - _Requirements: 1.1, 1.2, 12.1, 12.2_

  - [~] 5.2 Update `FormBlueprint.cmdImport_Click` to use `DeterministicUUID.Generate` for global blueprints
    - _Requirements: 1.1, 1.2, 12.1, 12.2_

  - [~] 5.3 Update `FormBlueprint.btnSave_Click` / `BlueprintViewModel.Save` to use `DeterministicUUID.Generate` for global blueprints
    - _Requirements: 1.1, 1.2, 12.1, 12.2_

- [~] 6. Checkpoint — Ensure all tests pass

- [ ] 7. Externalize GameConstants to BaselineData.json
  - [~] 7.1 Create `OE2EmpireTracker/Models/BaselineGameConstants.cs` with 5 fields
    - RefiningBaseRate (int, default 25), CommoditiesPerCycle (int, default 10), CommodityCycleSeconds (long, default 600), StructureCap (int, default 65), WorkerVolume (decimal, default 50)
    - Add `<Compile Include>` to csproj
    - _Requirements: 7.1_

  - [~] 7.2 Add `GameConstants` property to `BaselineRoot` and `EmpireContext`
    - Load from JSON with fallback to defaults if missing
    - _Requirements: 7.1, 7.2, 7.3_

  - [~] 7.3 Refactor `GameConstants.cs` — change 5 game-derived `const` fields to `static` properties reading from `EmpireContext.getInstance().GameConstants`
    - Keep internal constants (SecondsPerHour, PropBuilt, etc.) as `const`
    - _Requirements: 7.2, 7.3, 7.4_

  - [~] 7.4 Add `GameConstants` section to BaselineData.json (main + test)
    - _Requirements: 7.1_

- [ ] 8. Externalize Commodities to BaselineData.json
  - [~] 8.1 Add `Commodity[]` array to `BaselineRoot`
    - _Requirements: 8.1_

  - [~] 8.2 Update `EmpireContext` to load commodities from BaselineRoot with fallback to hardcoded list
    - _Requirements: 8.2, 8.3, 8.4_

  - [~] 8.3 Serialize all 209 commodities into BaselineData.json (main + test)
    - _Requirements: 8.3_

  - [~] 8.4 Update `Commodity.cs` — keep model class, change static initializer to fallback only
    - _Requirements: 8.2, 8.4_

- [ ] 9. Externalize RefiningRecipes to BaselineData.json
  - [~] 9.1 Add `RefiningRecipe[]` array to `BaselineRoot`
    - _Requirements: 9.1_

  - [~] 9.2 Update `EmpireContext` to load recipes from BaselineRoot with fallback to hardcoded list
    - _Requirements: 9.2, 9.3, 9.4_

  - [~] 9.3 Serialize all 6 recipes into BaselineData.json (main + test)
    - _Requirements: 9.3_

  - [~] 9.4 Update `RefiningRecipes.cs` — change static list to load from EmpireContext with fallback
    - _Requirements: 9.2, 9.4_

- [ ] 10. Externalize ResearchTimeLookup to BaselineData.json
  - [~] 10.1 Create `OE2EmpireTracker/Models/ResearchTimeEntry.cs` with Evolution and ResearchTimeSeconds
    - Add `<Compile Include>` to csproj
    - _Requirements: 10.1_

  - [~] 10.2 Add `ResearchTime[]` array to `BaselineRoot`
    - _Requirements: 10.1_

  - [~] 10.3 Update `EmpireContext` to load research times from BaselineRoot with fallback
    - _Requirements: 10.2, 10.3, 10.4_

  - [~] 10.4 Serialize all 15 entries into BaselineData.json (main + test)
    - _Requirements: 10.3_

  - [~] 10.5 Update `ResearchTimeLookup.cs` — change static dictionary to load from EmpireContext with fallback
    - _Requirements: 10.2, 10.4_

- [~] 11. Checkpoint — Ensure all tests pass

- [ ] 12. Replace double with decimal for game data numeric types
  - [~] 12.1 Update `ColonyStructureStatus` — change all 10 numeric fields from double to decimal
    - _Requirements: 13.1_

  - [~] 12.2 Update `Item.Volume` from double to decimal
    - _Requirements: 13.2_

  - [~] 12.3 Update `ItemProperty.BaseValue` and `AdjustedValue` from double to decimal
    - _Requirements: 13.3_

  - [~] 12.4 Update `PropertyBag` — rename `getDouble` to `getDecimal`, update `setProperty(double)` to `setProperty(decimal)`
    - _Requirements: 13.4_

  - [~] 12.5 Update `ColonyStatusCalculator` — all accumulator variables, `GetBlueprintDouble` → `GetBlueprintDecimal`, `AppendStatus` parameters
    - _Requirements: 13.5_

  - [~] 12.6 Update `Colony.ProcessColony` — extractionMultiplier, refiningMultiplier, quantity calculations
    - _Requirements: 13.5_

  - [~] 12.7 Update `ColonyBootstrap` — miningRate, adjustedRate, refinedOutput, BestResourceEntry fields
    - _Requirements: 13.5_

  - [~] 12.8 Update `ColonyInactivityCollector` — GetMiningOutputRate, totalMiningOutput, totalConsumption, supply, available
    - _Requirements: 13.5_

  - [~] 12.9 Update `EvolutionChainService` — percentage calculations, SegmentInfo, EvolutionGraphData series
    - _Requirements: 13.5_

  - [~] 12.10 Update `FormColony.GetItemVolume` and `ColonyStructure` power display
    - _Requirements: 13.5_

  - [~] 12.11 Update all callers of `PropertyBag.getDouble` → `getDecimal` across the codebase
    - _Requirements: 13.4, 13.5_

  - [~] 12.12 Write property test: decimal round-trip (Property 6)
    - Serialize decimal values to JSON and back, verify exact equality
    - _Requirements: 13.6, 13.9_

- [~] 13. Checkpoint — Ensure all tests pass

- [~] 14. Final checkpoint — Full build, all tests pass, verify migration on test data
  - Build and run all tests
  - Verify BaselineData.json test data loads and migrates correctly
  - Verify PlayerData.json test data loads and migrates correctly
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- New `.cs` files require `<Compile Include="...">` entries in the old-style `.csproj`
- New files under `Services/Migration/` need the folder created and csproj entries added
- Build with MSBuild: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug`
- Run tests with vstest.console: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll`
- The double→decimal conversion (task 12) touches many files but is mechanical — change type, fix literal suffixes (add `m`), update method names
- Commodity serialization (task 8.3) is the largest data task — 209 entries to serialize from code to JSON
