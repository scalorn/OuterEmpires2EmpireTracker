# Implementation Plan: BL-121 Contacts Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Contacts form. Dual-entity form managing Faction and ExternalCharacter. Create ContactsViewModel and ContactsService.

## Tasks

- [ ] 1. DTO request models and PlayerContext accessors
  - [ ] 1.1 Create FactionUpdateRequest, FactionCreateRequest, ExternalCharacterUpdateRequest, ExternalCharacterCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1, 8.2, 9.1, 9.2_
  - [ ] 1.2 Add FindMutableFaction and FindMutableExternalCharacter internal methods to PlayerContext
    - _Requirements: 11.1, 11.2_

- [ ] 2. Create ContactsViewModel as disconnected edit buffer
  - [ ] 2.1 Create ContactsViewModel class
    - Dual-entity ViewModel with faction and character sections
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3_

- [ ] 3. Create ContactsService with CRUD methods
  - [ ] 3.1 Create ContactsService class
    - Faction CRUD: UpdateFaction, CreateFaction, DeleteFaction
    - Character CRUD: UpdateCharacter, CreateCharacter, DeleteCharacter
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1, 8.2, 8.3, 9.1, 9.2, 9.3, 12.1_

- [ ] 4. Checkpoint --- Verify new classes compile cleanly

- [ ] 5. Migrate FormContacts to ReadOnly wrappers and service
  - [ ] 5.1 Replace mutable entity references with ReadOnly wrappers
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 3.1_
  - [ ] 5.2 Wire ViewModel as edit buffer for factions
    - _Requirements: 4.1, 4.2, 4.3_
  - [ ] 5.3 Wire ViewModel as edit buffer for characters
    - _Requirements: 5.1, 5.2, 5.3_
  - [ ] 5.4 Wire Save/Delete buttons through service
  - [ ] 5.5 Wire Delete with reference protection for factions
    - _Requirements: 10.1, 10.2, 10.3_

- [ ] 6. Add unsaved changes prompts
  - [ ] 6.1 Add unsaved changes prompts for factions and characters
    - _Requirements: 7.1, 7.2_

- [ ] 7. Checkpoint --- Verify form migration compiles and existing tests pass

- [ ] 8. Add ViewModel property tests
  - [ ] 8.1 Write property tests for ContactsViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ContactsViewModelPropertyTests.cs
    - Add Compile Include to test csproj

- [ ] 9. Add ViewModel unit tests
  - [ ] 9.1 Write unit tests for ContactsViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ContactsViewModelTests.cs
    - Add Compile Include to test csproj

- [ ] 10. Add Service property tests
  - [ ] 10.1 Write property tests for ContactsService
    - Create OE2EmpireTracker.Tests/Services/ContactsServicePropertyTests.cs
    - Add Compile Include to test csproj

- [ ] 11. Add Service unit tests
  - [ ] 11.1 Write unit tests for ContactsService
    - Create OE2EmpireTracker.Tests/Services/ContactsServiceTests.cs
    - Add Compile Include to test csproj

- [ ] 12. Add mutation guard test
  - [ ] 12.1 Write mutation guard test for Faction and ExternalCharacter
    - Create OE2EmpireTracker.Tests/Services/ContactsMutationGuardTests.cs
    - Add Compile Include to test csproj

- [ ] 13. Final checkpoint --- Full build, all tests pass, audit clean

## Notes

- Dual-entity form: Faction and ExternalCharacter
- Faction has 2 scalar fields (Name, Description), no nested collections
- ExternalCharacter has 2 scalar fields (Name, FactionUUID), no nested collections
- No OwnerUUID on either entity
- FactionReferenceCounter checks ExternalCharacters, PlayerProfiles, and SupplyChains
- ReadOnly wrappers already complete
- No write locks
- Build: "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
- Tests: "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
