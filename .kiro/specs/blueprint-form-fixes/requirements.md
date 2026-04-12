# Requirements Document

## Introduction

This spec addresses two Blueprint form bugs from the backlog:

- **BL-062**: Importing the resources tab of an evolved blueprint creates a new broken blueprint instead of updating the currently selected one. The resources-tab HTML lacks key dedup fields (class, tech level, blueprint type), so `FindByDedupKey` cannot match the import to the existing blueprint and falls through to creating a new entry.
- **BL-066**: The filter combo boxes on the Blueprint form (Type, Class, Tech Level, Evolution, and the "And Above" checkbox) do not persist their state between form close and reopen. They are created dynamically in `InitFilterPanel()` without `Name` properties, so `WindowStateHelper` skips them during save/restore.

## Glossary

- **Blueprint_Form**: The `FormBlueprint` WinForms form used to view, create, edit, and import blueprints.
- **Blueprint_Scanner**: The `BlueprintScanner` class that parses clipboard HTML into a temporary `Blueprint` object.
- **Import_Handler**: The `cmdImport_Click` method in `FormBlueprint` that orchestrates individual blueprint import from clipboard.
- **Dedup_Engine**: The `MarketBlueprintImporter.FindByDedupKey` method that matches blueprints by Name, Evolution, BluePrintType, Class, and TechLevel.
- **Resources_Only_Import**: A clipboard import where the parsed `Blueprint` has resources but is missing key dedup fields (BluePrintType is null/empty, Class is 0, TechLevel is null/empty) because the HTML came from the game's resources tab rather than the statistics tab.
- **Selected_Blueprint**: The blueprint currently selected in the Blueprint_Form's list view, represented by `viewModel.Data`.
- **Filter_Combo_Boxes**: The dynamically created combo boxes in the Blueprint_Form filter panel: `cmbFilterType`, `cmbFilterClass`, `cmbFilterTechLevel`, `cmbFilterEvolution`, and `chkEvolutionAndAbove`.
- **Window_State_Helper**: The `WindowStateHelper` static class that saves and restores form control states to `UIPreferences.json`.
- **Preferences_Store**: The `PreferencesStore` singleton that manages reading and writing `UIPreferences.json`.

## Requirements

### Requirement 1: Detect Resources-Only Import

**User Story:** As a player, I want the import handler to recognize when clipboard data contains only resource information (no statistics/metadata), so that it can route the import correctly instead of creating a broken duplicate blueprint.

#### Acceptance Criteria

1. WHEN the Blueprint_Scanner parses clipboard HTML into a temporary Blueprint, THE Import_Handler SHALL classify the import as a Resources_Only_Import when the temporary Blueprint has at least one resource entry AND BluePrintType is null or empty AND Class equals zero AND TechLevel is null or empty.
2. WHEN the temporary Blueprint has a non-empty BluePrintType OR a non-zero Class OR a non-empty TechLevel, THE Import_Handler SHALL classify the import as a full import (not a Resources_Only_Import), regardless of whether resources are present.
3. WHEN the temporary Blueprint has zero resource entries and is missing dedup fields, THE Import_Handler SHALL classify the import as a full import (not a Resources_Only_Import).

### Requirement 2: Route Resources-Only Import to Selected Blueprint

**User Story:** As a player, I want a resources-only import to update the currently selected blueprint rather than attempting dedup matching, so that I can import the resources tab after importing the statistics tab without creating a broken duplicate.

#### Acceptance Criteria

1. WHEN a Resources_Only_Import is detected AND a Selected_Blueprint exists (has a non-empty UUID), THE Import_Handler SHALL merge the parsed resources into the Selected_Blueprint.
2. WHEN a Resources_Only_Import is detected AND a Selected_Blueprint exists, THE Import_Handler SHALL preserve all existing fields on the Selected_Blueprint (Name, BluePrintType, Class, TechLevel, Evolution, Properties, UUID, OwnerUUID, NickName, CopyCost, Description, BaseBlueprintUUID).
3. WHEN a Resources_Only_Import is detected AND no Selected_Blueprint exists (UUID is null or empty), THE Import_Handler SHALL display an error message instructing the user to select or import a blueprint first.
4. WHEN a Resources_Only_Import is detected AND a Selected_Blueprint exists, THE Import_Handler SHALL persist the updated blueprint to the appropriate data store (global or player) and raise the BlueprintDataChanged event.
5. WHEN a Resources_Only_Import is detected AND a Selected_Blueprint exists, THE Import_Handler SHALL refresh the form to display the updated resource data.

### Requirement 3: Resources-Only Import Merges Resources Correctly

**User Story:** As a player, I want the resources-only import to correctly replace the selected blueprint's resources with the imported data, so that the resource list reflects the current game state.

#### Acceptance Criteria

1. WHEN a Resources_Only_Import merges into the Selected_Blueprint, THE Import_Handler SHALL replace the Selected_Blueprint's entire Resources dictionary with the parsed resources from the clipboard.
2. WHEN a Resources_Only_Import merges into the Selected_Blueprint, THE Import_Handler SHALL preserve any Properties already present on the Selected_Blueprint.
3. WHEN the parsed clipboard contains properties in addition to resources, THE Import_Handler SHALL merge those properties into the Selected_Blueprint using the same protected-property logic as `UpdateExisting` (preserving "Manufacture Run Time" and "Power Required").

### Requirement 4: Assign Name Properties to Filter Combo Boxes

**User Story:** As a player, I want the Blueprint form's filter combo boxes to have proper control names, so that the window state persistence system can save and restore their selections.

#### Acceptance Criteria

1. THE Blueprint_Form SHALL assign the Name property "cmbFilterType" to the Type filter combo box during `InitFilterPanel`.
2. THE Blueprint_Form SHALL assign the Name property "cmbFilterClass" to the Class filter combo box during `InitFilterPanel`.
3. THE Blueprint_Form SHALL assign the Name property "cmbFilterTechLevel" to the Tech Level filter combo box during `InitFilterPanel`.
4. THE Blueprint_Form SHALL assign the Name property "cmbFilterEvolution" to the Evolution filter combo box during `InitFilterPanel`.
5. THE Blueprint_Form SHALL assign the Name property "chkEvolutionAndAbove" to the "And Above" checkbox during `InitFilterPanel`.

### Requirement 5: Persist Filter Combo Box State on Form Close

**User Story:** As a player, I want the Blueprint form's filter selections to be saved when I close the form, so that they are available for restoration when I reopen it.

#### Acceptance Criteria

1. WHEN the Blueprint_Form is closed, THE Window_State_Helper SHALL save the selected value of each Filter_Combo_Box to UIPreferences.json under the form's window state entry.
2. WHEN the Blueprint_Form is closed, THE Window_State_Helper SHALL save the checked state of the "And Above" checkbox to UIPreferences.json under the form's window state entry.

### Requirement 6: Restore Filter Combo Box State on Form Open

**User Story:** As a player, I want the Blueprint form's filter selections to be restored when I reopen the form, so that I can continue working with the same filtered view without re-selecting filters each time.

#### Acceptance Criteria

1. WHEN the Blueprint_Form is opened AND UIPreferences.json contains saved filter combo box state, THE Window_State_Helper SHALL restore each Filter_Combo_Box to its previously saved selection.
2. WHEN the Blueprint_Form is opened AND UIPreferences.json contains saved checkbox state, THE Window_State_Helper SHALL restore the "And Above" checkbox to its previously saved checked state.
3. WHEN the Blueprint_Form is opened AND UIPreferences.json does not contain saved filter state, THE Blueprint_Form SHALL display all Filter_Combo_Boxes at their default values (index 0, blank/no filter).
4. WHEN a saved filter value no longer exists in the combo box's item list (e.g., a blueprint type was removed), THE Window_State_Helper SHALL leave the combo box at its default value (index 0) rather than selecting an invalid item.
5. WHEN filter state is restored, THE Blueprint_Form SHALL refresh the blueprint list view to reflect the restored filter selections.
