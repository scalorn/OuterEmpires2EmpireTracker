# Requirements Document

## Introduction

BL-121 restructures FormContacts so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a ContactsService that applies changes atomically. The form never directly mutates a Faction or ExternalCharacter --- only the service does.

### Key Characteristics

1. **Dual-entity form** --- FormContacts manages both Faction and ExternalCharacter in a single form with two panels.
2. **Faction** has scalar fields only: Name, Description. No nested collections.
3. **ExternalCharacter** has scalar fields only: Name, FactionUUID. No nested collections.
4. **No OwnerUUID on entities** --- Faction and ExternalCharacter do not have OwnerUUID fields.
5. **ReadOnly wrappers already complete** --- ReadOnlyFaction and ReadOnlyExternalCharacter are fully implemented.
6. **No write locks** --- No ReaderWriterLockSlim.
7. **Reference protection** --- FactionReferenceCounter checks ExternalCharacters and PlayerProfiles before allowing faction deletion.

## Glossary

- **Faction**: The mutable entity with UUID, Name, Description.
- **ExternalCharacter**: The mutable entity with UUID, Name, FactionUUID.
- **ReadOnlyFaction**: Immutable wrapper exposing UUID, Name, Description as read-only.
- **ReadOnlyExternalCharacter**: Immutable wrapper exposing UUID, Name, FactionUUID as read-only.
- **ContactsViewModel**: The ViewModel class holding local edit buffers for both Faction and ExternalCharacter.
- **ContactsService**: The sole mutator of Faction and ExternalCharacter entities.
- **FormContacts**: The WinForms form with faction and character panels.
- **FactionReferenceCounter**: Counts references from ExternalCharacters, PlayerProfiles, and SupplyChains.

## Requirements

## Phase 1: Read-Only Consumer Migration

### Requirement 1: Faction List Uses ReadOnly Wrappers
#### Acceptance Criteria
1. WHEN populating the faction list, THE form SHALL use ReadOnlyFaction in ListViewItem Tags.
2. WHEN selecting a faction, THE form SHALL extract ReadOnlyFaction and pass to ViewModel.

### Requirement 2: Character List Uses ReadOnly Wrappers
#### Acceptance Criteria
1. WHEN populating the character list, THE form SHALL use ReadOnlyExternalCharacter in ListViewItem Tags.
2. WHEN selecting a character, THE form SHALL extract ReadOnlyExternalCharacter and pass to ViewModel.

### Requirement 3: No Mutable Entity in Read-Only Paths
#### Acceptance Criteria
1. THE form SHALL NOT hold direct references to mutable Faction or ExternalCharacter in read-only paths.

## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Faction Fields
#### Acceptance Criteria
1. THE ViewModel SHALL copy Name and Description from ReadOnlyFaction into local properties.
2. THE ViewModel SHALL retain the original snapshot for dirty comparison.
3. THE ViewModel SHALL store UUID.

### Requirement 5: ViewModel Copies Character Fields
#### Acceptance Criteria
1. THE ViewModel SHALL copy Name and FactionUUID from ReadOnlyExternalCharacter into local properties.
2. THE ViewModel SHALL retain the original snapshot for dirty comparison.
3. THE ViewModel SHALL store UUID.

### Requirement 6: Dirty Tracking
#### Acceptance Criteria
1. THE ViewModel SHALL expose IsFactionDirty and IsCharacterDirty properties.
2. IsFactionDirty SHALL compare Name and Description.
3. IsCharacterDirty SHALL compare Name and FactionUUID.

### Requirement 7: Unsaved Changes Prompts
#### Acceptance Criteria
1. Selection change, New, form close, and application exit SHALL check dirty state and prompt.
2. Three-button dialog: Save, Discard, Cancel.

## Phase 3: ContactsService

### Requirement 8: Faction CRUD
#### Acceptance Criteria
1. UpdateFaction(uuid, request): applies Name and Description, persists, fires event, returns ReadOnlyFaction.
2. CreateFaction(request): creates new Faction with UUID, fields from request, persists, fires event.
3. DeleteFaction(uuid): removes, persists, fires event. No-op if not found.

### Requirement 9: Character CRUD
#### Acceptance Criteria
1. UpdateCharacter(uuid, request): applies Name and FactionUUID, persists, fires event, returns ReadOnlyExternalCharacter.
2. CreateCharacter(request): creates new ExternalCharacter with UUID, fields from request, persists, fires event.
3. DeleteCharacter(uuid): removes, persists, fires event. No-op if not found.

### Requirement 10: Delete Flow with Reference Protection (Faction)
#### Acceptance Criteria
1. WHEN deleting a faction, THE form SHALL check FactionReferenceCounter.
2. IF references > 0, THE form SHALL show warning and prevent deletion.
3. IF no references, THE form SHALL prompt for confirmation.

### Requirement 11: PlayerContext FindMutable Methods
#### Acceptance Criteria
1. FindMutableFaction internal method.
2. FindMutableExternalCharacter internal method.

### Requirement 12: Service Is the Only Mutator
#### Acceptance Criteria
1. Faction and ExternalCharacter entities SHALL only be mutated by the service, deserialization, and migration code.

## Phase 4: Verification

### Requirement 13: Existing Tests Pass
### Requirement 14: Audit Clean
### Requirement 15: No Direct Mutation Outside Service

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves Faction Fields
### Property 2: LoadFrom Round-Trip Preserves Character Fields
### Property 3: IsDirty False After LoadFrom (Faction)
### Property 4: IsDirty False After LoadFrom (Character)
### Property 5: Service.UpdateFaction Round-Trip
### Property 6: Service.CreateFaction Round-Trip
### Property 7: Service.DeleteFaction Removes Faction
### Property 8: No Direct Mutation Outside Service

## Out of Scope

- Changing other entity types to the service pattern.
- Undo/redo.
- Adding OwnerUUID to Faction/ExternalCharacter.
