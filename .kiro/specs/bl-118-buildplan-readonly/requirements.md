# Requirements Document

## Introduction

BL-118 restructures FormBuildPlanner so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a BuildPlanMutationService that applies changes atomically. The form never directly mutates a BuildPlan or BuildItem --- only the service does. This follows the same immutable data model pattern established in BL-108 through BL-117.

### Key Characteristics

1. **Complex entity** --- BuildPlan has scalar fields (Name, Description, IsActive) plus a nested Items list of BuildItem entries, each with many fields (ItemType, Status, BlueprintUUID, Quantity, BuildLocationUUID, StructureUUID, Notes, SequenceInStructure, DependsOnUUID, AssemblyLocationUUID, MiningSurveyUUID, etc.).
2. **No reference protection needed** --- Build plans are referenced by stock plans via ReplenishmentBuildPlanUUID, but BuildPlanReferenceCounter already exists and is informational only.
3. **Item operations are buffered** --- Add/remove/modify build items accumulate in the ViewModel edit buffer until Save.
4. **Structure allocation dialog** --- FormStructureAllocation is a modal dialog that returns colony/structure UUIDs. It reads from PlayerContext but does not mutate BuildPlan directly.
5. **ReadOnly wrappers already complete** --- ReadOnlyBuildPlan and ReadOnlyBuildItem are fully implemented.
6. **No write locks** --- No ReaderWriterLockSlim on BuildPlan.
7. **Execution features remain as-is** --- Start Manufacturing, Queue Calc, Generate Delivery, and Auto-Assign read from the ViewModel local state but their mutation targets (colony structures, delivery plans) are out of scope.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers:
- **Plan-level scalar fields:** Name, Description, IsActive
- **Items list:** BuildItem add/remove/modify operations accumulate in the edit buffer until Save

### Scoping Decision: Execution Features

Start Manufacturing, Queue Calc, Generate Delivery, Auto-Assign, and Allocate features read from the current plan state. After migration they read from the ViewModel local state instead of the entity. These features mutate other entities (colony structures, delivery plans) not the BuildPlan itself. They remain as-is for BL-118.

## Glossary

- **BuildPlan**: The mutable entity representing a build plan with Name, Description, IsActive, and a list of BuildItem entries.
- **BuildItem**: A nested object within BuildPlan representing a single build item with ItemType, Status, BlueprintUUID, Quantity, BuildLocationUUID, StructureUUID, Notes, SequenceInStructure, DependsOnUUID, AssemblyLocationUUID, MiningSurveyUUID, and other fields.
- **ReadOnlyBuildPlan**: An immutable wrapper around BuildPlan that exposes only getter properties and a read-only Items list.
- **ReadOnlyBuildItem**: An immutable wrapper around BuildItem exposing all fields as read-only.
- **BuildPlanViewModel**: The ViewModel class that holds a local edit buffer of plan fields and items, disconnected from the entity.
- **BuildPlanMutationService**: A new service class that is the sole mutator of BuildPlan entities (create, update, delete).
- **FormBuildPlanner**: The WinForms form for viewing and editing build plans.
- **FormStructureAllocation**: A modal dialog for assigning build items to colony structures.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of fields in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether fields have been modified since the last load or save.
- **BuildPlanReferenceCounter**: A utility that counts how many stock plans reference a given build plan UUID.

## Requirements