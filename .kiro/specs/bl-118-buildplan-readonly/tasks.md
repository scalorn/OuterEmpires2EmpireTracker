# Implementation Plan: BL-118 BuildPlan Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the BuildPlan form. Create BuildPlanViewModel as a disconnected edit buffer for plan Name, Description, IsActive, and Items list. Create BuildPlanMutationService as the sole mutator with CRUD methods. Create DTO request objects. Migrate FormBuildPlanner to use ReadOnly wrappers in list view, route all mutations through the service, and add unsaved changes prompts. All item operations (add, remove, modify) accumulate in the ViewModel edit buffer until Save. Add property-based tests, unit tests, and a mutation guard test.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create BuildPlanUpdateRequest DTO
    - Create OE2EmpireTracker/Models/BuildPlanUpdateRequest.cs
    - Properties: Original (ReadOnlyBuildPlan), Name (string), Description (string), IsActive (bool), Items (List of BuildItem)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1_

  - [ ] 1.2 Create BuildPlanCreateRequest DTO
    - Create OE2EmpireTracker/Models/BuildPlanCreateRequest.cs
    - Properties: Name (string), Description (string), IsActive (bool), Items (List of BuildItem)
    - No UUID (service assigns), no OwnerUUID (service sets from current player)
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 12.1_

  - [ ] 1.3 Add FindMutableBuildPlan internal method to PlayerContext
    - Follow the same cache-based lookup pattern as other FindMutable methods
    - Mark method internal so only the service can access it
    - _Requirements: 14.1, 14.2, 14.3_

- [ ] 2. Create BuildPlanViewModel as disconnected edit buffer
  - [ ] 2.1 Create BuildPlanViewModel class
    - Create OE2EmpireTracker/ViewModels/BuildPlanViewModel.cs
    - Private fields: _original (ReadOnlyBuildPlan), _uuid, _ownerUUID, local Name (string), Description (string), IsActive (bool), local _items (List of BuildItem)
    - Public properties: Name (get/set), Description (get/set), IsActive (get/set), Items (List of BuildItem), UUID, OwnerUUID, Original, IsNew, IsDirty
    - LoadFrom(ReadOnlyBuildPlan): copies all scalar fields, deep-copies Items (new BuildItem instances preserving all fields), stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates BuildPlanUpdateRequest from local state with deep-copied Items
    - BuildCreateRequest(): creates BuildPlanCreateRequest from local state with deep-copied Items
    - IsDirty: compares Name, Description, IsActive, and Items against _original snapshot
    - AddItem(BuildItem): adds to local Items list
    - RemoveItem(string uuid): removes from local Items list
    - FindItem(string uuid): finds item in local list by UUID
    - Keep NLog Logger declaration
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 3. Create BuildPlanMutationService with CRUD methods
  - [ ] 3.1 Create BuildPlanMutationService class
    - Create OE2EmpireTracker/Services/BuildPlanMutationService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, BuildPlanUpdateRequest): looks up mutable entity via FindMutableBuildPlan, applies Name, Description, IsActive from request, replaces Items list with deep copy, persists via WriteContext(), fires BuildPlanDataChanged event, returns ReadOnlyBuildPlan. Throws InvalidOperationException if UUID not found.
    - Create(BuildPlanCreateRequest): creates new BuildPlan with generated UUID, sets OwnerUUID from current player, populates fields and Items from request, adds to PlayerContext, persists, fires event, returns ReadOnlyBuildPlan
    - Delete(string uuid): looks up mutable entity, removes from PlayerContext via RemoveBuildPlan, persists, fires event. Returns silently if UUID empty or not found.
    - Include NLog Logger declaration
    - No write locks
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 12.1, 12.2, 12.3, 12.4, 12.5, 13.1, 13.2, 13.3, 17.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly
  - Build with zero errors and zero warnings
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate FormBuildPlanner to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulatePlanList to store ReadOnlyBuildPlan in ListViewItem Tags
    - Change filter logic to use ReadOnlyBuildPlan properties
    - Remove direct mutable BuildPlan references from list view code paths
    - _Requirements: 1.1, 1.2, 1.3, 2.1_

  - [ ] 5.2 Wire ViewModel as edit buffer
    - Add BuildPlanViewModel _viewModel field and BuildPlanMutationService _buildPlanService field
    - Change selection handler to extract ReadOnlyBuildPlan from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - Remove _selectedPlan mutable reference field
    - _Requirements: 1.2, 3.1, 3.2, 4.1, 4.3_

  - [ ] 5.3 Replace write-through with local-only ViewModel updates
    - Change TxtPlanName_TextChanged to set _viewModel.Name
    - Change TxtDescription_TextChanged to set _viewModel.Description
    - Change ChkIsActive_CheckedChanged to set _viewModel.IsActive
    - Change item add/remove handlers to call _viewModel item methods
    - Remove direct WriteContext() calls from control change handlers
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call service.Create; else call service.Update
    - After save, refresh list view and reload _viewModel from returned ReadOnlyBuildPlan
    - _Requirements: 15.1, 15.2, 15.3, 15.4_

  - [ ] 5.5 Wire Delete button through service with reference protection
    - Check BuildPlanReferenceCounter for references before delete
    - If references > 0, show warning with count
    - If no references, prompt for confirmation before calling service.Delete
    - After deletion, clear form and refresh list view
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [ ] 5.6 Wire execution features to read from ViewModel
    - Change Start Manufacturing, Queue Calc, Generate Delivery, Auto-Assign, Allocate to read from _viewModel instead of _selectedPlan
    - No behavioral change --- these features read from local state, do not mutate BuildPlan

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompt on selection change
    - In plan list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 6.2 Add unsaved changes prompt on New button
    - In CmdNew_Click handler, check _viewModel.IsDirty before clearing form
    - Same three-button dialog
    - _Requirements: 9.1, 9.2_

  - [ ] 6.3 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Cancel sets e.Cancel = true to prevent close
    - _Requirements: 8.1, 8.2, 10.1, 10.2_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Build with zero errors and zero warnings
  - All existing tests pass

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property test: LoadFrom round-trip preserves all fields
    - Create OE2EmpireTracker.Tests/ViewModels/BuildPlanViewModelPropertyTests.cs
    - Create ValidBuildPlanGen() generator producing random BuildPlan entities with random Name, Description, IsActive, UUID, OwnerUUID, and 0-5 BuildItem entries
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 3.1, 3.2, 3.3**
    - Add Compile Include to test csproj

  - [ ] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 6.1, 6.4**

  - [ ] 8.3 Write property test: IsDirty detects scalar field change
    - **Property 3: IsDirty Detects Scalar Field Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 6.1, 6.2**

  - [ ] 8.4 Write property test: IsDirty detects Items change
    - **Property 4: IsDirty Detects Items Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - **Validates: Requirements 6.1, 6.3**

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for BuildPlanViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/BuildPlanViewModelTests.cs
    - Tests: Reset clears all fields, IsNew true after Reset, IsNew false after LoadFrom, IsDirty true for new plan with non-empty Name, BuildUpdateRequest copies all fields, BuildCreateRequest copies all fields, UUID and OwnerUUID preserved from LoadFrom, AddItem increases count, RemoveItem decreases count, FindItem returns correct item
    - Add Compile Include to test csproj
    - _Requirements: 3.1, 3.2, 6.1, 6.4, 6.5_

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/BuildPlanMutationServicePropertyTests.cs
    - **Property 5: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 11.3, 11.4, 11.7**
    - Add Compile Include to test csproj

  - [ ] 10.2 Write property test: Service.Create round-trip
    - **Property 6: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 12.2, 12.4**

  - [ ] 10.3 Write property test: Service.Delete removes plan
    - **Property 7: Service.Delete Removes Plan**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.1, 13.2**

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for BuildPlanMutationService
    - Create OE2EmpireTracker.Tests/Services/BuildPlanMutationServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires BuildPlanDataChanged event, Create fires BuildPlanDataChanged event, Delete fires BuildPlanDataChanged event, Update replaces Items list, Create populates Items list from request
    - Add Compile Include to test csproj
    - _Requirements: 11.7, 11.8, 12.2, 12.3, 12.5, 13.3_

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for BuildPlan
    - Create OE2EmpireTracker.Tests/Services/BuildPlanMutationGuardTests.cs
    - Follow existing MutationGuardTests pattern
    - Scan for direct BuildPlan property sets (Name, UUID, OwnerUUID, Description, IsActive), assert they only appear in BuildPlanMutationService.cs, BuildPlan.cs, PlayerContext.cs, and test code
    - Scan for direct BuildItem property sets outside allowed files
    - Scan for direct BuildPlan.Items list mutation (Add, Remove, Clear, Items =)
    - **Validates: Property 8**
    - **Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2**
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - node .kiro/tools/audit.js reports no new findings

## Notes

- BuildPlan has 3 scalar fields (Name, Description, IsActive) plus Items list
- BuildItem has 20+ fields --- deep copy must preserve all of them
- BuildPlanMutationService is simple: no write locks, no immediate operations, no import method
- All item operations accumulate in the ViewModel edit buffer until Save
- ReadOnlyBuildPlan and ReadOnlyBuildItem are already complete
- BuildPlanReferenceCounter checks stock plans (1 source type)
- Execution features (Start Manufacturing, etc.) read from ViewModel but mutate other entities --- remain as-is
- FormStructureAllocation reads from PlayerContext, does not mutate BuildPlan --- remains as-is
- GetCurrentPlayerReadOnlyBuildPlans needs to be added to PlayerContext (or GetCurrentPlayerBuildPlans already returns ReadOnly)
- FindMutableBuildPlan needs to be added
- BuildPlanDataChanged event and OnBuildPlanDataChanged already exist in PlayerContext
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
