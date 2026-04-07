# Implementation Plan: Directory Restructure

## Overview

Reorganize `Baseline/` and `Data/` into `Models/`, `Services/`, `Parsers/`, and `Persistence/` across both the main project and test project. Each batch is an atomic commit with build + test verification. Use `smartRelocate` for file moves (handles import updates), then manually fix namespace declarations and csproj Compile Include entries.

## Tasks

- [x] 1. Batch 1a — Move Data/ → Models/ (20 files, main project)
  - [x] 1.1 Move all 20 Data/ files to Models/ using smartRelocate
    - Files: Blueprint, Commodity, CommodityGroup, CommodityIndustry, CountDownTime, Item, ItemBag, ItemProperty, ItemType, LockTracking, PlayerProfile, PlayerRank, PlayerSkill, PropertyBag, Resource, ResourceClass, ResourceGroup, ResourcePurity, SubResource, WorkerDetail
    - For each file: `smartRelocate` from `OE2EmpireTracker/Data/{File}.cs` to `OE2EmpireTracker/Models/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Data` to `OE2EmpireTracker.Models`
    - After each move: update csproj Compile Include from `Data\{File}.cs` to `Models\{File}.cs`
    - _Requirements: 1.1, 1.3, 1.4, 1.5, 9.1, 9.2_

  - [x] 1.2 Verify Batch 1a — build and test
    - Run `getDiagnostics` on all moved files and files with updated usings
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll`
    - Verify 746 tests pass
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [-] 2. Batch 1b — Move Baseline/ data types → Models/ (13 files, main project)
  - [x] 2.1 Move 13 Baseline/ data-type files to Models/ using smartRelocate
    - Files: Colony, ColonyStructure, ColonyStructureStatus, ColonyWorker, CommodityRequested, DeliveryRoute, DeliveryPlan, Survey, ShipClass, TechLevel, BlueprintType, UIPreferences, IColonyStructureWorkers
    - For each file: `smartRelocate` from `OE2EmpireTracker/Baseline/{File}.cs` to `OE2EmpireTracker/Models/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Baseline` to `OE2EmpireTracker.Models`
    - After each move: update csproj Compile Include from `Baseline\{File}.cs` to `Models\{File}.cs`
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 9.1, 9.2_

  - [-] 2.2 Verify Batch 1b — build and test
    - Run `getDiagnostics` on all moved files and files with updated usings
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 3. Batch 2 — Move Baseline/ services → Services/ (11 files, main project)
  - [~] 3.1 Move 11 Baseline/ service files to Services/ using smartRelocate
    - Files: EmpireContext, PlayerContext, BackgroundProcessor, ColonyStatusCalculator, ColonyActivityCollector, ColonyBuildEligibility, ColonyBootstrap, BuildOrderOptimizer, BuildTimeCalculator, DeliveryFulfillment, PreferencesStore
    - For each file: `smartRelocate` from `OE2EmpireTracker/Baseline/{File}.cs` to `OE2EmpireTracker/Services/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Baseline` to `OE2EmpireTracker.Services`
    - After each move: update csproj Compile Include from `Baseline\{File}.cs` to `Services\{File}.cs`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 9.1, 9.2_

  - [~] 3.2 Verify Batch 2 — build and test
    - Run `getDiagnostics` on all moved files and files with updated usings
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 4. Batch 3 — Move Baseline/ parsers → Parsers/ (2 files, main project)
  - [~] 4.1 Move 2 Baseline/ parser files to Parsers/ using smartRelocate
    - Files: ColonyParser, SurveyParser
    - For each file: `smartRelocate` from `OE2EmpireTracker/Baseline/{File}.cs` to `OE2EmpireTracker/Parsers/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Baseline` to `OE2EmpireTracker.Parsers`
    - After each move: update csproj Compile Include from `Baseline\{File}.cs` to `Parsers\{File}.cs`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 9.1, 9.2_

  - [~] 4.2 Verify Batch 3 — build and test
    - Run `getDiagnostics` on all moved files and files with updated usings
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 5. Batch 4 — Move Baseline/ persistence → Persistence/ (3 files, main project)
  - [~] 5.1 Move 3 Baseline/ persistence files to Persistence/ using smartRelocate
    - Files: SafeFileWriter, WindowStateHelper, BoundsValidator
    - For each file: `smartRelocate` from `OE2EmpireTracker/Baseline/{File}.cs` to `OE2EmpireTracker/Persistence/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Baseline` to `OE2EmpireTracker.Persistence`
    - After each move: update csproj Compile Include from `Baseline\{File}.cs` to `Persistence\{File}.cs`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 9.1, 9.2_

  - [~] 5.2 Verify Batch 4 — build and test
    - Run `getDiagnostics` on all moved files and files with updated usings
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass
    - Commit with descriptive message
    - After this batch, `Baseline/` in main project should be empty
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [~] 6. Checkpoint — Main project restructure complete
  - Ensure all tests pass, ask the user if questions arise.
  - At this point all 49 main project files have been moved. `Data/` is empty (since Batch 1a) and `Baseline/` is empty (since Batch 4). Verify no stale `using OE2EmpireTracker.Data;` or `using OE2EmpireTracker.Baseline;` remain in main project files.

- [ ] 7. Batch 5 — Test project restructure (mirror all moves)
  - [~] 7.1 Move 15 Data/ test files to Models/ in test project
    - Files: BlueprintTests, CommodityTests, CountDownTimeTests, ItemBagTests, ItemTests, ItemTypeTests, LockTrackingTests, PlayerProfileTests, PropertyBagTests, ResourceClassTests, ResourceGroupTests, ResourcePurityTests, ResourceTests, WorkerDetailTests, WorkerTypeInfoTests
    - For each file: `smartRelocate` from `OE2EmpireTracker.Tests/Data/{File}.cs` to `OE2EmpireTracker.Tests/Models/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Tests.Data` to `OE2EmpireTracker.Tests.Models`
    - After each move: update test csproj Compile Include from `Data\{File}.cs` to `Models\{File}.cs`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 9.3, 9.4_

  - [~] 7.2 Move 8 Baseline/ model-test files to Models/ in test project
    - Files: BlueprintTypeTests, ColonyStructureTests, ColonyBuildCompletionTests, ColonyProcessingTests, DeliveryPlanTests, DeliveryRouteTests, SurveyTests, UIPreferencesTests
    - For each file: `smartRelocate` from `OE2EmpireTracker.Tests/Baseline/{File}.cs` to `OE2EmpireTracker.Tests/Models/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Tests.Baseline` to `OE2EmpireTracker.Tests.Models`
    - After each move: update test csproj Compile Include from `Baseline\{File}.cs` to `Models\{File}.cs`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 9.3, 9.4_

  - [~] 7.3 Move 10 Baseline/ service-test files to Services/ in test project
    - Files: BackgroundProcessorTests, BuildTimeCalculatorTests, ColonyActivityCollectorTests, ColonyBuildEligibilityTests, ColonyStatusCalculatorTests, ContextFilePathTests, DeliveryFulfillmentTests, DeliveryPlanViewModelTests, DeliveryRouteViewModelTests, PreferencesStoreTests
    - For each file: `smartRelocate` from `OE2EmpireTracker.Tests/Baseline/{File}.cs` to `OE2EmpireTracker.Tests/Services/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Tests.Baseline` to `OE2EmpireTracker.Tests.Services`
    - After each move: update test csproj Compile Include from `Baseline\{File}.cs` to `Services\{File}.cs`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 9.3, 9.4_

  - [~] 7.4 Move 2 Baseline/ parser-test files to Parsers/ in test project
    - Files: ColonyParserTests, SurveyParserTests
    - For each file: `smartRelocate` from `OE2EmpireTracker.Tests/Baseline/{File}.cs` to `OE2EmpireTracker.Tests/Parsers/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Tests.Baseline` to `OE2EmpireTracker.Tests.Parsers`
    - After each move: update test csproj Compile Include from `Baseline\{File}.cs` to `Parsers\{File}.cs`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 9.3, 9.4_

  - [~] 7.5 Move 2 Baseline/ persistence-test files to Persistence/ in test project
    - Files: SafeFileWriterTests, BoundsValidatorTests
    - For each file: `smartRelocate` from `OE2EmpireTracker.Tests/Baseline/{File}.cs` to `OE2EmpireTracker.Tests/Persistence/{File}.cs`
    - After each move: update namespace from `OE2EmpireTracker.Tests.Baseline` to `OE2EmpireTracker.Tests.Persistence`
    - After each move: update test csproj Compile Include from `Baseline\{File}.cs` to `Persistence\{File}.cs`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 9.3, 9.4_

  - [~] 7.6 Verify Batch 5 — build and test
    - Run `getDiagnostics` on all moved test files
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass (no tests lost to namespace/discovery issues)
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 8. Batch 6 — Cleanup
  - [~] 8.1 Delete empty legacy directories
    - Delete `OE2EmpireTracker/Baseline/` (should be empty after Batches 1b–4)
    - Delete `OE2EmpireTracker/Data/` (should be empty after Batch 1a)
    - Delete `OE2EmpireTracker.Tests/Baseline/` (should be empty after Batch 5)
    - Delete `OE2EmpireTracker.Tests/Data/` (should be empty after Batch 5)
    - Verify no Compile Include entries reference `Baseline\` or `Data\` in either csproj
    - _Requirements: 5.1, 5.2, 5.3, 6.5, 6.6_

  - [~] 8.2 Update steering docs to reflect new structure
    - Update `.kiro/steering/structure.md` to replace `Baseline/` and `Data/` with `Models/`, `Services/`, `Parsers/`, `Persistence/`
    - _Requirements: 5.1, 5.2_

  - [~] 8.3 Verify Batch 6 — final build and test
    - Build: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
    - Test: `vstest.console` against test DLL
    - Verify 746 tests pass
    - Commit with descriptive message
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [~] 9. Final checkpoint — Restructure complete
  - Ensure all tests pass, ask the user if questions arise.
  - Verify: no `Baseline/` or `Data/` directories remain, no stale csproj paths, no stale using statements, all 746 tests pass.
  - _Requirements: 5.3, 7.1, 7.2, 7.3, 7.4, 7.5_

## Notes

- Use `smartRelocate` for every file move — it handles `using` statement updates in consuming files automatically
- After each `smartRelocate`: manually fix (1) namespace declaration in moved file, (2) csproj Compile Include entry
- Do NOT use `dotnet test` — use `vstest.console` against the built test DLL
- Build with: `msbuild OE2EmpireTracker.sln /p:Configuration=Debug`
- Directories that must NOT be touched: `Constants/`, `Controls/`, `ViewModels/`, `Forms/`, `Properties/` (Requirement 7)
- Each batch is committed separately for easy rollback (Requirement 8.4)
