# Requirements Document

## Introduction

BL-110 restructures FormSurvey so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a SurveyService that applies changes atomically. The form never directly mutates a Survey --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-111 (player profiles), and BL-123 (pricing plans).

### Key Differences from BL-123 (PricingPlan)

1. **Clipboard import** --- Survey has a clipboard import feature (HTML from game browser). The service needs an Import method that handles dedup (find by PlanetName+SurveyID), merge, and asteroid auto-creation.
2. **Resource grid with nested objects** --- Survey has a `Dictionary<string, SurveyResource>` where each SurveyResource has Resource, Purity, and Amount strings. More complex than PricingPlan flat `Dictionary<string, decimal>`.
3. **Properties dictionary** --- Survey has a `Dictionary<string, string>` for sensor readings (SensorAbundance, PurityModifier, ScanLevel). These are additional key-value pairs beyond the scalar fields.
4. **Scanner blueprint combo** --- Survey has a FilteredTextComboSet for selecting the scanner blueprint used. The ViewModel stores the ScannerBlueprintUUID.
5. **Reference counter for delete protection** --- SurveyReferenceCounter checks mining rig assignments and build plan items before allowing delete.
6. **ReadOnlySurvey gap fill required** --- The existing ReadOnlySurvey is missing ScannedBy, DateTime, ScannerBlueprintUUID, and Properties. These must be added.
7. **SurveyType enum** --- Survey has a SurveyType (Planet or Asteroid) that affects form behavior (label text, asteroid linking).
8. **Asteroid auto-linking** --- On import, asteroid surveys auto-create or link to an Asteroid entity via deterministic UUID.
9. **Date/time handling** --- Survey stores DateTime as ISO string, displays via SurveyDateTimeParser formatting.

### Similarities to BL-123

1. **Flat scalar fields** --- PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, AsteroidUUID, OwnerUUID are all simple strings.
2. **Single form** --- One form manages the full CRUD lifecycle.
3. **List view with filters** --- Survey list has text filter, resource filter, type filter, purity filter, and min amount filter.
4. **No timer** --- No live countdown or timer logic.
5. **Always player-scoped** --- No global/player split.

## Glossary

- **Survey**: The mutable entity representing a planet or asteroid resource survey with identity fields, sensor readings, and resource data.
- **SurveyResource**: A nested object within Survey containing Resource name, Purity, and Amount strings for a single surveyed resource.
- **ReadOnlySurvey**: An immutable wrapper around Survey that exposes only getter properties and read-only resource access.
- **ReadOnlySurveyResource**: An immutable wrapper around SurveyResource exposing Resource, Purity, Amount as read-only.
- **SurveyViewModel**: The ViewModel class that holds a local edit buffer of all Survey fields, disconnected from the entity.
- **SurveyService**: A new service class that is the sole mutator of Survey entities (create, update, delete, import).
- **FormSurvey**: The WinForms form for viewing and editing surveys.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of field values in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any local field differs from the original snapshot.
- **SurveyUpdateRequest**: A DTO carrying the current local state from the ViewModel to the service for an update operation.
- **SurveyCreateRequest**: A DTO carrying field values for creating a new survey.
- **SurveyReferenceCounter**: A utility that counts how many mining rigs and build plan items reference a given survey UUID, used for delete protection.
- **SurveyImportHelper**: A static helper class containing FindByKey, CreateFromTemp, MergeData, and LinkOrCreateAsteroid methods for clipboard import.
- **SurveyParser**: The HTML parser that extracts survey data from game clipboard content into a temporary Survey object.
- **SurveyType**: An enum (Planet, Asteroid) indicating what type of celestial body the survey covers.
- **FilteredTextComboSet**: A custom combo control used for the scanner blueprint selection with type-ahead filtering.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: ReadOnlySurvey Gap Fill

**User Story:** As a developer, I want ReadOnlySurvey to expose all properties the form needs, so that the form can operate entirely through read-only wrappers without accessing the mutable entity.

#### Acceptance Criteria

1. THE ReadOnlySurvey SHALL expose a ScannedBy property returning the entity ScannedBy string.
2. THE ReadOnlySurvey SHALL expose a DateTime property returning the entity DateTime string.
3. THE ReadOnlySurvey SHALL expose a ScannerBlueprintUUID property returning the entity ScannerBlueprintUUID string.
4. THE ReadOnlySurvey SHALL expose a Properties property returning an IReadOnlyDictionary<string, string> view of the entity Properties dictionary.
5. THE ReadOnlySurvey SHALL continue to expose UUID, Name, OwnerUUID, PlanetName, SystemName, SurveyID, NickName, SurveyType, AsteroidUUID, ExtendedName, and Resources as read-only properties.
6. THE ReadOnlySurvey SHALL NOT expose any setters or mutation methods.

### Requirement 2: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the survey list view to store ReadOnlySurvey in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormSurvey populates the list view, THE FormSurvey SHALL create ListViewItem Tags containing ReadOnlySurvey instances obtained from PlayerContext.GetCurrentPlayerReadOnlySurveys().
2. WHEN the user selects a survey in the list view, THE FormSurvey SHALL extract the ReadOnlySurvey from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormSurvey filters the survey list, THE FormSurvey SHALL use ReadOnlySurvey properties for the filter comparison.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable Survey, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormSurvey SHALL NOT hold a direct reference to a mutable Survey in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE SurveyViewModel SHALL NOT expose the mutable Survey entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlySurvey into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a survey, THE SurveyViewModel SHALL copy all scalar field values from the ReadOnlySurvey into local properties: PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, AsteroidUUID, SurveyType.
2. WHEN the user selects a survey, THE SurveyViewModel SHALL deep-copy the Resources dictionary into a local Dictionary<string, SurveyResource> with new SurveyResource instances.
3. WHEN the user selects a survey, THE SurveyViewModel SHALL deep-copy the Properties dictionary into a local Dictionary<string, string>.
4. THE SurveyViewModel SHALL retain the original ReadOnlySurvey snapshot for dirty comparison.
5. THE SurveyViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
6. THE SurveyViewModel SHALL NOT hold a reference to the mutable Survey entity.

### Requirement 5: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want all editable controls to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormSurvey text boxes (txtPlanetName, txtSystemName, txtSurveyID, txtNickName, txtScannedBy, txtSensorAbundance, txtPurityModifier, txtScanLevel) SHALL read from and write to the SurveyViewModel local fields.
2. THE FormSurvey date picker (dtpScanDateTime) SHALL read from and write to the SurveyViewModel local DateTime field.
3. THE FormSurvey survey type combo (cmbSurveyTypeEdit) SHALL read from and write to the SurveyViewModel local SurveyType field.
4. THE FormSurvey scanner blueprint combo (cmbScannerBlueprint) SHALL read from and write to the SurveyViewModel local ScannerBlueprintUUID field.
5. THE FormSurvey resource grid (dgvResources) SHALL read from and write to the SurveyViewModel local Resources dictionary.

### Requirement 6: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the Survey entity on every keystroke, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE SurveyViewModel SHALL NOT write changes to the Survey entity on every keystroke or control change.
2. THE current write-through pattern (TextChanged handler sets viewModel property which sets entity property) SHALL be replaced with local-only state changes in the ViewModel.
3. THE dgvResources CellValueChanged handler SHALL update the ViewModel local Resources dictionary instead of the mutable entity Resources.

### Requirement 7: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether any field has been modified since the last load or save, so that the Save button enables only when changes exist.

#### Acceptance Criteria

1. THE SurveyViewModel SHALL expose an IsDirty property that returns true when any local field differs from the original ReadOnlySurvey snapshot.
2. THE IsDirty check SHALL compare all scalar fields: PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, SurveyType.
3. THE IsDirty check SHALL compare the Resources dictionary (same keys, and for each key the same Resource, Purity, and Amount values).
4. THE IsDirty check SHALL compare the Properties dictionary (same keys and same values).
5. WHEN the ViewModel is loaded from a ReadOnlySurvey, THE IsDirty property SHALL return false.
6. WHEN the ViewModel represents a new unsaved survey (original is null), THE IsDirty property SHALL return true once any field has a non-default value.
7. THE FormSurvey SHALL enable the Save button only when the SurveyViewModel IsDirty property returns true.

### Requirement 8: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different survey, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different survey in the list view and the SurveyViewModel is dirty, THE FormSurvey SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormSurvey SHALL call SurveyService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormSurvey SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormSurvey SHALL cancel the selection change and keep the current survey selected.

### Requirement 9: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the survey form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormSurvey (X button or MDI close) and the SurveyViewModel is dirty, THE FormSurvey SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormSurvey SHALL save and then close.
3. WHEN the user chooses Discard, THE FormSurvey SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormSurvey SHALL cancel the close and keep the form open.

### Requirement 10: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormSurvey has unsaved changes, THE FormSurvey OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormSurvey SHALL cancel the application exit by setting e.Cancel to true.

### Requirement 11: Unsaved Changes Prompt on New Survey

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the SurveyViewModel is dirty, THE FormSurvey SHALL prompt before clearing the form for the new survey.
2. WHEN the user chooses Save, THE FormSurvey SHALL save the current survey, then reset the form for a new survey.
3. WHEN the user chooses Discard, THE FormSurvey SHALL discard changes and reset the form for a new survey.
4. WHEN the user chooses Cancel, THE FormSurvey SHALL cancel the New operation and keep the current survey.

### Requirement 12: Unsaved Changes Prompt on Import

**User Story:** As a user, I want to be prompted about unsaved changes when I click Import, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks Import while the SurveyViewModel is dirty, THE FormSurvey SHALL prompt before proceeding with the import.
2. WHEN the user chooses Save, THE FormSurvey SHALL save the current survey, then proceed with the import.
3. WHEN the user chooses Discard, THE FormSurvey SHALL discard changes and proceed with the import.
4. WHEN the user chooses Cancel, THE FormSurvey SHALL cancel the Import operation and keep the current survey.


## Phase 3: SurveyService

### Requirement 13: SurveyService.Update

**User Story:** As a developer, I want a service method that applies survey changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE SurveyService SHALL provide an Update method accepting a UUID string and a SurveyUpdateRequest.
2. WHEN Update is called, THE SurveyService SHALL look up the mutable Survey by UUID via PlayerContext.FindMutableSurvey.
3. WHEN Update is called, THE SurveyService SHALL apply all changed scalar fields from the request to the entity: PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, SurveyType, AsteroidUUID.
4. WHEN Update is called, THE SurveyService SHALL replace the entity Resources dictionary with the request Resources dictionary.
5. WHEN Update is called, THE SurveyService SHALL replace the entity Properties dictionary with the request Properties dictionary.
6. WHEN Update is called, THE SurveyService SHALL persist via PlayerContext.WriteContext().
7. WHEN Update is called, THE SurveyService SHALL fire SurveyDataChanged event with the survey UUID.
8. WHEN Update is called, THE SurveyService SHALL return the updated ReadOnlySurvey.
9. IF the UUID is not found, THEN THE SurveyService SHALL throw an InvalidOperationException.

### Requirement 14: SurveyService.Create

**User Story:** As a developer, I want a service method that creates a new survey, so that survey creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE SurveyService SHALL provide a Create method accepting a SurveyCreateRequest.
2. WHEN Create is called, THE SurveyService SHALL create a new Survey entity with a generated UUID.
3. WHEN Create is called, THE SurveyService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE SurveyService SHALL populate all fields from the request: PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, SurveyType, AsteroidUUID, Resources, Properties.
5. WHEN Create is called, THE SurveyService SHALL add the survey to PlayerContext via AddSurvey.
6. WHEN Create is called, THE SurveyService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE SurveyService SHALL fire SurveyDataChanged event with the new survey UUID.
8. WHEN Create is called, THE SurveyService SHALL return the new ReadOnlySurvey.

### Requirement 15: SurveyService.Delete

**User Story:** As a developer, I want a service method that deletes a survey, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE SurveyService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE SurveyService SHALL remove the Survey from PlayerContext via RemoveSurvey.
3. WHEN Delete is called, THE SurveyService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE SurveyService SHALL fire SurveyDataChanged event with the deleted survey UUID.
5. IF the UUID is empty or the survey is not found, THEN THE SurveyService SHALL return without error.

### Requirement 16: SurveyService.Import

**User Story:** As a developer, I want a service method that handles clipboard import, so that import goes through the controlled gate instead of the form directly mutating entities.

#### Acceptance Criteria

1. THE SurveyService SHALL provide an Import method accepting a parsed temporary Survey object (from SurveyParser).
2. WHEN Import is called and an existing survey matches by PlanetName+SurveyID (case-insensitive via SurveyImportHelper.FindByKey), THE SurveyService SHALL merge the parsed data into the existing survey using SurveyImportHelper.MergeData, preserving the existing UUID and NickName.
3. WHEN Import is called and no existing survey matches, THE SurveyService SHALL create a new survey using SurveyImportHelper.CreateFromTemp with the current player UUID.
4. WHEN Import is called with an asteroid survey, THE SurveyService SHALL call SurveyImportHelper.LinkOrCreateAsteroid to auto-create or link the asteroid entity.
5. WHEN Import is called, THE SurveyService SHALL persist via PlayerContext.WriteContext().
6. WHEN Import is called, THE SurveyService SHALL fire SurveyDataChanged event with the imported survey UUID.
7. WHEN Import is called, THE SurveyService SHALL return the ReadOnlySurvey of the imported or updated survey.

### Requirement 17: PlayerContext.FindMutableSurvey

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable Survey entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableSurvey internal method accepting a UUID string.
2. THE FindMutableSurvey method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint.
3. THE FindMutableSurvey method SHALL be marked internal so only the service project can access it.

### Requirement 18: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new survey (IsNew is true), THE FormSurvey SHALL call SurveyService.Create with a SurveyCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing survey, THE FormSurvey SHALL call SurveyService.Update with the UUID and a SurveyUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlySurvey, THE FormSurvey SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlySurvey.
4. AFTER a successful save, THE SurveyViewModel IsDirty property SHALL return false.

### Requirement 19: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that surveys in use by mining rigs or build plans cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormSurvey SHALL check SurveyReferenceCounter for references to the current survey.
2. IF the survey has references (TotalCount > 0), THEN THE FormSurvey SHALL display a warning message and prevent deletion.
3. IF the survey has no references, THE FormSurvey SHALL prompt for confirmation before calling SurveyService.Delete.
4. AFTER successful deletion, THE FormSurvey SHALL clear the form and refresh the list view.

### Requirement 20: Import Flow Through Service

**User Story:** As a developer, I want the Import button to route through the service, so that clipboard import never directly mutates entities from the form.

#### Acceptance Criteria

1. WHEN the user clicks Import, THE FormSurvey SHALL validate clipboard content (HTML present, correct content type, player selected).
2. WHEN clipboard validation passes, THE FormSurvey SHALL call SurveyParser.ParseClipboardToTemp to get a temporary Survey object.
3. WHEN parsing succeeds, THE FormSurvey SHALL call SurveyService.Import with the temporary Survey object.
4. WHEN the service returns the ReadOnlySurvey, THE FormSurvey SHALL refresh the list view, select the imported survey, and load it into the ViewModel.

### Requirement 21: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the Survey entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE Survey entity SHALL only be mutated by SurveyService methods (Update, Create, Delete, Import), SurveyImportHelper (called by the service), JSON deserialization (loading from file), and migration code.
2. THE FormSurvey SHALL NOT directly set properties on a Survey.
3. THE SurveyViewModel SHALL NOT directly set properties on a Survey.


## Phase 4: Verification

### Requirement 22: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 23: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 24: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates Survey entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct Survey property sets SHALL only find matches in SurveyService, SurveyImportHelper, SurveyParser (populating temp objects), JSON deserialization, migration code, and the Survey class itself.
2. AFTER migration, a grep for direct SurveyResource property sets SHALL only find matches in SurveyService, SurveyImportHelper, SurveyParser (populating temp objects), JSON deserialization, and the SurveyResource class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid Survey entities, wrapping in ReadOnlySurvey and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, SurveyType, AsteroidUUID, and every key-value pair in Resources and Properties).

**Validates:** Requirements 4.1, 4.2, 4.3

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid Survey entities, wrapping in ReadOnlySurvey and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 7.1, 7.5

### Property 3: IsDirty Detects Any Single Field Change

FOR ALL valid Survey entities, after LoadFrom, changing any single field (PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, SurveyType, or any single Resources entry, or any single Properties entry) to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 7.1, 7.2, 7.3, 7.4

### Property 4: Service.Update Round-Trip

FOR ALL valid existing Survey entities and valid SurveyUpdateRequest values, calling Service.Update SHALL produce a ReadOnlySurvey whose fields match the request values (all scalar fields, Resources, and Properties).

**Validates:** Requirements 13.3, 13.4, 13.5, 13.8

### Property 5: Service.Create Round-Trip

FOR ALL valid SurveyCreateRequest values, calling Service.Create SHALL produce a ReadOnlySurvey whose fields match the request values and whose UUID is non-empty.

**Validates:** Requirements 14.2, 14.4, 14.8

### Property 6: Service.Delete Removes Survey

FOR ALL valid existing Survey entities, calling Service.Delete with the survey UUID SHALL cause the survey to no longer be findable via PlayerContext.

**Validates:** Requirements 15.1, 15.2

### Property 7: Import Dedup --- Existing Survey Preserves UUID and NickName

FOR ALL valid existing Survey entities and valid temporary Survey objects with matching PlanetName+SurveyID, calling Service.Import SHALL preserve the existing survey UUID and NickName while updating data fields.

**Validates:** Requirements 16.2

### Property 8: Import New --- Creates Survey with Non-Empty UUID

FOR ALL valid temporary Survey objects with no matching existing survey, calling Service.Import SHALL produce a ReadOnlySurvey with a non-empty UUID and fields matching the temporary object.

**Validates:** Requirements 16.3, 16.7

### Property 9: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Survey property sets SHALL only find matches in SurveyService, SurveyImportHelper, SurveyParser, JSON deserialization, and the Survey class itself.

**Validates:** Requirements 21.1, 21.2, 21.3, 24.1

## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the SurveyParser HTML parsing logic --- it continues to parse into temp Survey objects. The service handles the merge.
- Modifying the resource grid visual design or adding new resource types.
- Changing the SurveyReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Modifying the SurveyImportHelper logic --- it continues to work as-is, called by the service instead of the form.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- Multi-survey import (batch import) --- future enhancement.
