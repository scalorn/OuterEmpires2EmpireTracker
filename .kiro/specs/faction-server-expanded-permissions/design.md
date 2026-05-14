# Faction Server Expanded Permissions — Design

> **Status:** Draft — awaiting requirements iteration before design work begins.

## Overview

This spec extends the base faction server permission model (Requirement 13 of `remote-faction-service`) with concepts from DarkCrusader:

1. **Named Capabilities** — Fine-grained permission strings beyond the three-role hierarchy
2. **User Groups** — Faction-scoped roles with inherited permissions and sharing templates
3. **Clearance Levels** — Numeric tiered visibility for sensitive shared data
4. **Intelligence Comments** — Classified notes on external players
5. **Feature Flags** — Server-side behavior toggles per character
6. **Permission Audit Trail** — Append-only log of all permission changes

## Relationship to Base Spec

This spec is ADDITIVE to `remote-faction-service/requirements.md` Requirement 13. The base three-role model (Owner/Leader/Character) remains the foundation. This spec adds layers on top:

```
Owner (all permissions)
  └── Faction Leader (faction management + own data)
       └── Character (read all + edit own)
            + Capabilities (named permissions, individually or via group)
            + Clearance Level (tiered data visibility)
            + Feature Flags (server behavior toggles)
```

## Data Model Sketch

```
Capability
  - UUID
  - Name (string, unique within scope)
  - Description
  - ScopeType (enum: Faction, Character)
  - ScopeUUID (FactionUUID or CharacterUUID — who owns/defined this capability)

FactionGroup
  - UUID
  - FactionUUID
  - Name
  - Description
  - DefaultClearanceLevel (int, 1-5)
  - Capabilities (List<string> — capability names)
  - SharingTemplate (List<SharingRule>)

CharacterPermissions (per character, per faction)
  - CharacterUUID
  - FactionUUID
  - GroupUUID (nullable — at most one group)
  - ClearanceLevel (int, 1-5)
  - IndividualCapabilities (List<string>)
  - FeatureFlags (Dictionary<string, bool>)

IntelComment
  - UUID
  - TargetCharacterUUID (the external character this is about)
  - SubmitterCharacterUUID
  - FactionUUID (which faction's intel this belongs to)
  - ClassificationLevel (int, 1-5)
  - Text
  - CreatedUtc

PermissionAuditEntry
  - UUID
  - Timestamp
  - ActorCharacterUUID
  - TargetCharacterUUID
  - ActionType (enum: CapabilityGranted, CapabilityRevoked, GroupAssigned, GroupRemoved, ClearanceChanged, FeatureFlagChanged)
  - OldValue
  - NewValue
```

## Design Decisions (pending)

Awaiting requirements iteration. Key decisions needed:

1. Storage location — extend existing character/faction entities or separate permission tables?
2. API surface — new endpoints or extend existing ones?
3. Effective permission computation — computed on every request or cached?
4. Sharing template application — eager (copy rules on assignment) or lazy (resolve at query time)?
