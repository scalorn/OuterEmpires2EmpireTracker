# Implementation Plan: BL-122 Asteroid Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Asteroid form. Create AsteroidViewModel as a disconnected edit buffer. Create AsteroidService as the sole mutator.

## Tasks

- [x] 1. DTO request models and PlayerContext accessors
  - [x] 1.1 Create AsteroidUpdateRequest and AsteroidCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 7.1, 7.2_
  - [x] 1.2 Add FindMutableAsteroid internal method to PlayerContext
    - _Requirements: 8.1_

- [x] 2. Create AsteroidViewModel as disconnected edit buffer
  - [x] 2.1 Create AsteroidViewModel class
    - Fields: Name, SystemName, Reserves list
    - Methods: LoadFrom, Reset, BuildUpdateRequest, BuildCreateRequest, IsDirty
    - Reserve operations: AddReserve, RemoveReserve, UpdateReserve
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3_

- [x] 3. Create AsteroidService with CRUD methods
  - [x] 3.1 Create AsteroidService class
    - Update, Create, Delete methods
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 9.1_

- [x] 4. Checkpoint --- Verify new classes compile cleanly

- [x] 5. Migrate FormAsteroid to ReadOnly wrappers and service
  - [x] 5.1 Replace mutable entity references with ReadOnly wrappers
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2_
  - [x] 5.2 Wire ViewModel as edit buffer
    - _Requirements: 3.1, 4.1, 4.2, 4.3_
  - [x] 5.3 Replace write-through with local-only ViewModel updates
  - [x] 5.4 Wire Save/Delete buttons through service

- [x] 6. Add unsaved changes prompts
  - [x] 6.1 Add unsaved changes prompts on selection change, New, form close, app exit
    - _Requirements: 6.1, 6.2_

- [x] 7. Checkpoint --- Verify form migration compiles and existing tests pass

- [x] 8. Add ViewModel property tests
  - [x] 8.1 Write property tests for AsteroidViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/AsteroidViewModelPropertyTests.cs
    - Add Compile Include to test csproj

- [x] 9. Add ViewModel unit tests
  - [x] 9.1 Write unit tests for AsteroidViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/AsteroidViewModelTests.cs
    - Add Compile Include to test csproj

- [x] 10. Add Service property tests
  - [x] 10.1 Write property tests for AsteroidService
    - Create OE2EmpireTracker.Tests/Services/AsteroidServicePropertyTests.cs
    - Add Compile Include to test csproj

- [x] 11. Add Service unit tests
  - [x] 11.1 Write unit tests for AsteroidService
    - Create OE2EmpireTracker.Tests/Services/AsteroidServiceTests.cs
    - Add Compile Include to test csproj

- [x] 12. Add mutation guard test
  - [x] 12.1 Write mutation guard test for Asteroid
    - Create OE2EmpireTracker.Tests/Services/AsteroidMutationGuardTests.cs
    - Add Compile Include to test csproj

- [x] 13. Final checkpoint --- Full build, all tests pass, audit clean

## Notes

- Asteroid has 2 scalar fields (Name, SystemName) plus Reserves list
- AsteroidReserve has 5 fields
- No OwnerUUID on Asteroid
- No reference protection needed
- ReadOnly wrappers already complete
- No write locks
- Linked surveys display reads from PlayerContext, does not mutate Asteroid
- AsteroidDataChanged event already exists in PlayerContext
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
