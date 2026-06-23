# Requirements Document

## Introduction

BL-111 restructures FormPlayerProfile so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a PlayerProfileService that applies changes atomically. The form never directly mutates a PlayerProfile — only the service does. This follows the same immutable data model pattern established in BL-108 (blueprint form).

### Key Differences from BL-108

1. **Skills are complex nested objects** — PlayerProfile has a `Dictionary<string, PlayerSkill>` where each skill has Level, TrainingStarted, CompletionTime. The edit buffer needs to handle this dictionary of objects.
2. **Ranks are nested objects** — Three PlayerRank objects (Public, Private, Military) each with Rank, CurrentXP, NextXP, Title.
3. **Skill training timer** — PlayerSkillBlock has a timer that fires when training completes. In the edit buffer model, the timer updates the ViewModel's local state, not the entity directly. Save commits the training completion.
4. **Cascade delete** — Deleting a profile removes all owned data across multiple entity types.
5. **No global/player split** — Unlike blueprints, profiles are always player-scoped. No MoveToGlobal/MoveToPlayer needed.
6. **Faction text field** — The form has a faction text field, not a combo bound to a list.

## Glossary

- **PlayerProfile**: The mutable entity representing a player character's profile data, including name, faction, credits, ranks, skills, and skill groups.
- **ReadOnlyPlayerProfile**: An immutable wrapper around PlayerProfile that exposes only getter properties and read-only skill/rank access.
- **ReadOnlyPlayerRank**: An immutable wrapper around PlayerRank exposing Rank, CurrentXP, NextXP, Title as read-only.
- **ReadOnlyPlayerSkill**: An immutable wrapper around PlayerSkill exposing Level, TrainingStarted, CompletionTime as read-only.
- **PlayerProfileViewModel**: The ViewModel that wraps profile data for UI binding. Currently uses write-through; will become a local edit buffer.
- **PlayerProfileService**: A new service class that is the sole mutator of PlayerProfile entities (create, update, delete, import).
- **PlayerSkillBlock**: A custom UserControl that displays and edits a single skill's level, training status, and completion countdown.
- **FormPlayerProfile**: The WinForms form for viewing and editing player profiles.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of field values in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any local field differs from the original snapshot.
- **PlayerProfileUpdateRequest**: A DTO carrying the current local state from the ViewModel to the service for an update operation.
- **PlayerProfileCreateRequest**: A DTO carrying field values for creating a new profile.
- **CascadeDelete**: The operation that removes a profile and all owned data (colonies, blueprints, surveys, etc.) across multiple entity types.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: ReadOnlyPlayerProfile Gap Fill

**User Story:** As a developer, I want ReadOnlyPlayerProfile to expose all properties the form needs, so that the form can operate entirely through read-only wrappers without accessing the mutable entity.

#### Acceptance Criteria

1. THE ReadOnlyPlayerProfile SHALL expose a Faction property returning the entity's Faction string.
2. THE ReadOnlyPlayerProfile SHALL expose a TotalCredits property returning the entity's TotalCredits decimal value.
3. THE ReadOnlyPlayerProfile SHALL expose a SkillPoints property returning the entity's SkillPoints integer value.
4. THE ReadOnlyPlayerProfile SHALL expose a CitizenId property returning the entity's CitizenId string.
5. THE ReadOnlyPlayerProfile SHALL expose a RegistrationDate property returning the entity's RegistrationDate string.
6. THE ReadOnlyPlayerProfile SHALL expose an ActiveTime property returning the entity's ActiveTime string.
7. THE ReadOnlyPlayerProfile SHALL expose a Skills property returning an IReadOnlyDictionary mapping skill name strings to ReadOnlyPlayerSkill wrappers.
8. THE ReadOnlyPlayerProfile SHALL expose a SkillGroups accessor that allows iterating all skill group names and their boolean states.

### Requirement 2: ReadOnlyPlayerSkill Gap Fill

**User Story:** As a developer, I want ReadOnlyPlayerSkill to expose training status and completion time, so that skill blocks can display training state through read-only wrappers.

#### Acceptance Criteria

1. THE ReadOnlyPlayerSkill SHALL expose a TrainingStarted property returning the entity's TrainingStarted boolean.
2. THE ReadOnlyPlayerSkill SHALL expose a CompletionTime property returning a read-only view of the entity's CountDownTime (TimeRemaining, TimeRemainingString, StartTime, EndTime).

### Requirement 3: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the profile list view to store ReadOnlyPlayerProfile in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormPlayerProfile populates the list view, THE FormPlayerProfile SHALL create ListViewItem Tags containing ReadOnlyPlayerProfile instances obtained from PlayerContext.GetReadOnlyPlayerProfileList().
2. WHEN the user selects a profile in the list view, THE FormPlayerProfile SHALL extract the ReadOnlyPlayerProfile from the selected item's Tag and pass it to the ViewModel's LoadFrom method.

### Requirement 4: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable PlayerProfile, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormPlayerProfile SHALL NOT hold a direct reference to a mutable PlayerProfile in any read-only code path (list view Tags, display-only fields, filter logic).
2. THE PlayerProfileViewModel SHALL NOT expose the mutable PlayerProfile entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 5: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyPlayerProfile into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a profile, THE PlayerProfileViewModel SHALL copy all field values from the ReadOnlyPlayerProfile into local properties: Name, Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime.
2. WHEN the user selects a profile, THE PlayerProfileViewModel SHALL copy all three rank objects (Public, Private, Military) into local rank structs containing Rank, CurrentXP, NextXP, Title.
3. WHEN the user selects a profile, THE PlayerProfileViewModel SHALL copy the skills dictionary into a local dictionary of skill data objects containing Level, TrainingStarted, and CompletionTime state.
4. WHEN the user selects a profile, THE PlayerProfileViewModel SHALL copy all skill group boolean states into a local dictionary.
5. THE PlayerProfileViewModel SHALL retain the original ReadOnlyPlayerProfile snapshot for dirty comparison.
6. THE PlayerProfileViewModel SHALL NOT hold a reference to the mutable PlayerProfile entity.

### Requirement 6: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want all editable controls to read from and write to the ViewModel's local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormPlayerProfile text boxes (txtPlayerName, cmbFaction, txtTotalCredits, txtSkillPoints) SHALL read from and write to the PlayerProfileViewModel's local fields.
2. THE FormPlayerProfile rank text boxes (txtPublicRank, txtPublicRankCurXP, txtPublicRankNextXP, txtPrivateRank, txtPrivateRankCurXP, txtPrivateRankNextXP, txtMilitaryRank, txtMilitaryRankCurXP, txtMilitaryRankNextXP) SHALL read from and write to the PlayerProfileViewModel's local rank data.
3. THE FormPlayerProfile skill group checkboxes SHALL read from and write to the PlayerProfileViewModel's local skill group dictionary.
4. THE PlayerSkillBlock controls SHALL read from and write to the PlayerProfileViewModel's local skill data, not to the mutable PlayerSkill entity.

### Requirement 7: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the PlayerProfile entity on every keystroke, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE PlayerProfileViewModel SHALL NOT write changes to the PlayerProfile entity on every keystroke or control change.
2. THE current write-through pattern (TextChanged handler sets viewModel property which sets entity property) SHALL be replaced with local-only state changes in the ViewModel.
3. THE PlayerSkillBlock SHALL NOT directly mutate the PlayerSkill entity when the user starts training, when the timer completes, or when the user edits the completion time.

### Requirement 8: Skill Training Timer in Edit Buffer

**User Story:** As a developer, I want the skill training timer to update the ViewModel's local state instead of the entity, so that training completion is committed only on Save.

#### Acceptance Criteria

1. WHEN a skill training timer fires and the countdown reaches zero, THE PlayerSkillBlock SHALL update the ViewModel's local skill data (set TrainingStarted to false, increment Level by 1) instead of mutating the entity.
2. WHEN the user clicks Start Training on a skill, THE PlayerSkillBlock SHALL update the ViewModel's local skill data (set TrainingStarted to true, set CompletionTime) instead of mutating the entity.
3. WHEN the user manually edits a skill's completion time text box, THE PlayerSkillBlock SHALL update the ViewModel's local skill data instead of mutating the entity.
4. WHILE a skill is training, THE PlayerSkillBlock SHALL display the countdown from the ViewModel's local CompletionTime state.

### Requirement 9: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether any field has been modified since the last load or save, so that the Save button enables only when changes exist.

#### Acceptance Criteria

1. THE PlayerProfileViewModel SHALL expose an IsDirty property that returns true when any local field differs from the original ReadOnlyPlayerProfile snapshot.
2. THE IsDirty check SHALL compare all scalar fields: Name, Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime.
3. THE IsDirty check SHALL compare all three rank objects field-by-field (Rank, CurrentXP, NextXP, Title for each of Public, Private, Military).
4. THE IsDirty check SHALL compare the skills dictionary (Level, TrainingStarted, CompletionTime state for each skill).
5. THE IsDirty check SHALL compare the skill groups dictionary (boolean state for each group).
6. WHEN the ViewModel is loaded from a ReadOnlyPlayerProfile, THE IsDirty property SHALL return false.
7. WHEN the ViewModel represents a new unsaved profile (original is null), THE IsDirty property SHALL return true once any field has a non-default value.
8. THE FormPlayerProfile SHALL enable the Save button only when the PlayerProfileViewModel IsDirty property returns true.


### Requirement 10: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different profile, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different profile in the list view and the PlayerProfileViewModel is dirty, THE FormPlayerProfile SHALL prompt: "Save changes to '{name}'?" with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormPlayerProfile SHALL call PlayerProfileService.Update, then load the new selection.
3. WHEN the user chooses Discard, THE FormPlayerProfile SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormPlayerProfile SHALL cancel the selection change and keep the current profile selected.

### Requirement 11: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the profile form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormPlayerProfile (X button or MDI close) and the PlayerProfileViewModel is dirty, THE FormPlayerProfile SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormPlayerProfile SHALL save and then close.
3. WHEN the user chooses Discard, THE FormPlayerProfile SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormPlayerProfile SHALL cancel the close and keep the form open.

### Requirement 12: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormPlayerProfile has unsaved changes, THE FormPlayerProfile OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormPlayerProfile SHALL cancel the application exit by setting e.Cancel to true.

### Requirement 13: Unsaved Changes Prompt on New Profile

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the PlayerProfileViewModel is dirty, THE FormPlayerProfile SHALL prompt before clearing the form for the new profile.
2. WHEN the user chooses Save, THE FormPlayerProfile SHALL save the current profile, then reset the form for a new profile.
3. WHEN the user chooses Discard, THE FormPlayerProfile SHALL discard changes and reset the form for a new profile.
4. WHEN the user chooses Cancel, THE FormPlayerProfile SHALL cancel the New operation and keep the current profile.


## Phase 3: PlayerProfileService

### Requirement 14: PlayerProfileService.Update

**User Story:** As a developer, I want a service method that applies profile changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE PlayerProfileService SHALL provide an Update method accepting a UUID string and a PlayerProfileUpdateRequest.
2. WHEN Update is called, THE PlayerProfileService SHALL look up the mutable PlayerProfile by UUID via PlayerContext.
3. WHEN Update is called, THE PlayerProfileService SHALL apply all changed fields from the request to the entity: Name, Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime.
4. WHEN Update is called, THE PlayerProfileService SHALL apply rank changes for all three rank tracks (Public, Private, Military): Rank, CurrentXP, NextXP, Title.
5. WHEN Update is called, THE PlayerProfileService SHALL apply skill changes for all skills in the request: Level, TrainingStarted, CompletionTime state.
6. WHEN Update is called, THE PlayerProfileService SHALL apply skill group changes for all groups in the request.
7. WHEN Update is called, THE PlayerProfileService SHALL persist via PlayerContext.WriteContext().
8. WHEN Update is called, THE PlayerProfileService SHALL fire PlayerProfileDataChanged event.
9. WHEN Update is called, THE PlayerProfileService SHALL return the updated ReadOnlyPlayerProfile.
10. IF the UUID is not found, THEN THE PlayerProfileService SHALL throw an InvalidOperationException.

### Requirement 15: PlayerProfileService.Create

**User Story:** As a developer, I want a service method that creates a new profile, so that profile creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE PlayerProfileService SHALL provide a Create method accepting a PlayerProfileCreateRequest.
2. WHEN Create is called, THE PlayerProfileService SHALL create a new PlayerProfile entity with a generated UUID.
3. WHEN Create is called, THE PlayerProfileService SHALL populate all fields from the request: Name, Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime, ranks, skills, skill groups.
4. WHEN Create is called, THE PlayerProfileService SHALL add the profile to PlayerContext via AddPlayerProfile.
5. WHEN Create is called, THE PlayerProfileService SHALL persist via PlayerContext.WriteContext().
6. WHEN Create is called, THE PlayerProfileService SHALL fire PlayerProfilesChanged and PlayerProfileDataChanged events.
7. WHEN Create is called, THE PlayerProfileService SHALL return the new ReadOnlyPlayerProfile.

### Requirement 16: PlayerProfileService.Delete

**User Story:** As a developer, I want a service method that deletes a profile and cascades to all owned data, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE PlayerProfileService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE PlayerProfileService SHALL remove the PlayerProfile from PlayerContext via RemovePlayerProfile.
3. WHEN Delete is called, THE PlayerProfileService SHALL cascade delete all owned data by calling PlayerContext.CascadeDeletePlayer with the profile UUID.
4. WHEN Delete is called, THE PlayerProfileService SHALL persist via PlayerContext.WriteContext().
5. WHEN Delete is called, THE PlayerProfileService SHALL fire PlayerProfilesChanged and PlayerProfileDataChanged events.
6. IF the UUID is empty or the profile is not found, THEN THE PlayerProfileService SHALL return without error.

### Requirement 17: PlayerProfileService.Import

**User Story:** As a developer, I want a service method that handles clipboard import, so that import goes through the controlled gate instead of the form directly mutating entities.

#### Acceptance Criteria

1. THE PlayerProfileService SHALL provide an Import method accepting a parsed PlayerProfile (temp object from the parser).
2. WHEN Import is called and an existing profile matches by name (case-insensitive), THE PlayerProfileService SHALL merge the parsed data into the existing profile using the MergeProfile logic, preserving the existing UUID.
3. WHEN Import is called and no existing profile matches, THE PlayerProfileService SHALL create a new profile with a generated UUID and add it to PlayerContext.
4. WHEN Import is called, THE PlayerProfileService SHALL persist via PlayerContext.WriteContext().
5. WHEN Import is called, THE PlayerProfileService SHALL fire PlayerProfilesChanged and PlayerProfileDataChanged events.
6. WHEN Import is called, THE PlayerProfileService SHALL return the ReadOnlyPlayerProfile of the imported or updated profile.

### Requirement 18: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new profile (IsNew is true), THE FormPlayerProfile SHALL call PlayerProfileService.Create with a PlayerProfileCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing profile, THE FormPlayerProfile SHALL call PlayerProfileService.Update with the UUID and a PlayerProfileUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyPlayerProfile, THE FormPlayerProfile SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyPlayerProfile.
4. AFTER a successful save, THE PlayerProfileViewModel IsDirty property SHALL return false.

### Requirement 19: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the PlayerProfile entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE PlayerProfile entity SHALL only be mutated by PlayerProfileService methods (Update, Create, Delete, Import), JSON deserialization (loading from file), and migration code.
2. THE FormPlayerProfile SHALL NOT directly set properties on a PlayerProfile.
3. THE PlayerProfileViewModel SHALL NOT directly set properties on a PlayerProfile.
4. THE PlayerSkillBlock SHALL NOT directly set properties on a PlayerSkill.


## Phase 4: Verification

### Requirement 20: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 21: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 22: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates PlayerProfile entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct PlayerProfile property sets SHALL only find matches in PlayerProfileService, JSON deserialization, migration code, and the PlayerProfile class itself.
2. AFTER migration, a grep for direct PlayerSkill property sets SHALL only find matches in PlayerProfileService, JSON deserialization, PlayerProfileParser (populating temp objects), and the PlayerSkill class itself.
3. AFTER migration, a grep for direct PlayerRank property sets SHALL only find matches in PlayerProfileService, JSON deserialization, PlayerProfileParser (populating temp objects), and the PlayerRank class itself.

## Out of Scope

- Changing other entity types (Colony, Survey, etc.) to the service pattern — separate BL items.
- Actual remote service calls — this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo — future enhancement on top of the edit buffer pattern.
- Changing the PlayerSkillBlock's visual design or adding new skill-related features.
- Modifying the PlayerProfileParser — it continues to parse into temp PlayerProfile objects. The service handles the merge.