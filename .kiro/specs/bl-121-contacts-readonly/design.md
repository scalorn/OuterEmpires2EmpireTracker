# BL-121 Design: Contacts Immutable Data Model with Service Layer

## Overview

BL-121 applies the immutable data model pattern to the Contacts form. This is a dual-entity form managing Faction and ExternalCharacter. The ViewModel becomes a disconnected edit buffer for both entities. A new ContactsService is the sole mutator.

## Architecture

Form never touches entities. ViewModel holds disconnected edit buffers for both Faction and ExternalCharacter. Service is the only mutator.

## Components and Interfaces

### ContactsViewModel (Edit Buffer)

Dual-entity ViewModel:

Faction section: _factionOriginal, _factionUUID, FactionName, FactionDescription (get/set), IsFactionNew, IsFactionDirty, LoadFactionFrom(ReadOnlyFaction), ResetFaction(), BuildFactionUpdateRequest(), BuildFactionCreateRequest().

Character section: _characterOriginal, _characterUUID, CharacterName, CharacterFactionUUID (get/set), IsCharacterNew, IsCharacterDirty, LoadCharacterFrom(ReadOnlyExternalCharacter), ResetCharacter(), BuildCharacterUpdateRequest(), BuildCharacterCreateRequest().

### ContactsService

Faction methods: UpdateFaction, CreateFaction, DeleteFaction.
Character methods: UpdateCharacter, CreateCharacter, DeleteCharacter.

## Data Models

### FactionUpdateRequest (New)
- Original (ReadOnlyFaction), Name, Description

### FactionCreateRequest (New)
- Name, Description

### ExternalCharacterUpdateRequest (New)
- Original (ReadOnlyExternalCharacter), Name, FactionUUID

### ExternalCharacterCreateRequest (New)
- Name, FactionUUID

## Testing Strategy

- OE2EmpireTracker.Tests/ViewModels/ContactsViewModelPropertyTests.cs
- OE2EmpireTracker.Tests/ViewModels/ContactsViewModelTests.cs
- OE2EmpireTracker.Tests/Services/ContactsServicePropertyTests.cs
- OE2EmpireTracker.Tests/Services/ContactsServiceTests.cs
- OE2EmpireTracker.Tests/Services/ContactsMutationGuardTests.cs
