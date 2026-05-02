# Implementation Plan: BL-111 PlayerProfile Immutable Data Model with Service Layer

## Overview

This plan migrates FormPlayerProfile to the immutable data model pattern established in BL-108. The form stops directly mutating PlayerProfile entities. The ViewModel becomes a disconnected edit buffer. A new PlayerProfileService is the sole mutator. Implementation follows the four phases from the requirements: read-only wrapper gap fill, ViewModel edit buffer, service layer, and verification.

## Tasks

- [x] 1. ReadOnly wrapper gap fill
  - [x] 1.1 Add missing properties to ReadOnlyPlayerProfile
    - Add Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime properties
    - Add Skills property returning IReadOnlyDictionary<string, ReadOnlyPlayerSkill>
    - Verify SkillGroups accessor already exists, add if missing
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

  - [x] 1.2 Add missing properties to ReadOnlyPlayerSkill
    - Add TrainingStarted property
    - Add CompletionStartTime, CompletionEndTime, CompletionTimeRemaining, CompletionTimeRemainingString properties (expose CountDownTime fields as read-only scalars)
    - _Requirements: 2.1, 2.2_

  - [ ] 1.3 Write unit tests for ReadOnly wrapper gap fill
    - Test ReadOnlyPlayerProfile exposes Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime
    - Test ReadOnlyPlayerProfile.Skills returns wrapped ReadOnlyPlayerSkill entries
    - Test ReadOnlyPlayerSkill exposes TrainingStarted and CompletionTime fields
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 2.2_

- [x] 2. PlayerContext: FindMutablePlayerProfile
  - [x] 2.1 Add FindMutablePlayerProfile internal method to PlayerContext
    - Follow the same pattern as FindMutableBlueprint
    - Uses existing _playerProfileCache, builds cache on first call
    - Mark as internal - only for service use
    - _Requirements: 14.2_

- [x] 3. Data transfer objects
  - [x] 3.1 Create SkillUpdateData class
    - Level, TrainingStarted, CompletionStartTime, CompletionEndTime properties
    - Place in Models/ folder
    - _Requirements: 14.5_

  - [x] 3.2 Create PlayerProfileUpdateRequest class
    - Original (ReadOnlyPlayerProfile), scalar fields, rank fields (flat per track), Skills dictionary, SkillGroups dictionary
    - _Requirements: 14.1, 14.3, 14.4, 14.5, 14.6_

  - [x] 3.3 Create PlayerProfileCreateRequest class
    - Same fields as update request but without Original or UUID
    - _Requirements: 15.1, 15.3_

- [x] 4. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. ViewModel edit buffer
  - [x] 5.1 Create LocalSkillData and LocalRankData classes
    - LocalSkillData: Level, TrainingStarted, CompletionStartTime, CompletionEndTime, computed TimeRemaining and TimeRemainingString
    - LocalRankData: Rank, CurrentXP, NextXP, Title
    - Place in ViewModels/ folder
    - _Requirements: 5.3, 5.2_

  - [x] 5.2 Rewrite PlayerProfileViewModel as edit buffer
    - Replace mutable entity reference with local fields: Name, Faction, TotalCredits, SkillPoints, CitizenId, RegistrationDate, ActiveTime
    - Add local rank data (PublicRank, PrivateRank, MilitaryRank as LocalRankData)
    - Add local skills dictionary (Dictionary<string, LocalSkillData>)
    - Add local skill groups dictionary (Dictionary<string, bool>)
    - Retain _original ReadOnlyPlayerProfile snapshot for dirty comparison
    - Remove Data property and any direct mutable entity reference
    - Implement LoadFrom(ReadOnlyPlayerProfile) with deep copy of all fields, ranks, skills, skill groups
    - Implement Reset() to clear all fields to defaults
    - Implement IsNew property (true when _original is null)
    - Implement UUID property
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 6.1, 6.2, 6.3, 7.1, 7.2_

  - [x] 5.3 Implement dirty tracking in PlayerProfileViewModel
    - Implement IsDirty property comparing all local fields against _original snapshot
    - Compare scalar fields, all three rank tracks field-by-field, skills dictionary, skill groups dictionary
    - Handle new profile case: IsDirty true once any field has non-default value
    - Handle loaded profile case: IsDirty false immediately after LoadFrom
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

  - [x] 5.4 Implement BuildUpdateRequest and BuildCreateRequest on ViewModel
    - BuildUpdateRequest: builds PlayerProfileUpdateRequest from local state including Original snapshot
    - BuildCreateRequest: builds PlayerProfileCreateRequest from local state
    - _Requirements: 18.1, 18.2_

  - [x] 5.5 Write property test: LoadFrom round-trip preserves all fields (Property 1)
    - **Property 1: LoadFrom round-trip preserves all fields**
    - Generate random PlayerProfile with arbitrary scalars, ranks, skills, skill groups
    - Wrap in ReadOnlyPlayerProfile, call LoadFrom, verify all local fields match
    - **Validates: Requirements 1.7, 1.8, 5.1, 5.2, 5.3, 5.4**

  - [x] 5.6 Write property test: IsDirty is false immediately after LoadFrom (Property 2)
    - **Property 2: IsDirty is false immediately after LoadFrom**
    - Generate random PlayerProfile, wrap, LoadFrom, assert IsDirty == false
    - **Validates: Requirements 9.1, 9.6**

  - [x] 5.7 Write property test: IsDirty detects any single field change (Property 3)
    - **Property 3: IsDirty detects any single field change**
    - Generate random PlayerProfile, LoadFrom, change one random field to a different value, assert IsDirty == true
    - **Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5, 9.7**

- [x] 6. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. PlayerProfileService
  - [x] 7.1 Create PlayerProfileService with Update method
    - Accept UUID and PlayerProfileUpdateRequest
    - Look up mutable profile via PlayerContext.FindMutablePlayerProfile
    - Apply all scalar fields, ranks, skills, skill groups to entity
    - Persist via WriteContext, fire PlayerProfileDataChanged event
    - Return updated ReadOnlyPlayerProfile
    - Throw InvalidOperationException if UUID not found
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 14.9, 14.10_

  - [x] 7.2 Add Create method to PlayerProfileService
    - Accept PlayerProfileCreateRequest
    - Create new PlayerProfile with generated UUID, populate all fields
    - Add to PlayerContext, persist, fire PlayerProfilesChanged and PlayerProfileDataChanged events
    - Return new ReadOnlyPlayerProfile
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7_

  - [x] 7.3 Add Delete method to PlayerProfileService
    - Accept UUID string
    - Remove profile via RemovePlayerProfile, cascade delete via CascadeDeletePlayer
    - Persist, fire events
    - Return silently if UUID empty or profile not found
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6_

  - [x] 7.4 Add Import method to PlayerProfileService
    - Accept parsed PlayerProfile temp object
    - Find existing by name (case-insensitive) - merge if found, create new if not
    - Move MergeProfile logic from FormPlayerProfile to service
    - Persist, fire events, return ReadOnlyPlayerProfile
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6_

  - [x] 7.5 Write property test: Service.Update round-trip (Property 4)
    - **Property 4: Service.Update round-trip**
    - Generate random existing profile and random update request
    - Call Update, verify returned ReadOnlyPlayerProfile matches request values
    - **Validates: Requirements 14.3, 14.4, 14.5, 14.6, 14.9**

  - [x] 7.6 Write property test: Service.Create round-trip (Property 5)
    - **Property 5: Service.Create round-trip**
    - Generate random create request, call Create, verify returned profile matches request and has non-empty UUID
    - **Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7**

  - [x] 7.7 Write property test: Service.Delete removes profile (Property 6)
    - **Property 6: Service.Delete removes profile**
    - Generate random existing profile, call Delete, verify profile no longer findable
    - **Validates: Requirements 16.1, 16.2, 16.3, 16.4, 16.5**

  - [x] 7.8 Write property test: Service.Import preserves UUID on name match (Property 7)
    - **Property 7: Service.Import preserves UUID on name match**
    - Generate random existing profile and parsed profile with same name (case-insensitive), call Import, verify UUID preserved and fields updated
    - **Validates: Requirements 17.2, 17.3**

  - [x] 7.9 Write unit tests for PlayerProfileService
    - Test Update throws InvalidOperationException on unknown UUID
    - Test Update fires PlayerProfileDataChanged event
    - Test Create fires both PlayerProfilesChanged and PlayerProfileDataChanged events
    - Test Delete is no-op on empty UUID
    - Test Delete calls CascadeDeletePlayer
    - Test Import creates new profile when no name match
    - Test Import fires both events
    - Test MergeProfile preserves UUID
    - _Requirements: 14.8, 14.10, 15.6, 16.3, 16.6, 17.3, 17.5_

- [x] 8. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Migrate FormPlayerProfile to edit buffer and service
  - [x] 9.1 Migrate list view to use ReadOnlyPlayerProfile Tags
    - PopulateListView stores ReadOnlyPlayerProfile in ListViewItem.Tag via GetReadOnlyPlayerProfileList()
    - ItemSelectionChanged extracts ReadOnlyPlayerProfile from Tag, calls viewModel.LoadFrom()
    - Filter logic uses ReadOnlyPlayerProfile.Name
    - _Requirements: 3.1, 3.2, 4.1_

  - [x] 9.2 Rebind form controls to ViewModel local state
    - Text boxes (txtPlayerName, cmbFaction, txtTotalCredits, txtSkillPoints) read/write ViewModel local fields
    - Rank text boxes read/write ViewModel local rank data
    - Skill group checkboxes read/write ViewModel local skill group dictionary
    - Remove all write-through to mutable entity from TextChanged handlers
    - _Requirements: 6.1, 6.2, 6.3, 7.1, 7.2_

  - [x] 9.3 Migrate PlayerSkillBlock to use LocalSkillData
    - Replace PlayerSkill reference with LocalSkillData property
    - Timer completion updates LocalSkillData (TrainingStarted=false, Level+=1) instead of entity
    - Start Training updates LocalSkillData instead of entity
    - Manual completion time edit updates LocalSkillData instead of entity
    - Countdown display reads from LocalSkillData
    - _Requirements: 6.4, 7.3, 8.1, 8.2, 8.3, 8.4_

  - [x] 9.4 Wire Save button to PlayerProfileService
    - New profile (IsNew): call BuildCreateRequest then service.Create
    - Existing profile: call BuildUpdateRequest then service.Update
    - Refresh list view and reload ViewModel from returned ReadOnlyPlayerProfile
    - Enable Save button only when IsDirty is true
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 9.8_

  - [x] 9.5 Wire Delete button to PlayerProfileService
    - Call service.Delete(uuid), reset ViewModel, refresh list
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5_

  - [x] 9.6 Wire Import button to PlayerProfileService
    - Parse clipboard, call service.Import(tempProfile)
    - Refresh list, select imported profile, reload ViewModel
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6_

  - [x] 9.7 Implement unsaved changes prompts
    - Add shared PromptUnsavedChanges() method with Save/Discard/Cancel dialog
    - Wire into: selection change, form close, application exit, New button
    - Save: call service, then proceed; Discard: proceed without saving; Cancel: cancel action
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 11.1, 11.2, 11.3, 11.4, 12.1, 12.2, 13.1, 13.2, 13.3, 13.4_

  - [x] 9.8 Remove all direct entity mutation from form and ViewModel
    - Remove Data property from ViewModel
    - Remove any remaining direct PlayerProfile property sets in form code
    - Remove any remaining direct PlayerSkill property sets in PlayerSkillBlock
    - Verify no mutable entity references leak through public accessors
    - _Requirements: 4.2, 7.1, 7.2, 7.3, 19.1, 19.2, 19.3, 19.4_

- [x] 10. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Verification
  - [x] 11.1 Run existing test suite and fix any regressions
    - Build solution, run all tests, fix any failures introduced by migration
    - _Requirements: 20.1_

  - [x] 11.2 Run audit and fix any new findings
    - Run node .kiro/tools/audit.js, fix any new findings beyond accepted baseline
    - _Requirements: 21.1_

  - [x] 11.3 Write verification tests for no direct mutation outside service
    - Test that grep for direct PlayerProfile property sets only finds matches in service, deserialization, migration, and class itself
    - Test that grep for direct PlayerSkill property sets only finds matches in service, parser, deserialization, and class itself
    - Test that grep for direct PlayerRank property sets only finds matches in service, parser, deserialization, and class itself
    - _Requirements: 22.1, 22.2, 22.3_

- [x] 12. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with * are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (Properties 1-7)
- Unit tests validate specific examples and edge cases
- The design uses C# throughout - all code examples use C# targeting .NET Framework 4.8.1
- FsCheck is used for property-based testing with NUnit integration
