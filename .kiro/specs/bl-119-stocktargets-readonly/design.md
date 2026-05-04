# BL-119 Design: StockTargets Immutable Data Model with Service Layer

## Overview

BL-119 applies the immutable data model pattern to the StockTargets form. This is a dual-entity form managing both StockPlan and StockProfile. The ViewModel becomes a disconnected edit buffer for both entities. A new StockTargetMutationService is the sole mutator.

## Architecture

Form never touches entities. ViewModel holds disconnected edit buffers for both StockPlan and StockProfile. Service is the only mutator.

## Components and Interfaces

### StockTargetViewModel (Edit Buffer)

Dual-entity ViewModel with separate plan and profile sections:

Plan section: _planOriginal, _planUUID, _planOwnerUUID, PlanName, PlanIsActive, ReplenishmentBuildPlanUUID, Targets list, IsPlanNew, IsPlanDirty, LoadPlanFrom, ResetPlan, BuildPlanUpdateRequest, BuildPlanCreateRequest, AddTarget, RemoveTarget.

Profile section: _profileOriginal, _profileUUID, _profileOwnerUUID, ProfileName, ProfileIsActive, Entries list, IsProfileNew, IsProfileDirty, LoadProfileFrom, ResetProfile, BuildProfileUpdateRequest, BuildProfileCreateRequest, AddEntry, RemoveEntry.

### StockTargetMutationService

Plan methods: UpdatePlan, CreatePlan, DeletePlan.
Profile methods: UpdateProfile, CreateProfile, DeleteProfile.

## Data Models

### StockPlanUpdateRequest (New)
- Original (ReadOnlyStockPlan), Name, IsActive, ReplenishmentBuildPlanUUID, Targets (List of StockTarget)

### StockPlanCreateRequest (New)
- Name, IsActive, ReplenishmentBuildPlanUUID, Targets (List of StockTarget)

### StockProfileUpdateRequest (New)
- Original (ReadOnlyStockProfile), Name, IsActive, Entries (List of StockProfileEntry)

### StockProfileCreateRequest (New)
- Name, IsActive, Entries (List of StockProfileEntry)

## Correctness Properties

Properties 1-4: LoadFrom round-trip and IsDirty for both Plan and Profile.
Properties 5-7: Service CRUD round-trips for Plan.
Property 8: No Direct Mutation Outside Service.

## Testing Strategy

- OE2EmpireTracker.Tests/ViewModels/StockTargetViewModelPropertyTests.cs
- OE2EmpireTracker.Tests/ViewModels/StockTargetViewModelTests.cs
- OE2EmpireTracker.Tests/Services/StockTargetMutationServicePropertyTests.cs
- OE2EmpireTracker.Tests/Services/StockTargetMutationServiceTests.cs
- OE2EmpireTracker.Tests/Services/StockTargetMutationGuardTests.cs
