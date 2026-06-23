# BL-122 Design: Asteroid Immutable Data Model with Service Layer

## Overview

BL-122 applies the immutable data model pattern to the Asteroid form. The form stops directly mutating Asteroid and AsteroidReserve entities. The ViewModel becomes a disconnected edit buffer for Name, SystemName, and the Reserves list. A new AsteroidService is the sole mutator.

## Architecture

Form never touches the entity. ViewModel is a disconnected edit buffer. Service is the only mutator. All reserve operations accumulate in the ViewModel until Save.

## Components and Interfaces

### AsteroidViewModel (Edit Buffer)

Private fields: _original, _uuid, local Name, SystemName, local _reserves (List of AsteroidReserve).

Public properties: Name (get/set), SystemName (get/set), Reserves, UUID, Original, IsNew, IsDirty.

Methods:
- LoadFrom(ReadOnlyAsteroid): copies fields, deep-copies Reserves, stores snapshot
- Reset(): clears all fields
- BuildUpdateRequest(), BuildCreateRequest()
- AddReserve(AsteroidReserve), RemoveReserve(int index), UpdateReserve(int index, AsteroidReserve)

### AsteroidService

- Update(uuid, AsteroidUpdateRequest): applies fields, replaces Reserves, persists, fires event
- Create(AsteroidCreateRequest): creates new entity, persists, fires event
- Delete(uuid): removes, persists, fires event

## Data Models

### AsteroidUpdateRequest (New)
- Original (ReadOnlyAsteroid), Name, SystemName, Reserves (List of AsteroidReserve)

### AsteroidCreateRequest (New)
- Name, SystemName, Reserves (List of AsteroidReserve)

## Testing Strategy

- OE2EmpireTracker.Tests/ViewModels/AsteroidViewModelPropertyTests.cs
- OE2EmpireTracker.Tests/ViewModels/AsteroidViewModelTests.cs
- OE2EmpireTracker.Tests/Services/AsteroidServicePropertyTests.cs
- OE2EmpireTracker.Tests/Services/AsteroidServiceTests.cs
- OE2EmpireTracker.Tests/Services/AsteroidMutationGuardTests.cs
