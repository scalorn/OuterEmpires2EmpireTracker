# Requirements Document

## Introduction

BL-120 restructures FormSupplyChain so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a SupplyChainMutationService that applies changes atomically. The form never directly mutates a SupplyChain or SupplyChainStage --- only the service does.

### Key Characteristics

1. **SupplyChain** has scalar fields (Name, IsActive) plus a nested Stages list of SupplyChainStage entries.
2. **SupplyChainStage** has fields: Sequence, StageType, LocationType, LocationUUID, ResourceName, ResourcePurity, AccumulationThreshold, ProductionRatePerHour, DeliveryRouteUUID.
3. **Stage operations are buffered** --- Add/remove/reorder/update stages accumulate in the ViewModel until Save.
4. **ReadOnly wrappers already complete** --- ReadOnlySupplyChain and ReadOnlySupplyChainStage are fully implemented.
5. **No write locks** --- No ReaderWriterLockSlim.
6. **No reference protection needed** --- Supply chains are not referenced by other entities.

## Glossary

- **SupplyChain**: The mutable entity with UUID, Name, OwnerUUID, IsActive, and a list of SupplyChainStage entries.
- **SupplyChainStage**: A nested object with Sequence, StageType, LocationType, LocationUUID, ResourceName, ResourcePurity, AccumulationThreshold, ProductionRatePerHour, DeliveryRouteUUID.
- **ReadOnlySupplyChain**: Immutable wrapper exposing only getter properties and read-only Stages list.
- **ReadOnlySupplyChainStage**: Immutable wrapper exposing all fields as read-only.
- **SupplyChainViewModel**: The ViewModel class holding a local edit buffer disconnected from the entity.
- **SupplyChainMutationService**: The sole mutator of SupplyChain entities.
- **FormSupplyChain**: The WinForms form for viewing and editing supply chains.

## Requirements

## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

#### Acceptance Criteria

1. WHEN populating the chain list view, THE form SHALL use ReadOnlySupplyChain in ListViewItem Tags.
2. WHEN selecting a chain, THE form SHALL extract ReadOnlySupplyChain and pass to ViewModel.
3. WHEN filtering, THE form SHALL use ReadOnlySupplyChain properties.

### Requirement 2: No Mutable Entity in Read-Only Paths

#### Acceptance Criteria

1. THE form SHALL NOT hold direct references to mutable SupplyChain in read-only code paths.
2. THE ViewModel SHALL NOT expose the mutable entity.

## Phase 2: ViewModel as Local Edit Buffer

### Requirement 3: ViewModel Copies Fields from ReadOnly

#### Acceptance Criteria

1. THE ViewModel SHALL copy Name and IsActive from ReadOnlySupplyChain into local properties.
2. THE ViewModel SHALL deep-copy the Stages list into local List of SupplyChainStage.
3. THE ViewModel SHALL retain the original snapshot for dirty comparison.
4. THE ViewModel SHALL store UUID and OwnerUUID.

### Requirement 4: Controls Bind to ViewModel Local State

#### Acceptance Criteria

1. Chain name and active checkbox SHALL read from and write to ViewModel local fields.
2. Stages grid SHALL display stages from ViewModel local Stages list.
3. Stage add/remove/reorder/update SHALL modify ViewModel local Stages list only.

### Requirement 5: Dirty Tracking

#### Acceptance Criteria

1. THE ViewModel SHALL expose an IsDirty property.
2. IsDirty SHALL compare Name, IsActive, and Stages against original snapshot.
3. Immediately after LoadFrom, IsDirty SHALL return false.

### Requirement 6: Unsaved Changes Prompts

#### Acceptance Criteria

1. Selection change, New, form close, and application exit SHALL check dirty state and prompt.
2. Three-button dialog: Save, Discard, Cancel.

## Phase 3: SupplyChainMutationService

### Requirement 7: Service CRUD

#### Acceptance Criteria

1. Update(uuid, request): applies fields, replaces Stages with correct Sequence numbering, persists, fires event, returns ReadOnlySupplyChain.
2. Create(request): creates new entity with UUID, OwnerUUID, fields from request, persists, fires event.
3. Delete(uuid): removes, persists, fires event. No-op if not found.
4. IF UUID not found on Update, THEN throw InvalidOperationException.

### Requirement 8: PlayerContext.FindMutableSupplyChain

#### Acceptance Criteria

1. Internal method following cache-based lookup pattern.

### Requirement 9: Service Is the Only Mutator

#### Acceptance Criteria

1. SupplyChain entities SHALL only be mutated by the service, deserialization, and migration code.

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
### Property 6: Service.Delete Removes Chain
### Property 7: No Direct Mutation Outside Service

## Out of Scope

- Changing other entity types to the service pattern.
- Undo/redo.
- Flow summary computation --- reads from ViewModel, does not mutate SupplyChain.
