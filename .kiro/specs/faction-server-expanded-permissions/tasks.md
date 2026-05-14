# Faction Server Expanded Permissions — Tasks

> Each task references the requirement(s) and acceptance criteria it satisfies.
> Format: _Satisfies: Req N, Criterion "quoted text"_

---

## Phase 1: Data Models and Storage (Server)

- [ ] 1.1 Create FactionCapability entity model (UUID, FactionUUID, Name, Description)
  - _Satisfies: Req 1, Criterion "The service SHALL support named capability permissions (strings) that can be granted to individual characters"_
  - _Satisfies: Req 1, Criterion "Capabilities SHALL be scoped to either a faction OR a character"_

- [ ] 1.2 Create FactionClearanceLevel entity model (UUID, FactionUUID, Level, Name, Description)
  - _Satisfies: Req 3, Criterion "Clearance levels SHALL be stored as a user-definable table, owned by either a faction or a character"_
  - _Satisfies: Req 3, Criterion "Each clearance level SHALL have: a numeric Level (int, for ordering), a Name (display text), and an optional Description"_

- [ ] 1.3 Create FactionPermissionGroup entity model (UUID, FactionUUID, Name, Description, DefaultClearanceLevelUUID)
  - _Satisfies: Req 2, Criterion "The service SHALL support permission groups that can be defined by either a faction or a character"_
  - _Satisfies: Req 2, Criterion "Each group SHALL have a name, description, and a set of granted capabilities"_
  - _Satisfies: Req 3, Criterion "Groups (Requirement 2) SHALL have a default clearance level assigned to members on join"_

- [ ] 1.4 Create FactionGroupCapability junction model (GroupUUID, CapabilityUUID)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a name, description, and a set of granted capabilities"_

- [ ] 1.5 Create FactionGroupSharingRule entity model (UUID, GroupUUID, DataType, EntityUUID, MinClearanceLevelUUID)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group"_
  - _Satisfies: Req 3, Criterion "Each sharing rule SHALL optionally specify a minimum clearance level required to access the shared data"_

- [ ] 1.6 Create FactionMemberPermissions entity model (CharacterUUID, FactionUUID, GroupUUID, ClearanceLevelUUID)
  - _Satisfies: Req 2, Criterion "A character SHALL belong to at most one group within a given scope"_
  - _Satisfies: Req 3, Criterion "Each character SHALL have an assigned clearance level within each scope they participate in"_

- [ ] 1.7 Create FactionMemberCapability junction model (CharacterUUID, FactionUUID, CapabilityUUID)
  - _Satisfies: Req 1, Criterion "Faction-scoped capabilities are grantable to any member of that faction"_
  - _Satisfies: Req 1, Criterion "Capabilities SHALL be additive — a character has their role permissions PLUS any explicitly granted capabilities"_

- [ ] 1.8 Create CharacterCapability entity model (UUID, OwnerCharacterUUID, Name, Description)
  - _Satisfies: Req 1, Criterion "Capabilities SHALL be scoped to either a faction OR a character"_
  - _Satisfies: Req 1, Criterion "There SHALL be full parity between faction-level and character-level capability operations"_

- [ ] 1.9 Create CharacterClearanceLevel entity model (UUID, OwnerCharacterUUID, Level, Name, Description)
  - _Satisfies: Req 3, Criterion "Clearance levels SHALL be stored as a user-definable table, owned by either a faction or a character"_
  - _Satisfies: Req 3, Criterion "There SHALL be full parity between faction-level and character-level clearance level operations"_

- [ ] 1.10 Create CharacterPermissionGroup entity model (UUID, OwnerCharacterUUID, Name, Description, DefaultClearanceLevelUUID)
  - _Satisfies: Req 2, Criterion "The service SHALL support permission groups that can be defined by either a faction or a character"_
  - _Satisfies: Req 2, Criterion "There SHALL be full parity between faction-level and character-level group operations"_

- [ ] 1.11 Create CharacterGroupCapability junction model (GroupUUID, CapabilityUUID)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a name, description, and a set of granted capabilities"_

- [ ] 1.12 Create CharacterGroupSharingRule entity model (UUID, GroupUUID, DataType, EntityUUID — no MinClearanceLevelUUID)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group"_

- [ ] 1.13 Create CharacterGranteePermissions entity model (OwnerCharacterUUID, GranteeType, GranteeUUID, GroupUUID, ClearanceLevelUUID)
  - _Satisfies: Req 1, Criterion "Character-scoped capabilities are grantable to any character the granting character has a sharing relationship with"_
  - _Satisfies: Req 2, Criterion "Faction-scoped groups are assignable to faction members. Character-scoped groups are assignable to anyone the character shares with"_

- [ ] 1.14 Create CharacterGranteeCapability junction model (OwnerCharacterUUID, GranteeType, GranteeUUID, CapabilityUUID)
  - _Satisfies: Req 1, Criterion "Character-scoped capabilities are grantable to any character the granting character has a sharing relationship with"_

- [ ] 1.15 Create IntelComment entity model (UUID, TargetCharacterUUID, SubmitterCharacterUUID, Text, CreatedUtc)
  - _Satisfies: Req 4, Criterion "The service SHALL support intelligence comments attached to ExternalCharacter entities"_
  - _Satisfies: Req 4, Criterion "Each comment SHALL have: submitter character UUID, timestamp, and text content"_

- [ ] 1.16 Create IntelCommentFactionShare entity model (UUID, IntelCommentUUID, FactionUUID, ClassificationLevelUUID, ClassifiedByCharacterUUID, SharedUtc, ClassifiedUtc)
  - _Satisfies: Req 4, Criterion "A comment can be shared with multiple factions independently — each faction has its own classification state"_
  - _Satisfies: Req 4, Criterion "When shared with a faction, the comment SHALL arrive in an 'unclassified' state (ClassificationLevelUUID = null)"_
  - _Satisfies: Req 4, Criterion "The classifier's identity and timestamp SHALL be recorded on the share record"_

- [ ] 1.17 Create PermissionAuditEntry entity model (UUID, Timestamp, ActorCharacterUUID, TargetCharacterUUID, ActionType, OldValue, NewValue)
  - _Satisfies: Req 5, Criterion "The audit log SHALL record: timestamp, actor (who made the change), target (who was affected), action type, old value, new value"_

- [ ] 1.18 Add storage backend methods for all faction-scoped entities (CRUD operations on FactionCapability, FactionClearanceLevel, FactionPermissionGroup, FactionGroupCapability, FactionGroupSharingRule, FactionMemberPermissions, FactionMemberCapability)
  - _Satisfies: Req 1, Req 2, Req 3 — storage layer for all faction-scoped permission data_

- [ ] 1.19 Add storage backend methods for all character-scoped entities (CRUD operations on CharacterCapability, CharacterClearanceLevel, CharacterPermissionGroup, CharacterGroupCapability, CharacterGroupSharingRule, CharacterGranteePermissions, CharacterGranteeCapability)
  - _Satisfies: Req 1, Req 2, Req 3 — storage layer for all character-scoped permission data_

- [ ] 1.20 Add storage backend methods for shared entities (IntelComment, IntelCommentFactionShare, PermissionAuditEntry)
  - _Satisfies: Req 4, Req 5 — storage layer for intel and audit data_

- [ ] 1.21 Implement starting set seeding — create default capabilities on faction creation (global_data_write, server_processing_admin, export_any_character, manage_faction_members, classify_intel, server_side_processing, real_time_push, bulk_export, market_analytics)
  - _Satisfies: Req 1, Criterion "The following capabilities SHALL be created as a starting set when a faction is first created"_
  - _Satisfies: Req 4, Criterion "classify_intel SHALL be included in the starting capability set for new factions"_

- [ ] 1.22 Implement starting set seeding — create default clearance levels on faction/character creation (1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader)
  - _Satisfies: Req 3, Criterion "The following starting set SHALL be created when a faction or character first needs clearance levels: 1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader"_

---

## Phase 2: Faction Permission API Endpoints (Server)

- [ ] 2.1 Implement POST /api/factions/{uuid}/capabilities (create capability)
  - _Satisfies: Req 1, Criterion "A faction SHALL be able to define capabilities via POST /api/factions/{uuid}/capabilities"_

- [ ] 2.2 Implement GET /api/factions/{uuid}/capabilities (list all faction capabilities)
  - _Satisfies: Req 1, Criterion "GET /api/factions/{uuid}/capabilities SHALL return all capabilities defined by that faction"_

- [ ] 2.3 Implement PUT/DELETE /api/factions/{uuid}/capabilities/{capId} (rename, delete)
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction"_
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to delete, rename, or recreate any capability — including the starting set. There are no undeletable capabilities"_

- [ ] 2.4 Implement POST/GET/PUT/DELETE /api/factions/{uuid}/clearance-levels (CRUD)
  - _Satisfies: Req 3, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/clearance-levels"_
  - _Satisfies: Req 3, Criterion "Faction Leaders and characters SHALL be able to delete, rename, reorder, or recreate clearance levels however they want"_

- [ ] 2.5 Implement POST/GET/PUT/DELETE /api/factions/{uuid}/groups (CRUD for permission groups)
  - _Satisfies: Req 2, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/groups"_

- [ ] 2.6 Implement PUT /api/factions/{uuid}/groups/{groupId}/members (assign character to group)
  - _Satisfies: Req 2, Criterion "Assign character to group: PUT /api/{scope}/{uuid}/groups/{groupId}/members"_
  - _Satisfies: Req 2, Criterion "When a character is assigned to a group, they SHALL inherit the group's capabilities AND sharing template"_

- [ ] 2.7 Implement DELETE /api/factions/{uuid}/groups/{groupId}/members/{characterUUID} (remove from group)
  - _Satisfies: Req 2, Criterion "Remove character from group: DELETE /api/{scope}/{uuid}/groups/{groupId}/members/{characterUUID}"_

- [ ] 2.8 Implement group sharing rule management (add/remove FactionGroupSharingRules for a group)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group"_
  - _Satisfies: Req 2, Criterion "When a group's capabilities or sharing template change, all members SHALL be updated"_

- [ ] 2.9 Implement group capability grants (add/remove FactionGroupCapability for a group)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a name, description, and a set of granted capabilities"_
  - _Satisfies: Req 2, Criterion "When a group's capabilities or sharing template change, all members SHALL be updated"_

- [ ] 2.10 Implement PUT /api/factions/{uuid}/members/{charUUID}/capabilities (individual capability grant/revoke)
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction"_
  - _Satisfies: Req 1, Criterion "Faction-scoped capabilities are grantable to any member of that faction"_

- [ ] 2.11 Implement PUT /api/factions/{uuid}/members/{charUUID}/clearance (set member clearance level)
  - _Satisfies: Req 3, Criterion "Faction Leaders SHALL be able to set a character's clearance level via PUT /api/factions/{uuid}/members/{characterUUID}/clearance"_

- [ ] 2.12 Implement GET /api/factions/{uuid}/members with clearance level in response
  - _Satisfies: Req 3, Criterion "GET /api/factions/{uuid}/members SHALL include each member's clearance level (name + numeric value) in the response"_

- [ ] 2.13 Implement authorization checks — Leader/Owner only for faction mutations
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction"_
  - _Satisfies: Req 1, Criterion "The Owner SHALL be able to manage all capabilities regardless of scope"_
  - _Satisfies: Req 2, Criterion "Group membership SHALL be managed by the group's owner (Faction Leader for faction groups, character for their own groups, Owner for any)"_

- [ ] 2.14 Implement custom capability usage in sharing rules
  - _Satisfies: Req 1, Criterion "Custom capabilities SHALL be usable in sharing rules"_

---

## Phase 3: Character Permission API Endpoints (Server)

- [ ] 3.1 Implement POST /api/characters/{uuid}/capabilities (create capability)
  - _Satisfies: Req 1, Criterion "A character SHALL be able to define capabilities via POST /api/characters/{uuid}/capabilities"_

- [ ] 3.2 Implement GET /api/characters/{uuid}/capabilities (list capabilities granted to or defined by character)
  - _Satisfies: Req 1, Criterion "GET /api/characters/{uuid}/capabilities SHALL return all capabilities granted to or defined by a specific character"_

- [ ] 3.3 Implement PUT/DELETE /api/characters/{uuid}/capabilities/{capId} (rename, delete)
  - _Satisfies: Req 1, Criterion "Characters SHALL be able to create, delete, rename, grant, and revoke any capability they own"_

- [ ] 3.4 Implement POST/GET/PUT/DELETE /api/characters/{uuid}/clearance-levels (CRUD)
  - _Satisfies: Req 3, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/characters/{uuid}/clearance-levels"_
  - _Satisfies: Req 3, Criterion "Faction Leaders and characters SHALL be able to delete, rename, reorder, or recreate clearance levels however they want"_

- [ ] 3.5 Implement POST/GET/PUT/DELETE /api/characters/{uuid}/groups (CRUD for permission groups)
  - _Satisfies: Req 2, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/characters/{uuid}/groups"_

- [ ] 3.6 Implement PUT /api/characters/{uuid}/groups/{groupId}/members (assign grantee — character or faction)
  - _Satisfies: Req 2, Criterion "Assign character to group: PUT /api/{scope}/{uuid}/groups/{groupId}/members"_
  - _Satisfies: Req 2, Criterion "Faction-scoped groups are assignable to faction members. Character-scoped groups are assignable to anyone the character shares with"_

- [ ] 3.7 Implement DELETE /api/characters/{uuid}/groups/{groupId}/members/{granteeUUID} (remove grantee from group)
  - _Satisfies: Req 2, Criterion "Remove character from group: DELETE /api/{scope}/{uuid}/groups/{groupId}/members/{characterUUID}"_

- [ ] 3.8 Implement group sharing rule management for character groups (add/remove CharacterGroupSharingRules)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group"_

- [ ] 3.9 Implement group capability grants for character groups (add/remove CharacterGroupCapability)
  - _Satisfies: Req 2, Criterion "Each group SHALL have a name, description, and a set of granted capabilities"_

- [ ] 3.10 Implement individual grantee capability grants (CharacterGranteeCapability)
  - _Satisfies: Req 1, Criterion "Characters SHALL be able to create, delete, rename, grant, and revoke any capability they own"_
  - _Satisfies: Req 1, Criterion "Character-scoped capabilities are grantable to any character the granting character has a sharing relationship with"_

- [ ] 3.11 Implement authorization checks — owner only for character mutations
  - _Satisfies: Req 1, Criterion "Characters SHALL be able to create, delete, rename, grant, and revoke any capability they own"_
  - _Satisfies: Req 1, Criterion "The Owner SHALL be able to manage all capabilities regardless of scope"_
  - _Satisfies: Req 2, Criterion "Group membership SHALL be managed by the group's owner (Faction Leader for faction groups, character for their own groups, Owner for any)"_

---

## Phase 4: Intel Comment API Endpoints (Server)

- [ ] 4.1 Implement POST /api/characters/{uuid}/intel (create private comment)
  - _Satisfies: Req 4, Criterion "POST /api/characters/{uuid}/intel SHALL add a comment (private by default — visible only to submitter)"_

- [ ] 4.2 Implement POST /api/characters/{uuid}/intel/{commentId}/share (share with faction)
  - _Satisfies: Req 4, Criterion "The submitter SHALL be able to share a comment with one or more factions via POST /api/characters/{uuid}/intel/{commentId}/share with a target FactionUUID"_
  - _Satisfies: Req 4, Criterion "When shared with a faction, the comment SHALL arrive in an 'unclassified' state (ClassificationLevelUUID = null)"_

- [ ] 4.3 Implement DELETE /api/characters/{uuid}/intel/{commentId}/share/{factionUUID} (revoke faction share)
  - _Satisfies: Req 4, Criterion "The submitter SHALL be able to revoke a faction share via DELETE /api/characters/{uuid}/intel/{commentId}/share/{factionUUID}"_
  - _Satisfies: Req 4, Criterion "When a share is revoked, the comment becomes invisible to that faction immediately"_

- [ ] 4.4 Implement PUT /api/factions/{uuid}/intel/{shareId}/classify (assign classification level)
  - _Satisfies: Req 4, Criterion "A member with classify_intel SHALL be able to assign a classification level via PUT /api/factions/{uuid}/intel/{shareId}/classify"_
  - _Satisfies: Req 4, Criterion "Once classified, the comment SHALL become visible to faction members whose clearance level >= the assigned classification"_
  - _Satisfies: Req 4, Criterion "The classifier's identity and timestamp SHALL be recorded on the share record"_

- [ ] 4.5 Implement classify_intel capability check for unclassified comment visibility
  - _Satisfies: Req 4, Criterion "Unclassified comments SHALL only be visible to faction members who hold the classify_intel capability"_

- [ ] 4.6 Implement GET /api/characters/{uuid}/intel (query comments — private + faction-shared filtered by clearance)
  - _Satisfies: Req 4, Criterion "GET /api/characters/{uuid}/intel SHALL return: the requester's own private comments + faction-shared comments they have clearance to see (classified only, unless they have classify_intel)"_
  - _Satisfies: Req 4, Criterion "The submitter's name SHALL be included in the response"_

- [ ] 4.7 Implement DELETE /api/characters/{uuid}/intel/{commentId} (delete comment and all shares)
  - _Satisfies: Req 4, Criterion "DELETE /api/characters/{uuid}/intel/{commentId} SHALL remove a comment and all its faction shares (submitter, Faction Leader, or Owner only)"_

- [ ] 4.8 Enforce comment immutability (no edit endpoint, text cannot be modified after creation)
  - _Satisfies: Req 4, Criterion "Comment text SHALL be immutable after creation (no edit, only share/revoke/classify/delete)"_

---

## Phase 5: Visibility Resolution and Caching (Server)

- [ ] 5.1 Implement effective permission computation (role + group capabilities + individual capabilities)
  - _Satisfies: Req 2, Criterion "A character's effective permissions SHALL be: role permissions + group capabilities + individual capabilities"_
  - _Satisfies: Req 2, Criterion "A character's effective sharing access SHALL be: individual sharing rules + group sharing template"_

- [ ] 5.2 Implement visibility query resolution — two-layer system (character sharing ceiling ∩ faction clearance gate)
  - _Satisfies: Req 3, Criterion "A character SHALL only see shared data where their assigned clearance level's numeric value >= the sharing rule's minimum level's numeric value"_
  - _Satisfies: Req 3, Criterion "If no clearance level is specified on a sharing rule, it SHALL default to the lowest defined level (visible to everyone)"_

- [ ] 5.3 Implement Owner effective clearance override (always highest level in all scopes)
  - _Satisfies: Req 3, Criterion "The Owner SHALL always have effective clearance at the highest defined level in all scopes"_

- [ ] 5.4 Implement Faction Leader effective clearance override (at or above second-highest level)
  - _Satisfies: Req 3, Criterion "Faction Leaders SHALL always have effective clearance at or above the second-highest defined level in their faction"_

- [ ] 5.5 Implement permission cache with invalidation triggers (group change, member assignment, clearance change, capability grant/revoke, clearance level definition change)
  - _Satisfies: Req 2, Criterion "When a group's capabilities or sharing template change, all members SHALL be updated"_
  - _Satisfies: Req 2, Criterion "When a character is assigned to a group, they SHALL inherit the group's capabilities AND sharing template"_

- [ ] 5.6 Implement audit trail logging on all permission changes (capability grant/revoke, group assignment/removal, clearance level change)
  - _Satisfies: Req 5, Criterion "Every permission change (capability grant/revoke, group assignment, clearance level change) SHALL be logged"_

---

## Phase 6: Audit Trail API (Server)

- [ ] 6.1 Implement GET /api/audit/permissions with filters (date range, action type, actor, target)
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history (Owner only, or Faction Leader for their faction's changes)"_

- [ ] 6.2 Implement configurable retention policy (default 90 days) with background cleanup
  - _Satisfies: Req 5, Criterion "Audit records SHALL be retained for a configurable period (default: 90 days)"_

- [ ] 6.3 Enforce append-only semantics — no DELETE endpoint, no update of existing records
  - _Satisfies: Req 5, Criterion "Audit records SHALL NOT be deletable (append-only)"_

- [ ] 6.4 Implement authorization for audit endpoint (Owner sees all, Faction Leader sees their faction's changes only)
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history (Owner only, or Faction Leader for their faction's changes)"_

---

## Phase 7: Client UI — FormPermissionManager

- [ ] 7.1 Create FormPermissionManager with tab-based layout (My Sharing tab + one tab per faction)
  - _Satisfies: Req 1, Criterion "There SHALL be full parity between faction-level and character-level capability operations"_
  - _Satisfies: Req 2, Criterion "There SHALL be full parity between faction-level and character-level group operations"_
  - _Satisfies: Req 3, Criterion "There SHALL be full parity between faction-level and character-level clearance level operations"_

- [ ] 7.2 Implement My Sharing tab — character permission groups (list + CRUD + detail panel with sharing rules, capabilities, members)
  - _Satisfies: Req 2, Criterion "Faction-scoped groups are assignable to faction members. Character-scoped groups are assignable to anyone the character shares with"_
  - _Satisfies: Req 2, Criterion "Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group"_

- [ ] 7.3 Implement My Sharing tab — character clearance levels (editable grid with Level, Name, Description)
  - _Satisfies: Req 3, Criterion "Clearance levels SHALL be stored as a user-definable table, owned by either a faction or a character"_
  - _Satisfies: Req 3, Criterion "Faction Leaders and characters SHALL be able to delete, rename, reorder, or recreate clearance levels however they want"_

- [ ] 7.4 Implement My Sharing tab — character capabilities (list + CRUD)
  - _Satisfies: Req 1, Criterion "Characters SHALL be able to create, delete, rename, grant, and revoke any capability they own"_

- [ ] 7.5 Implement Faction tab — faction permission groups (list + CRUD + detail panel with sharing rules, capabilities, members; min clearance on rules)
  - _Satisfies: Req 2, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/groups"_
  - _Satisfies: Req 3, Criterion "Each sharing rule SHALL optionally specify a minimum clearance level required to access the shared data"_

- [ ] 7.6 Implement Faction tab — faction clearance levels (editable grid, Leader/Owner only for editing)
  - _Satisfies: Req 3, Criterion "CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/clearance-levels"_

- [ ] 7.7 Implement Faction tab — faction capabilities (list + CRUD + individual grant to members)
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction"_

- [ ] 7.8 Implement Faction tab — members overview (grid showing Name, Group, Clearance Level, Individual Capabilities count; click to edit)
  - _Satisfies: Req 3, Criterion "GET /api/factions/{uuid}/members SHALL include each member's clearance level (name + numeric value) in the response"_
  - _Satisfies: Req 2, Criterion "Group membership SHALL be managed by the group's owner"_

- [ ] 7.9 Add "Manage → Permissions" menu item to MainWindow (opens FormPermissionManager)
  - _Satisfies: Req 1, Req 2, Req 3 — UI entry point for all permission management_

---

## Phase 8: Client UI — FormIntelComments

- [ ] 8.1 Create FormIntelComments with target character selector and section layout
  - _Satisfies: Req 4, Criterion "The service SHALL support intelligence comments attached to ExternalCharacter entities"_

- [ ] 8.2 Implement My Private Notes section (create comment, share with faction, delete)
  - _Satisfies: Req 4, Criterion "POST /api/characters/{uuid}/intel SHALL add a comment (private by default — visible only to submitter)"_
  - _Satisfies: Req 4, Criterion "The submitter SHALL be able to share a comment with one or more factions"_

- [ ] 8.3 Implement Pending Review section (visible only to classify_intel holders; classify button with clearance level dropdown)
  - _Satisfies: Req 4, Criterion "Unclassified comments SHALL only be visible to faction members who hold the classify_intel capability"_
  - _Satisfies: Req 4, Criterion "A member with classify_intel SHALL be able to assign a classification level"_

- [ ] 8.4 Implement Classified Intel section (filtered by member's clearance level)
  - _Satisfies: Req 4, Criterion "Once classified, the comment SHALL become visible to faction members whose clearance level >= the assigned classification"_
  - _Satisfies: Req 4, Criterion "The submitter's name SHALL be included in the response"_

- [ ] 8.5 Add "Intel" button to FormContacts ExternalCharacter detail (opens FormIntelComments for that target)
  - _Satisfies: Req 4 — UI integration point for intel comments from contact view_

- [ ] 8.6 Add "Manage → Intel" menu item to MainWindow (opens FormIntelComments with no target pre-selected)
  - _Satisfies: Req 4 — UI entry point for intel comments_

---

## Phase 9: Client UI — FormAuditLog

- [ ] 9.1 Create FormAuditLog with filterable paginated grid
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history"_

- [ ] 9.2 Implement date range filter (from/to date pickers)
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history"_

- [ ] 9.3 Implement action type filter (dropdown: All, CapabilityGranted, CapabilityRevoked, GroupAssigned, GroupRemoved, ClearanceChanged)
  - _Satisfies: Req 5, Criterion "The audit log SHALL record: timestamp, actor (who made the change), target (who was affected), action type, old value, new value"_

- [ ] 9.4 Implement actor and target character filters
  - _Satisfies: Req 5, Criterion "The audit log SHALL record: timestamp, actor (who made the change), target (who was affected), action type, old value, new value"_

---

## Phase 10: Tests and Verification

- [ ] 10.1 Property-based tests for visibility resolution (two-layer intersection: character sharing ceiling ∩ faction clearance gate)
  - _Satisfies: Req 3, Criterion "A character SHALL only see shared data where their assigned clearance level's numeric value >= the sharing rule's minimum level's numeric value"_
  - _Satisfies: Req 3, Criterion "If no clearance level is specified on a sharing rule, it SHALL default to the lowest defined level (visible to everyone)"_

- [ ] 10.2 Property-based tests for effective permission computation (role + group + individual = additive union)
  - _Satisfies: Req 1, Criterion "Capabilities SHALL be additive — a character has their role permissions PLUS any explicitly granted capabilities"_
  - _Satisfies: Req 2, Criterion "A character's effective permissions SHALL be: role permissions + group capabilities + individual capabilities"_

- [ ] 10.3 Unit tests for Owner/Leader clearance overrides
  - _Satisfies: Req 3, Criterion "The Owner SHALL always have effective clearance at the highest defined level in all scopes"_
  - _Satisfies: Req 3, Criterion "Faction Leaders SHALL always have effective clearance at or above the second-highest defined level in their faction"_

- [ ] 10.4 Unit tests for intel comment lifecycle (create private → share → classify → visible by clearance → revoke → invisible)
  - _Satisfies: Req 4, Criterion "POST /api/characters/{uuid}/intel SHALL add a comment (private by default)"_
  - _Satisfies: Req 4, Criterion "Once classified, the comment SHALL become visible to faction members whose clearance level >= the assigned classification"_
  - _Satisfies: Req 4, Criterion "When a share is revoked, the comment becomes invisible to that faction immediately"_

- [ ] 10.5 Unit tests for permission group inheritance (assign to group → inherit capabilities + sharing template; change group → members updated)
  - _Satisfies: Req 2, Criterion "When a character is assigned to a group, they SHALL inherit the group's capabilities AND sharing template"_
  - _Satisfies: Req 2, Criterion "When a group's capabilities or sharing template change, all members SHALL be updated"_

- [ ] 10.6 Unit tests for single-group constraint (character belongs to at most one group per scope)
  - _Satisfies: Req 2, Criterion "A character SHALL belong to at most one group within a given scope"_

- [ ] 10.7 Unit tests for audit trail (append-only, correct fields recorded, retention cleanup)
  - _Satisfies: Req 5, Criterion "Every permission change (capability grant/revoke, group assignment, clearance level change) SHALL be logged"_
  - _Satisfies: Req 5, Criterion "Audit records SHALL NOT be deletable (append-only)"_
  - _Satisfies: Req 5, Criterion "Audit records SHALL be retained for a configurable period (default: 90 days)"_

- [ ] 10.8 Unit tests for authorization checks (Leader/Owner can mutate faction permissions; character can mutate own; unauthorized returns 403)
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction"_
  - _Satisfies: Req 1, Criterion "The Owner SHALL be able to manage all capabilities regardless of scope"_
  - _Satisfies: Req 5, Criterion "GET /api/audit/permissions SHALL return permission change history (Owner only, or Faction Leader for their faction's changes)"_

- [ ] 10.9 Unit tests for starting set seeding (faction creation seeds capabilities + clearance levels; all are deletable/renameable)
  - _Satisfies: Req 1, Criterion "The following capabilities SHALL be created as a starting set when a faction is first created"_
  - _Satisfies: Req 1, Criterion "Faction Leaders SHALL be able to delete, rename, or recreate any capability — including the starting set"_
  - _Satisfies: Req 3, Criterion "The following starting set SHALL be created when a faction or character first needs clearance levels"_

- [ ] 10.10 Integration tests for API endpoints (full request/response cycle for all CRUD operations)
  - _Satisfies: Req 1, Req 2, Req 3, Req 4, Req 5 — end-to-end verification of all API contracts_

- [ ] 10.11 Build verification — solution compiles with zero errors and zero warnings
  - _Satisfies: All requirements — code quality gate_

---

## Traceability Matrix

### Requirement 1: Named Capability Permissions (15 criteria)

| Criterion | Task(s) |
|-----------|---------|
| Named capability permissions (strings) granted to characters | 1.1 |
| Capabilities additive (role + granted) | 1.7, 5.1, 10.2 |
| Scoped to faction OR character with full parity | 1.1, 1.8, 7.1 |
| POST /api/factions/{uuid}/capabilities | 2.1 |
| POST /api/characters/{uuid}/capabilities | 3.1 |
| Faction-scoped grantable to any member | 1.7, 2.10 |
| Character-scoped grantable to sharing relationship | 1.13, 1.14, 3.10 |
| Faction Leaders can create/delete/rename/grant/revoke | 2.3, 2.10, 2.13 |
| Characters can create/delete/rename/grant/revoke own | 3.3, 3.10, 3.11 |
| Owner manages all regardless of scope | 2.13, 3.11 |
| Starting set on faction creation | 1.21 |
| No undeletable capabilities | 2.3, 10.9 |
| Custom capabilities usable in sharing rules | 2.14 |
| GET /api/factions/{uuid}/capabilities | 2.2 |
| GET /api/characters/{uuid}/capabilities | 3.2 |
| Full parity faction/character | 1.8, 7.1, 10.2 |

### Requirement 2: Permission Groups (14 criteria)

| Criterion | Task(s) |
|-----------|---------|
| Groups defined by faction or character | 1.3, 1.10, 7.1 |
| Faction groups → members; Character groups → sharing targets | 3.6, 2.6 |
| Name, description, set of capabilities | 1.3, 1.4, 1.10, 1.11 |
| Sharing template (set of sharing rules) | 1.5, 1.12, 2.8, 3.8 |
| At most one group per scope | 1.6, 10.6 |
| Managed by group owner (Leader/character/Owner) | 2.13, 3.11 |
| Inherit capabilities AND sharing template on assign | 2.6, 5.1, 10.5 |
| Group changes update all members | 2.8, 2.9, 5.5, 10.5 |
| CRUD endpoints factions + characters | 2.5, 3.5 |
| Assign to group (PUT) | 2.6, 3.6 |
| Remove from group (DELETE) | 2.7, 3.7 |
| Effective permissions = role + group + individual | 5.1, 10.2 |
| Effective sharing = individual + group template | 5.1, 5.2 |
| Full parity faction/character | 1.10, 3.5, 7.1 |

### Requirement 3: Clearance Levels (15 criteria)

| Criterion | Task(s) |
|-----------|---------|
| User-definable table, faction or character owned | 1.2, 1.9 |
| Level (int), Name, optional Description | 1.2, 1.9 |
| Starting set (1-5) on creation | 1.22 |
| Delete/rename/reorder/recreate freely | 2.4, 3.4, 10.9 |
| CRUD endpoints factions + characters | 2.4, 3.4 |
| Each character has assigned clearance per scope | 1.6, 2.11 |
| Leader sets clearance via PUT | 2.11 |
| Sharing rule optionally specifies min clearance | 1.5, 2.8 |
| Character sees data only if clearance >= min level | 5.2, 10.1 |
| Default to lowest level if unspecified | 5.2, 10.1 |
| Groups have default clearance on join | 1.3, 1.10 |
| Owner always highest level | 5.3, 10.3 |
| Leader always >= second-highest | 5.4, 10.3 |
| GET members includes clearance | 2.12 |
| Full parity faction/character | 1.9, 3.4, 7.1 |

### Requirement 4: Intelligence Comments (17 criteria)

| Criterion | Task(s) |
|-----------|---------|
| Comments attached to ExternalCharacter | 1.15, 8.1 |
| Submitter UUID, timestamp, text | 1.15 |
| POST creates private comment | 4.1, 8.2 |
| Share with factions via POST | 4.2, 8.2 |
| Multiple factions independently | 1.16, 4.2 |
| Arrives unclassified (null) | 1.16, 4.2 |
| Unclassified visible only to classify_intel holders | 4.5, 8.3 |
| classify_intel can assign level | 4.4, 8.3 |
| Classified visible by clearance | 4.4, 8.4, 10.4 |
| Classifier identity + timestamp recorded | 1.16, 4.4 |
| Revoke share via DELETE | 4.3, 10.4 |
| Revoked = invisible immediately | 4.3, 10.4 |
| GET returns private + clearance-filtered | 4.6 |
| Submitter name in response | 4.6 |
| Text immutable after creation | 4.8 |
| DELETE removes comment + all shares | 4.7 |
| classify_intel in starting set | 1.21 |

### Requirement 5: Audit Trail (5 criteria)

| Criterion | Task(s) |
|-----------|---------|
| Every permission change logged | 5.6, 10.7 |
| Record: timestamp, actor, target, action, old/new value | 1.17, 5.6 |
| GET /api/audit/permissions (Owner/Leader filtered) | 6.1, 6.4 |
| Retained for configurable period (default 90 days) | 6.2, 10.7 |
| Not deletable (append-only) | 6.3, 10.7 |
