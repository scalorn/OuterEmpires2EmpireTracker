# Implementation Plan: BL-110 Survey Immutable Data Model

## Overview

Apply the immutable data model pattern (established in BL-108 blueprints, BL-111 player profiles, and BL-123 pricing plans) to the Survey form. Fill ReadOnlySurvey gaps, rewrite SurveyViewModel as a disconnected edit buffer, create SurveyService as the sole mutator with CRUD and Import methods, create DTO request objects, and migrate FormSurvey to use ReadOnly wrappers. Add unsaved changes prompts, delete with reference protection, import flow through service, property-based tests, unit tests, and a mutation guard test.

## Tasks

- [x] 1. ReadOnlySurvey gap fill and DTO request models
  - [x] 1.1 Add missing properties to ReadOnlySurvey
    - Add ScannedBy property returning _entity.ScannedBy
    - Add DateTime property returning _entity.DateTime
    - Add ScannerBlueprintUUID property returning _entity.ScannerBlueprintUUID
    - Add Properties property returning IReadOnlyDictionary<string, string> view of _entity.Properties
    - All existing properties remain unchanged
    - No setters or mutation methods
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 1.2 Create SurveyUpdateRequest DTO
    - Create OE2EmpireTracker/Models/SurveyUpdateRequest.cs
    - Properties: Original (ReadOnlySurvey), PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, AsteroidUUID, SurveyType, Resources (Dictionary<string, SurveyResource>), Properties (Dictionary<string, string>)
    - Add <Compile Include= Models\SurveyUpdateRequest.cs /> to OE2EmpireTracker.csproj
    - _Requirements: 13.1_

  - [x] 1.3 Create SurveyCreateRequest DTO
    - Create OE2EmpireTracker/Models/SurveyCreateRequest.cs
    - Properties: PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, AsteroidUUID, SurveyType, Resources (Dictionary<string, SurveyResource>), Properties (Dictionary<string, string>)
    - No UUID (service assigns it), no OwnerUUID (service sets from current player)
    - Add <Compile Include=Models\SurveyCreateRequest.cs /> to OE2EmpireTracker.csproj
    - _Requirements: 14.1_

- [x] 2. Rewrite SurveyViewModel as disconnected edit buffer
  - [x] 2.1 Rewrite SurveyViewModel class
    - Rewrite OE2EmpireTracker/ViewModels/SurveyViewModel.cs as a disconnected edit buffer
    - Remove constructor dependencies on PlayerContext and EmpireContext
    - Remove mutable Survey reference (_survey field and Data property)
    - Add private fields: _original (ReadOnlySurvey), _uuid, _ownerUUID, and local edit fields for all scalars (PlanetName, SystemName, SurveyID, NickName, ScannedBy, DateTime, ScannerBlueprintUUID, AsteroidUUID, SurveyType)
    - Add local edit collections: _resources (Dictionary<string, SurveyResource>), _properties (Dictionary<string, string>)
    - Public properties: all scalar fields with get/set, Resources, Properties, UUID, OwnerUUID, Original, IsNew, IsDirty, DisplayDateTime
    - Sensor reading convenience accessors: SensorAbundance, PurityModifier, ScanLevel (read/write Properties dictionary)
    - LoadFrom(ReadOnlySurvey): copies all scalar fields, deep-copies Resources (new SurveyResource per entry), deep-copies Properties, stores original snapshot
    - Reset(): clears all fields to defaults, sets _original to null
    - BuildUpdateRequest(): creates SurveyUpdateRequest with deep-copied Resources and Properties
    - BuildCreateRequest(): creates SurveyCreateRequest with deep-copied Resources and Properties
    - IsDirty: compares all scalars, Resources (same keys + same Resource/Purity/Amount per key), and Properties (same keys + same values) against _original; for new surveys (_original == null), returns true once any field has non-default value
    - Private helpers: ResourcesEqual, PropertiesEqual, DeepCopyResources
    - Keep NLog Logger declaration
    - _Requirements: 3.2, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

- [x] 3. Create PlayerContext.FindMutableSurvey and SurveyService
  - [x] 3.1 Add FindMutableSurvey internal method to PlayerContext
    - Follow the same cache-based lookup pattern as FindMutableBlueprint and FindMutablePricingPlan
    - Add _surveyCache dictionary field with lock-based lazy initialization
    - Mark method internal so only the service can access it
    - Invalidate cache when survey list changes (AddSurvey, RemoveSurvey)
    - _Requirements: 17.1, 17.2, 17.3_

  - [x] 3.2 Create SurveyService class
    - Create OE2EmpireTracker/Services/SurveyService.cs
    - Constructor takes PlayerContext dependency
    - Update(string uuid, SurveyUpdateRequest): looks up mutable entity via FindMutableSurvey, applies all scalar fields, replaces Resources and Properties dictionaries, persists via WriteContext(), fires SurveyDataChanged event, returns ReadOnlySurvey. Throws InvalidOperationException if UUID not found.
    - Create(SurveyCreateRequest): creates new Survey with generated UUID, sets OwnerUUID from current player, populates all fields from request, adds to PlayerContext, persists, fires event, returns ReadOnlySurvey
    - Delete(string uuid): removes Survey from PlayerContext via RemoveSurvey, persists, fires event. Returns silently if UUID empty or not found.
    - Import(Survey tempSurvey): calls SurveyImportHelper.FindByKey to check for existing match; if found merges via MergeData (preserving UUID and NickName); if not found creates via CreateFromTemp; if asteroid calls LinkOrCreateAsteroid; adds new survey to PlayerContext if created; persists; fires event; returns ReadOnlySurvey
    - Include NLog Logger declaration
    - Add <Compile Include= Services\SurveyService.cs /> to OE2EmpireTracker.csproj
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 15.1, 15.2, 15.3, 15.4, 15.5, 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7_

- [x] 4. Checkpoint --- Verify new classes compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Migrate FormSurvey to ReadOnly wrappers and ViewModel
  - [x] 5.1 Replace mutable entity references with ReadOnly wrappers in list view
    - Change PopulateSurveyList to store ReadOnlySurvey in ListViewItem Tags (from PlayerContext.GetCurrentPlayerReadOnlySurveys())
    - Change filter logic to use ReadOnlySurvey properties for filter comparison
    - Remove any direct mutable Survey references from list view code paths
    - _Requirements: 2.1, 2.3, 3.1_

  - [x] 5.2 Wire ViewModel as edit buffer
    - Add SurveyViewModel _viewModel field (no constructor dependencies) and SurveyService _surveyService field
    - Change selection handler to extract ReadOnlySurvey from Tag and call _viewModel.LoadFrom()
    - Change PopulateForm to read from _viewModel local fields instead of entity
    - Change ClearForm to call _viewModel.Reset()
    - _Requirements: 2.2, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 5.3 Replace write-through with local-only ViewModel updates
    - Change text box TextChanged handlers (txtPlanetName, txtSystemName, txtSurveyID, txtNickName, txtScannedBy, txtSensorAbundance, txtPurityModifier, txtScanLevel) to set _viewModel local fields instead of entity fields
    - Change date picker handler to set _viewModel.DateTime instead of entity.DateTime
    - Change survey type combo handler to set _viewModel.SurveyTypeValue instead of entity.SurveyType
    - Change scanner blueprint combo handler to set _viewModel.ScannerBlueprintUUID instead of entity.ScannerBlueprintUUID
    - Change dgvResources CellValueChanged to update _viewModel.Resources instead of entity.Resources
    - Remove any direct WriteContext() calls from control change handlers
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 5.4 Wire Save button through service
    - Change Save handler: if _viewModel.IsNew, call _surveyService.Create(BuildCreateRequest()); else call _surveyService.Update(uuid, BuildUpdateRequest())
    - After save, refresh list view and reload _viewModel from returned ReadOnlySurvey
    - Enable Save button only when _viewModel.IsDirty is true
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 7.7_

  - [x] 5.5 Wire Delete button through service with reference protection
    - Check SurveyReferenceCounter for references before delete
    - If TotalCount > 0, show warning message listing reference counts and prevent deletion
    - If no references, prompt for confirmation before calling _surveyService.Delete(uuid)
    - After deletion, clear form and refresh list view
    - _Requirements: 19.1, 19.2, 19.3, 19.4_

  - [x] 5.6 Wire Import button through service
    - Validate clipboard content (HTML present, correct content type, player selected)
    - Call SurveyParser.ParseClipboardToTemp to get temporary Survey object
    - Call _surveyService.Import(tempSurvey) instead of directly creating/merging entities
    - After import, refresh list view, select imported survey, load into ViewModel
    - _Requirements: 20.1, 20.2, 20.3, 20.4_

- [x] 6. Add unsaved changes prompts
  - [x] 6.1 Add unsaved changes prompt on selection change
    - In survey list selection handler, check _viewModel.IsDirty before loading new selection
    - Show three-button dialog: Save, Discard, Cancel
    - Save: call service Create/Update, then load new selection
    - Discard: discard changes, load new selection
    - Cancel: cancel selection change, keep current survey selected
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 6.2 Add unsaved changes prompt on New button
    - In New handler, check _viewModel.IsDirty before clearing form
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [x] 6.3 Add unsaved changes prompt on Import button
    - In Import handler, check _viewModel.IsDirty before proceeding with import
    - Same three-button dialog: Save, Discard, Cancel
    - _Requirements: 12.1, 12.2, 12.3, 12.4_

  - [x] 6.4 Add unsaved changes prompt on form close and application exit
    - Override OnFormClosing to check _viewModel.IsDirty
    - Show same three-button dialog
    - Cancel sets e.Cancel = true to prevent close
    - Handles both form close (X button) and application exit (MainWindow closing)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 10.1, 10.2_

- [x] 7. Checkpoint --- Verify form migration compiles and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Add ViewModel property tests
  - [x] 8.1 Write property test: LoadFrom round-trip preserves all fields
    - Create OE2EmpireTracker.Tests/ViewModels/SurveyViewModelPropertyTests.cs
    - Create ValidSurveyGen() generator producing random Survey entities with random scalar fields, random SurveyType, random Resources (0-5 entries with random Resource name, Purity from GameConstants purities, numeric Amount), random Properties (0-3 entries from SensorAbundance, PurityModifier, ScanLevel)
    - **Property 1: LoadFrom Round-Trip Preserves All Fields**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 4.1, 4.2, 4.3**
    - Add <Compile Include= ViewModels\SurveyViewModelPropertyTests.cs /> to test csproj

  - [x] 8.2 Write property test: IsDirty false immediately after LoadFrom
    - **Property 2: IsDirty False Immediately After LoadFrom**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 7.1, 7.5**

  - [x] 8.3 Write property test: IsDirty detects any single field change
    - **Property 3: IsDirty Detects Any Single Field Change**
    - [FsCheck.NUnit.Property(MaxTest = 50)]
    - Test changing each scalar field, each Resources entry, and each Properties entry individually
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4**

- [x] 9. Add ViewModel unit tests
  - [x] 9.1 Write unit tests for SurveyViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/SurveyViewModelTests.cs
    - Tests: Reset clears all fields to defaults, IsNew returns true after Reset, IsNew returns false after LoadFrom, IsDirty returns true for new survey with non-default PlanetName, BuildUpdateRequest copies all fields including Resources and Properties, BuildCreateRequest copies all fields including Resources and Properties, Resources deep copy is independent (modifying request does not affect ViewModel), Properties deep copy is independent, SensorAbundance/PurityModifier/ScanLevel convenience accessors read/write Properties dictionary
    - Add <Compile Include= ViewModels\SurveyViewModelTests.cs /> to test csproj
    - _Requirements: 4.1, 4.2, 7.1, 7.5, 7.6_

- [x] 10. Add Service property tests
  - [x] 10.1 Write property test: Service.Update round-trip
    - Create OE2EmpireTracker.Tests/Services/SurveyServicePropertyTests.cs
    - Reuse ValidSurveyGen() from ViewModel property tests
    - **Property 4: Service.Update Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 13.3, 13.4, 13.5, 13.8**
    - Add <Compile Include= Services\SurveyServicePropertyTests.cs /> to test csproj

  - [x] 10.2 Write property test: Service.Create round-trip
    - **Property 5: Service.Create Round-Trip**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 14.2, 14.4, 14.8**

  - [x] 10.3 Write property test: Service.Delete removes survey
    - **Property 6: Service.Delete Removes Survey**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 15.1, 15.2**

  - [x] 10.4 Write property test: Import dedup preserves UUID and NickName
    - **Property 7: Import Dedup --- Existing Survey Preserves UUID and NickName**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 16.2**

  - [x] 10.5 Write property test: Import new creates survey with non-empty UUID
    - **Property 8: Import New --- Creates Survey with Non-Empty UUID**
    - [FsCheck.NUnit.Property(MaxTest = 25)]
    - **Validates: Requirements 16.3, 16.7**

- [x] 11. Add Service unit tests
  - [x] 11.1 Write unit tests for SurveyService
    - Create OE2EmpireTracker.Tests/Services/SurveyServiceTests.cs
    - Tests: Update with non-existent UUID throws InvalidOperationException, Delete with empty UUID returns without error, Delete with non-existent UUID returns without error, Create assigns non-empty UUID, Create sets OwnerUUID to current player UUID, Update fires SurveyDataChanged event, Create fires SurveyDataChanged event, Delete fires SurveyDataChanged event, Import with matching survey merges data (preserves UUID and NickName), Import with no match creates new survey, Import with asteroid survey calls LinkOrCreateAsteroid
    - Add <Compile Include= Services\SurveyServiceTests.cs /> to test csproj
    - _Requirements: 13.7, 13.8, 13.9, 14.2, 14.3, 14.7, 15.4, 15.5, 16.2, 16.3, 16.4_

- [x] 12. Add mutation guard test
  - [x] 12.1 Write mutation guard test for Survey
    - Create OE2EmpireTracker.Tests/Services/SurveyMutationGuardTests.cs
    - Follow BlueprintMutationGuardTests and PricingPlanMutationGuardTests pattern
    - First check: scan for direct Survey property sets, assert they only appear in SurveyService, SurveyImportHelper, SurveyParser, Survey.cs, JSON deserialization, migration code, and test code
    - Second check: scan for direct SurveyResource property sets, assert they only appear in SurveyService, SurveyImportHelper, SurveyParser, SurveyResource (within Survey.cs), JSON deserialization, and test code
    - Verify FormSurvey.cs does not directly set properties on Survey
    - Verify SurveyViewModel.cs does not directly set properties on Survey
    - **Validates: Requirements 21.1, 21.2, 21.3, 24.1, 24.2**
    - Add <Compile Include=Services\SurveyMutationGuardTests.cs /> to test csproj

- [x] 13. Final checkpoint --- Full build, all tests pass, audit clean
  - Build with zero errors and zero warnings
  - All existing and new tests pass
  - `node .kiro/tools/audit.js` reports no new findings
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with * are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- New .cs files require <Compile Include= ...> entries in the old-style .csproj
- ReadOnlySurvey gap fill adds 4 new properties (ScannedBy, DateTime, ScannerBlueprintUUID, Properties) --- all other properties already exist
- SurveyViewModel is a full rewrite from write-through wrapper to disconnected edit buffer (no constructor dependencies on PlayerContext/EmpireContext)
- SurveyService is more complex than PricingPlanService due to Import method with dedup, merge, and asteroid auto-linking via SurveyImportHelper
- Resources dictionary requires deep copy (new SurveyResource per entry) unlike PricingPlan flat Dictionary<string, decimal>
- Properties dictionary requires shallow copy (new Dictionary<string, string>)
- Reuse SurveyImportHelper (FindByKey, CreateFromTemp, MergeData, LinkOrCreateAsteroid) --- the service calls these helpers instead of the form calling them directly
- SurveyReferenceCounter is unchanged --- the form calls it before invoking the service Delete method
- Build with MSBuild: `D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe OE2EmpireTracker.sln /p:Configuration=Debug`
- Run tests with vstest.console: `D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
