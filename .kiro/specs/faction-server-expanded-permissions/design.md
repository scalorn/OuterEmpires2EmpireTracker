# Faction Server Expanded Permissions — Design

> **Status:** Draft — awaiting requirements iteration before design work begins.

## Overview

This spec extends the base faction server permission model (Requirement 13 of `remote-faction-service`) with concepts from DarkCrusader:

1. **Named Capabilities** — Fine-grained permission strings beyond the three-role hierarchy (includes both action permissions and server behavior opt-ins)
2. **Permission Groups** — Faction or character-scoped roles with inherited permissions and sharing templates
3. **Clearance Levels** — Numeric tiered visibility for sensitive shared data
4. **Intelligence Comments** — Classified notes on external players
5. **Permission Audit Trail** — Append-only log of all permission changes

## Relationship to Base Spec

This spec is ADDITIVE to `remote-faction-service/requirements.md` Requirement 13. The base three-role model (Owner/Leader/Character) remains the foundation. This spec adds layers on top:

```
Owner (all permissions)
  └── Faction Leader (faction management + own data)
       └── Character (read all + edit own)
            + Capabilities (named permissions — actions AND server behaviors)
            + Clearance Level (tiered data visibility)
```

## Data Model

```
DataType (fixed enum — maps to storage collections and API endpoints)
  Values:
    - blueprints
    - colonies
    - surveys
    - delivery-routes
    - delivery-plans
    - build-plans
    - ships
    - ship-templates
    - stations
    - asteroids
    - market-listings
    - market-transactions
    - pricing-plans
    - stock-plans
    - stock-profiles
    - supply-chains
  Not user-extensible — adding new types requires code changes.

Capability (gates what you can DO — operations, actions, and server behavior opt-ins)
  - UUID
  - Name (string, unique within scope)
  - Description
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — who owns/defined this capability)

ClearanceLevel (gates what you can SEE — tiered data visibility)
  - UUID
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — who owns this level definition)
  - Level (int — numeric ordering, higher = more access)
  - Name (string — display name, e.g. "Recruit", "Member", "Officer", "Command", "Leader")
  - Description (string, optional)
  Starting set on creation: 1=Recruit, 2=Member, 3=Officer, 4=Command, 5=Leader
  Owners can wipe and rebuild with any levels/names they want.

PermissionGroup
  - UUID
  - Name
  - Description
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — who owns this group)
  - DefaultClearanceLevelUUID → ClearanceLevel.UUID (assigned to members on join)

GroupCapability (junction: which capabilities a group grants)
  - GroupUUID → PermissionGroup.UUID
  - CapabilityUUID → Capability.UUID

GroupSharingRule (sharing template applied to group members)
  - UUID
  - GroupUUID → PermissionGroup.UUID
  - DataType (nullable — category-level rule)
  - EntityUUID (nullable — item-level rule)
  - MinClearanceLevelUUID → ClearanceLevel.UUID (minimum level to see this data)

CharacterPermissions (per character, per scope — links character to a group + clearance)
  - CharacterUUID → Character.UUID
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — whose group they're in)
  - GroupUUID → PermissionGroup.UUID (nullable — at most one group per scope)
  - ClearanceLevelUUID → ClearanceLevel.UUID (character's assigned clearance)

CharacterCapability (individual capability grants, outside of groups)
  - CharacterUUID → Character.UUID
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — who granted it)
  - CapabilityUUID → Capability.UUID

IntelComment
  - UUID
  - TargetCharacterUUID → Character.UUID (the external character this is about)
  - SubmitterCharacterUUID → Character.UUID (who wrote it)
  - FactionUUID → Faction.UUID (nullable — which faction can see it, null = private to submitter)
  - ClassificationLevelUUID → ClearanceLevel.UUID (nullable — minimum clearance to view within faction, null when private)
  - Text
  - CreatedUtc
  Notes:
    - When FactionUUID is null, only the submitter can see the comment (private note).
    - The submitter can share a private comment with their faction by setting FactionUUID.
    - The submitter can remove faction visibility by clearing FactionUUID back to null.
    - ClassificationLevelUUID only applies when FactionUUID is set (faction-visible comments).

PermissionAuditEntry (append-only log)
  - UUID
  - Timestamp
  - ActorCharacterUUID → Character.UUID
  - TargetCharacterUUID → Character.UUID
  - ActionType (enum: CapabilityGranted, CapabilityRevoked, GroupAssigned, GroupRemoved, ClearanceChanged)
  - OldValue
  - NewValue
```

## Design Decisions (pending)

Awaiting requirements iteration. Key decisions needed:

1. Storage location — extend existing character/faction entities or separate permission tables?
2. API surface — new endpoints or extend existing ones?
3. Effective permission computation — computed on every request or cached?
4. Sharing template application — eager (copy rules on assignment) or lazy (resolve at query time)?

## Database Schema

### Faction-Scoped Relationships

Starting from Faction — what a faction owns and how members connect to it:

```mermaid
erDiagram
    Faction {
        string UUID PK
        string Name
        string Description
    }

    Faction ||--o{ ClearanceLevel : "defines levels"
    Faction ||--o{ Capability : "defines capabilities"
    Faction ||--o{ PermissionGroup : "defines groups"
    Faction ||--o{ CharacterPermissions : "members have"
    Faction ||--o{ IntelComment : "shared intel (nullable)"

    ClearanceLevel {
        string UUID PK
        string ScopeType "Faction"
        string ScopeUUID FK "FactionUUID"
        int Level "numeric ordering"
        string Name "e.g. Recruit, Officer"
    }

    Capability {
        string UUID PK
        string Name
        string ScopeType "Faction"
        string ScopeUUID FK "FactionUUID"
        string Description
    }

    PermissionGroup {
        string UUID PK
        string Name
        string ScopeType "Faction"
        string ScopeUUID FK "FactionUUID"
        string DefaultClearanceLevelUUID FK
    }

    PermissionGroup }o--|| ClearanceLevel : "default level"
    PermissionGroup ||--o{ GroupCapability : "grants"
    PermissionGroup ||--o{ GroupSharingRule : "templates"

    GroupCapability {
        string GroupUUID FK
        string CapabilityUUID FK
    }

    GroupSharingRule {
        string UUID PK
        string GroupUUID FK
        string DataType "nullable"
        string EntityUUID "nullable"
        string MinClearanceLevelUUID FK
    }

    GroupSharingRule }o--|| ClearanceLevel : "min level"
    Capability ||--o{ GroupCapability : "granted via"

    CharacterPermissions {
        string CharacterUUID FK
        string ScopeType "Faction"
        string ScopeUUID FK "FactionUUID"
        string GroupUUID FK "nullable"
        string ClearanceLevelUUID FK
    }

    CharacterPermissions }o--|| PermissionGroup : "assigned group"
    CharacterPermissions }o--|| ClearanceLevel : "assigned level"

    CharacterCapability {
        string CharacterUUID FK
        string ScopeType "Faction"
        string ScopeUUID FK "FactionUUID"
        string CapabilityUUID FK
    }

    CharacterCapability }o--|| Capability : "grants"
    CharacterPermissions ||--o{ CharacterCapability : "individual grants"

    IntelComment {
        string UUID PK
        string TargetCharacterUUID FK
        string SubmitterCharacterUUID FK
        string FactionUUID FK "nullable"
        string ClassificationLevelUUID FK "nullable"
        string Text
        datetime CreatedUtc
    }

    IntelComment }o--|| ClearanceLevel : "classification"
```

### Character-Scoped Relationships

Starting from Character — what a character owns and how they grant access to others:

```mermaid
erDiagram
    Character {
        string UUID PK
        string Name
        string FactionUUID FK "nullable"
    }

    Character ||--o{ ClearanceLevel : "defines levels"
    Character ||--o{ Capability : "defines capabilities"
    Character ||--o{ PermissionGroup : "defines groups"
    Character ||--o{ CharacterPermissions : "grants to others"
    Character ||--o{ IntelComment : "submits"
    Character ||--o{ PermissionAuditEntry : "actor or target"

    ClearanceLevel {
        string UUID PK
        string ScopeType "Character"
        string ScopeUUID FK "CharacterUUID"
        int Level "numeric ordering"
        string Name "e.g. Trusted, Inner Circle"
    }

    Capability {
        string UUID PK
        string Name
        string ScopeType "Character"
        string ScopeUUID FK "CharacterUUID"
        string Description
    }

    PermissionGroup {
        string UUID PK
        string Name
        string ScopeType "Character"
        string ScopeUUID FK "CharacterUUID"
        string DefaultClearanceLevelUUID FK
    }

    PermissionGroup }o--|| ClearanceLevel : "default level"
    PermissionGroup ||--o{ GroupCapability : "grants"
    PermissionGroup ||--o{ GroupSharingRule : "templates"

    GroupCapability {
        string GroupUUID FK
        string CapabilityUUID FK
    }

    GroupSharingRule {
        string UUID PK
        string GroupUUID FK
        string DataType "nullable"
        string EntityUUID "nullable"
        string MinClearanceLevelUUID FK
    }

    GroupSharingRule }o--|| ClearanceLevel : "min level"
    Capability ||--o{ GroupCapability : "granted via"

    CharacterPermissions {
        string CharacterUUID FK "the grantee"
        string ScopeType "Character"
        string ScopeUUID FK "granting CharacterUUID"
        string GroupUUID FK "nullable"
        string ClearanceLevelUUID FK
    }

    CharacterPermissions }o--|| PermissionGroup : "assigned group"
    CharacterPermissions }o--|| ClearanceLevel : "assigned level"

    CharacterCapability {
        string CharacterUUID FK "the grantee"
        string ScopeType "Character"
        string ScopeUUID FK "granting CharacterUUID"
        string CapabilityUUID FK
    }

    CharacterCapability }o--|| Capability : "grants"
    CharacterPermissions ||--o{ CharacterCapability : "individual grants"

    IntelComment {
        string UUID PK
        string TargetCharacterUUID FK "about"
        string SubmitterCharacterUUID FK "author"
        string FactionUUID FK "nullable (null=private)"
        string ClassificationLevelUUID FK "nullable"
        string Text
        datetime CreatedUtc
    }

    IntelComment }o--|| Character : "about (target)"

    PermissionAuditEntry {
        string UUID PK
        datetime Timestamp
        string ActorCharacterUUID FK
        string TargetCharacterUUID FK
        string ActionType "enum"
        string OldValue
        string NewValue
    }
```
