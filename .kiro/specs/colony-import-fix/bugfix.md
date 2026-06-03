# Bugfix Requirements Document

## Introduction

The colony structure import from the Game API (`ColonyMergeService.MergeBuildings`) incorrectly reconciles API building data with the local structure pool. The current implementation processes each API building independently without maintaining pool consumption state, leading to multiple structures marked as "building" simultaneously, spurious new structure entries, and missing staged-state assignments from warehouse flatpack inventory.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the Game API returns multiple structures with future completion dates THEN the system marks all of them as "building" (Built=false with active BuildCompletionTime), violating the one-building-at-a-time invariant

1.2 WHEN the Game API returns a built structure whose type matches an existing unassigned pool entry AND a second unassigned pool entry of the same type also exists THEN the system may fail to match the correct pool entry or create a duplicate new structure entry instead of consuming from the pool

1.3 WHEN the Game API returns structures that have been built THEN the system does not mark remaining unmatched pool entries as "staged" based on the colony's warehouse flatpack inventory

1.4 WHEN the colony's warehouse contains flatpacks of a type matching remaining pool entries THEN the system ignores the warehouse entirely during structure reconciliation, leaving those entries without Staged=true

1.5 WHEN a pool entry has no corresponding API building and no matching warehouse flatpack THEN the system does not explicitly preserve it as "planned" (unbuilt, unstaged) — its state may be corrupted by prior incorrect merges

1.6 WHEN the Game API returns structures sorted by completion date THEN the system assigns DisplaySequence/BuildQueueSequence based on API order without considering that only built/building structures should update sequence from API — remaining pool entries should keep their user-defined BuildQueueSequence order

### Expected Behavior (Correct)

2.1 WHEN the Game API returns structures with completion dates THEN the system SHALL mark at most 1 structure as "building" — specifically the one (if any) whose ConstructingBuildingFinish is in the future. The game enforces this invariant; the tool does not need to handle a hypothetical case of multiple future dates.

2.2 WHEN matching API structures to the local pool THEN the system SHALL walk API structures in completion-date order (ascending by ConstructingBuildingFinish), consuming matching pool entries one-by-one (match by BuildingID first, then by FlatpackBlueprintUUID for unassigned entries), and only create a new structure entry when no matching pool entry exists for that structure type

2.3 WHEN API structures have been matched (built or building) THEN the system SHALL examine remaining unmatched pool entries and the colony's warehouse flatpack inventory (Colony.Items), marking pool entries as Staged=true (via Properties["Staged"]) for each matching flatpack type up to the warehouse quantity for that type. Match key: structure's FlatpackBlueprintUUID == warehouse item's BaseItemTypeID (both are blueprint UUIDs).

2.4 WHEN the warehouse has fewer flatpacks of a type than remaining pool entries of that type THEN the system SHALL mark only the first N entries (by BuildQueueSequence order) as staged, where N equals the warehouse flatpack count for that type

2.5 WHEN remaining pool entries have no matching warehouse flatpack THEN the system SHALL explicitly set them as "planned" — Properties["Built"]=false, Properties["Staged"]=false, BuildCompletionTime=null

2.6 WHEN the system creates staged entries from warehouse matching THEN the system SHALL NOT automatically add new structure entries for warehouse flatpacks that have no corresponding pool entry — flatpacks may be manufactured at the colony without being intended for local construction

2.7 WHEN API structures are matched to pool entries THEN the system SHALL reassign BuildQueueSequence for built/building entries in API completion-date order (1, 2, 3...), followed by staged entries in their prior user-defined relative order, followed by planned entries in their prior user-defined relative order. The Game API order is authoritative for built structures.

2.8 WHEN the warehouse data has not yet been synced for this cycle (Colony.Items is empty or stale) THEN the staged matching SHALL use whatever warehouse data is currently available. If data is incomplete, the next sync cycle will correct the staged assignments.

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the Game API returns structure property data (ResourceId, ResourceIcon, DurabilityCurrent, DurabilityMax, ManufactureAmountPerRun, OpsStatusEffects, Industries, DetailsRequired, BuildingAttributes, ExtraProperties) THEN the system SHALL CONTINUE TO merge those field values onto the matched local structure

3.2 WHEN a matched structure has an empty FlatpackBlueprintUUID and the API provides a BlueprintDesignName THEN the system SHALL CONTINUE TO resolve and assign the FlatpackBlueprintUUID via the flatpack lookup

3.3 WHEN a matched structure has a null BuildCompletionTime and the API provides a ConstructingBuildingFinish THEN the system SHALL CONTINUE TO create a BuildCompletionTime with the correct remaining seconds

3.4 WHEN a matched structure has an empty MiningSurveyResource and the API provides a ResourceName THEN the system SHALL CONTINUE TO assign the MiningSurveyResource

3.5 WHEN the Game API returns structures with BuildingId THEN the system SHALL CONTINUE TO set the local structure's BuildingID for future correlation

3.6 WHEN the Game API returns a structure with no match in the pool (new type never planned) THEN the system SHALL CONTINUE TO create a new structure entry with a BuildQueueSequence at the end of the list

3.7 WHEN matching structures by BuildingID (previously synced) THEN the system SHALL CONTINUE TO use BuildingID as the primary match key before falling back to type-based matching

