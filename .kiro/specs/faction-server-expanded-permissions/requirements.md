# Faction Server Expanded Permissions — Requirements

> **Origin:** Features extracted from the DarkCrusader codebase (OE1 clan management tool) that extend the base faction server permission model defined in `remote-faction-service/requirements.md` Requirement 13.

## Context

The base faction server has three fixed roles (Owner, Faction Leader, Character) with per-entity sharing rules. DarkCrusader had a more flexible permission system with arbitrary named permissions, user groups, and numeric clearance levels. This spec captures those concepts adapted for OE2.

## Requirement 1: Named Capability Permissions

### Background (from DarkCrusader)

DarkCrusader defined permissions as named strings (`access_empire`, `access_faction_bank`, `access_scans`, `access_faction_research`, `access_private_chat`, `access_admin_panel`) that could be granted or denied per group. This allowed fine-grained feature gating beyond the three-role hierarchy.

### User Stories

- As a faction leader, I want to grant specific characters the ability to import global blueprints without giving them full Owner access.
- As a faction leader, I want to restrict certain features (e.g., viewing market data, modifying delivery routes) to trusted members only.
- As the server owner, I want to define custom capabilities that gate access to specific API operations.

### Acceptance Criteria

- [ ] The service SHALL support named capability permissions (strings) that can be granted to individual characters.
- [ ] Capabilities SHALL be additive — a character has their role permissions PLUS any explicitly granted capabilities.
- [ ] Capabilities SHALL be scoped to either a faction OR a character — both can define, grant, and revoke capabilities with full parity.
- [ ] A faction SHALL be able to define capabilities via POST /api/factions/{uuid}/capabilities.
- [ ] A character SHALL be able to define capabilities via POST /api/characters/{uuid}/capabilities.
- [ ] Faction-scoped capabilities are grantable to any member of that faction.
- [ ] Character-scoped capabilities are grantable to any character the granting character has a sharing relationship with.
- [ ] Faction Leaders SHALL be able to create, delete, rename, grant, and revoke any capability within their faction.
- [ ] Characters SHALL be able to create, delete, rename, grant, and revoke any capability they own.
- [ ] The Owner SHALL be able to manage all capabilities regardless of scope.
- [ ] The following capabilities SHALL be created as a starting set when a faction is first created:
  - `global_data_write` — Can write to global/baseline data (normally Owner-only)
  - `server_processing_admin` — Can enable/disable server-side processing
  - `export_any_character` — Can export any character's data (normally Owner-only)
  - `manage_faction_members` — Can accept/reject join requests and remove members (normally Leader-only)
- [ ] Faction Leaders SHALL be able to delete, rename, or recreate any capability — including the starting set. There are no undeletable capabilities.
- [ ] Custom capabilities SHALL be usable in sharing rules (e.g., "share this data with anyone who has capability X").
- [ ] GET /api/factions/{uuid}/capabilities SHALL return all capabilities defined by that faction.
- [ ] GET /api/characters/{uuid}/capabilities SHALL return all capabilities granted to or defined by a specific character.
- [ ] There SHALL be full parity between faction-level and character-level capability operations — anything a faction can do with capabilities, a character can do with their own.

## Requirement 2: User Groups / Permission Sets

### Background (from DarkCrusader)

DarkCrusader organized users into groups (e.g., "Root Admin", "Premium Member", "Regular Member", "Recruit") where each group had a predefined set of permissions. This made bulk permission management practical — assign a user to a group and they inherit all its permissions.

### User Stories

- As a faction leader, I want to define roles like "Officer", "Logistics", "Scout" that automatically grant the right permissions and sharing access.
- As a faction leader, I want to assign a new member to a role and have them immediately get the correct access level.
- As a faction leader, I want to change a role's permissions and have all members in that role updated automatically.

### Acceptance Criteria

- [ ] The service SHALL support user-defined groups (roles) within a faction.
- [ ] Each group SHALL have a name, description, and a set of granted capabilities.
- [ ] Each group SHALL have a sharing template — a set of sharing rules that are automatically applied to members of the group.
- [ ] A character SHALL belong to at most one group within their faction.
- [ ] Group membership SHALL be managed by Faction Leaders (or Owner).
- [ ] When a character is assigned to a group, they SHALL inherit the group's capabilities AND sharing template.
- [ ] When a group's capabilities or sharing template change, all members SHALL be updated.
- [ ] Groups SHALL be faction-scoped (each faction defines its own groups independently).
- [ ] CRUD endpoints: POST/GET/PUT/DELETE /api/factions/{uuid}/groups.
- [ ] Assign character to group: PUT /api/factions/{uuid}/groups/{groupId}/members.
- [ ] Remove character from group: DELETE /api/factions/{uuid}/groups/{groupId}/members/{characterUUID}.
- [ ] A character's effective permissions SHALL be: role permissions + group capabilities + individual capabilities.
- [ ] A character's effective sharing access SHALL be: individual sharing rules + group sharing template.

## Requirement 3: Intelligence Clearance Levels

### Background (from DarkCrusader)

DarkCrusader had a numeric `classification_level` on intelligence comments and a `group_clearance_level` on user groups. Users could only see intel at or below their clearance level. This created a tiered visibility system for sensitive information.

### User Stories

- As a faction leader, I want to classify shared data by sensitivity level so that new recruits see less than trusted officers.
- As a faction leader, I want to share strategic information (build plans, supply chains) only with high-clearance members.
- As a character, I want to see all data I'm cleared for without needing individual sharing rules for each item.

### Acceptance Criteria

- [ ] The service SHALL support numeric clearance levels (1–5, where 5 is highest).
- [ ] Each character SHALL have an effective clearance level within their faction (default: 1).
- [ ] Faction Leaders SHALL be able to set a character's clearance level via PUT /api/factions/{uuid}/members/{characterUUID}/clearance.
- [ ] Each sharing rule SHALL optionally specify a minimum clearance level required to access the shared data.
- [ ] A character SHALL only see shared data where their clearance level >= the sharing rule's minimum level.
- [ ] If no clearance level is specified on a sharing rule, it SHALL default to 1 (visible to all faction members with any clearance).
- [ ] Clearance levels SHALL be faction-scoped (a character's level in Faction A is independent of their level in Faction B).
- [ ] Groups (Requirement 2) SHALL have a default clearance level assigned to members.
- [ ] The Owner SHALL always have effective clearance level 5 (maximum) in all factions.
- [ ] Faction Leaders SHALL always have effective clearance level >= 4 in their faction.
- [ ] GET /api/factions/{uuid}/members SHALL include each member's clearance level in the response.

## Requirement 4: Intelligence Comments System

### Background (from DarkCrusader)

DarkCrusader had an intelligence system where faction members could post classified comments about external players. Comments had a classification level, submitter, timestamp, and the target player name. Only users with sufficient clearance could see higher-classified comments.

### User Stories

- As a faction member, I want to record notes about external players (allies, enemies, traders) that other faction members can see.
- As a faction leader, I want to classify sensitive intel (e.g., spy reports, diplomatic negotiations) at a higher level so only officers see it.
- As a faction member, I want to see all intel about a player when I encounter them in-game.

### Acceptance Criteria

- [ ] The service SHALL support intelligence comments attached to ExternalCharacter entities.
- [ ] Each comment SHALL have: submitter character UUID, timestamp, classification level (1–5), and text content.
- [ ] POST /api/characters/{uuid}/intel SHALL add a comment to the specified external character.
- [ ] GET /api/characters/{uuid}/intel SHALL return all comments the requesting character is cleared to see (classification_level <= viewer's clearance).
- [ ] Comments SHALL be visible to all faction members with sufficient clearance (not just the submitter's faction — if the external character is known to multiple factions, each faction sees only their own comments).
- [ ] The submitter's name SHALL be included in the response.
- [ ] Comments SHALL be immutable once created (no edit, only delete by submitter or Leader/Owner).
- [ ] DELETE /api/characters/{uuid}/intel/{commentId} SHALL remove a comment (submitter, Faction Leader, or Owner only).
- [ ] Intel comments SHALL be included in faction shared data views (filtered by clearance).

## Requirement 5: Feature Gating / Premium Tiers

### Background (from DarkCrusader)

DarkCrusader had a `premium` boolean on user groups that unlocked advanced features (market analytics, personal bank history, detailed transaction tracking). This gated expensive-to-compute or sensitive features behind a tier.

### User Stories

- As a server owner, I want to gate resource-intensive features (e.g., server-side processing, bulk exports) behind a flag so I can control server load.
- As a faction leader, I want to offer advanced analytics only to active/contributing members.

### Acceptance Criteria

- [ ] The service SHALL support feature flags that can be enabled/disabled per character.
- [ ] Feature flags SHALL be distinct from capabilities — capabilities gate API operations, feature flags gate optional behaviors.
- [ ] Built-in feature flags:
  - `server_side_processing` — Character's colonies are processed server-side (already in Req 17 of base spec)
  - `real_time_push` — Character receives WebSocket push events (already in Req 18 of base spec)
  - `bulk_export` — Character can use the bulk export endpoint
  - `market_analytics` — Character can access aggregated market analytics endpoints
- [ ] The Owner SHALL be able to set feature flags per character via PUT /api/characters/{uuid}/features.
- [ ] Faction Leaders SHALL be able to set feature flags for their faction members (within limits set by the Owner).
- [ ] The Owner SHALL be able to set server-wide defaults for feature flags (e.g., "all new characters get real_time_push enabled").
- [ ] GET /api/characters/{uuid}/features SHALL return the character's active feature flags.

## Requirement 6: Audit Trail for Permission Changes

### Background (from DarkCrusader)

DarkCrusader logged all user actions via `LoggedActionBean`. For a permission system, audit trails are critical — who granted what to whom, and when.

### User Stories

- As a faction leader, I want to see who changed a member's clearance level or group assignment.
- As the server owner, I want to audit all permission changes for security.

### Acceptance Criteria

- [ ] Every permission change (capability grant/revoke, group assignment, clearance level change, feature flag change) SHALL be logged.
- [ ] The audit log SHALL record: timestamp, actor (who made the change), target (who was affected), action type, old value, new value.
- [ ] GET /api/audit/permissions SHALL return permission change history (Owner only, or Faction Leader for their faction's changes).
- [ ] Audit records SHALL be retained for a configurable period (default: 90 days).
- [ ] Audit records SHALL NOT be deletable (append-only).

---

## Open Questions

1. **Capability inheritance** — Should capabilities be strictly additive, or should groups be able to DENY capabilities that the role would normally grant? DarkCrusader supported deny (value=0) in addition to grant (value=1) and inherit (value=-1).

2. **Cross-faction clearance** — If a character is in multiple factions (alliance scenario), should clearance levels be independent per faction? (Current spec says yes — faction-scoped.)

3. **Sharing template complexity** — Group sharing templates could become complex. Should they support the full sharing rule syntax (per-entity, per-category, per-clearance-level) or a simplified subset?

4. **Intel comment scope** — Should intel comments be faction-scoped (only your faction's comments on a player) or global (all factions' comments visible if you have clearance)? DarkCrusader was single-faction so this wasn't an issue.

5. **Feature flag vs. capability** — The line between "feature flag" and "capability" is blurry. Should they be unified into a single permission system, or kept separate (capabilities = what you CAN do, features = what the server WILL do for you)?
