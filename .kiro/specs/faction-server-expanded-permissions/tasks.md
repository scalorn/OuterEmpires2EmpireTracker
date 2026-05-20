# Implementation Plan: Faction Server Expanded Permissions

## Overview

Extends the faction server with named capabilities, permission groups, clearance levels, intel comments, and audit trail. Each task targets ≤5 files, ≤200 new lines, and ≤3 acceptance criteria per the task-sizing rules.

## Tasks

- [x] 1. Faction-scoped entity models
  - [x] 1.1 Create FactionCapability model class
    - Add `FactionCapability` (UUID, FactionUUID, Name, Description) to a new file `Storage/PermissionModels.cs`
    - _Requirements: 1.1, 1.3_
    - Verify: `dotnet build` compiles cleanly

  - [x] 1.2 Create FactionClearanceLevel model class
    - Add `FactionClearanceLevel` (UUID, FactionUUID, Level, Name, Description) to `Storage/PermissionModels.cs`
    - _Requirements: 3.1, 3.2_
    - Verify: `dotnet build` compiles cleanly

  - [x] 1.3 Create FactionPermissionGroup model class
    - Add `FactionPermissionGroup` (UUID, FactionUUID, Name, Description, DefaultClearanceLevelUUID) to `Storage/PermissionModels.cs`
    - _Requirements: 2.1, 2.3, 3.11_
    - Verify: `dotnet build` compiles cleanly

  - [x] 1.4 Create FactionGroupCapability and FactionGroupSharingRule models
    - Add `FactionGroupCapability` (GroupUUID, CapabilityUUID) junction model
    - Add `FactionGroupSharingRule` (UUID, GroupUUID, DataType, EntityUUID, MinClearanceLevelUUID)
    - _Requirements: 2.3, 2.4, 3.8_
    - Verify: `dotnet build` compiles cleanly

  - [x] 1.5 Create FactionMemberPermissions and FactionMemberCapability models
    - Add `FactionMemberPermissions` (CharacterUUID, FactionUUID, GroupUUID, ClearanceLevelUUID)
    - Add `FactionMemberCapability` (CharacterUUID, FactionUUID, CapabilityUUID)
    - _Requirements: 2.5, 3.6, 1.6_
    - Verify: `dotnet build` compiles cleanly


- [x] 2. Character-scoped entity models
  - [x] 2.1 Create CharacterCapability model class
    - Add `CharacterCapability` (UUID, OwnerCharacterUUID, Name, Description) to `Storage/PermissionModels.cs`
    - _Requirements: 1.3, 1.14_
    - Verify: `dotnet build` compiles cleanly

  - [x] 2.2 Create CharacterClearanceLevel model class
    - Add `CharacterClearanceLevel` (UUID, OwnerCharacterUUID, Level, Name, Description)
    - _Requirements: 3.1, 3.15_
    - Verify: `dotnet build` compiles cleanly

  - [x] 2.3 Create CharacterPermissionGroup model class
    - Add `CharacterPermissionGroup` (UUID, OwnerCharacterUUID, Name, Description, DefaultClearanceLevelUUID)
    - _Requirements: 2.1, 2.14_
    - Verify: `dotnet build` compiles cleanly

  - [x] 2.4 Create CharacterGroupCapability and CharacterGroupSharingRule models
    - Add `CharacterGroupCapability` (GroupUUID, CapabilityUUID) junction
    - Add `CharacterGroupSharingRule` (UUID, GroupUUID, DataType, EntityUUID) — no MinClearanceLevelUUID
    - _Requirements: 2.3, 2.4_
    - Verify: `dotnet build` compiles cleanly

  - [x] 2.5 Create CharacterGranteePermissions and CharacterGranteeCapability models
    - Add `CharacterGranteePermissions` (OwnerCharacterUUID, GranteeType, GranteeUUID, GroupUUID, ClearanceLevelUUID)
    - Add `CharacterGranteeCapability` (OwnerCharacterUUID, GranteeType, GranteeUUID, CapabilityUUID)
    - Add `GranteeType` enum (Character, Faction)
    - _Requirements: 1.7, 2.2_
    - Verify: `dotnet build` compiles cleanly


- [x] 3. Shared entity models (Intel + Audit)
  - [x] 3.1 Create IntelComment model class
    - Add `IntelComment` (UUID, TargetCharacterUUID, SubmitterCharacterUUID, Text, CreatedUtc)
    - _Requirements: 4.1, 4.2_
    - Verify: `dotnet build` compiles cleanly

  - [x] 3.2 Create IntelCommentFactionShare model class
    - Add `IntelCommentFactionShare` (UUID, IntelCommentUUID, FactionUUID, ClassificationLevelUUID, ClassifiedByCharacterUUID, SharedUtc, ClassifiedUtc)
    - _Requirements: 4.5, 4.6, 4.10_
    - Verify: `dotnet build` compiles cleanly

  - [x] 3.3 Create PermissionAuditEntry model class
    - Add `PermissionAuditEntry` (UUID, Timestamp, ActorCharacterUUID, TargetCharacterUUID, ActionType, OldValue, NewValue)
    - Add `PermissionActionType` enum (CapabilityGranted, CapabilityRevoked, GroupAssigned, GroupRemoved, ClearanceChanged)
    - _Requirements: 5.2_
    - Verify: `dotnet build` compiles cleanly

  - [x] 3.4 Create DataType enum
    - Add `DataType` enum with all 16 values from design (blueprints, colonies, surveys, etc.)
    - Used by sharing rules to specify category-level access
    - _Requirements: 2.4_
    - Verify: `dotnet build` compiles cleanly

- [x] 4. Checkpoint — All models compile
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - Ask the user if questions arise about model design


- [ ] 5. Storage backend — FactionCapability CRUD
  - [ ] 5.1 Add IStorageBackend methods for FactionCapability
    - Add to `IStorageBackend.cs`: GetFactionCapabilitiesAsync, UpsertFactionCapabilityAsync, DeleteFactionCapabilityAsync
    - _Requirements: 1.4_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 5.2 Implement FactionCapability storage in JsonFileStorageBackend
    - Implement the three methods in `JsonFileStorageBackend.cs` using JSON file per faction
    - _Requirements: 1.4_
    - Verify: `dotnet build` compiles cleanly

- [ ] 6. Storage backend — FactionClearanceLevel CRUD
  - [ ] 6.1 Add IStorageBackend methods for FactionClearanceLevel
    - Add: GetFactionClearanceLevelsAsync, UpsertFactionClearanceLevelAsync, DeleteFactionClearanceLevelAsync
    - _Requirements: 3.5_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 6.2 Implement FactionClearanceLevel storage in JsonFileStorageBackend
    - Implement the three methods using JSON file per faction
    - _Requirements: 3.5_
    - Verify: `dotnet build` compiles cleanly

- [ ] 7. Storage backend — FactionPermissionGroup CRUD
  - [ ] 7.1 Add IStorageBackend methods for FactionPermissionGroup
    - Add: GetFactionGroupsAsync, GetFactionGroupAsync, UpsertFactionGroupAsync, DeleteFactionGroupAsync
    - _Requirements: 2.9_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 7.2 Implement FactionPermissionGroup storage in JsonFileStorageBackend
    - Implement the four methods using JSON file per faction
    - _Requirements: 2.9_
    - Verify: `dotnet build` compiles cleanly


- [ ] 8. Storage backend — FactionGroupCapability and FactionGroupSharingRule
  - [ ] 8.1 Add IStorageBackend methods for FactionGroupCapability
    - Add: GetFactionGroupCapabilitiesAsync, AddFactionGroupCapabilityAsync, RemoveFactionGroupCapabilityAsync
    - _Requirements: 2.3_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 8.2 Implement FactionGroupCapability storage in JsonFileStorageBackend
    - Implement the three methods
    - _Requirements: 2.3_
    - Verify: `dotnet build` compiles cleanly

  - [ ] 8.3 Add IStorageBackend methods for FactionGroupSharingRule
    - Add: GetFactionGroupSharingRulesAsync, UpsertFactionGroupSharingRuleAsync, DeleteFactionGroupSharingRuleAsync
    - _Requirements: 2.4, 3.8_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 8.4 Implement FactionGroupSharingRule storage in JsonFileStorageBackend
    - Implement the three methods
    - _Requirements: 2.4, 3.8_
    - Verify: `dotnet build` compiles cleanly

- [ ] 9. Storage backend — FactionMemberPermissions and FactionMemberCapability
  - [ ] 9.1 Add IStorageBackend methods for FactionMemberPermissions
    - Add: GetFactionMemberPermissionsAsync, UpsertFactionMemberPermissionsAsync, GetAllFactionMembersPermissionsAsync
    - _Requirements: 2.5, 3.6_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 9.2 Implement FactionMemberPermissions storage in JsonFileStorageBackend
    - Implement the three methods
    - _Requirements: 2.5, 3.6_
    - Verify: `dotnet build` compiles cleanly

  - [ ] 9.3 Add IStorageBackend methods for FactionMemberCapability
    - Add: GetFactionMemberCapabilitiesAsync, AddFactionMemberCapabilityAsync, RemoveFactionMemberCapabilityAsync
    - _Requirements: 1.6_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 9.4 Implement FactionMemberCapability storage in JsonFileStorageBackend
    - Implement the three methods
    - _Requirements: 1.6_
    - Verify: `dotnet build` compiles cleanly


- [ ] 10. Storage backend — CharacterCapability CRUD
  - [ ] 10.1 Add IStorageBackend methods for CharacterCapability
    - Add: GetCharacterCapabilitiesAsync, UpsertCharacterCapabilityAsync, DeleteCharacterCapabilityAsync
    - _Requirements: 1.3_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 10.2 Implement CharacterCapability storage in JsonFileStorageBackend
    - Implement the three methods using JSON file per character
    - _Requirements: 1.3_
    - Verify: `dotnet build` compiles cleanly

- [ ] 11. Storage backend — CharacterClearanceLevel CRUD
  - [ ] 11.1 Add IStorageBackend methods for CharacterClearanceLevel
    - Add: GetCharacterClearanceLevelsAsync, UpsertCharacterClearanceLevelAsync, DeleteCharacterClearanceLevelAsync
    - _Requirements: 3.5_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 11.2 Implement CharacterClearanceLevel storage in JsonFileStorageBackend
    - Implement the three methods
    - _Requirements: 3.5_
    - Verify: `dotnet build` compiles cleanly

- [ ] 12. Storage backend — CharacterPermissionGroup CRUD
  - [ ] 12.1 Add IStorageBackend methods for CharacterPermissionGroup
    - Add: GetCharacterGroupsAsync, GetCharacterGroupAsync, UpsertCharacterGroupAsync, DeleteCharacterGroupAsync
    - _Requirements: 2.9_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 12.2 Implement CharacterPermissionGroup storage in JsonFileStorageBackend
    - Implement the four methods
    - _Requirements: 2.9_
    - Verify: `dotnet build` compiles cleanly


- [ ] 13. Storage backend — CharacterGroupCapability, CharacterGroupSharingRule, CharacterGrantee entities
  - [ ] 13.1 Add IStorageBackend methods for CharacterGroupCapability and CharacterGroupSharingRule
    - Add: GetCharacterGroupCapabilitiesAsync, AddCharacterGroupCapabilityAsync, RemoveCharacterGroupCapabilityAsync
    - Add: GetCharacterGroupSharingRulesAsync, UpsertCharacterGroupSharingRuleAsync, DeleteCharacterGroupSharingRuleAsync
    - _Requirements: 2.3, 2.4_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 13.2 Implement CharacterGroupCapability and CharacterGroupSharingRule in JsonFileStorageBackend
    - Implement the six methods
    - _Requirements: 2.3, 2.4_
    - Verify: `dotnet build` compiles cleanly

  - [ ] 13.3 Add IStorageBackend methods for CharacterGranteePermissions and CharacterGranteeCapability
    - Add: GetCharacterGranteesAsync, UpsertCharacterGranteePermissionsAsync, DeleteCharacterGranteePermissionsAsync
    - Add: GetCharacterGranteeCapabilitiesAsync, AddCharacterGranteeCapabilityAsync, RemoveCharacterGranteeCapabilityAsync
    - _Requirements: 1.7, 2.2_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 13.4 Implement CharacterGrantee storage in JsonFileStorageBackend
    - Implement the six methods
    - _Requirements: 1.7, 2.2_
    - Verify: `dotnet build` compiles cleanly

- [ ] 14. Storage backend — IntelComment and IntelCommentFactionShare
  - [ ] 14.1 Add IStorageBackend methods for IntelComment
    - Add: GetIntelCommentsForTargetAsync, GetIntelCommentAsync, UpsertIntelCommentAsync, DeleteIntelCommentAsync
    - _Requirements: 4.1, 4.2_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 14.2 Implement IntelComment storage in JsonFileStorageBackend
    - Implement the four methods
    - _Requirements: 4.1, 4.2_
    - Verify: `dotnet build` compiles cleanly

  - [ ] 14.3 Add IStorageBackend methods for IntelCommentFactionShare
    - Add: GetIntelSharesForCommentAsync, GetIntelSharesForFactionAsync, UpsertIntelShareAsync, DeleteIntelShareAsync
    - _Requirements: 4.4, 4.5, 4.11_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 14.4 Implement IntelCommentFactionShare storage in JsonFileStorageBackend
    - Implement the four methods
    - _Requirements: 4.4, 4.5, 4.11_
    - Verify: `dotnet build` compiles cleanly


- [ ] 15. Storage backend — PermissionAuditEntry
  - [ ] 15.1 Add IStorageBackend methods for PermissionAuditEntry
    - Add: GetPermissionAuditEntriesAsync (with filters: dateRange, actionType, actor, target), AppendPermissionAuditEntryAsync, DeleteExpiredAuditEntriesAsync
    - _Requirements: 5.1, 5.2, 5.4_
    - Verify: `dotnet build` compiles (stubs in backends)

  - [ ] 15.2 Implement PermissionAuditEntry storage in JsonFileStorageBackend
    - Implement the three methods (append-only file, no update/delete of individual records)
    - _Requirements: 5.1, 5.2, 5.5_
    - Verify: `dotnet build` compiles cleanly

- [ ] 16. Checkpoint — All storage methods compile
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All IStorageBackend methods have implementations in JsonFileStorageBackend
  - Other backends (Dynamo, Postgres, Sqlite) have NotImplementedException stubs
  - Ask the user if questions arise

- [ ] 17. Starting set seeding — faction capabilities
  - Implement logic to create the 9 default capabilities when a faction is first created
    - global_data_write, server_processing_admin, export_any_character, manage_faction_members, classify_intel, server_side_processing, real_time_push, bulk_export, market_analytics
  - Hook into faction creation flow (FactionEndpoints.CreateFaction or a post-creation service)
  - _Requirements: 1.11, 4.17_
  - Verify: Creating a faction via POST produces 9 capabilities in storage

- [ ] 18. Starting set seeding — clearance levels
  - Implement logic to create default clearance levels (1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader) on faction creation
  - Implement same for character scope on first use
  - _Requirements: 3.3_
  - Verify: Creating a faction produces 5 clearance levels in storage


- [ ] 19. Faction Capability endpoints — POST and GET
  - [ ] 19.1 Implement POST /api/v1/factions/{uuid}/capabilities
    - Create new `Endpoints/FactionCapabilityEndpoints.cs`
    - Validate: faction exists, caller is Leader/Owner, name not empty, name unique within faction
    - _Requirements: 1.4_
    - Verify: Integration test — POST returns 201 with capability UUID

  - [ ] 19.2 Implement GET /api/v1/factions/{uuid}/capabilities
    - Return all capabilities defined by the faction
    - _Requirements: 1.13_
    - Verify: Integration test — GET returns list of capabilities

- [ ] 20. Faction Capability endpoints — PUT and DELETE
  - [ ] 20.1 Implement PUT /api/v1/factions/{uuid}/capabilities/{capId}
    - Rename capability (update Name/Description)
    - Validate: capability exists, caller is Leader/Owner
    - _Requirements: 1.8, 1.12_
    - Verify: Integration test — PUT returns 200 with updated capability

  - [ ] 20.2 Implement DELETE /api/v1/factions/{uuid}/capabilities/{capId}
    - Delete capability (including starting set — no undeletable capabilities)
    - Cascade: remove from FactionGroupCapability and FactionMemberCapability
    - _Requirements: 1.8, 1.12_
    - Verify: Integration test — DELETE returns 204, capability gone from storage

- [ ] 21. Faction ClearanceLevel endpoints — POST and GET
  - [ ] 21.1 Implement POST /api/v1/factions/{uuid}/clearance-levels
    - Create new clearance level with Level (int), Name, Description
    - Validate: faction exists, caller is Leader/Owner
    - _Requirements: 3.5_
    - Verify: Integration test — POST returns 201

  - [ ] 21.2 Implement GET /api/v1/factions/{uuid}/clearance-levels
    - Return all clearance levels for the faction, ordered by Level ascending
    - _Requirements: 3.5_
    - Verify: Integration test — GET returns ordered list

- [ ] 22. Faction ClearanceLevel endpoints — PUT and DELETE
  - [ ] 22.1 Implement PUT /api/v1/factions/{uuid}/clearance-levels/{levelId}
    - Update Level number, Name, or Description
    - Validate: level exists, caller is Leader/Owner
    - _Requirements: 3.4_
    - Verify: Integration test — PUT returns 200

  - [ ] 22.2 Implement DELETE /api/v1/factions/{uuid}/clearance-levels/{levelId}
    - Delete clearance level (including starting set)
    - Validate: caller is Leader/Owner
    - _Requirements: 3.4_
    - Verify: Integration test — DELETE returns 204


- [ ] 23. Faction PermissionGroup endpoints — POST and GET
  - [ ] 23.1 Implement POST /api/v1/factions/{uuid}/groups
    - Create new `Endpoints/FactionGroupEndpoints.cs`
    - Create group with Name, Description, DefaultClearanceLevelUUID
    - Validate: faction exists, caller is Leader/Owner, DefaultClearanceLevelUUID references valid level
    - _Requirements: 2.9_
    - Verify: Integration test — POST returns 201

  - [ ] 23.2 Implement GET /api/v1/factions/{uuid}/groups
    - Return all permission groups for the faction
    - _Requirements: 2.9_
    - Verify: Integration test — GET returns list

- [ ] 24. Faction PermissionGroup endpoints — PUT and DELETE
  - [ ] 24.1 Implement PUT /api/v1/factions/{uuid}/groups/{groupId}
    - Update group Name, Description, DefaultClearanceLevelUUID
    - Validate: group exists, caller is Leader/Owner
    - _Requirements: 2.9_
    - Verify: Integration test — PUT returns 200

  - [ ] 24.2 Implement DELETE /api/v1/factions/{uuid}/groups/{groupId}
    - Delete group, cascade: remove FactionGroupCapability, FactionGroupSharingRule, clear GroupUUID from FactionMemberPermissions
    - _Requirements: 2.9_
    - Verify: Integration test — DELETE returns 204, members' GroupUUID nulled

- [ ] 25. Faction Group member management
  - [ ] 25.1 Implement PUT /api/v1/factions/{uuid}/groups/{groupId}/members
    - Assign character to group (set FactionMemberPermissions.GroupUUID)
    - Enforce single-group constraint: if character already in another group, move them
    - Assign DefaultClearanceLevelUUID if character has no clearance yet
    - _Requirements: 2.10, 2.5, 2.7_
    - Verify: Integration test — PUT returns 200, character's group updated

  - [ ] 25.2 Implement DELETE /api/v1/factions/{uuid}/groups/{groupId}/members/{characterUUID}
    - Remove character from group (null out GroupUUID in FactionMemberPermissions)
    - _Requirements: 2.11_
    - Verify: Integration test — DELETE returns 204

- [ ] 26. Faction Group sharing rules and capability grants
  - [ ] 26.1 Implement POST /api/v1/factions/{uuid}/groups/{groupId}/sharing-rules
    - Add a FactionGroupSharingRule to the group
    - Validate: MinClearanceLevelUUID references valid level
    - _Requirements: 2.4, 3.8_
    - Verify: Integration test — POST returns 201

  - [ ] 26.2 Implement DELETE /api/v1/factions/{uuid}/groups/{groupId}/sharing-rules/{ruleId}
    - Remove a sharing rule from the group
    - _Requirements: 2.4_
    - Verify: Integration test — DELETE returns 204

  - [ ] 26.3 Implement POST /api/v1/factions/{uuid}/groups/{groupId}/capabilities
    - Add a FactionGroupCapability to the group
    - Validate: CapabilityUUID references valid faction capability
    - _Requirements: 2.3_
    - Verify: Integration test — POST returns 201

  - [ ] 26.4 Implement DELETE /api/v1/factions/{uuid}/groups/{groupId}/capabilities/{capId}
    - Remove a capability grant from the group
    - _Requirements: 2.3_
    - Verify: Integration test — DELETE returns 204


- [ ] 27. Faction member clearance and individual capabilities
  - [ ] 27.1 Implement PUT /api/v1/factions/{uuid}/members/{charUUID}/clearance
    - Set a member's clearance level (update FactionMemberPermissions.ClearanceLevelUUID)
    - Validate: level exists, caller is Leader/Owner
    - _Requirements: 3.7_
    - Verify: Integration test — PUT returns 200

  - [ ] 27.2 Implement PUT /api/v1/factions/{uuid}/members/{charUUID}/capabilities (grant)
    - Add individual FactionMemberCapability
    - Validate: capability exists, caller is Leader/Owner
    - _Requirements: 1.6, 1.8_
    - Verify: Integration test — PUT returns 200

  - [ ] 27.3 Implement DELETE /api/v1/factions/{uuid}/members/{charUUID}/capabilities/{capId} (revoke)
    - Remove individual FactionMemberCapability
    - _Requirements: 1.8_
    - Verify: Integration test — DELETE returns 204

- [ ] 28. Implement GET /api/v1/factions/{uuid}/members with clearance info
  - Extend existing members endpoint to include each member's clearance level (name + numeric value) and group name
  - _Requirements: 3.14_
  - Verify: Integration test — GET response includes clearance fields

- [ ] 29. Checkpoint — Faction API endpoints compile and pass basic tests
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All faction permission endpoints registered in Program.cs
  - Ask the user if questions arise


- [ ] 30. Character Capability endpoints — POST and GET
  - [ ] 30.1 Implement POST /api/v1/characters/{uuid}/capabilities
    - Create new `Endpoints/CharacterCapabilityEndpoints.cs`
    - Validate: character exists, caller is character owner or Owner, name unique within character
    - _Requirements: 1.5_
    - Verify: Integration test — POST returns 201

  - [ ] 30.2 Implement GET /api/v1/characters/{uuid}/capabilities
    - Return all capabilities defined by or granted to the character
    - _Requirements: 1.14_
    - Verify: Integration test — GET returns list

- [ ] 31. Character Capability endpoints — PUT and DELETE
  - [ ] 31.1 Implement PUT /api/v1/characters/{uuid}/capabilities/{capId}
    - Rename capability (update Name/Description)
    - Validate: capability exists, caller is owner or Owner
    - _Requirements: 1.9_
    - Verify: Integration test — PUT returns 200

  - [ ] 31.2 Implement DELETE /api/v1/characters/{uuid}/capabilities/{capId}
    - Delete capability, cascade: remove from CharacterGroupCapability and CharacterGranteeCapability
    - _Requirements: 1.9_
    - Verify: Integration test — DELETE returns 204

- [ ] 32. Character ClearanceLevel endpoints — POST and GET
  - [ ] 32.1 Implement POST /api/v1/characters/{uuid}/clearance-levels
    - Create clearance level for character scope
    - Validate: character exists, caller is owner or Owner
    - _Requirements: 3.5_
    - Verify: Integration test — POST returns 201

  - [ ] 32.2 Implement GET /api/v1/characters/{uuid}/clearance-levels
    - Return all clearance levels for the character, ordered by Level ascending
    - _Requirements: 3.5_
    - Verify: Integration test — GET returns ordered list

- [ ] 33. Character ClearanceLevel endpoints — PUT and DELETE
  - [ ] 33.1 Implement PUT /api/v1/characters/{uuid}/clearance-levels/{levelId}
    - Update Level number, Name, or Description
    - _Requirements: 3.4_
    - Verify: Integration test — PUT returns 200

  - [ ] 33.2 Implement DELETE /api/v1/characters/{uuid}/clearance-levels/{levelId}
    - Delete clearance level
    - _Requirements: 3.4_
    - Verify: Integration test — DELETE returns 204


- [ ] 34. Character PermissionGroup endpoints — POST and GET
  - [ ] 34.1 Implement POST /api/v1/characters/{uuid}/groups
    - Create new `Endpoints/CharacterGroupEndpoints.cs`
    - Create group with Name, Description, DefaultClearanceLevelUUID
    - Validate: character exists, caller is owner or Owner
    - _Requirements: 2.9_
    - Verify: Integration test — POST returns 201

  - [ ] 34.2 Implement GET /api/v1/characters/{uuid}/groups
    - Return all permission groups for the character
    - _Requirements: 2.9_
    - Verify: Integration test — GET returns list

- [ ] 35. Character PermissionGroup endpoints — PUT and DELETE
  - [ ] 35.1 Implement PUT /api/v1/characters/{uuid}/groups/{groupId}
    - Update group Name, Description, DefaultClearanceLevelUUID
    - _Requirements: 2.9_
    - Verify: Integration test — PUT returns 200

  - [ ] 35.2 Implement DELETE /api/v1/characters/{uuid}/groups/{groupId}
    - Delete group, cascade: remove CharacterGroupCapability, CharacterGroupSharingRule, clear GroupUUID from CharacterGranteePermissions
    - _Requirements: 2.9_
    - Verify: Integration test — DELETE returns 204

- [ ] 36. Character Group member (grantee) management
  - [ ] 36.1 Implement PUT /api/v1/characters/{uuid}/groups/{groupId}/members
    - Assign grantee (character or faction) to group
    - Enforce single-group constraint per granting character
    - _Requirements: 2.10, 2.2_
    - Verify: Integration test — PUT returns 200

  - [ ] 36.2 Implement DELETE /api/v1/characters/{uuid}/groups/{groupId}/members/{granteeUUID}
    - Remove grantee from group
    - _Requirements: 2.11_
    - Verify: Integration test — DELETE returns 204

- [ ] 37. Character Group sharing rules and capability grants
  - [ ] 37.1 Implement POST /api/v1/characters/{uuid}/groups/{groupId}/sharing-rules
    - Add a CharacterGroupSharingRule (DataType, optional EntityUUID — no MinClearanceLevel)
    - _Requirements: 2.4_
    - Verify: Integration test — POST returns 201

  - [ ] 37.2 Implement DELETE /api/v1/characters/{uuid}/groups/{groupId}/sharing-rules/{ruleId}
    - Remove a sharing rule
    - _Requirements: 2.4_
    - Verify: Integration test — DELETE returns 204

  - [ ] 37.3 Implement POST /api/v1/characters/{uuid}/groups/{groupId}/capabilities
    - Add a CharacterGroupCapability
    - _Requirements: 2.3_
    - Verify: Integration test — POST returns 201

  - [ ] 37.4 Implement DELETE /api/v1/characters/{uuid}/groups/{groupId}/capabilities/{capId}
    - Remove a capability grant from the group
    - _Requirements: 2.3_
    - Verify: Integration test — DELETE returns 204


- [ ] 38. Character individual grantee capabilities
  - [ ] 38.1 Implement PUT /api/v1/characters/{uuid}/grantees/{granteeUUID}/capabilities (grant)
    - Add individual CharacterGranteeCapability
    - _Requirements: 1.7, 1.9_
    - Verify: Integration test — PUT returns 200

  - [ ] 38.2 Implement DELETE /api/v1/characters/{uuid}/grantees/{granteeUUID}/capabilities/{capId} (revoke)
    - Remove individual CharacterGranteeCapability
    - _Requirements: 1.9_
    - Verify: Integration test — DELETE returns 204

- [ ] 39. Checkpoint — Character API endpoints compile and pass basic tests
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All character permission endpoints registered in Program.cs
  - Ask the user if questions arise

- [ ] 40. Intel Comment endpoints — create and query
  - [ ] 40.1 Implement POST /api/v1/characters/{uuid}/intel
    - Create new `Endpoints/IntelEndpoints.cs`
    - Create private intel comment (visible only to submitter)
    - Validate: target character UUID provided, text not empty
    - _Requirements: 4.3_
    - Verify: Integration test — POST returns 201, comment is private

  - [ ] 40.2 Implement GET /api/v1/characters/{uuid}/intel
    - Return: submitter's own private comments + faction-shared comments filtered by clearance
    - Include submitter name in response
    - If caller has `classify_intel`, include unclassified comments too
    - _Requirements: 4.13, 4.14_
    - Verify: Integration test — GET returns filtered results

- [ ] 41. Intel Comment endpoints — share and revoke
  - [ ] 41.1 Implement POST /api/v1/characters/{uuid}/intel/{commentId}/share
    - Share comment with a faction (create IntelCommentFactionShare with null classification)
    - Validate: caller is submitter, faction exists
    - _Requirements: 4.4, 4.6_
    - Verify: Integration test — POST returns 201, share record created

  - [ ] 41.2 Implement DELETE /api/v1/characters/{uuid}/intel/{commentId}/share/{factionUUID}
    - Revoke faction share (delete IntelCommentFactionShare)
    - Validate: caller is submitter
    - _Requirements: 4.11, 4.12_
    - Verify: Integration test — DELETE returns 204, comment invisible to faction


- [ ] 42. Intel Comment endpoints — classify and delete
  - [ ] 42.1 Implement PUT /api/v1/factions/{uuid}/intel/{shareId}/classify
    - Assign classification level to a shared comment
    - Validate: caller has `classify_intel` capability, share exists, level exists
    - Record classifier identity and timestamp
    - _Requirements: 4.8, 4.9, 4.10_
    - Verify: Integration test — PUT returns 200, classification fields populated

  - [ ] 42.2 Implement DELETE /api/v1/characters/{uuid}/intel/{commentId}
    - Delete comment and all its faction shares
    - Validate: caller is submitter, Faction Leader, or Owner
    - _Requirements: 4.16_
    - Verify: Integration test — DELETE returns 204, shares also removed

  - [ ] 42.3 Enforce comment immutability
    - Ensure no PUT endpoint exists for comment text
    - If a PUT is attempted on the comment body, return 405 Method Not Allowed
    - _Requirements: 4.15_
    - Verify: Integration test — PUT on comment text returns 405

- [ ] 43. Intel visibility logic — classify_intel gating
  - Implement the logic that filters GET /intel results:
    - Unclassified comments visible only to members with `classify_intel` capability
    - Classified comments visible only to members with clearance >= classification level
  - This is the query-time filter in the GET handler (task 40.2), extracted as a helper/service
  - _Requirements: 4.7, 4.9_
  - Verify: Unit test — unclassified hidden from non-classifiers; classified filtered by clearance


- [ ] 44. Effective permission computation service
  - Create `Services/PermissionResolver.cs` (or similar)
  - Compute a character's effective capabilities: role permissions + group capabilities + individual capabilities
  - Compute a character's effective sharing access: individual sharing rules + group sharing template
  - _Requirements: 2.12, 2.13_
  - Verify: Unit test — effective permissions are additive union of all sources

- [ ] 45. Visibility resolution — two-layer system
  - Implement the data visibility query logic:
    - Layer 1: What the character shared outward (CharacterGroupSharingRules)
    - Layer 2: What the faction's clearance rules permit (FactionGroupSharingRules + member clearance)
    - Result: intersection of both layers
  - _Requirements: 3.9, 3.10_
  - Verify: Unit test — member sees only data at intersection of shared + clearance-permitted

- [ ] 46. Owner and Leader clearance overrides
  - Owner always has effective clearance at the highest defined level in all scopes
  - Faction Leader always has effective clearance at or above the second-highest defined level
  - Implement in PermissionResolver
  - _Requirements: 3.12, 3.13_
  - Verify: Unit test — Owner sees everything; Leader sees all but highest-only content

- [ ] 47. Permission cache with invalidation
  - Cache computed effective permissions per character
  - Invalidate on: group capability change, group sharing rule change, member group assignment, member clearance change, individual capability grant/revoke, clearance level definition change
  - _Requirements: 2.8 (members updated on group change)_
  - Verify: Unit test — cache invalidates correctly on each trigger

- [ ] 48. Checkpoint — Visibility and permission logic complete
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All permission resolution logic has unit tests
  - Ask the user if questions arise


- [ ] 49. Audit trail logging service
  - Create audit logging helper that records PermissionAuditEntry on every permission mutation
  - Integrate into: capability grant/revoke, group assignment/removal, clearance level change endpoints
  - _Requirements: 5.1, 5.2_
  - Verify: Unit test — each mutation type produces correct audit entry

- [ ] 50. Audit trail API endpoint — GET with filters
  - Implement GET /api/v1/audit/permissions
    - Filters: date range, action type, actor, target
    - Pagination support
  - _Requirements: 5.3_
  - Verify: Integration test — GET returns filtered, paginated results

- [ ] 51. Audit trail — authorization and append-only enforcement
  - Owner sees all audit entries; Faction Leader sees only their faction's changes
  - No DELETE or PUT endpoints for audit records
  - _Requirements: 5.3, 5.5_
  - Verify: Integration test — Leader cannot see other faction's entries; DELETE returns 405

- [ ] 52. Audit trail — retention cleanup
  - Implement background cleanup of audit entries older than configurable period (default 90 days)
  - Add configuration setting for retention period
  - _Requirements: 5.4_
  - Verify: Unit test — entries older than retention period are removed by cleanup

- [ ] 53. Authorization checks — faction endpoint guards
  - Ensure all faction permission endpoints enforce:
    - Leader/Owner can mutate faction permissions
    - Regular characters get 403 on mutation attempts
    - Owner can manage all regardless of scope
  - _Requirements: 1.8, 1.10, 2.6_
  - Verify: Integration test — unauthorized caller gets 403

- [ ] 54. Authorization checks — character endpoint guards
  - Ensure all character permission endpoints enforce:
    - Only the owning character (or Owner) can mutate their permissions
    - Other characters get 403
  - _Requirements: 1.9, 1.10, 2.6_
  - Verify: Integration test — non-owner caller gets 403

- [ ] 55. Custom capabilities in sharing rules
  - Implement logic allowing sharing rules to reference custom capabilities
  - A sharing rule can specify "share with anyone who has capability X"
  - _Requirements: 1.13 ("Custom capabilities SHALL be usable in sharing rules")_
  - Verify: Unit test — sharing rule with capability gate correctly filters access

- [ ] 56. Checkpoint — All server logic complete
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All endpoints registered, all authorization enforced, audit trail active
  - Ask the user if questions arise


- [ ] 57. FormPermissionManager — shell and tab layout
  - Create `FormPermissionManager.cs` with tab control: "My Sharing" tab + one tab per faction
  - Wire into MainWindow "Manage → Permissions" menu item
  - _Requirements: 1.3, 2.1, 3.1_
  - Verify: Form opens from menu, tabs display correctly

- [ ] 58. FormPermissionManager — My Sharing tab: Permission Groups list panel
  - Left panel: ListView of CharacterPermissionGroups (Name, member count)
  - New / Rename / Delete buttons
  - Selecting a group populates the right detail panel
  - _Requirements: 2.1, 2.14_
  - Verify: Groups load from server, CRUD operations work

- [ ] 59. FormPermissionManager — My Sharing tab: Group Detail panel (sharing rules)
  - Right panel section: Sharing Rules grid (DataType dropdown + EntityUUID + Add/Remove)
  - _Requirements: 2.4_
  - Verify: Adding/removing sharing rules persists to server

- [ ] 60. FormPermissionManager — My Sharing tab: Group Detail panel (capabilities + members)
  - Right panel section: Capabilities Granted grid (list + Add/Remove)
  - Right panel section: Members grid (grantees assigned to group + Add/Remove)
  - _Requirements: 2.3, 2.10_
  - Verify: Adding/removing capabilities and members persists to server

- [ ] 61. FormPermissionManager — My Sharing tab: Clearance Levels section
  - Bottom section: Editable grid (Level int, Name text, Description text)
  - Add / Remove / Reorder buttons
  - _Requirements: 3.1, 3.4_
  - Verify: Clearance level CRUD persists to server

- [ ] 62. FormPermissionManager — My Sharing tab: Capabilities section
  - Collapsible section: List of CharacterCapabilities defined by this character
  - New / Rename / Delete buttons
  - _Requirements: 1.3, 1.9_
  - Verify: Capability CRUD persists to server


- [ ] 63. FormPermissionManager — Faction tab: Permission Groups list panel
  - Left panel: ListView of FactionPermissionGroups (Name, member count, default clearance)
  - New / Rename / Delete buttons (Leader/Owner only)
  - _Requirements: 2.1, 2.6_
  - Verify: Groups load from server, editing restricted by role

- [ ] 64. FormPermissionManager — Faction tab: Group Detail panel (sharing rules with min clearance)
  - Right panel section: Sharing Rules grid (DataType + EntityUUID + MinClearanceLevel dropdown + Add/Remove)
  - _Requirements: 2.4, 3.8_
  - Verify: Adding rules with min clearance persists correctly

- [ ] 65. FormPermissionManager — Faction tab: Group Detail panel (capabilities + members)
  - Right panel section: Capabilities Granted grid (list + Add/Remove)
  - Right panel section: Members grid (faction members in group + clearance + Add/Remove)
  - _Requirements: 2.3, 2.10_
  - Verify: Adding/removing capabilities and members persists to server

- [ ] 66. FormPermissionManager — Faction tab: Clearance Levels section
  - Bottom section: Editable grid (Level, Name, Description) — Leader/Owner only for editing
  - Members see their own assigned level (read-only)
  - _Requirements: 3.1, 3.4, 3.5_
  - Verify: Clearance level CRUD restricted by role

- [ ] 67. FormPermissionManager — Faction tab: Capabilities section
  - Collapsible section: List of FactionCapabilities
  - New / Rename / Delete (Leader/Owner)
  - Individual grant: select member → grant/revoke specific capabilities
  - _Requirements: 1.4, 1.8_
  - Verify: Capability management and individual grants work

- [ ] 68. FormPermissionManager — Faction tab: Members Overview section
  - Collapsible section: Grid showing all faction members (Name, Group, Clearance Level, Individual Capabilities count)
  - Click member to edit group assignment and clearance (Leader/Owner)
  - _Requirements: 3.14, 2.6_
  - Verify: Member overview loads, editing restricted by role

- [ ] 69. Checkpoint — FormPermissionManager complete
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - Both tabs functional with server communication
  - Ask the user if questions arise


- [ ] 70. FormIntelComments — shell and target selector
  - Create `FormIntelComments.cs` with target character selector (filtered combo) at top
  - Wire into MainWindow "Manage → Intel" menu item
  - _Requirements: 4.1_
  - Verify: Form opens from menu, target selector populates

- [ ] 71. FormIntelComments — My Private Notes section
  - List of private IntelComments (no faction share)
  - New comment: text box + Submit button
  - Per-comment actions: Share with Faction (dropdown) / Delete
  - _Requirements: 4.3, 4.4, 4.16_
  - Verify: Create, share, and delete operations work

- [ ] 72. FormIntelComments — Pending Review section
  - Only visible if character has `classify_intel` capability
  - List of unclassified IntelCommentFactionShares for current faction
  - Classify button → clearance level dropdown → Confirm
  - _Requirements: 4.7, 4.8_
  - Verify: Classification updates share record on server

- [ ] 73. FormIntelComments — Classified Intel section
  - List of classified comments the character has clearance to see
  - Shows: text, submitter, classification level name, classified by, date
  - Filter by classification level (dropdown)
  - _Requirements: 4.9, 4.13, 4.14_
  - Verify: Only comments at or below member's clearance are shown

- [ ] 74. FormIntelComments — integration with FormContacts
  - Add "Intel" button to FormContacts ExternalCharacter detail view
  - Opens FormIntelComments pre-filtered to that target character
  - _Requirements: 4.1_
  - Verify: Button opens intel form with correct target

- [ ] 75. FormAuditLog — shell with filters
  - Create `FormAuditLog.cs` with date range, action type, actor, target filters
  - Search button triggers query
  - _Requirements: 5.3_
  - Verify: Form opens, filters populate

- [ ] 76. FormAuditLog — results grid with pagination
  - DataGridView: Timestamp, Actor, Target, Action, Old Value, New Value
  - Pagination controls (Prev/Next)
  - _Requirements: 5.2, 5.3_
  - Verify: Results display correctly, pagination works

- [ ] 77. Checkpoint — All UI forms complete
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All forms accessible from MainWindow menu
  - Ask the user if questions arise


- [ ] 78. Tests — effective permission computation
  - [ ] 78.1 Unit tests for additive permission model
    - Test: role + group capabilities + individual capabilities = union
    - Test: no deny mechanism — having capability from any source means you have it
    - _Requirements: 1.2, 2.12_

  - [ ] 78.2 Unit tests for single-group constraint
    - Test: assigning to new group removes from old group
    - Test: character belongs to at most one group per scope
    - _Requirements: 2.5_

- [ ] 79. Tests — visibility resolution
  - [ ] 79.1 Unit tests for two-layer visibility
    - Test: member sees intersection of (what character shared) ∩ (what clearance permits)
    - Test: if character didn't share, faction clearance doesn't matter — result is empty
    - _Requirements: 3.9, 3.10_

  - [ ] 79.2 Unit tests for clearance defaults
    - Test: no clearance on sharing rule defaults to lowest level (visible to everyone)
    - Test: clearance below minimum blocks access
    - _Requirements: 3.10_

  - [ ] 79.3 Unit tests for Owner/Leader clearance overrides
    - Test: Owner always sees everything regardless of clearance level
    - Test: Leader sees all but highest-only content
    - _Requirements: 3.12, 3.13_

- [ ] 80. Tests — intel comment lifecycle
  - [ ] 80.1 Unit tests for comment creation and privacy
    - Test: new comment is private (only submitter sees it)
    - Test: sharing with faction creates unclassified share
    - _Requirements: 4.3, 4.6_

  - [ ] 80.2 Unit tests for classification and visibility
    - Test: unclassified visible only to classify_intel holders
    - Test: classified visible to members with sufficient clearance
    - Test: classified invisible to members below clearance
    - _Requirements: 4.7, 4.9_

  - [ ] 80.3 Unit tests for share revocation
    - Test: revoking share makes comment invisible to faction immediately
    - Test: deleting comment removes all shares
    - _Requirements: 4.11, 4.12, 4.16_

- [ ] 81. Tests — audit trail
  - [ ] 81.1 Unit tests for audit entry creation
    - Test: each permission mutation type produces correct audit entry
    - Test: audit entry has correct actor, target, action type, old/new values
    - _Requirements: 5.1, 5.2_

  - [ ] 81.2 Unit tests for audit retention and immutability
    - Test: entries older than retention period are cleaned up
    - Test: no mechanism to delete individual entries
    - _Requirements: 5.4, 5.5_

- [ ] 82. Tests — authorization
  - [ ] 82.1 Unit tests for faction endpoint authorization
    - Test: Leader can mutate faction permissions
    - Test: Owner can mutate any faction's permissions
    - Test: regular character gets 403 on faction mutations
    - _Requirements: 1.8, 1.10_

  - [ ] 82.2 Unit tests for character endpoint authorization
    - Test: character can mutate own permissions
    - Test: Owner can mutate any character's permissions
    - Test: other character gets 403
    - _Requirements: 1.9, 1.10_

- [ ] 83. Tests — starting set seeding
  - [ ] 83.1 Unit tests for faction creation seeding
    - Test: new faction gets 9 default capabilities
    - Test: new faction gets 5 default clearance levels
    - Test: all seeded items are deletable/renameable (no special protection)
    - _Requirements: 1.11, 1.12, 3.3_

- [ ] 84. Final checkpoint — All tests pass, solution builds cleanly
  - Ensure `dotnet build` passes with zero errors and zero warnings
  - All tests pass
  - Ask the user if questions arise



---

### Desktop UI (Avalonia/.NET 8) — OE2EmpireTracker.Desktop

> Mirrors the WinForms UI tasks (57-77) for the Avalonia Desktop project.
> ViewModels are shared from Common; Views are Avalonia AXAML.

- [ ] 84. Create PermissionManagerView shell (Avalonia Window with TabControl: My Sharing + Faction tab)
  - _Satisfies: Req 1, Req 2, Req 3 — Desktop UI entry point for permission management_
  - _Inputs: OE2EmpireTracker.Desktop/Views/, design.md mockups_
  - _Output: PermissionManagerView.axaml + .axaml.cs_
  - _Verification: View opens without error, tabs render_

- [ ] 85. Implement My Sharing tab — permission groups list + CRUD (Desktop/Avalonia)
  - _Satisfies: Req 2, Criterion "Character-scoped groups are assignable to anyone the character shares with"_
  - _Output: Views/Permissions/MySharingGroupsView.axaml_
  - _Verification: Groups list populates from API, add/delete works_

- [ ] 86. Implement My Sharing tab — group detail panel (sharing rules, capabilities, members) (Desktop/Avalonia)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template"_
  - _Output: Views/Permissions/GroupDetailView.axaml_
  - _Verification: Selecting a group shows its rules/capabilities/members_

- [ ] 87. Implement My Sharing tab — clearance levels grid (Desktop/Avalonia)
  - _Satisfies: Req 3, Criterion "Clearance levels SHALL be stored as a user-definable table"_
  - _Output: Views/Permissions/ClearanceLevelsView.axaml_
  - _Verification: Grid shows levels, add/remove/reorder works_

- [ ] 88. Implement My Sharing tab — capabilities list + CRUD (Desktop/Avalonia)
  - _Satisfies: Req 1, Criterion "Characters SHALL be able to create, delete, rename capabilities"_
  - _Output: Views/Permissions/CapabilitiesView.axaml_
  - _Verification: Capabilities list populates, CRUD operations work_

- [ ] 89. Implement Faction tab — permission groups + detail (Desktop/Avalonia)
  - _Satisfies: Req 2, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/groups"_
  - _Output: Views/Permissions/FactionGroupsView.axaml_
  - _Verification: Faction groups list + detail panel renders with min clearance on rules_

- [ ] 90. Implement Faction tab — clearance levels + capabilities + members overview (Desktop/Avalonia)
  - _Satisfies: Req 3, Criterion "GET /api/factions/{uuid}/members SHALL include clearance level"_
  - _Output: Views/Permissions/FactionOverviewView.axaml_
  - _Verification: Members grid shows Name, Group, Clearance, Capabilities count_

- [ ] 91. Create IntelCommentsView (Avalonia Window with target selector + sections)
  - _Satisfies: Req 4, Criterion "The service SHALL support intelligence comments attached to ExternalCharacter entities"_
  - _Output: Views/Intel/IntelCommentsView.axaml + .axaml.cs_
  - _Verification: View opens, target selector populates_

- [ ] 92. Implement private notes + pending review + classified sections (Desktop/Avalonia)
  - _Satisfies: Req 4, Criterion "POST creates private comment", "classify_intel can assign level", "classified visible by clearance"_
  - _Output: Views/Intel/IntelSectionsView.axaml_
  - _Verification: Create comment, share, classify, filter by clearance all work_

- [ ] 93. Create AuditLogView (Avalonia Window with filterable paginated grid)
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history"_
  - _Output: Views/Audit/AuditLogView.axaml + .axaml.cs_
  - _Verification: Grid populates with audit entries, filters work_

- [ ] 94. Wire Desktop menu items (Permissions, Intel, Audit) to new views
  - _Satisfies: Req 1-5 — Desktop UI navigation entry points_
  - _Output: MainWindow.axaml menu additions + navigation wiring_
  - _Verification: Menu items open correct views_

- [ ] 95. Wire Intel button on Contacts detail view (Desktop/Avalonia)
  - _Satisfies: Req 4 — UI integration point for intel from contact view_
  - _Output: Views/Contacts/ modification_
  - _Verification: Intel button opens IntelCommentsView with target pre-selected_


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Storage tasks are split by entity to keep each task under 5 files / 200 lines
- API endpoints are split by HTTP method (POST/GET separate from PUT/DELETE)
- Visibility resolution logic is separate from the endpoints that use it
- UI forms are split by section/panel
- The design uses C# (ASP.NET Core minimal APIs) — no language selection needed

## Traceability Matrix

### Requirement 1: Named Capability Permissions

| Criterion | Task(s) |
|-----------|---------|
| Named capability permissions (strings) | 1.1, 2.1 |
| Capabilities additive (role + granted) | 44, 78.1 |
| Scoped to faction OR character | 1.1–1.5, 2.1–2.5 |
| POST /api/factions/{uuid}/capabilities | 19.1 |
| POST /api/characters/{uuid}/capabilities | 30.1 |
| Faction-scoped grantable to any member | 27.2 |
| Character-scoped grantable to sharing relationship | 38.1 |
| Faction Leaders can create/delete/rename/grant/revoke | 19.1, 20.1, 20.2, 27.2, 27.3 |
| Characters can create/delete/rename/grant/revoke own | 30.1, 31.1, 31.2, 38.1, 38.2 |
| Owner manages all regardless of scope | 53, 54 |
| Starting set on faction creation | 17 |
| No undeletable capabilities | 20.2, 83.1 |
| Custom capabilities usable in sharing rules | 55 |
| GET /api/factions/{uuid}/capabilities | 19.2 |
| GET /api/characters/{uuid}/capabilities | 30.2 |
| Full parity faction/character | 30–38 mirror 19–27 |

### Requirement 2: Permission Groups

| Criterion | Task(s) |
|-----------|---------|
| Groups defined by faction or character | 23.1, 34.1 |
| Faction groups → members; Character groups → sharing targets | 25.1, 36.1 |
| Name, description, set of capabilities | 1.3, 1.4, 2.3, 2.4 |
| Sharing template | 1.5, 2.4, 26.1, 37.1 |
| At most one group per scope | 25.1, 36.1, 78.2 |
| Managed by group owner | 53, 54 |
| Inherit capabilities AND sharing template on assign | 25.1, 44 |
| Group changes update all members | 47 |
| CRUD endpoints factions + characters | 23–24, 34–35 |
| Assign to group (PUT) | 25.1, 36.1 |
| Remove from group (DELETE) | 25.2, 36.2 |
| Effective permissions = role + group + individual | 44, 78.1 |
| Effective sharing = individual + group template | 44, 45 |
| Full parity faction/character | 34–37 mirror 23–26 |

### Requirement 3: Clearance Levels

| Criterion | Task(s) |
|-----------|---------|
| User-definable table, faction or character owned | 1.2, 2.2 |
| Level (int), Name, optional Description | 1.2, 2.2 |
| Starting set (1-5) on creation | 18 |
| Delete/rename/reorder/recreate freely | 22, 33, 83.1 |
| CRUD endpoints factions + characters | 21–22, 32–33 |
| Each character has assigned clearance per scope | 1.5, 27.1 |
| Leader sets clearance via PUT | 27.1 |
| Sharing rule optionally specifies min clearance | 26.1 |
| Character sees data only if clearance >= min level | 45, 79.1 |
| Default to lowest level if unspecified | 45, 79.2 |
| Groups have default clearance on join | 1.3, 2.3, 25.1 |
| Owner always highest level | 46, 79.3 |
| Leader always >= second-highest | 46, 79.3 |
| GET members includes clearance | 28 |
| Full parity faction/character | 32–33 mirror 21–22 |

### Requirement 4: Intelligence Comments

| Criterion | Task(s) |
|-----------|---------|
| Comments attached to ExternalCharacter | 3.1, 40.1 |
| Submitter UUID, timestamp, text | 3.1 |
| POST creates private comment | 40.1, 80.1 |
| Share with factions via POST | 41.1, 80.1 |
| Multiple factions independently | 3.2, 41.1 |
| Arrives unclassified (null) | 3.2, 41.1 |
| Unclassified visible only to classify_intel holders | 43, 80.2 |
| classify_intel can assign level | 42.1, 80.2 |
| Classified visible by clearance | 42.1, 43, 80.2 |
| Classifier identity + timestamp recorded | 3.2, 42.1 |
| Revoke share via DELETE | 41.2, 80.3 |
| Revoked = invisible immediately | 41.2, 80.3 |
| GET returns private + clearance-filtered | 40.2 |
| Submitter name in response | 40.2 |
| Text immutable after creation | 42.3 |
| DELETE removes comment + all shares | 42.2, 80.3 |
| classify_intel in starting set | 17 |

### Requirement 5: Audit Trail

| Criterion | Task(s) |
|-----------|---------|
| Every permission change logged | 49, 81.1 |
| Record: timestamp, actor, target, action, old/new value | 3.3, 49, 81.1 |
| GET /api/audit/permissions (Owner/Leader filtered) | 50, 51 |
| Retained for configurable period (default 90 days) | 52, 81.2 |
| Not deletable (append-only) | 51, 81.2 |
