# Bugfix Requirements Document

## Introduction

When a user manually adds structures to a colony via the colony form, each structure receives a local UUID but `gameSequence` defaults to 0. When the user subsequently imports colony HTML from the game clipboard, `ColonyParser.ParseColonyBuildingsFromJson` indexes existing structures by `gameSequence` to find merge candidates. Manually-added structures (gameSequence=0) never match any parsed building (which carries a real buildingID like 47, 48, etc.), so every parsed building is appended as a new entry — duplicating the structures the user already added manually.

The fix approach is defined in AMB-032: rename `gameSequence` to `displaySequence` (per-type UI sequence number), add a new `buildingID` field for the game's unique identifier, and change merge detection to use `FlatpackBlueprintUUID + displaySequence` instead of `gameSequence`. Commodity manufactory types require special positional matching because their game sequence is not stable.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a user manually adds structures via the colony form (gameSequence=0) and then imports colony HTML, THEN the system fails to match any manually-added structure to a parsed building because the merge key (gameSequence) is 0 for manual structures and a real buildingID for parsed buildings

1.2 WHEN a user imports colony HTML into a colony that already has manually-added structures, THEN the system duplicates every structure — adding parsed buildings as new entries alongside the existing manual ones

1.3 WHEN the parser processes a building from JSON, THEN the system stores the game's `buildingID` into `gameSequence`, conflating two distinct concepts: the game's unique building identifier and the per-type UI display sequence number

1.4 WHEN a user manually adds a structure via the colony form, THEN the system leaves `gameSequence` at 0 (default) with no calculated display sequence, making the structure unmatchable during import

1.5 WHEN a user imports colony HTML containing commodity manufactory types (Agridome, Administration Block, etc.) into a colony with manually-added commodity manufactories, THEN the system fails to match them because commodity manufactory game sequences are not stable and the current merge logic has no special handling for these types

### Expected Behavior (Correct)

2.1 WHEN a user manually adds structures via the colony form and then imports colony HTML, THEN the system SHALL match parsed buildings to existing structures using `FlatpackBlueprintUUID + displaySequence` (per-type UI sequence number), merging rather than duplicating

2.2 WHEN a user imports colony HTML into a colony that already has manually-added structures, THEN the system SHALL update existing structures in place and only add structures that have no matching `FlatpackBlueprintUUID + displaySequence` pair

2.3 WHEN the parser processes a building from JSON, THEN the system SHALL store the game's `buildingID` in a new dedicated `buildingID` field on ColonyStructure, and SHALL derive the `displaySequence` from the building's UI name pattern (`#<num> - <name>`) rather than from `buildingID`

2.4 WHEN a user manually adds a structure via the colony form, THEN the system SHALL calculate and assign a `displaySequence` matching the game UI convention: per flatpack type, numbered sequentially starting at 1 (e.g., first Habitat is 1, second Habitat is 2)

2.5 WHEN a user imports colony HTML containing commodity manufactory types into a colony with manually-added commodity manufactories, THEN the system SHALL use best-guess positional matching: sort existing commodity manufactories by build order (list position), sort parsed ones by UI order, and match 1:1 by position within each commodity manufactory sub-type

### Unchanged Behavior (Regression Prevention)

3.1 WHEN colony HTML is imported into an empty colony (no pre-existing structures), THEN the system SHALL CONTINUE TO create one structure per parsed building with correct flatpack UUID, properties, worker assignments, and mining/manufacturing data

3.2 WHEN colony HTML is imported twice into the same colony (idempotency), THEN the system SHALL CONTINUE TO produce the same structure count and values after the second import as after the first

3.3 WHEN colony HTML is imported for a non-local colony (workers fallback path), THEN the system SHALL CONTINUE TO create structures from the colony-workers workforce detail with correct flatpack UUIDs

3.4 WHEN colony HTML contains commodity demands, THEN the system SHALL CONTINUE TO parse and merge commodity requests by name, preserving locally-tracked Delivered values

3.5 WHEN the parser extracts mining resource info, planet overview data, or building attributes from HTML, THEN the system SHALL CONTINUE TO populate those fields identically to current behavior
