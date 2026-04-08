# Requirements Document

## Introduction

The game "Outer Empires 2" periodically updates its sprite sheet and adds new blueprint types, which shifts icon positions for existing types and introduces new ones. The tracker application needs a repeatable process to extract icon positions from refreshed MarketSample HTML files, compare them against BaselineData.json, update changed positions, and add new BlueprintType entries. All MarketSample HTML files in TestData have been refreshed with current game data.

The game's market lists items randomly, so the MarketSample HTML files captured at any point in time may not cover every BlueprintType defined in BaselineData. Some types may simply not have appeared in the market when samples were taken. This applies across all blueprint categories — ship components, flatpacks, commodity industry variants, etc. Gap tracking is a first-class concern: the extractor must identify which BlueprintTypes lack HTML coverage and surface those gaps as actionable backlog items so the developer knows which market pages to capture next.

Additionally, the game's market listings use the commodity industry name directly as the blueprint name (e.g. "Agridome", "Administration Block"), with each industry having its own distinct icon. The tracker currently models all commodity factories as a single `Flatpacks/CommodityFactory` type with a `Commodity Industry` property. The data model will be split into per-industry BlueprintType entries (e.g. `Flatpacks/CommodityFactory/Agridome`) to match how the game actually represents these types, with each entry carrying its own IconPosition. No data migration is needed — the developer reimports all colonies regularly to pick up CommodityRequests changes.

## Glossary

- **BaselineData**: The JSON file (`BaselineData.json`) containing shared game data including BlueprintType definitions, ship classes, and tech levels. Both the main application and the test project maintain a copy.
- **BlueprintType**: A data record in BaselineData defining a category of blueprint (e.g. Reactor, Hull, Flatpacks/MiningRig). Contains Id, Name, Properties, IconPosition, and OutputItemType.
- **IconPosition**: A CSS sprite coordinate string (e.g. "-328px -62px") stored on each BlueprintType. Used by the BlueprintScanner to resolve market listing icons to their BlueprintType during HTML parsing.
- **BlueprintScanner**: The parser class that processes market HTML and resolves blueprint type icons via `EmpireContext.FindBlueprintTypeByIcon()`.
- **EmpireContext**: The singleton service that loads BaselineData.json and provides lookup methods including `FindBlueprintTypeByIcon()`.
- **MarketSample_HTML**: The set of HTML fixture files in `OE2EmpireTracker.Tests/TestData/` named `MarketSample*.html`. Each file contains a market listing page for one or more blueprint types, captured from the live game.
- **IconPositionExtractor**: A test utility or tool that parses all MarketSample HTML files, extracts the icon position for each distinct blueprint type found, and produces a structured report of type-to-icon mappings.
- **OreHopper**: A new ship component blueprint type added to the game. Requires a new BlueprintType entry in BaselineData.
- **CommodityIndustryName**: The name displayed in the game's market listings for commodity factory flatpacks (e.g. "Agridome", "Administration Block"). The game uses the commodity industry name directly as the blueprint name in market listings. Each commodity industry has its own distinct icon position in the game UI.
- **CommodityIndustry**: An enum/model in the tracker listing the 14 commodity industry names: Administration Block, Agridome, Centre of Economics, Engineering Block, Healthcare Institute, Institute of Defence, Leisure Industry Centre, Logistics Centre, Manufacturing Industry Centre, Mining Industry Centre, OffWorld Living Institute, Refining Industry Centre, Science Centre, Technology Institute.
- **PerIndustryBlueprintType**: The chosen data model for commodity factories. Each commodity industry gets its own BlueprintType entry in BaselineData with Id pattern `Flatpacks/CommodityFactory/{IndustryName}` (e.g. `Flatpacks/CommodityFactory/Agridome`). Each entry has its own IconPosition, Properties array, and OutputItemType="Flatpack". The existing single `Flatpacks/CommodityFactory` entry is replaced by these 14 per-industry entries.
- **IsCommodityFactory**: A helper method (or `StartsWith("Flatpacks/CommodityFactory/")` check) used by code that previously compared against the single `BlueprintTypes.CommodityFactory` constant. Required because the type Id is no longer a single value but a family of values sharing a common prefix.
- **MarketBlueprintImporter**: The service that takes parsed MarketBlueprint objects and imports them into the player or global blueprint storage.
- **BlueprintTypes_Constants**: The static constants file (`BlueprintTypes.cs`) that defines string constants for blueprint type Ids to avoid magic strings.
- **CoverageGap**: A BlueprintType defined in BaselineData that has no matching icon position found in any MarketSample HTML file. Gaps arise because the game's market lists items randomly and samples may not cover every type.
- **Backlog**: The file `spec/BACKLOG.md` that tracks open features, enhancements, and action items. Uses `BL-NNN` identifiers.

## Requirements

### Requirement 1: Repeatable icon position extraction from MarketSample HTML

**User Story:** As a developer, I want a repeatable tool or test that extracts icon positions from all MarketSample HTML files, so that I can re-run it whenever the game updates its sprite sheet.

#### Acceptance Criteria

1. THE IconPositionExtractor SHALL parse every `MarketSample*.html` file in the TestData directory and extract the icon position string for each expanded blueprint listing.
2. THE IconPositionExtractor SHALL group extracted icon positions by resolved BlueprintType Id (or by raw icon position string when no BlueprintType match exists).
3. THE IconPositionExtractor SHALL produce a log/report listing each unique icon position, the BlueprintType Id it maps to (or "UNKNOWN"), and the MarketSample file it was found in.
4. WHEN an icon position is found that does not match any BlueprintType in BaselineData, THE IconPositionExtractor SHALL flag the position as unmatched and include the blueprint name from the HTML listing.
5. THE IconPositionExtractor SHALL be implemented as an NUnit test or test fixture so it can be executed via the existing test runner.
6. WHEN the IconPositionExtractor discovers a new BlueprintType not present in BaselineData, THE IconPositionExtractor SHALL automatically add a new BlueprintType entry to BaselineData.json with the discovered name and icon position.

### Requirement 2: Compare extracted positions against BaselineData and automatically update differences

**User Story:** As a developer, I want the extraction process to compare results against the current BaselineData.json and automatically apply updates, so that changed icon positions and new types are written without manual intervention.

#### Acceptance Criteria

1. WHEN the IconPositionExtractor runs, THE IconPositionExtractor SHALL compare each extracted icon position against the corresponding BlueprintType's IconPosition in BaselineData.
2. WHEN a BlueprintType's icon position in the HTML differs from its IconPosition in BaselineData, THE IconPositionExtractor SHALL automatically update the IconPosition in BaselineData.json and log the type Id, old position, and new position.
3. WHEN an icon position is extracted for a blueprint name that has no corresponding BlueprintType entry in BaselineData, THE IconPositionExtractor SHALL automatically add a new BlueprintType entry to BaselineData.json and log the name and icon position.
4. WHEN a BlueprintType in BaselineData has an IconPosition but no matching icon position appears in any MarketSample HTML file, THE IconPositionExtractor SHALL report the type Id as having no HTML coverage (see Requirement 9 for full gap tracking).

### Requirement 3: Automatically write updated BaselineData.json files

**User Story:** As a developer, I want the IconPositionExtractor to automatically write updated BaselineData.json files (both main and test copies), so that no manual file editing is required after extraction.

#### Acceptance Criteria

1. WHEN the IconPositionExtractor detects changed icon positions or new BlueprintType entries, THE IconPositionExtractor SHALL automatically write the updated BaselineData.json to the main application directory (`OE2EmpireTracker/BaselineData.json`).
2. WHEN the IconPositionExtractor writes the main BaselineData.json, THE IconPositionExtractor SHALL also automatically write the updated BaselineData.json to the test project directory (`OE2EmpireTracker.Tests/TestData/BaselineData.json`).
3. THE BaselineData in the test project SHALL contain the same BlueprintType entries and IconPosition values as the main BaselineData after the IconPositionExtractor completes.
4. WHEN `EmpireContext.FindBlueprintTypeByIcon()` is called with any icon position string found in the MarketSample HTML files, THE EmpireContext SHALL return the correct BlueprintType matching that position.
5. THE IconPositionExtractor SHALL produce a summary log of all changes written (updated positions, new entries added) for developer visibility.

### Requirement 4: Add OreHopper BlueprintType

**User Story:** As a player, I want OreHopper blueprints from the market to be recognized and imported correctly, so that I can track this new ship component type.

#### Acceptance Criteria

1. THE BaselineData SHALL contain a new BlueprintType entry with Id "OreHopper" and a descriptive Name.
2. THE OreHopper BlueprintType entry SHALL include an IconPosition value matching the icon position extracted from `MarketSampleOreHopper.html`.
3. THE OreHopper BlueprintType entry SHALL include a Properties array derived from the actual properties found in the `MarketSampleOreHopper.html` listings (extracted from the HTML, not predefined).
4. THE OreHopper BlueprintType entry SHALL include an OutputItemType value of "ShipPart".
5. THE BlueprintTypes_Constants class SHALL contain a public const string field for OreHopper with the value matching the OreHopper BlueprintType Id in BaselineData.

### Requirement 5: Split commodity factory into per-industry BlueprintType entries

**User Story:** As a developer, I want each commodity industry to have its own BlueprintType entry in BaselineData, so that icon-based type resolution works correctly and each industry's distinct icon is properly mapped.

#### Acceptance Criteria

1. THE IconPositionExtractor SHALL extract and report all distinct icon positions found in `MarketSampleAllFlatpacks.html` for listings whose blueprint name matches a commodity industry name (e.g. "Agridome", "Administration Block").
2. THE BaselineData SHALL contain 14 separate BlueprintType entries for commodity industries, one per non-None `CommodityIndustryEnum` value, using the Id pattern `Flatpacks/CommodityFactory/{IndustryName}` (e.g. `Flatpacks/CommodityFactory/Agridome`, `Flatpacks/CommodityFactory/AdministrationBlock`). All 14 entries SHALL always be present in BaselineData regardless of whether a MarketSample HTML file exists for that industry. Entries without HTML coverage SHALL have IconPosition set to null. The 14 industries are: Administration Block, Agridome, Centre of Economics, Engineering Block, Healthcare Institute, Institute of Defence, Leisure Industry Centre, Logistics Centre, Manufacturing Industry Centre, Mining Industry Centre, OffWorld Living Institute, Refining Industry Centre, Science Centre, Technology Institute.
3. Each per-industry BlueprintType entry SHALL have its own IconPosition, Properties array, and OutputItemType value of "Flatpack".
4. THE existing single `Flatpacks/CommodityFactory` BlueprintType entry in BaselineData SHALL be replaced by the 14 per-industry entries.
5. THE BlueprintScanner SHALL resolve each commodity industry variant's icon to its specific per-industry BlueprintType entry.
6. THE BlueprintTypes_Constants class SHALL replace the single `CommodityFactory` constant with either per-industry constants or a `CommodityFactoryPrefix` constant (e.g. `"Flatpacks/CommodityFactory/"`) and provide an `IsCommodityFactory()` helper method or extension that checks `StartsWith("Flatpacks/CommodityFactory/")`.
7. WHEN code currently checks `== BlueprintTypes.CommodityFactory`, THE code SHALL be updated to use `StartsWith("Flatpacks/CommodityFactory/")` or the `IsCommodityFactory()` helper. Affected locations include: ColonyParser, Colony.ProcessCommodityFactory, ColonyStatusCalculator, ColonyStructure form, ColonyActivityCollector, ColonyInactivityCollector, and delivery planning services.
8. THE `Commodity Industry` property on commodity factory blueprints SHALL become derivable from the BlueprintType Id itself (the industry name is encoded in the Id). Existing code that reads or writes this property SHALL be updated accordingly.
9. WHEN the available MarketSample HTML files do not contain listings for all 14 CommodityIndustry variants, THE IconPositionExtractor SHALL identify the missing variants by comparing discovered variants against the full CommodityIndustry list.
10. WHEN a CommodityIndustry variant is identified as missing from MarketSample coverage, THE IconPositionExtractor SHALL include the missing variant in the coverage gap report so it can be tracked as a backlog item (see Requirement 10).

### Requirement 6: Existing integration tests pass with updated data

**User Story:** As a developer, I want all existing market import integration tests to continue passing after the icon position and data model updates, so that no regressions are introduced.

#### Acceptance Criteria

1. WHEN existing integration tests parse MarketSample HTML files, THE BlueprintScanner SHALL resolve all expanded blueprint icons to the correct BlueprintType using the updated IconPosition values.
2. IF an existing integration test fails due to changed icon positions or data model changes, THEN THE test data and BaselineData in the test project SHALL be updated to make the test pass.
3. THE integration test suite SHALL pass for all MarketSample HTML files that have refreshed game data.

### Requirement 7: New integration tests for OreHopper and commodity industry types

**User Story:** As a developer, I want integration tests that verify OreHopper and commodity industry market HTML parsing, so that the new and updated blueprint types are covered by automated tests.

#### Acceptance Criteria

1. WHEN the integration test parses `MarketSampleOreHopper.html`, THE BlueprintScanner SHALL produce at least one MarketBlueprint with BluePrintType "OreHopper".
2. WHEN the integration test imports parsed OreHopper blueprints, THE MarketBlueprintImporter SHALL create blueprint records with BluePrintType "OreHopper" and populated properties.
3. WHEN the integration test parses `MarketSampleAllFlatpacks.html`, THE BlueprintScanner SHALL produce MarketBlueprint entries with the correct BlueprintType for each commodity industry listing (whether single type or per-variant, as determined by Requirement 5).
4. WHEN the integration test imports the same MarketSample HTML twice, THE MarketBlueprintImporter SHALL update existing records without creating duplicates (idempotency).
5. THE test project csproj SHALL include `MarketSampleOreHopper.html` as a Content item with `CopyToOutputDirectory` set to `PreserveNewest`.

### Requirement 8: Repeatable end-to-end refresh workflow

**User Story:** As a developer, I want a fully automated, repeatable workflow for refreshing icon positions whenever the game updates, so that future updates require only refreshing the MarketSample HTML files and running the extractor.

#### Acceptance Criteria

1. THE IconPositionExtractor test SHALL be runnable on demand without manual setup beyond refreshing the MarketSample HTML files in TestData.
2. WHEN new MarketSample HTML files are added to TestData, THE IconPositionExtractor SHALL automatically include the new files in its extraction pass.
3. WHEN the IconPositionExtractor runs, THE IconPositionExtractor SHALL automatically extract positions, compare against BaselineData, update both main and test BaselineData.json files, add new BlueprintType entries, and produce a change log — requiring no manual file editing by the developer.
4. WHEN a new blueprint type is discovered by the IconPositionExtractor, THE IconPositionExtractor SHALL automatically add the BlueprintType entry to BaselineData.json and log the blueprint name, icon position, and source file.

### Requirement 9: Coverage gap tracking across all BlueprintTypes

**User Story:** As a developer, I want the extractor to clearly identify which BlueprintTypes in BaselineData have no MarketSample HTML coverage, so that I know exactly which market pages I need to capture next time I'm in the game.

#### Acceptance Criteria

1. WHEN the IconPositionExtractor completes its extraction pass, THE IconPositionExtractor SHALL compare the full set of BlueprintTypes in BaselineData against the set of BlueprintTypes found in MarketSample HTML files.
2. THE IconPositionExtractor SHALL produce a dedicated coverage gap report listing every BlueprintType Id in BaselineData that was not found in any MarketSample HTML file.
3. THE coverage gap report SHALL be separate from (or clearly distinguished within) the icon position change report, so gaps are not buried among position-change entries.
4. WHEN separate BlueprintType entries exist per CommodityIndustry variant, THE coverage gap report SHALL list each missing variant individually rather than treating commodity industries as a single type.
5. THE coverage gap report SHALL indicate whether each uncovered BlueprintType has an existing IconPosition in BaselineData (stale but unverifiable) or has no IconPosition at all (never characterized).

### Requirement 10: Backlog integration for uncovered BlueprintTypes

**User Story:** As a developer, I want uncovered BlueprintTypes to be surfaced as actionable backlog items in `spec/BACKLOG.md`, so that I have a clear list of which market pages to capture next time I'm in the game.

#### Acceptance Criteria

1. WHEN the IconPositionExtractor identifies a BlueprintType with no MarketSample HTML coverage, THE extraction report SHALL recommend adding a backlog item to `spec/BACKLOG.md` for capturing a MarketSample for that type.
2. THE recommended backlog item SHALL follow the `BL-NNN` format and include the BlueprintType Id and a description such as "Need a MarketSample for blueprint type X".
3. WHEN a new BlueprintType is discovered but insufficient MarketSample data exists to fully characterize it (e.g. only one listing found, or properties cannot be determined), THE extraction report SHALL recommend a backlog item noting the type needs additional MarketSample captures.
4. WHEN CommodityIndustry variants are missing from MarketSample coverage, THE extraction report SHALL recommend a backlog item per missing variant (or a single grouped item listing all missing variants) so the developer knows which specific commodity industry market pages to look for.
5. THE backlog items SHALL be added to the "Open (Existing)" or a new "MarketSample Coverage Gaps" section in `spec/BACKLOG.md` as appropriate.
