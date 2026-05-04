# Implementation Plan: BL-119 StockTargets Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the StockTargets form. This is a dual-entity form managing StockPlan and StockProfile. Create StockTargetViewModel as a disconnected edit buffer for both entities. Create StockTargetMutationService as the sole mutator with CRUD methods for both plans and profiles.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create StockPlanUpdateRequest and StockPlanCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 9.1, 9.2_

  - [ ] 1.2 Create StockProfileUpdateRequest and StockProfileCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 10.1, 10.2_

  - [ ] 1.3 Add FindMutableStockPlan and FindMutableStockProfile internal methods to PlayerContext
    - _Requirements: 11.1, 11.2_

- [ ] 2. Create StockTargetViewModel as disconnected edit buffer
  - [ ] 2.1 Create StockTargetViewModel class
    - Dual-entity ViewModel with plan and profile sections
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 7.4_

- [ ] 3. Create StockTargetMutationService with CRUD methods
  - [ ] 3.1 Create StockTargetMutationService class
    - Plan CRUD: UpdatePlan, CreatePlan, DeletePlan
    - Profile CRUD: UpdateProfile, CreateProfile, DeleteProfile
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 9.1, 9.2, 9.3, 10.1, 10.2, 10.3, 12.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly

- [ ] 5. Migrate FormStockTargets to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers in both list views
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 3.2_
  - [ ] 5.2 Wire ViewModel as edit buffer for Plans tab
    - _Requirements: 4.1, 4.2, 6.1_
  - [ ] 5.3 Wire ViewModel as edit buffer for Profiles tab
    - _Requirements: 5.1, 5.2, 6.2_
  - [ ] 5.4 Replace write-through with local-only ViewModel updates
    - _Requirements: 6.1, 6.2, 6.3_
  - [ ] 5.5 Wire Save/Delete buttons through service for both tabs

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompts for Plans tab
    - _Requirements: 8.1, 8.2, 8.3_
  - [ ] 6.2 Add unsaved changes prompts for Profiles tab
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property tests for StockPlan LoadFrom and IsDirty
    - Create OE2EmpireTracker.Tests/ViewModels/StockTargetViewModelPropertyTests.cs
    - Add Compile Include to test csproj
  - [ ] 8.2 Write property tests for StockProfile LoadFrom and IsDirty

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for StockTargetViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/StockTargetViewModelTests.cs
    - Add Compile Include to test csproj

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property tests for plan CRUD
    - Create OE2EmpireTracker.Tests/Services/StockTargetMutationServicePropertyTests.cs
    - Add Compile Include to test csproj

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for StockTargetMutationService
    - Create OE2EmpireTracker.Tests/Services/StockTargetMutationServiceTests.cs
    - Add Compile Include to test csproj

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for StockPlan and StockProfile
    - Create OE2EmpireTracker.Tests/Services/StockTargetMutationGuardTests.cs
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean

## Notes

- Dual-entity form: StockPlan (Plans tab) and StockProfile (Profiles tab)
- StockTarget has 9 fields, StockProfileEntry has 2 fields
- No reference protection needed for StockPlan deletion
- ReadOnly wrappers already complete for all 4 types
- No write locks
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
