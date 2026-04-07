# Requirements Document

## Introduction

The OE2EmpireTracker project currently uses two top-level directories — `Baseline/` and `Data/` — that have grown into catch-all buckets mixing POCOs, domain logic, singletons, parsers, persistence helpers, and UI utilities. This feature reorganizes the project into a purpose-driven directory structure (`Models/`, `Services/`, `Parsers/`, `Persistence/`) so that each folder has a clear, single responsibility. The reorganization covers both the main project and the mirrored test project, and must preserve full build and test integrity throughout.

## Glossary

- **Main_Project**: The `OE2EmpireTracker` WinForms application project
- **Test_Project**: The `OE2EmpireTracker.Tests` NUnit test project
- **Csproj**: The old-style MSBuild `.csproj` file that requires explicit `<Compile Include="...">` entries for every source file
- **Models_Directory**: The new `Models/` folder for all POCOs, data types, enums, and interfaces
- **Services_Directory**: The new `Services/` folder for singletons, processing logic, and business rules
- **Parsers_Directory**: The new `Parsers/` folder for HTML and data import classes
- **Persistence_Directory**: The new `Persistence/` folder for file I/O and window-state helpers
- **Batch**: A logically grouped set of file moves that are committed together as one atomic unit
- **Namespace**: The C# namespace declaration in each source file, which must match the new directory path
- **Compile_Include**: An XML element in the old-style csproj that registers a source file for compilation

## Requirements

### Requirement 1: Consolidate POCOs and Data Types into Models Directory

**User Story:** As a developer, I want all POCOs, data types, enums, and interfaces consolidated into a single `Models/` directory, so that I can find any data structure in one predictable location.

#### Acceptance Criteria

1. WHEN the restructure is complete, THE Main_Project SHALL contain a `Models/` directory holding all files currently in `Data/` (Blueprint, Commodity, CommodityGroup, CommodityIndustry, CountDownTime, Item, ItemBag, ItemProperty, ItemType, LockTracking, PlayerProfile, PlayerRank, PlayerSkill, PropertyBag, Resource, ResourceClass, ResourceGroup, ResourcePurity, SubResource, WorkerDetail)
2. WHEN the restructure is complete, THE Models_Directory SHALL also contain the following data-type files moved from `Baseline/`: Colony, ColonyStructure, ColonyStructureStatus, ColonyWorker, CommodityRequested, DeliveryRoute, DeliveryPlan, Survey, ShipClass, TechLevel, BlueprintType, UIPreferences, IColonyStructureWorkers
3. WHEN a file is moved to Models_Directory, THE Main_Project SHALL update the file's namespace to `OE2EmpireTracker.Models`
4. WHEN a file is moved to Models_Directory, THE Csproj SHALL update the corresponding Compile_Include path to reference the new `Models\` location
5. WHEN a namespace changes, THE Main_Project SHALL update all `using` statements across the entire codebase that reference the old namespace

### Requirement 2: Extract Services into Services Directory

**User Story:** As a developer, I want singletons, processing logic, and business-rule classes in a dedicated `Services/` directory, so that domain logic is clearly separated from data types.

#### Acceptance Criteria

1. WHEN the restructure is complete, THE Main_Project SHALL contain a `Services/` directory holding: EmpireContext, PlayerContext, BackgroundProcessor, ColonyStatusCalculator, ColonyActivityCollector, ColonyBuildEligibility, ColonyBootstrap, BuildOrderOptimizer, BuildTimeCalculator, DeliveryFulfillment, PreferencesStore
2. WHEN a file is moved to Services_Directory, THE Main_Project SHALL update the file's namespace to `OE2EmpireTracker.Services`
3. WHEN a file is moved to Services_Directory, THE Csproj SHALL update the corresponding Compile_Include path to reference the new `Services\` location
4. WHEN a namespace changes, THE Main_Project SHALL update all `using` statements across the entire codebase that reference the old namespace

### Requirement 3: Extract Parsers into Parsers Directory

**User Story:** As a developer, I want HTML and data-import parsers in a dedicated `Parsers/` directory, so that parsing logic is easy to locate and maintain.

#### Acceptance Criteria

1. WHEN the restructure is complete, THE Main_Project SHALL contain a `Parsers/` directory holding: ColonyParser, SurveyParser
2. WHEN a file is moved to Parsers_Directory, THE Main_Project SHALL update the file's namespace to `OE2EmpireTracker.Parsers`
3. WHEN a file is moved to Parsers_Directory, THE Csproj SHALL update the corresponding Compile_Include path to reference the new `Parsers\` location
4. WHEN a namespace changes, THE Main_Project SHALL update all `using` statements across the entire codebase that reference the old namespace

### Requirement 4: Extract Persistence and I/O Helpers into Persistence Directory

**User Story:** As a developer, I want file I/O and window-state helpers in a dedicated `Persistence/` directory, so that infrastructure concerns are isolated from domain logic.

#### Acceptance Criteria

1. WHEN the restructure is complete, THE Main_Project SHALL contain a `Persistence/` directory holding: SafeFileWriter, WindowStateHelper, BoundsValidator
2. WHEN a file is moved to Persistence_Directory, THE Main_Project SHALL update the file's namespace to `OE2EmpireTracker.Persistence`
3. WHEN a file is moved to Persistence_Directory, THE Csproj SHALL update the corresponding Compile_Include path to reference the new `Persistence\` location
4. WHEN a namespace changes, THE Main_Project SHALL update all `using` statements across the entire codebase that reference the old namespace

### Requirement 5: Remove Empty Legacy Directories

**User Story:** As a developer, I want the old `Baseline/` and `Data/` directories removed after all files are relocated, so that the project has no stale empty folders.

#### Acceptance Criteria

1. WHEN all files have been moved out of `Baseline/`, THE Main_Project SHALL no longer contain a `Baseline/` directory
2. WHEN all files have been moved out of `Data/`, THE Main_Project SHALL no longer contain a `Data/` directory
3. WHEN legacy directories are removed, THE Csproj SHALL contain zero Compile_Include entries referencing `Baseline\` or `Data\` paths

### Requirement 6: Mirror Test Project Structure

**User Story:** As a developer, I want the test project directory structure to mirror the new main project structure, so that test files are easy to find alongside the code they test.

#### Acceptance Criteria

1. WHEN the restructure is complete, THE Test_Project SHALL contain `Models/`, `Services/`, `Parsers/`, and `Persistence/` directories mirroring the Main_Project
2. WHEN a test file's corresponding source file moves to a new directory, THE Test_Project SHALL move that test file to the matching directory
3. WHEN a test file is moved, THE Test_Project SHALL update the test file's namespace to match the new directory (e.g., `OE2EmpireTracker.Tests.Models`)
4. WHEN a test file is moved, THE Test_Project Csproj SHALL update the corresponding Compile_Include path
5. WHEN all test files have been moved out of `Baseline/` in the Test_Project, THE Test_Project SHALL no longer contain a `Baseline/` directory
6. WHEN all test files have been moved out of `Data/` in the Test_Project, THE Test_Project SHALL no longer contain a `Data/` directory

### Requirement 7: Preserve Unchanged Directories

**User Story:** As a developer, I want directories that are already well-organized to remain untouched, so that the restructure is scoped and predictable.

#### Acceptance Criteria

1. THE Main_Project SHALL retain the `Constants/` directory and its contents without modification
2. THE Main_Project SHALL retain the `Controls/` directory and its contents without modification
3. THE Main_Project SHALL retain the `ViewModels/` directory and its contents without modification
4. THE Main_Project SHALL retain the `Forms/` directory and its contents without modification
5. THE Test_Project SHALL retain the `Blueprint/`, `Constants/`, `Controls/`, `Forms/`, and `TestData/` directories without modification

### Requirement 8: Maintain Build and Test Integrity per Batch

**User Story:** As a developer, I want each batch of file moves to result in a buildable solution with all 746 tests passing, so that I can roll back any individual batch if something breaks.

#### Acceptance Criteria

1. WHEN a Batch of file moves is completed, THE Main_Project SHALL compile without errors
2. WHEN a Batch of file moves is completed, THE Test_Project SHALL compile without errors
3. WHEN a Batch of file moves is completed, all existing tests SHALL pass with no regressions
4. WHEN a Batch is verified as passing, THE Batch SHALL be committed as a separate git commit with a descriptive message

### Requirement 9: Update Csproj Compile Include Entries

**User Story:** As a developer, I want the old-style csproj files to have correct Compile Include entries after every move, so that MSBuild can locate all source files.

#### Acceptance Criteria

1. WHEN a source file is moved in the Main_Project, THE Csproj SHALL remove the old Compile_Include entry referencing the previous path
2. WHEN a source file is moved in the Main_Project, THE Csproj SHALL add a new Compile_Include entry referencing the new path
3. WHEN a test file is moved in the Test_Project, THE Test_Project Csproj SHALL remove the old Compile_Include entry referencing the previous path
4. WHEN a test file is moved in the Test_Project, THE Test_Project Csproj SHALL add a new Compile_Include entry referencing the new path
5. IF a Compile_Include entry references a file that no longer exists at that path, THEN THE build SHALL fail, indicating the entry must be corrected before proceeding
