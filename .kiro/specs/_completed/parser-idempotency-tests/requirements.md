# Requirements Document

## Introduction

Add repeated-import (idempotency) verification tests for the Survey and Colony parsers. When the same HTML is imported into the same model object multiple times, the parsers must not create duplicate child records. This is a test-only feature — no production code changes are expected unless the tests reveal actual bugs.

## Glossary

- **Survey_Parser**: The `SurveyParser` class (`OE2EmpireTracker/Parsers/SurveyParser.cs`) that parses survey HTML clipboard data into a `Survey` model object.
- **Colony_Parser**: The `ColonyParser` class (`OE2EmpireTracker/Parsers/ColonyParser.cs`) that parses colony Administration tab HTML clipboard data into a `Colony` model object.
- **Survey**: The `Survey` model object containing a `Resources` dictionary of `SurveyResource` entries keyed by resource name.
- **Colony**: The `Colony` model object containing a `Structures` list of `ColonyStructure` entries and a `Commodities` list of `CommodityRequested` entries.
- **Idempotent_Import**: Parsing the same HTML into the same model object N times (N ≥ 2) produces the same child record counts and data values as parsing it once.
- **Resource_Count**: The number of entries in `Survey.Resources` after parsing.
- **Structure_Count**: The number of entries in `Colony.Structures` after parsing.
- **Commodity_Count**: The number of entries in `Colony.Commodities` after parsing.

## Requirements

### Requirement 1: Survey Parser Idempotent Import

**User Story:** As a developer, I want to verify that re-importing the same survey HTML does not create duplicate resources, so that I can trust the parser handles repeated clipboard pastes correctly.

#### Acceptance Criteria

1. WHEN the same survey HTML is parsed into the same Survey object twice, THE Survey_Parser SHALL produce the same Resource_Count as parsing the HTML once.
2. WHEN the same survey HTML is parsed into the same Survey object three times, THE Survey_Parser SHALL produce the same Resource_Count as parsing the HTML once.
3. WHEN the same survey HTML is parsed into the same Survey object twice, THE Survey_Parser SHALL preserve the resource name, purity, and amount values from the first parse.
4. FOR ALL available survey test data HTML files, WHEN each file is parsed into the same Survey object twice, THE Survey_Parser SHALL produce the same Resource_Count as parsing the file once.

### Requirement 2: Colony Parser Idempotent Structure Import

**User Story:** As a developer, I want to verify that re-importing the same colony HTML does not create duplicate structures, so that I can trust the parser handles repeated clipboard pastes correctly.

#### Acceptance Criteria

1. WHEN the same colony HTML is parsed into the same Colony object twice, THE Colony_Parser SHALL produce the same Structure_Count as parsing the HTML once.
2. WHEN the same colony HTML is parsed into the same Colony object three times, THE Colony_Parser SHALL produce the same Structure_Count as parsing the HTML once.
3. WHEN the same colony HTML is parsed into the same Colony object twice, THE Colony_Parser SHALL preserve the FlatpackBlueprintUUID and gameSequence values of each structure from the first parse.
4. FOR ALL available colony test data HTML files, WHEN each file is parsed into the same Colony object twice, THE Colony_Parser SHALL produce the same Structure_Count as parsing the file once.

### Requirement 3: Colony Parser Idempotent Commodity Import

**User Story:** As a developer, I want to verify that re-importing the same colony HTML does not create duplicate commodity requests, so that I can trust the parser handles repeated clipboard pastes correctly.

#### Acceptance Criteria

1. WHEN the same colony HTML (containing commodity demands) is parsed into the same Colony object twice, THE Colony_Parser SHALL produce the same Commodity_Count as parsing the HTML once.
2. WHEN the same colony HTML (containing commodity demands) is parsed into the same Colony object three times, THE Colony_Parser SHALL produce the same Commodity_Count as parsing the HTML once.
3. WHEN the same colony HTML is parsed into the same Colony object twice, THE Colony_Parser SHALL preserve the commodity name, requested amount, and fulfilled status from the first parse.
4. FOR ALL available colony test data HTML files that contain commodity demands, WHEN each file is parsed into the same Colony object twice, THE Colony_Parser SHALL produce the same Commodity_Count as parsing the file once.
