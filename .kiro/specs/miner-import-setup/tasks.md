# Implementation Plan: Miner Import Setup

## Overview

Automate mining rig and refinery setup during colony import/reimport. Adds survey assignment, default survey creation, warehouse resource seeding, and timer start for miners and refineries.

## Tasks

- [x] 1. Extract maxRate from game JSON during parsing
  - [x] 1.1 Modify `ParseBuilding` to extract `maxRate` from building JSON and return it alongside the structure (via out parameter or by collecting in the caller)
    - _Requirements: 6.7_
  - [x] 1.2 Build a `Dictionary<string, decimal>` mapping structure UUID → maxRate in `ParseColonyBuildingsFromJson` during the merge loop
    - _Requirements: 6.7_
  - [x] 1.3 Write unit test: ParseBuilding extracts maxRate from game JSON correctly
    - _Validates: Requirements 6.7_

- [x] 2. Add DeterministicUUID.GenerateDefaultSurvey
  - [x] 2.1 Add `DefaultSurveyNamespace` GUID and `GenerateDefaultSurvey(ownerUUID, planetName, systemName)` to DeterministicUUID.cs
    - _Requirements: 6.1_
  - [x] 2.2 Write property test: deterministic default survey UUID round-trip (same inputs → same UUID, different inputs → different UUID)
    - _Validates: Requirements 6.1_

- [x] 3. Implement MinerSetupHelper.FindBestSurvey
  - [x] 3.1 Create `MinerSetupHelper.cs` with `FindBestSurvey` method — searches real surveys by planet/resource/purity, selects closest match to maxRate (or highest amount if maxRate=0)
    - Add `<Compile Include>` entry to OE2EmpireTracker.csproj
    - _Requirements: 1.1, 1.2, 1.3, 1.6_
  - [x] 3.2 Write unit tests for FindBestSurvey: closest match, highest when maxRate=0, no match returns null, excludes default surveys
    - _Validates: Requirements 1.1, 1.2, 1.3, 1.6_

- [x] 4. Implement MinerSetupHelper.CreateOrUpdateDefaultSurvey
  - [x] 4.1 Add `CreateOrUpdateDefaultSurvey` method — creates/updates default survey with deterministic UUID, SurveyID="DEFAULT", resource from maxRate
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  - [x] 4.2 Write unit tests: creates new default survey, updates existing, sets SurveyID="DEFAULT", NickName null, amount="0" when maxRate=0
    - _Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 5. Implement MinerSetupHelper.AssignSurvey
  - [x] 5.1 Add `AssignSurvey` method — handles survey selection logic (preserve valid real, upgrade default→real, find best, fallback to default)
    - _Requirements: 1.4, 1.5, 3.1, 3.2, 3.3_
  - [x] 5.2 Write unit tests: preserves valid real survey, upgrades default to real, assigns best survey, falls back to default when no real exists, handles deleted survey
    - _Validates: Requirements 1.4, 1.5, 3.1, 3.2, 3.3_

- [x] 6. Implement EnsureWarehouseResource utility
  - [x] 6.1 Add `EnsureWarehouseResource` static method (shared between miner and refinery helpers) — creates warehouse resource record with qty 0 if missing
    - _Requirements: 7.1, 7.2, 7.3_
  - [x] 6.2 Write unit tests: creates missing resource, does not overwrite existing, handles null/empty inputs
    - _Validates: Requirements 7.1, 7.2, 7.3_

- [x] 7. Implement MinerSetupHelper.SetupTimer
  - [x] 7.1 Add `SetupTimer` method — creates repeating timer aligned to next hour boundary if maxRate > 0 and no existing timer
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  - [x] 7.2 Write unit tests: creates timer when maxRate > 0, skips when maxRate = 0, preserves existing timer, timer is repeating at SecondsPerHour
    - _Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 8. Implement MinerSetupHelper.CleanupDefaultSurvey
  - [x] 8.1 Add `CleanupDefaultSurvey` method — removes unmined resources from default survey, deletes survey if empty
    - _Requirements: 6.9_
  - [x] 8.2 Write unit tests: removes stale resources, deletes empty default survey, preserves active resources
    - _Validates: Requirements 6.9_

- [-] 9. Implement MinerSetupHelper.SetupMiners orchestrator
  - [-] 9.1 Add `SetupMiners(colony, empireContext, maxRates)` method — iterates structures, identifies mining rigs, looks up maxRate from dictionary, calls AssignSurvey + EnsureWarehouseResource + SetupTimer + CleanupDefaultSurvey
    - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 5.4, 5.5_
  - [-] 9.2 Write integration test: full miner setup with real survey, default survey fallback, timer start, warehouse seeding
    - _Validates: Requirements 4.1, 4.2, 4.3_

- [ ] 10. Implement RefinerySetupHelper
  - [~] 10.1 Create `RefinerySetupHelper.cs` with `SetupRefineries` method — iterates refineries, ensures warehouse resource, starts timer
    - Add `<Compile Include>` entry to OE2EmpireTracker.csproj
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
  - [~] 10.2 Write unit tests: ensures warehouse resource for refinery, starts timer, preserves existing timer, skips if not built/online
    - _Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 11. Wire up helpers in ParseColonyBuildingsFromJson
  - [~] 11.1 Add calls to `MinerSetupHelper.SetupMiners(colony, empireContext, maxRates)` and `RefinerySetupHelper.SetupRefineries(colony, empireContext)` after the merge loop in `ParseColonyBuildingsFromJson`
    - _Requirements: 4.3, 8.5_

- [ ] 12. Update csproj files and verify build
  - [~] 12.1 Ensure OE2EmpireTracker.csproj has `<Compile Include>` for MinerSetupHelper.cs and RefinerySetupHelper.cs
  - [~] 12.2 Ensure OE2EmpireTracker.Tests.csproj has `<Compile Include>` entries for all new test files

- [ ] 13. Final checkpoint — Build and run all tests
  - Build solution and run all tests. Ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- New .cs files require `<Compile Include>` entries in the old-style csproj files
- Do NOT use `dotnet test` — use vstest.console against the built test DLL
- Do NOT use `semanticRename` — it doesn't work with this project type
- Tasks are ordered so each builds on the previous — no forward dependencies
- MinerSetupHelper and RefinerySetupHelper are static classes in the Parsers namespace
- EnsureWarehouseResource is shared between both helpers
