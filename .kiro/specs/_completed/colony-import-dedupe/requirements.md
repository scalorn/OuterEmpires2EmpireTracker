# Requirements Document

## Introduction

The colony clipboard import (`cmdImportColony_Click` in `FormColony`) currently parses HTML directly into the selected colony object, assuming the clipboard content belongs to that colony. This is often wrong — the user may have a different colony selected, or no colony selected at all. This feature changes the import to parse colony identity from the clipboard HTML first, then search existing colonies by name to find a match. If a match is found, the existing colony is updated in place. If no match is found, a new colony is created. This mirrors the dedup logic already applied to blueprint imports.

## Glossary

- **Import_Handler**: The `cmdImportColony_Click` method in `FormColony.cs` that handles colony clipboard import.
- **Colony_Parser**: The `ColonyParser` class that parses clipboard HTML into colony data (planet name, colony name, system name, structures, commodity demands).
- **Dedup_Key**: The planet name and system name (`PlanetName` + `SystemName`) used to match imported data against existing colonies. Matching is case-insensitive within the current player's colonies. This combination is unique per player and avoids issues with truncated colony names in the game.
- **Selected_Colony**: The colony currently selected in the FormColony list view, represented by `selectedColony`.
- **Colony_List**: The `BindingList<Colony>` in `PlayerContext` (`colonyList`) where colonies are stored.
- **Current_Player_Colonies**: The subset of Colony_List owned by the current player, returned by `PlayerContext.GetCurrentPlayerColonies()`.
- **ValidatedTextBox**: A custom TextBox control that supports `SetError(message)` to show a red background and `ClearError()` to restore normal appearance. Used for inline validation feedback.

## Requirements

### Requirement 1: Parse Colony Identity into a Temporary Object

**User Story:** As a user, I want the import to parse colony identity from the clipboard HTML into a temporary object, so that the system can determine which colony the data belongs to rather than assuming it matches my selection.

#### Acceptance Criteria

1. WHEN the user clicks Import Colony and the clipboard contains valid HTML, THE Colony_Parser SHALL parse the clipboard HTML into a temporary Colony object containing ColonyName, PlanetName, SystemName, Structures, and Commodities.
2. THE Import_Handler SHALL use the parsed temporary Colony object for Dedup_Key matching instead of writing directly into the Selected_Colony.

### Requirement 2: Search Existing Colonies by Name

**User Story:** As a user, I want the import to find the correct colony by name, so that data goes to the right colony regardless of which colony I have selected.

#### Acceptance Criteria

1. WHEN the temporary Colony has a non-empty PlanetName, THE Import_Handler SHALL search the Current_Player_Colonies for a colony whose PlanetName and SystemName match the temporary Colony's PlanetName and SystemName using case-insensitive comparison.
2. WHEN a matching colony is found in Current_Player_Colonies, THE Import_Handler SHALL use that existing colony as the merge target.
3. WHEN no matching colony is found in Current_Player_Colonies, THE Import_Handler SHALL create a new Colony with a generated UUID and add it to the Colony_List.

### Requirement 3: Update Existing Colony on Match

**User Story:** As a user, I want the import to update my existing colony when the clipboard data matches it by name, so that I don't end up with duplicate colonies.

#### Acceptance Criteria

1. WHEN the Import_Handler finds a matching colony, THE Colony_Parser SHALL merge the parsed structures into the existing colony's structure list using the existing merge logic (compound key matching by FlatpackBlueprintUUID and displaySequence).
2. WHEN the Import_Handler finds a matching colony, THE Colony_Parser SHALL merge the parsed commodity demands into the existing colony's commodity list using the existing merge logic (match by commodity name).
3. WHEN the Import_Handler finds a matching colony, THE Import_Handler SHALL update the existing colony's PlanetName and SystemName with the parsed values, but SHALL preserve the existing colony's ColonyName if it already has one (to protect user-corrected names from game truncation bugs).
4. WHEN the Import_Handler finds a matching colony, THE Import_Handler SHALL preserve the existing colony's UUID, OwnerUUID, Items, and any locally-configured structure state (mining survey assignments, refining assignments, manufacturing assignments).

### Requirement 4: Create New Colony When No Match Found

**User Story:** As a user, I want a new colony to be created when the imported data does not match any existing colony, so that new colonies are added automatically.

#### Acceptance Criteria

1. WHEN no matching colony is found, THE Import_Handler SHALL create a new Colony with a generated UUID.
2. WHEN a new colony is created, THE Import_Handler SHALL set the new colony's OwnerUUID to the current player's UUID.
3. WHEN a new colony is created, THE Import_Handler SHALL populate the new colony with all parsed data (ColonyName, PlanetName, SystemName, Structures, Commodities).
4. WHEN a new colony is created, THE Import_Handler SHALL add the new colony to the Colony_List.

### Requirement 5: Refresh UI and Select Imported Colony

**User Story:** As a user, I want the list view to refresh and select the imported colony after import, so that I can immediately see and verify the result.

#### Acceptance Criteria

1. WHEN an import completes (whether update or create), THE Import_Handler SHALL refresh the colony list view.
2. WHEN an import completes, THE Import_Handler SHALL select the imported or updated colony in the list view.
3. WHEN an import completes, THE Import_Handler SHALL populate the form fields with the imported colony's data.

### Requirement 6: Persist Changes After Import

**User Story:** As a user, I want my imported colony data to be saved automatically, so that I don't lose the import if I close the application.

#### Acceptance Criteria

1. WHEN the Import_Handler updates or creates a colony, THE Import_Handler SHALL persist the Colony_List by calling `PlayerContext.writeContext()`.
2. THE Import_Handler SHALL fire the `ColonyDataChanged` event after persisting changes so that other open forms reflect the update.

### Requirement 7: Handle Edge Cases

**User Story:** As a user, I want the import to handle error conditions gracefully, so that I get clear feedback when something goes wrong.

#### Acceptance Criteria

1. IF the clipboard does not contain HTML, THEN THE Import_Handler SHALL display an informational message and take no further action.
2. IF the Colony_Parser fails to parse a ColonyName from the clipboard HTML, THEN THE Import_Handler SHALL fall back to updating the Selected_Colony (current behavior) to avoid data loss.
3. IF no current player is selected, THEN THE Import_Handler SHALL display an informational message and take no further action.

### Requirement 8: Prevent Duplicate Colony Names on Manual Edit

**User Story:** As a user, I want the form to prevent me from manually entering a colony name that already exists for another colony, so that I don't accidentally create naming conflicts that break the dedup logic.

#### Acceptance Criteria

1. WHEN the user edits the colony name field, THE FormColony SHALL check whether the entered name (case-insensitive) matches any other colony in Current_Player_Colonies (excluding the currently selected colony).
2. IF the colony name matches another existing colony, THE colony name field SHALL display a validation error (red background via `ValidatedTextBox.SetError`) with a message indicating the name is already in use.
3. WHILE the colony name field has a duplicate-name validation error, THE Save button SHALL be disabled to prevent saving.
4. WHEN the user changes the colony name to a unique value, THE colony name field SHALL clear the validation error (via `ValidatedTextBox.ClearError`) and THE Save button SHALL be re-enabled.
