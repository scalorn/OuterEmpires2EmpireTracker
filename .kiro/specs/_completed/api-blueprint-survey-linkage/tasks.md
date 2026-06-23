# Implementation Plan: API Blueprint & Survey Linkage

## Overview

This plan implements the blueprint and survey linkage feature in discrete, verifiable steps. Each task is sized to ≤5 files modified, ≤200 new lines, and ≤3 acceptance criteria. The approach is: models first, then services (logic), then wiring (integration), then tests.

## Tasks

- [x] 1. Add PropertyTypeDefinition model and BaselineRoot field
  - [x] 1.1 Create PropertyTypeDefinition model class
    - Create `OE2EmpireTracker.Common/Models/PropertyTypeDefinition.cs`
    - Six properties: ModTypeId (int), PropertyName, FriendlyPropertyName, Unit (strings), ResearchPositive, CanResearch (bools)
    - JsonProperty attributes on all properties
    - _Requirements: 7.6_
    - _Verification: getDiagnostics on new file, build solution_

  - [x] 1.2 Add PropertyType array to BaselineRoot
    - Modify `OE2EmpireTracker.Common/Services/BaselineRoot.cs` to add `PropertyType` property (PropertyTypeDefinition[])
    - _Requirements: 7.2_
    - _Verification: getDiagnostics, build solution_

- [x] 2. Enrich DTOs and item models with new property fields
  - [x] 2.1 Add three fields to GameApiAssetItemProperty DTO
    - Modify `OE2EmpireTracker.Common/Models/GameApiAssetResponse.cs`
    - Add: OriginalPropertyValue (decimal, default 0), ResearchPositive (bool, default false), CanResearch (bool, default false)
    - JsonProperty attributes on each
    - _Requirements: 9.1, 9.2, 9.3_
    - _Verification: getDiagnostics, build solution_

  - [x] 2.2 Add three fields to ItemProperty model
    - Modify `OE2EmpireTracker.Common/Models/ItemProperty.cs`
    - Add: OriginalPropertyValue (string, default empty), ResearchPositive (bool), CanResearch (bool)
    - JsonProperty attributes on each
    - _Requirements: 9.1_
    - _Verification: getDiagnostics, build solution_

- [x] 3. Add PropertyTypeRegistry to EmpireContext
  - [x] 3.1 Add PropertyTypeRegistry collection and methods to EmpireContext
    - Modify `OE2EmpireTracker.Common/Services/EmpireContext.cs`
    - Add private List + Dictionary cache fields
    - Add PropertyTypeRegistry read-only property, FindPropertyType(int), UpsertPropertyType(PropertyTypeDefinition)
    - Add InitPropertyTypes(BaselineRoot) called from existing load path
    - _Requirements: 7.2, 7.3, 7.4, 7.5_
    - _Verification: getDiagnostics, build solution_

- [x] 4. Checkpoint - Verify model layer compiles
  - Ensure all tests pass, ask the user if questions arise.


- [x] 5. Implement BlueprintLinkageService
  - [x] 5.1 Create BlueprintLinkageService with ProcessItem, BuildCandidateBlueprint, and ClassifyBlueprintType
    - Create `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs`
    - Constructor takes PlayerContext, EmpireContext
    - ProcessItem: skip if empty properties, register property types, build candidate, find match, create or update, set BaseItemTypeID
    - BuildCandidateBlueprint: handles typeC="Bp" and typeC="S" items
    - ClassifyBlueprintType: Hu→Hull, Flatpack detection, fallback to MapShipPartType
    - _Requirements: 1.1, 1.7, 2.1, 2.5, 3.1, 3.3, 3.4_
    - _Verification: getDiagnostics, build solution_

  - [x] 5.2 Implement MapShipPartType, MergeProperties, BuildPropertyBag, and RegisterPropertyTypes
    - Continue in `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs`
    - MapShipPartType: full lookup table (Sh→Shield, Re→Reactor, etc.), unknown→raw value + warning log
    - MergeProperties: additive merge into PropertyBag with _orig_ prefix convention
    - BuildPropertyBag: fresh PropertyBag from API properties
    - RegisterPropertyTypes: upserts into EmpireContext.PropertyTypeRegistry
    - _Requirements: 3.2, 6.1, 6.2, 6.3, 7.1, 8.1, 8.2, 8.3_
    - _Verification: getDiagnostics, build solution_

  - [x] 5.3 Implement ownership routing and dedup logic in BlueprintLinkageService
    - Continue in `OE2EmpireTracker.Common/Services/BlueprintLinkageService.cs`
    - IsGlobalRoute: Evo 0 → EmpireContext (global), Evo > 0 → PlayerContext (player)
    - FindUnambiguousMatch against correct list based on routing
    - Set OwnerUUID = "" for global, ownerUUID param for player
    - Logging: Info on create (name, UUID, type, evo), Info on update (name, UUID, property count)
    - Error handling: catch per-item, log Error, continue
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 2.3, 2.4, 5.1, 5.2, 5.3, 10.3, 10.5, 11.1, 11.3, 12.1, 12.2, 12.5_
    - _Verification: getDiagnostics, build solution_

- [x] 6. Implement SurveyLinkageService
  - [x] 6.1 Create SurveyLinkageService with ProcessItem
    - Create `OE2EmpireTracker.Common/Services/SurveyLinkageService.cs`
    - Constructor takes PlayerContext
    - Regex: `^Survey Report:\s*(.+?)\s*\(([A-Fa-f0-9]+)\)$`
    - Parse planet name and hex code from resourceName
    - Find existing survey by PlanetName (case-insensitive)
    - If found: set BaseItemTypeID to existing UUID
    - If not found: create stub Survey (UUID, PlanetName, SurveyID=hexCode, OwnerUUID)
    - Logging: Info on link, Info on stub creation, Warn on malformed name
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 10.4, 11.2, 12.3, 12.4_
    - _Verification: getDiagnostics, build solution_

- [x] 7. Checkpoint - Verify service layer compiles
  - Ensure all tests pass, ask the user if questions arise.


- [x] 8. Wire linkage into ColonyMergeService
  - [x] 8.1 Add optional linkage parameters to ColonyMergeService.MergeWarehouse
    - Modify `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`
    - Add optional params: BlueprintLinkageService blueprintLinkage = null, SurveyLinkageService surveyLinkage = null
    - Add HasBlueprintProperties helper (typeC in {Bp, S} and non-empty properties)
    - Add IsSurveyItem helper (typeC = Sc)
    - After ProcessSingleAssetItemAndReturnUUID, invoke linkage services for qualifying items
    - _Requirements: 10.1, 10.2_
    - _Verification: getDiagnostics, build solution_

  - [x] 8.2 Update AssetMergeService.MapProperties to include enriched fields
    - Modify `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
    - Update MapProperties to map OriginalPropertyValue, ResearchPositive, CanResearch from DTO to ItemProperty
    - Update ArePropertiesEqual to compare the three new fields
    - _Requirements: 9.1_
    - _Verification: getDiagnostics, build solution_

  - [x] 8.3 Pass linkage services from caller into ColonyMergeService
    - Identify the caller of MergeWarehouse (likely in the sync/API pipeline)
    - Instantiate BlueprintLinkageService and SurveyLinkageService with PlayerContext, EmpireContext
    - Pass them to MergeWarehouse calls for colony warehouse syncs
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
    - _Verification: getDiagnostics, build solution_

- [x] 9. Checkpoint - Full build and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Write unit tests for BlueprintLinkageService
  - [x] 10.1 Create BlueprintLinkageServiceTests with creation and update tests
    - Create `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - Test: ProcessItem_WithBlueprintProperties_CreatesNewBlueprint
    - Test: ProcessItem_WithExistingMatch_UpdatesProperties
    - Test: ProcessItem_WithEmptyProperties_ReturnsFalseNoCreation
    - Test: ProcessItem_SetsBaseItemTypeID_ToCreatedBlueprintUUID
    - _Requirements: 1.1, 1.2, 1.3, 1.7, 5.1_
    - _Verification: vstest.console, all tests pass_

  - [x] 10.2 Add ship part and type classification tests
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - Test: ProcessItem_ShipPart_MapsShipPartTypeCorrectly
    - Test: ProcessItem_Hull_ClassifiesAsHullType
    - Test: ProcessItem_UnknownShipPartType_UsesRawValueAndLogsWarning
    - Test: ProcessItem_Flatpack_ClassifiesAsFlatpackType
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 3.3, 3.4_
    - _Verification: vstest.console, all tests pass_

  - [x] 10.3 Add ownership routing and registry tests
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - Test: ProcessItem_Evo0_CreatesGlobalBlueprint
    - Test: ProcessItem_EvoGreaterThan0_CreatesPlayerBlueprint
    - Test: ProcessItem_RegistersPropertyTypes_InRegistry
    - Test: ProcessItem_PreservesExistingProperties_NotInApiResponse
    - _Requirements: 1.4, 1.5, 6.1, 6.2, 7.1, 7.5_
    - _Verification: vstest.console, all tests pass_


- [x] 11. Write unit tests for SurveyLinkageService
  - [x] 11.1 Create SurveyLinkageServiceTests with all unit tests
    - Create `OE2EmpireTracker.Tests/Services/SurveyLinkageServiceTests.cs`
    - Test: ProcessItem_MatchesExistingSurvey_LinksByUUID
    - Test: ProcessItem_NoExistingSurvey_CreatesStub
    - Test: ProcessItem_MalformedName_SkipsAndLogsWarning
    - Test: ProcessItem_CaseInsensitiveMatch_FindsExisting
    - Test: ProcessItem_SetsBaseItemTypeID_ToSurveyUUID
    - Test: ProcessItem_StubSurvey_SetsSurveyIDToHexCode
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 11.2_
    - _Verification: vstest.console, all tests pass_

- [x] 12. Write property-based tests for idempotency and correctness
  - [x] 12.1 Write FsCheck property test for blueprint idempotency
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - **Property 1: Idempotency** — process same item N times, assert exactly one blueprint per dedup key
    - **Validates: Requirements 11.1**
    - _Verification: vstest.console, property test passes_

  - [x] 12.2 Write FsCheck property test for survey idempotency
    - Continue in `OE2EmpireTracker.Tests/Services/SurveyLinkageServiceTests.cs`
    - **Property 2: Survey Linkage Uniqueness** — process same survey N times, assert exactly one survey per planet name
    - **Validates: Requirements 11.2**
    - _Verification: vstest.console, property test passes_

  - [x] 12.3 Write FsCheck property test for property preservation
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - **Property 4: Property Preservation** — existing properties not in API are never removed after merge
    - **Validates: Requirements 6.1, 6.2**
    - _Verification: vstest.console, property test passes_

  - [x] 12.4 Write FsCheck property test for ownership routing
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - **Property 7: Ownership Correctness** — Evo > 0 → player, Evo = 0 → global
    - **Validates: Requirements 1.4, 1.5**
    - _Verification: vstest.console, property test passes_

  - [x] 12.5 Write FsCheck property test for PropertyTypeRegistry completeness
    - Continue in `OE2EmpireTracker.Tests/Services/BlueprintLinkageServiceTests.cs`
    - **Property 5: PropertyTypeRegistry Completeness** — after processing, every modTypeId in properties exists in registry
    - **Validates: Requirements 7.1, 7.5**
    - _Verification: vstest.console, property test passes_

- [x] 13. Final checkpoint - Full build, all tests pass, audit clean
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design uses `BlueprintService.IsGlobalRoute` for routing — tasks 5.3 references this
- Hull items arrive as typeC="S" with shipPartType="Hu" (no separate "SH" typeC)
- PropertyBag enrichment uses `_orig_` prefix convention for original values
- FsCheck 2.16.6 APIs only (no 3.x Fluent namespace)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "2.2"] },
    { "id": 1, "tasks": ["1.2", "3.1"] },
    { "id": 2, "tasks": ["5.1"] },
    { "id": 3, "tasks": ["5.2", "5.3", "6.1"] },
    { "id": 4, "tasks": ["8.1", "8.2"] },
    { "id": 5, "tasks": ["8.3"] },
    { "id": 6, "tasks": ["10.1", "11.1"] },
    { "id": 7, "tasks": ["10.2", "10.3"] },
    { "id": 8, "tasks": ["12.1", "12.2", "12.3", "12.4", "12.5"] }
  ]
}
```
