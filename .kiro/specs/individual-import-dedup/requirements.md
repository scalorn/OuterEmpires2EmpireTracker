# Requirements Document

## Introduction

The individual blueprint clipboard import (`cmdImport_Click` in `FormBlueprint`) currently overwrites the selected blueprint's data with whatever is on the clipboard. This assumes the clipboard content belongs to the selected blueprint, which is often wrong. This feature changes the import to parse a complete blueprint identity from the clipboard HTML, then use dedup-key matching to route the import to the correct blueprint — updating an existing match or creating a new one — using the same routing logic as the market importer.

## Glossary

- **Import_Handler**: The `cmdImport_Click` method in `FormBlueprint.cs` that handles individual blueprint clipboard import.
- **Blueprint_Scanner**: The `BlueprintScanner` class that parses clipboard HTML into blueprint data (name, type, evolution, class, tech level, properties, resources).
- **Dedup_Key**: The composite key used to match blueprints: Name + Evolution + BluePrintType + Class + TechLevel (exact, case-sensitive match).
- **Market_Importer**: The `MarketBlueprintImporter` class that handles bulk market imports with dedup and routing logic.
- **Selected_Blueprint**: The blueprint currently selected in the FormBlueprint list view, represented by `viewModel.Data`.
- **Target_List**: The `BindingList<Blueprint>` (either `globalBlueprintList` or `blueprintList`) where a blueprint should be stored based on routing rules.
- **Global_List**: The `EmpireContext.globalBlueprintList` storing shared blueprints in `BaselineData.json`.
- **Player_List**: The `PlayerContext.blueprintList` storing player-specific blueprints in `PlayerData.json`.

## Requirements

### Requirement 1: Parse Complete Blueprint Identity from Clipboard

**User Story:** As a user, I want the import to parse a full blueprint identity from the clipboard HTML, so that the system can determine where the imported data belongs rather than assuming it matches my selection.

#### Acceptance Criteria

1. WHEN the user clicks Import and the clipboard contains valid HTML, THE Blueprint_Scanner SHALL parse the clipboard HTML into a temporary Blueprint object containing Name, BluePrintType, Evolution, Class, TechLevel, Properties, and Resources.
2. THE Import_Handler SHALL use the parsed temporary Blueprint object for dedup-key matching instead of writing directly into the Selected_Blueprint.

### Requirement 2: Match Against Selected Blueprint First

**User Story:** As a user, I want the import to update my selected blueprint when the clipboard data matches it, so that the common case (importing data for the blueprint I'm looking at) still works seamlessly.

#### Acceptance Criteria

1. WHEN the parsed blueprint's Dedup_Key matches the Selected_Blueprint's Dedup_Key, THE Import_Handler SHALL update the Selected_Blueprint's Properties and Resources with the parsed data (current behavior).
2. WHEN the parsed blueprint's Dedup_Key matches the Selected_Blueprint's Dedup_Key, THE Import_Handler SHALL preserve the Selected_Blueprint's UUID, OwnerUUID, NickName, CopyCost, and baseBlueprintUUID.

### Requirement 3: Route Non-Matching Imports Using Market Dedup Logic

**User Story:** As a user, I want non-matching clipboard imports to be routed to the correct blueprint list using the same logic as market imports, so that blueprints end up in the right place without manual intervention.

#### Acceptance Criteria

1. WHEN the parsed blueprint's Dedup_Key does not match the Selected_Blueprint's Dedup_Key and the parsed Evolution is 0, THE Import_Handler SHALL route the blueprint to the Global_List.
2. WHEN the parsed blueprint's Dedup_Key does not match the Selected_Blueprint's Dedup_Key and the parsed Evolution is not 0 and a current player is selected, THE Import_Handler SHALL route the blueprint to the Player_List.
3. WHEN the parsed blueprint's Dedup_Key does not match the Selected_Blueprint's Dedup_Key and the parsed Evolution is not 0 and no current player is selected, THE Import_Handler SHALL route the blueprint to the Global_List.
4. WHEN the Target_List contains a blueprint with a matching Dedup_Key, THE Import_Handler SHALL update that existing blueprint's Properties and Resources with the parsed data.
5. WHEN the Target_List does not contain a blueprint with a matching Dedup_Key, THE Import_Handler SHALL create a new blueprint with a generated UUID and add it to the Target_List.
6. WHEN a new blueprint is created and routed to the Player_List, THE Import_Handler SHALL set the new blueprint's OwnerUUID to the current player's UUID.

### Requirement 4: Expose FindByDedupKey for Shared Use

**User Story:** As a developer, I want the dedup-key lookup to be reusable across both market and individual import paths, so that the matching logic stays consistent and is not duplicated.

#### Acceptance Criteria

1. THE Market_Importer SHALL expose the FindByDedupKey method with internal visibility so that the Import_Handler can use the same dedup logic.
2. THE FindByDedupKey method SHALL accept a `BindingList<Blueprint>` and a Blueprint and return the first matching blueprint or null.

### Requirement 5: Refresh UI and Select Imported Blueprint

**User Story:** As a user, I want the list view to refresh and select the imported blueprint after import, so that I can immediately see and verify the result.

#### Acceptance Criteria

1. WHEN an import completes (whether update or create), THE Import_Handler SHALL refresh the blueprint list view.
2. WHEN an import completes, THE Import_Handler SHALL select the imported or updated blueprint in the list view.
3. WHEN an import completes, THE Import_Handler SHALL populate the form fields with the imported blueprint's data.

### Requirement 6: Persist Changes After Import

**User Story:** As a user, I want my imported blueprint data to be saved automatically, so that I don't lose the import if I close the application.

#### Acceptance Criteria

1. WHEN the Import_Handler updates or creates a blueprint in the Global_List, THE Import_Handler SHALL persist the Global_List by calling `EmpireContext.writeContext()`.
2. WHEN the Import_Handler updates or creates a blueprint in the Player_List, THE Import_Handler SHALL persist the Player_List by calling `PlayerContext.writeContext()`.
3. THE Import_Handler SHALL fire the `BlueprintDataChanged` event after persisting changes so that other open forms reflect the update.

### Requirement 7: Handle Edge Cases

**User Story:** As a user, I want the import to handle error conditions gracefully, so that I get clear feedback when something goes wrong.

#### Acceptance Criteria

1. IF the clipboard does not contain HTML, THEN THE Import_Handler SHALL display an informational message and take no further action.
2. IF the Blueprint_Scanner fails to parse a Name from the clipboard HTML, THEN THE Import_Handler SHALL fall back to updating the Selected_Blueprint (current behavior) to avoid data loss.
3. IF the parsed blueprint has no BluePrintType and no Properties, THEN THE Import_Handler SHALL still proceed with the import using whatever fields were successfully parsed.
