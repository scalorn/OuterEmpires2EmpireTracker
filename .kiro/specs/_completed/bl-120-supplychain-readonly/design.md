# BL-120 Design: SupplyChain Immutable Data Model with Service Layer

## Overview

BL-120 applies the immutable data model pattern to the SupplyChain form. The form stops directly mutating SupplyChain and SupplyChainStage entities. The ViewModel becomes a disconnected edit buffer for chain Name, IsActive, and the Stages list. A new SupplyChainMutationService is the sole mutator.

## Architecture

Form never touches the entity. ViewModel is a disconnected edit buffer. Service is the only mutator. All stage operations (add, remove, reorder, update) accumulate in the ViewModel until Save.

## Components and Interfaces

### SupplyChainViewModel (Edit Buffer)

Private fields: _original, _uuid, _ownerUUID, local Name, IsActive, local _stages (List of SupplyChainStage).

Public properties: Name (get/set), IsActive (get/set), Stages, UUID, OwnerUUID, Original, IsNew, IsDirty.

Methods:
- LoadFrom(ReadOnlySupplyChain): copies fields, deep-copies Stages, stores snapshot
- Reset(): clears all fields
- BuildUpdateRequest(), BuildCreateRequest()
- AddStage(SupplyChainStage), RemoveStage(int index), UpdateStage(int index, SupplyChainStage)
- MoveStageUp(int index), MoveStageDown(int index), RenumberStages()

### SupplyChainMutationService

- Update(uuid, SupplyChainUpdateRequest): applies fields, replaces Stages with Sequence renumbering, persists, fires event
- Create(SupplyChainCreateRequest): creates new entity, persists, fires event
- Delete(uuid): removes, persists, fires event

## Data Models

### SupplyChainUpdateRequest (New)
- Original (ReadOnlySupplyChain), Name, IsActive, Stages (List of SupplyChainStage)

### SupplyChainCreateRequest (New)
- Name, IsActive, Stages (List of SupplyChainStage)

## Correctness Properties

Same pattern as BL-112: LoadFrom round-trip, IsDirty tracking, service CRUD round-trips, mutation guard.

## Testing Strategy

- OE2EmpireTracker.Tests/ViewModels/SupplyChainViewModelPropertyTests.cs
- OE2EmpireTracker.Tests/ViewModels/SupplyChainViewModelTests.cs
- OE2EmpireTracker.Tests/Services/SupplyChainMutationServicePropertyTests.cs
- OE2EmpireTracker.Tests/Services/SupplyChainMutationServiceTests.cs
- OE2EmpireTracker.Tests/Services/SupplyChainMutationGuardTests.cs
