# Requirements Document

## Introduction

The unit test suite currently takes 84 seconds to run 1940 tests. Of that, 53 seconds (64%) is concentrated in 19 slow tests that each take over 1 second. The root causes are:

1. **Excessive property-test iterations** - FsCheck property tests use MaxTest = 100 or MaxTest = 200, and hand-rolled iteration loops run 100 times. These tests exercise deterministic logic where 25 iterations provides equivalent confidence.
2. **Repeated disk I/O and JSON deserialization** - Approximately 60 test fixtures call EmpireContext.Reset() + EmpireContext.GetInstance() in [SetUp], each time reading BaselineData.json (856 KB) and PlayerData.json (249 KB) from disk and deserializing them via Newtonsoft.Json. This ~1.1 MB of I/O and deserialization per context load, repeated hundreds of times across the suite, dominates execution time.

The target is to reduce total suite time from 84 seconds to approximately 30-35 seconds.
## Glossary

- **Test_Suite**: The NUnit test project OE2EmpireTracker.Tests containing all unit and property tests
- **FsCheck_Property_Test**: A test method annotated with [FsCheck.NUnit.Property(MaxTest = N)] that generates random inputs and verifies a property holds for all of them
- **Hand_Rolled_Iteration_Test**: A test method containing a for loop that manually generates random inputs each iteration
- **EmpireContext**: The singleton service that loads and manages shared game data from BaselineData.json
- **PlayerContext**: The singleton service that loads and manages player-specific data from PlayerData.json
- **BaselineRoot**: The deserialized root object from BaselineData.json containing blueprint types, ship classes, tech levels, and other shared game data
- **PlayerRoot**: The deserialized root object from PlayerData.json containing player profiles, blueprints, surveys, colonies, and other player data
- **TestHelper**: The static helper class in the test project that configures file paths and context singletons for tests
- **Context_Load**: The process of calling EmpireContext.Reset() followed by EmpireContext.GetInstance(), which reads JSON from disk, deserializes it, and initializes all data collections
- **MaxTest**: The FsCheck attribute parameter controlling how many random inputs are generated per property test
## Requirements

### Requirement 1: Reduce FsCheck Property Test Iterations

**User Story:** As a developer, I want FsCheck property tests to run with fewer iterations, so that the test suite completes faster without sacrificing defect detection on deterministic logic.

#### Acceptance Criteria

1. WHEN a FsCheck property test has MaxTest = 100, THE Test_Suite SHALL use MaxTest = 25 instead
2. WHEN a FsCheck property test has MaxTest = 200, THE Test_Suite SHALL use MaxTest = 50 instead
3. THE Test_Suite SHALL apply the reduced MaxTest values to all FsCheck property tests across all test files
4. WHEN the reduced-iteration property tests are executed, THE Test_Suite SHALL produce the same pass/fail outcomes as the original iteration counts

### Requirement 2: Reduce Hand-Rolled Iteration Loop Counts

**User Story:** As a developer, I want hand-rolled iteration loops in tests to run fewer iterations, so that tests complete faster while still exercising the same logic paths.

#### Acceptance Criteria

1. WHEN a test method contains a hand-rolled iteration loop with bound 100, THE Test_Suite SHALL change the loop bound to 25
2. THE Test_Suite SHALL apply the reduced loop count to all hand-rolled iteration loops in MainMenuOverhaulTests, ColonyActivityCollectorTests, and ColonyInactivityCollectorTests
3. WHEN the reduced-iteration tests are executed, THE Test_Suite SHALL produce the same pass/fail outcomes as the original loop counts
### Requirement 3: Add Internal Constructor to PlayerContext Accepting Pre-Parsed PlayerRoot

**User Story:** As a developer, I want PlayerContext to accept a pre-parsed PlayerRoot object, so that tests can skip disk I/O and JSON deserialization on every context load.

#### Acceptance Criteria

1. THE PlayerContext SHALL expose an internal constructor that accepts a PlayerRoot parameter
2. WHEN the internal constructor is called with a PlayerRoot, THE PlayerContext SHALL initialize all data collections from the provided PlayerRoot without reading from disk
3. WHEN the internal constructor is called with a PlayerRoot, THE PlayerContext SHALL call the same Init methods (InitPlayerProfiles, InitBlueprints, InitSurveys, InitColonies, and all others) as the private parameterless constructor
4. WHEN the internal constructor is called with a PlayerRoot, THE PlayerContext SHALL set DataVersion from the provided PlayerRoot
5. WHEN the internal constructor is called with a PlayerRoot, THE PlayerContext SHALL run MigrateOwnerUUIDs, CleanupOrphanedData, and RestoreCurrentPlayer the same as the private constructor
6. THE PlayerContext SHALL remain accessible to the test project via [InternalsVisibleTo]

### Requirement 4: Add Internal Constructor to EmpireContext Accepting Pre-Parsed Roots

**User Story:** As a developer, I want EmpireContext to accept pre-parsed BaselineRoot and PlayerRoot objects, so that tests can skip all disk I/O and JSON deserialization.

#### Acceptance Criteria

1. THE EmpireContext SHALL expose an internal constructor that accepts a BaselineRoot parameter and a PlayerRoot parameter
2. WHEN the internal constructor is called, THE EmpireContext SHALL initialize all baseline data collections from the provided BaselineRoot without reading BaselineData.json from disk
3. WHEN the internal constructor is called, THE EmpireContext SHALL create the PlayerContext singleton using the provided PlayerRoot (via the PlayerContext internal constructor) without reading PlayerData.json from disk
4. WHEN the internal constructor is called, THE EmpireContext SHALL call the same Init methods (InitBlueprintTypes, InitShipClasses, InitTechLevels, and all others) as the private parameterless constructor
5. WHEN the internal constructor is called, THE EmpireContext SHALL run MigrationRunner.Run the same as the private constructor
6. WHEN the internal constructor is called, THE EmpireContext SHALL set DataVersion and GameConstants from the provided BaselineRoot
7. THE EmpireContext SHALL remain accessible to the test project via [InternalsVisibleTo]
### Requirement 5: TestHelper Caches Pre-Parsed Root Objects

**User Story:** As a developer, I want TestHelper to deserialize JSON files once and cache the results, so that all test fixtures reuse the same pre-parsed objects instead of re-reading from disk.

#### Acceptance Criteria

1. THE TestHelper SHALL deserialize BaselineData.json into a static BaselineRoot field on first access
2. THE TestHelper SHALL deserialize PlayerData.json into a static PlayerRoot field on first access
3. WHEN a test fixture requests a context reset, THE TestHelper SHALL provide deep copies of the cached root objects to the context constructors
4. THE TestHelper SHALL expose a method (e.g., ResetWithCachedData) that resets both singletons and initializes them from the cached root objects
5. WHEN ResetWithCachedData is called, THE TestHelper SHALL produce EmpireContext and PlayerContext instances with identical state to the current Reset() + SetAllFilePaths() + GetInstance() pattern

### Requirement 6: Test Fixtures Use In-Memory Context Loading

**User Story:** As a developer, I want test fixtures to use in-memory context loading, so that the test suite avoids repeated disk I/O.

#### Acceptance Criteria

1. WHEN a test fixture currently calls EmpireContext.Reset() + TestHelper.SetAllFilePaths() + EmpireContext.GetInstance(), THE Test_Suite SHALL replace this with TestHelper.ResetWithCachedData() or equivalent
2. THE Test_Suite SHALL migrate all test fixtures that load contexts from disk to use the in-memory path
3. IF a test fixture intentionally tests file-based loading behavior, THEN THE Test_Suite SHALL leave that fixture using the disk-based path
4. WHEN all fixtures are migrated, THE Test_Suite SHALL produce the same pass/fail outcomes as before migration

### Requirement 7: Deep Copy of Cached Root Objects

**User Story:** As a developer, I want each test to receive an independent copy of the cached data, so that mutations in one test do not affect other tests.

#### Acceptance Criteria

1. WHEN TestHelper provides cached root objects to a context constructor, THE TestHelper SHALL provide a deep copy so that mutations during the test do not affect the cached originals
2. THE deep copy mechanism SHALL produce PlayerRoot and BaselineRoot objects with all fields and nested collections identical to the originals
3. WHEN two tests run sequentially using ResetWithCachedData, THE second test SHALL receive data unaffected by any mutations the first test performed

### Requirement 8: Behavioral Equivalence

**User Story:** As a developer, I want the optimized test infrastructure to be behaviorally equivalent to the original, so that no tests break due to the optimization.

#### Acceptance Criteria

1. WHEN the full test suite is executed after all optimizations, THE Test_Suite SHALL have zero test failures that were not present before the optimizations
2. THE Test_Suite SHALL maintain the same test count (no tests removed or skipped)
3. WHEN a context is loaded via the in-memory path, THE EmpireContext and PlayerContext SHALL contain the same data collections, counts, and values as when loaded via the disk path

### Requirement 9: Performance Target

**User Story:** As a developer, I want the test suite to complete in approximately 30-35 seconds, so that the feedback loop during development is faster.

#### Acceptance Criteria

1. WHEN the full test suite is executed after all optimizations, THE Test_Suite SHALL complete in 35 seconds or less
2. THE Test_Suite SHALL reduce the time spent in the 19 slowest tests (currently 53 seconds) by at least 50%
