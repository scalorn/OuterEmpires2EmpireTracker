# Requirements Document

## Introduction

BL-122 restructures FormAsteroid so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through an AsteroidService that applies changes atomically. The form never directly mutates an Asteroid or AsteroidReserve --- only the service does.

### Key Characteristics

1. **Asteroid** has scalar fields (Name, SystemName) plus a nested Reserves list of AsteroidReserve entries.
2. **AsteroidReserve** has fields: ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp.
3. **No OwnerUUID** --- Asteroid does not have an OwnerUUID field.
4. **Reserve operations are buffered** --- Add/remove/edit reserves accumulate in the ViewModel until Save.
5. **ReadOnly wrappers already complete** --- ReadOnlyAsteroid and ReadOnlyAsteroidReserve are fully implemented.
6. **No write locks** --- No ReaderWriterLockSlim.
7. **No reference protection needed** --- Asteroids are not referenced by other entities.
8. **Linked surveys display** --- FormAsteroid shows linked surveys (read-only display), does not mutate them.

## Glossary

- **Asteroid**: The mutable entity with UUID, Name, SystemName, and a list of AsteroidReserve entries.
- **AsteroidReserve**: A nested object with ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp.
- **ReadOnlyAsteroid**: Immutable wrapper exposing only getter properties and read-only Reserves list.
- **ReadOnlyAsteroidReserve**: Immutable wrapper exposing all fields as read-only.
- **AsteroidViewModel**: The ViewModel class holding a local edit buffer disconnected from the entity.
- **AsteroidService**: The sole mutator of Asteroid entities.
- **FormAsteroid**: The WinForms form for viewing and editing asteroids.

## Requirements

## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers
#### Acceptance Criteria
1. WHEN populating the asteroid list, THE form SHALL use ReadOnlyAsteroid in ListViewItem Tags.
2. WHEN selecting an asteroid, THE form SHALL extract ReadOnlyAsteroid and pass to ViewModel.
3. WHEN filtering, THE form SHALL use ReadOnlyAsteroid properties.

### Requirement 2: No Mutable Entity in Read-Only Paths
#### Acceptance Criteria
1. THE form SHALL NOT hold direct references to mutable Asteroid in read-only code paths.
2. THE ViewModel SHALL NOT expose the mutable entity.

## Phase 2: ViewModel as Local Edit Buffer

### Requirement 3: ViewModel Copies Fields from ReadOnly
#### Acceptance Criteria
1. THE ViewModel SHALL copy Name and SystemName from ReadOnlyAsteroid into local properties.
2. THE ViewModel SHALL deep-copy the Reserves list into local List of AsteroidReserve.
3. THE ViewModel SHALL retain the original snapshot for dirty comparison.
4. THE ViewModel SHALL store UUID.

### Requirement 4: Controls Bind to ViewModel Local State
#### Acceptance Criteria
1. Asteroid name and system name text boxes SHALL read from and write to ViewModel local fields.
2. Reserves grid SHALL display reserves from ViewModel local Reserves list.
3. Reserve add/remove/edit SHALL modify ViewModel local Reserves list only.

### Requirement 5: Dirty Tracking
#### Acceptance Criteria
1. THE ViewModel SHALL expose an IsDirty property.
2. IsDirty SHALL compare Name, SystemName, and Reserves against original snapshot.
3. Immediately after LoadFrom, IsDirty SHALL return false.

### Requirement 6: Unsaved Changes Prompts
#### Acceptance Criteria
1. Selection change, New, form close, and application exit SHALL check dirty state and prompt.
2. Three-button dialog: Save, Discard, Cancel.

## Phase 3: AsteroidService

### Requirement 7: Service CRUD
#### Acceptance Criteria
1. Update(uuid, request): applies fields, replaces Reserves, persists, fires event, returns ReadOnlyAsteroid.
2. Create(request): creates new Asteroid with UUID, fields from request, persists, fires event.
3. Delete(uuid): removes, persists, fires event. No-op if not found.
4. IF UUID not found on Update, THEN throw InvalidOperationException.

### Requirement 8: PlayerContext.FindMutableAsteroid
#### Acceptance Criteria
1. Internal method following cache-based lookup pattern.

### Requirement 9: Service Is the Only Mutator
#### Acceptance Criteria
1. Asteroid entities SHALL only be mutated by the service, deserialization, and migration code.

## Phase 4: Verification

### Requirement 10: Existing Tests Pass
### Requirement 11: Audit Clean
### Requirement 12: No Direct Mutation Outside Service

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All Fields
### Property 2: IsDirty False After LoadFrom
### Property 3: IsDirty Detects Changes
### Property 4: Service.Update Round-Trip
### Property 5: Service.Create Round-Trip
### Property 6: Service.Delete Removes Asteroid
### Property 7: No Direct Mutation Outside Service

## Out of Scope

- Linked surveys display --- reads from PlayerContext, does not mutate Asteroid.
- Undo/redo.
- Adding OwnerUUID to Asteroid.
