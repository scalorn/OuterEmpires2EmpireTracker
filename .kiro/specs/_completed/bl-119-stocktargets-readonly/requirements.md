# Requirements Document

## Introduction

BL-119 restructures FormStockTargets so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a StockTargetMutationService that applies changes atomically. The form never directly mutates a StockPlan, StockTarget, StockProfile, or StockProfileEntry --- only the service does. This follows the same immutable data model pattern established in BL-108 through BL-118.

### Key Characteristics

1. **Dual-entity form** --- FormStockTargets manages both StockPlan (Plans tab) and StockProfile (Profiles tab) in a single form with two tabs.
2. **StockPlan** has scalar fields (Name, IsActive, ReplenishmentBuildPlanUUID) plus a nested Targets list of StockTarget entries.
3. **StockProfile** has scalar fields (Name, IsActive) plus a nested Entries list of StockProfileEntry entries.
4. **ReadOnly wrappers already complete** --- ReadOnlyStockPlan, ReadOnlyStockTarget, ReadOnlyStockProfile, ReadOnlyStockProfileEntry are fully implemented.
5. **No write locks** --- No ReaderWriterLockSlim on either entity.
6. **No reference protection needed for StockPlan** --- StockPlans are not referenced by other entities.
7. **StockPlan references BuildPlan** --- ReplenishmentBuildPlanUUID links to a BuildPlan but this is informational.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel covers both entities:
- **StockPlan:** Name, IsActive, ReplenishmentBuildPlanUUID, Targets list
- **StockProfile:** Name, IsActive, Entries list

## Glossary

- **StockPlan**: The mutable entity with UUID, Name, OwnerUUID, IsActive, ReplenishmentBuildPlanUUID, and a list of StockTarget entries.
- **StockTarget**: A nested object with UUID, ItemType, ItemReferenceID, ItemName, ShipTemplateUUID, TargetQuantity, CriticalThreshold, Scope, LocationUUID.
- **StockProfile**: The mutable entity with UUID, Name, OwnerUUID, IsActive, and a list of StockProfileEntry entries.
- **StockProfileEntry**: A nested object with GroupID and StockPlanUUID.
- **ReadOnlyStockPlan**: Immutable wrapper exposing only getter properties and read-only Targets list.
- **ReadOnlyStockTarget**: Immutable wrapper exposing all StockTarget fields as read-only.
- **ReadOnlyStockProfile**: Immutable wrapper exposing only getter properties and read-only Entries list.
- **ReadOnlyStockProfileEntry**: Immutable wrapper exposing GroupID and StockPlanUUID as read-only.
- **StockTargetViewModel**: The ViewModel class holding local edit buffers for both StockPlan and StockProfile.
- **StockTargetMutationService**: The sole mutator of StockPlan and StockProfile entities.
- **FormStockTargets**: The WinForms form with Plans and Profiles tabs.
- **PlayerContext**: The singleton service that manages player data persistence.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: Plan List View Uses ReadOnly Wrappers

#### Acceptance Criteria

1. WHEN the FormStockTargets populates the plan list view, THE form SHALL create ListViewItem Tags containing ReadOnlyStockPlan instances.
2. WHEN the user selects a plan, THE form SHALL extract ReadOnlyStockPlan from Tag and pass to ViewModel.
3. WHEN filtering, THE form SHALL use ReadOnlyStockPlan properties.

### Requirement 2: Profile List View Uses ReadOnly Wrappers

#### Acceptance Criteria

1. WHEN the FormStockTargets populates the profile list view, THE form SHALL create ListViewItem Tags containing ReadOnlyStockProfile instances.
2. WHEN the user selects a profile, THE form SHALL extract ReadOnlyStockProfile from Tag and pass to ViewModel.

### Requirement 3: No Mutable Entity in Read-Only Paths

#### Acceptance Criteria

1. THE FormStockTargets SHALL NOT hold direct references to mutable StockPlan or StockProfile in read-only code paths.
2. THE StockTargetViewModel SHALL NOT expose mutable entities via public properties.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies StockPlan Fields

#### Acceptance Criteria

1. THE ViewModel SHALL copy Name, IsActive, ReplenishmentBuildPlanUUID from ReadOnlyStockPlan into local properties.
2. THE ViewModel SHALL deep-copy the Targets list into local List of StockTarget.
3. THE ViewModel SHALL retain the original snapshot for dirty comparison.
4. THE ViewModel SHALL store UUID and OwnerUUID.

### Requirement 5: ViewModel Copies StockProfile Fields

#### Acceptance Criteria

1. THE ViewModel SHALL copy Name, IsActive from ReadOnlyStockProfile into local properties.
2. THE ViewModel SHALL deep-copy the Entries list into local List of StockProfileEntry.
3. THE ViewModel SHALL retain the original snapshot for dirty comparison.

### Requirement 6: Controls Bind to ViewModel Local State

#### Acceptance Criteria

1. Plan tab controls SHALL read from and write to ViewModel local plan fields.
2. Profile tab controls SHALL read from and write to ViewModel local profile fields.
3. No entity mutation on control changes.

### Requirement 7: Dirty Tracking

#### Acceptance Criteria

1. THE ViewModel SHALL expose IsPlanDirty and IsProfileDirty properties.
2. IsPlanDirty SHALL compare Name, IsActive, ReplenishmentBuildPlanUUID, and Targets.
3. IsProfileDirty SHALL compare Name, IsActive, and Entries.
4. Immediately after load, dirty properties SHALL return false.

### Requirement 8: Unsaved Changes Prompts

#### Acceptance Criteria

1. Selection change, New, form close, and application exit SHALL check dirty state and prompt.
2. Three-button dialog: Save, Discard, Cancel.
3. Cancel on form close sets e.Cancel = true.


## Phase 3: StockTargetMutationService

### Requirement 9: Service Plan CRUD

#### Acceptance Criteria

1. UpdatePlan(uuid, request): applies fields, replaces Targets, persists, fires event, returns ReadOnlyStockPlan.
2. CreatePlan(request): creates new StockPlan with UUID, OwnerUUID, fields from request, persists, fires event.
3. DeletePlan(uuid): removes from PlayerContext, persists, fires event. No-op if not found.

### Requirement 10: Service Profile CRUD

#### Acceptance Criteria

1. UpdateProfile(uuid, request): applies fields, replaces Entries, persists, fires event, returns ReadOnlyStockProfile.
2. CreateProfile(request): creates new StockProfile with UUID, OwnerUUID, fields from request, persists, fires event.
3. DeleteProfile(uuid): removes from PlayerContext, persists, fires event. No-op if not found.

### Requirement 11: PlayerContext FindMutable Methods

#### Acceptance Criteria

1. FindMutableStockPlan internal method following cache-based lookup pattern.
2. FindMutableStockProfile internal method following cache-based lookup pattern.

### Requirement 12: Service Is the Only Mutator

#### Acceptance Criteria

1. StockPlan and StockProfile entities SHALL only be mutated by the service, deserialization, and migration code.
2. FormStockTargets and ViewModel SHALL NOT directly set entity properties.


## Phase 4: Verification

### Requirement 13: Existing Tests Pass

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 14: Audit Clean

#### Acceptance Criteria

1. THE audit SHALL report no new findings beyond the accepted baseline.

### Requirement 15: No Direct Mutation Outside Service

#### Acceptance Criteria

1. Grep for direct StockPlan/StockProfile property sets SHALL only find matches in allowed files.


## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All StockPlan Fields

FOR ALL valid StockPlan entities, LoadFrom SHALL produce a ViewModel whose local fields exactly match.

**Validates:** Requirements 4.1, 4.2, 4.3

### Property 2: LoadFrom Round-Trip Preserves All StockProfile Fields

FOR ALL valid StockProfile entities, LoadFrom SHALL produce a ViewModel whose local fields exactly match.

**Validates:** Requirements 5.1, 5.2, 5.3

### Property 3: IsDirty False After LoadFrom (Plan)

FOR ALL valid StockPlan entities, IsPlanDirty SHALL return false after LoadFrom.

**Validates:** Requirements 7.1, 7.4

### Property 4: IsDirty False After LoadFrom (Profile)

FOR ALL valid StockProfile entities, IsProfileDirty SHALL return false after LoadFrom.

**Validates:** Requirements 7.1, 7.4

### Property 5: Service.UpdatePlan Round-Trip

FOR ALL valid update requests, UpdatePlan SHALL produce a ReadOnlyStockPlan matching the request.

**Validates:** Requirements 9.1

### Property 6: Service.CreatePlan Round-Trip

FOR ALL valid create requests, CreatePlan SHALL produce a ReadOnlyStockPlan with non-empty UUID.

**Validates:** Requirements 9.2

### Property 7: Service.DeletePlan Removes Plan

FOR ALL valid StockPlan entities, DeletePlan SHALL cause it to no longer be findable.

**Validates:** Requirements 9.3

### Property 8: No Direct Mutation Outside Service

Static analysis SHALL confirm property sets only appear in allowed files.

**Validates:** Requirements 12.1, 12.2, 15.1


## Out of Scope

- Changing other entity types to the service pattern.
- Undo/redo.
- Stock check/generate features --- they read from ViewModel but compute results, not mutate stock entities.
