# Implementation Plan: BL-121 Contacts Immutable Data Model with Service Layer

## Overview

Apply the immutable data model pattern to the Contacts form. Dual-entity form managing Faction and ExternalCharacter. Create ContactsViewModel and ContactsService.

## Tasks

- [x] 1. DTO request models and PlayerContext accessors
  - [x] 1.1 Create FactionUpdateRequest, FactionCreateRequest, ExternalCharacterUpdateRequest, ExternalCharacterCreateRequest DTOs
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1, 8.2, 9.1, 9.2_
  - [x] 1.2 Add FindMutableFaction and FindMutableExternalCharacter internal methods to PlayerContext
    - _Requirements: 11.1, 11.2_

- [x] 2. Create ContactsViewModel as disconnected edit buffer
  - [x] 2.1 Create ContactsViewModel class
    - Dual-entity ViewModel with faction and character sections
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3_

- [x] 3. Create ContactsService with CRUD methods
  - [x] 3.1 Create ContactsService class
    - Faction CRUD: UpdateFaction, CreateFaction, DeleteFaction
    - Character CRUD: UpdateCharacter, CreateCharacter, DeleteCharacter
    - Add Compile Include to OE2EmpireTracker.csproj
    - _Requirements: 8.1, 8.2, 8.3, 9.1, 9.2, 9.3, 12.1_

- [x] 4. Checkpoint --- Verify new classes compile cleanly

- [x] 5. Migrate FormContacts to ReadOnly wrappers and service
  - [x] 5.1 Replace mutable entity references with ReadOnly wrappers
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 3.1_
  - [x] 5.2 Wire ViewModel as edit buffer for factions
    - _Requirements: 4.1, 4.2, 4.3_
  - [x] 5.3 Wire ViewModel as edit buffer for characters
    - _Requirements: 5.1, 5.2, 5.3_
  - [x] 5.4 Wire Save/Delete buttons through service
  - [x] 5.5 Wire Delete with reference protection for factions
    - _Requirements: 10.1, 10.2, 10.3_

- [x] 6. Add unsaved changes prompts
  - [x] 6.1 Add unsaved changes prompts for factions and characters
    - _Requirements: 7.1, 7.2_

- [x] 7. Checkpoint --- Verify form migration compiles and existing tests pass

- [x] 8. Add ViewModel property tests
  - [x] 8.1 Write property tests for ContactsViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ContactsViewModelPropertyTests.cs
    - Add Compile Include to test csproj

- [x] 9. Add ViewModel unit tests
  - [x] 9.1 Write unit tests for ContactsViewModel
    - Create OE2EmpireTracker.Tests/ViewModels/ContactsViewModelTests.cs
    - Add Compile Include to test csproj

- [x] 10. Add Service property tests
  - [x] 10.1 Write property tests for ContactsService
    - Create OE2EmpireTracker.Tests/Services/ContactsServicePropertyTests.cs
    - Add Compile Include to test csproj

- [x] 11. Add Service unit tests
  - [x] 11.1 Write unit tests for ContactsService
    - Create OE2EmpireTracker.Tests/Services/ContactsServiceTests.cs
    - Add Compile Include to test csproj

- [x] 12. Add mutation guard test
  - [x] 12.1 Write mutation guard test for Faction and ExternalCharacter
    - Create OE2EmpireTracker.Tests/Services/ContactsMutationGuardTests.cs
    - Add Compile Include to test csproj

- [x] 13. Final checkpoint --- Full build, all tests pass, audit clean

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
