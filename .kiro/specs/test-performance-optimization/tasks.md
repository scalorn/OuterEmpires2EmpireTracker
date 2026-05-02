# Implementation Plan: Test Performance Optimization

## Overview

Optimize the test suite (1940 tests, 84s) to run in ~30-35s by: (1) adding internal constructors to PlayerContext and EmpireContext that accept pre-parsed root objects, (2) adding a caching layer in TestHelper that deserializes JSON once and provides deep copies, and (3) reducing FsCheck and hand-rolled iteration counts by 4x. All changes are in test infrastructure; production behavior is unchanged.

## Tasks

- [ ] 1. Add internal constructor to PlayerContext
  - [x] 1.1 Add internal constructor accepting PlayerRoot parameter to PlayerContext
    - Add `internal PlayerContext(PlayerRoot playerRoot)` that mirrors the private parameterless constructor
    - Set `_instance = this`, call all Init methods (InitPlayerProfiles through InitAsteroids), set DataVersion
    - Call MigrateOwnerUUIDs, CleanupOrphanedData, RestoreCurrentPlayer identically to the private constructor
    - Skip File.ReadAllText and JsonConvert.DeserializeObject — accept pre-parsed root directly
    - Ensure StyleCop compliance (SA1201 member ordering, SA1500 brace placement)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 2. Add internal constructor to EmpireContext
  - [x] 2.1 Add internal constructor accepting BaselineRoot and PlayerRoot parameters to EmpireContext
    - Add `internal EmpireContext(BaselineRoot baselineRoot, PlayerRoot playerRoot)` that mirrors the private parameterless constructor
    - Set `_instance = this`, create PlayerContext via its new internal constructor with the provided PlayerRoot
    - Set DataVersion and GameConstants from baselineRoot
    - Call all Init methods (InitBlueprintTypes through InitGlobalBlueprints)
    - Run MigrationRunner.Run and conditional WriteContext calls identically to the private constructor
    - Ensure StyleCop compliance
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

- [ ] 3. Add caching and ResetWithCachedData to TestHelper
  - [x] 3.1 Add static cache fields and EnsureCachePopulated method to TestHelper
    - Add static fields: `_cachedBaselineRoot`, `_cachedPlayerRoot`, `_cachedBaselineJson`, `_cachedPlayerJson`
    - Implement `EnsureCachePopulated()` that reads and deserializes BaselineData.json and PlayerData.json on first call
    - Use `TestDataPath()` for file paths (already exists in TestHelper)
    - _Requirements: 5.1, 5.2_
  - [x] 3.2 Add DeepCopyBaseline and DeepCopyPlayer methods to TestHelper
    - Implement `DeepCopyBaseline()` that deserializes from `_cachedBaselineJson` (not re-serializing the cached object)
    - Implement `DeepCopyPlayer()` that deserializes from `_cachedPlayerJson`
    - This ensures mutation isolation — cached originals are never touched
    - _Requirements: 7.1, 7.2, 7.3_
  - [x] 3.3 Add ResetWithCachedData method to TestHelper
    - Implement `ResetWithCachedData()` that calls `MigrationRunner.SuppressUI = true`, `EnsureCachePopulated()`, `EmpireContext.Reset()`, creates deep copies, and constructs `new EmpireContext(baselineCopy, playerCopy)`
    - This replaces the Reset() + SetAllFilePaths() + GetInstance() pattern with zero disk I/O
    - _Requirements: 5.3, 5.4, 5.5_

- [x] 4. Checkpoint — Verify internal constructors and caching compile cleanly
  - Build the solution and run getDiagnostics on modified files
  - Ensure zero errors and zero warnings (including StyleCop)
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate test fixtures to use ResetWithCachedData
  - [x] 5.1 Migrate test fixtures that use the standard Reset + SetAllFilePaths + GetInstance pattern
    - Search all test files for the pattern: `EmpireContext.Reset()` followed by `TestHelper.SetAllFilePaths()` followed by `EmpireContext.GetInstance()`
    - Replace with `TestHelper.ResetWithCachedData()`
    - Leave fixtures that intentionally test file-based loading behavior unchanged
    - Verify each migrated fixture's TearDown still calls Reset() appropriately
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  - [x] 5.2 Migrate test fixtures with non-standard context loading patterns
    - Handle fixtures that use `SetEmpireFilePath()` + `EmpireContext.GetInstance()` without `SetAllFilePaths()`
    - Handle fixtures that set custom file paths (e.g., `PlayerContext.FilePath = "nonexistent_player_data.json"`)
    - Leave these using the disk-based path if they intentionally test specific file scenarios
    - _Requirements: 6.2, 6.3_
  - [ ]* 5.3 Write property test for deep copy round-trip fidelity (Property 1)
    - **Property 1: Deep Copy Round-Trip Fidelity**
    - For any cached JSON string, deserializing to produce a deep copy and re-serializing SHALL produce equivalent JSON
    - Use FsCheck with MaxTest = 100 against the actual test data files
    - **Validates: Requirements 7.2**
  - [ ]* 5.4 Write property test for deep copy mutation isolation (Property 2)
    - **Property 2: Deep Copy Mutation Isolation**
    - For any deep copy, mutating the copy's collections SHALL leave the cached original unchanged
    - Test by adding/removing elements from deep-copied PlayerRoot and BaselineRoot, then verifying originals
    - **Validates: Requirements 5.3, 7.1, 7.3**
  - [ ]* 5.5 Write property test for in-memory vs disk behavioral equivalence (Property 3)
    - **Property 3: In-Memory vs Disk Behavioral Equivalence**
    - Constructing contexts via internal constructors SHALL produce identical collection counts, DataVersion, and GameConstants as the disk-based path
    - Compare EmpireContext and PlayerContext state between both construction paths
    - **Validates: Requirements 3.2, 3.4, 4.2, 4.6, 5.5, 8.3**

- [x] 6. Checkpoint — Verify fixture migration
  - Build the solution and ensure zero errors and zero warnings
  - Run the full test suite and confirm all 1940 tests pass
  - Confirm no tests were removed or skipped
  - Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 8.1, 8.2_

- [ ] 7. Reduce FsCheck property test iteration counts
  - [x] 7.1 Change all MaxTest = 100 attributes to MaxTest = 25
    - Mechanically replace `[FsCheck.NUnit.Property(MaxTest = 100)]` with `[FsCheck.NUnit.Property(MaxTest = 25)]` across all test files
    - Verify no MaxTest = 100 attributes remain after replacement
    - _Requirements: 1.1, 1.3_
  - [x] 7.2 Change all MaxTest = 200 attributes to MaxTest = 50
    - Mechanically replace `[FsCheck.NUnit.Property(MaxTest = 200)]` with `[FsCheck.NUnit.Property(MaxTest = 50)]` across all test files
    - Verify no MaxTest = 200 attributes remain after replacement
    - _Requirements: 1.2, 1.3_

- [ ] 8. Reduce hand-rolled iteration loop counts
  - [x] 8.1 Change loop bounds from 100 to 25 in MainMenuOverhaulTests.cs
    - Replace `iteration < 100` with `iteration < 25` in all 5 hand-rolled iteration loops
    - Do NOT change non-iteration loops (inner loops for data generation, assertion loops)
    - _Requirements: 2.1_
  - [x] 8.2 Change loop bounds from 100 to 25 in ColonyActivityCollectorTests.cs
    - Replace `iteration < 100` with `iteration < 25` in all 6 hand-rolled iteration loops
    - Do NOT change non-iteration loops (inner loops for data generation, assertion loops)
    - _Requirements: 2.1, 2.2_
  - [x] 8.3 Change loop bounds from 100 to 25 in ColonyInactivityCollectorTests.cs
    - Replace `iteration < 100` with `iteration < 25` in all 3 hand-rolled iteration loops
    - Do NOT change non-iteration loops (inner loops for data generation, assertion loops)
    - _Requirements: 2.1, 2.2_

- [x] 9. Final checkpoint — Full suite verification
  - Build the solution and ensure zero errors and zero warnings
  - Run the full test suite and confirm all 1940 tests pass with same pass/fail outcomes
  - Grep for `MaxTest = 100` and `MaxTest = 200` to confirm none remain
  - Grep for `iteration < 100` in the three hand-rolled test files to confirm none remain
  - Verify test count is unchanged (no tests removed or skipped)
  - Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 1.4, 2.3, 8.1, 8.2, 8.3, 9.1, 9.2_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The design uses C# throughout — no language selection needed
- All changes are confined to test infrastructure; no production behavior changes
- The existing [InternalsVisibleTo("OE2EmpireTracker.Tests")] attribute in AssemblyInfo.cs provides test project access to internal constructors
