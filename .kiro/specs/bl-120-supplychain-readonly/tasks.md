# Implementation Plan: BL-120 SupplyChain Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the SupplyChain form. Create SupplyChainViewModel as a disconnected edit buffer. Create SupplyChainMutationService as the sole mutator. Migrate FormSupplyChain to use ReadOnly wrappers and add unsaved changes prompts.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create SupplyChainUpdateRequest and SupplyChainCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 7.1, 7.2_
  - [ ] 1.2 Add FindMutableSupplyChain internal method to PlayerContext
    - _Requirements: 8.1_

- [ ] 2. Create SupplyChainViewModel as disconnected edit buffer
  - [ ] 2.1 Create SupplyChainViewModel class
    - Fields: Name, IsActive, Stages list
    - Methods: LoadFrom, Reset, BuildUpdateRequest, BuildCreateRequest, IsDirty
    - Stage operations: AddStage, RemoveStage, UpdateStage, MoveStageUp, MoveStageDown, RenumberStages
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3_

- [ ] 3. Create SupplyChainMutationService with CRUD methods
  - [ ] 3.1 Create SupplyChainMutationService class
    - Update, Create, Delete methods
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 9.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly

- [ ] 5. Migrate FormSupplyChain to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2_
  - [ ] 5.2 Wire ViewModel as edit buffer
    - _Requirements: 3.1, 4.1, 4.2, 4.3_
  - [ ] 5.3 Replace write-through with local-only ViewModel updates
  - [ ] 5.4 Wire Save/Delete buttons through service

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompts on selection change, New, form close, app exit
    - _Requirements: 6.1, 6.2_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property tests for SupplyChainViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/SupplyChainViewModelPropertyTests.cs
    - Add Compile Include to test csproj

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for SupplyChainViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/SupplyChainViewModelTests.cs
    - Add Compile Include to test csproj

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property tests for SupplyChainMutationService
    - Create OE2EmpireTracker.Tests/Services/SupplyChainMutationServicePropertyTests.cs
    - Add Compile Include to test csproj

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for SupplyChainMutationService
    - Create OE2EmpireTracker.Tests/Services/SupplyChainMutationServiceTests.cs
    - Add Compile Include to test csproj

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for SupplyChain
    - Create OE2EmpireTracker.Tests/Services/SupplyChainMutationGuardTests.cs
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean

## Notes

- SupplyChain has 2 scalar fields (Name, IsActive) plus Stages list
- SupplyChainStage has 9 fields including Sequence for ordering
- No reference protection needed
- ReadOnly wrappers already complete
- No write locks
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
