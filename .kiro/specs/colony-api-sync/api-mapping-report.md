# API Mapping Report

Generated: 2026-05-26 23:46:59 UTC

## TypeC Values Discovered

These are the `typeC` codes found in warehouse items. This is the key discovery
for mapping API item types to the local `ItemType` enum.

| TypeC | Count | Example ResourceName |
|-------|-------|---------------------|
| `R` | 398 | Lanthanides (Unrefined, Med Purity) |
| `Sc` | 151 | Survey Report: Alef Wynthoril I (06BA4C5) |
| `W` | 148 | White Collar Detail |
| `S` | 21 | Patrol |
| `Bp` | 16 | Patrol |
| `F` | 4 | Flatpack: Refinery |

## StatusId Values Discovered

These are the `statusId` values found in buildings. Maps to Built/Online/Constructing state.

| StatusId | Count | Example Building |
|----------|-------|-----------------|
| 1 | 3260 | Colony Command Centre |

## ColonyBuildingTypeId Values Discovered

| ColonyBuildingTypeId | Count | Example Building |
|---------------------|-------|-----------------|
| 33 | 74 | Colony Command Centre |
| 34 | 864 | Reactor Core |
| 35 | 491 | Habitation Block |
| 36 | 491 | Hydroponics Bay |
| 39 | 74 | Remote Operations Array |
| 40 | 259 | Mining Rig |
| 41 | 322 | Refinery |
| 43 | 74 | Manufactory |
| 44 | 76 | Warehouse |
| 45 | 74 | Research Laboratory |
| 46 | 447 | Entertainment Centre |
| 51 | 1 | Agridome |
| 52 | 1 | Logistics Centre |
| 53 | 1 | Technology Institute |
| 54 | 1 | Centre of Economics |
| 55 | 1 | Science Centre |
| 56 | 1 | Administration Block |
| 57 | 1 | Off-World Living Institute |
| 58 | 1 | Leisure Industry Centre |
| 59 | 1 | Engineering Block |
| 60 | 1 | Mining Industry Centre |
| 61 | 1 | Manufacturing Industry Centre |
| 62 | 1 | Refining Industry Centre |
| 63 | 1 | Institute of Defence |
| 64 | 1 | Healthcare Institute |

## Colony List Field Comparison

### API Fields (from raw JSON)

| JSON Field | DTO Property | Local Model Property | Notes |
|-----------|-------------|---------------------|-------|
| `atmosVariation` | AtmosVariation | AtmosVariation |  |
| `colonyId` | ColonyId | (no local equivalent — uses UUID) |  |
| `colonyName` | ColonyName | ColonyName |  |
| `colonySize` | ColonySize | ColonySize |  |
| `contentmentIndex` | ContentmentIndex | ContentmentIndex |  |
| `distance` | Distance | Distance |  |
| `hasManufacturing` | HasManufacturing | (activity flag — not stored) |  |
| `hasMining` | HasMining | (activity flag — not stored) |  |
| `hasRefining` | HasRefining | (activity flag — not stored) |  |
| `hasResearch` | HasResearch | (activity flag — not stored) |  |
| `hexValue` | HexValue | HexValue |  |
| `imagePreFix` | ImagePreFix | ImagePreFix |  |
| `manufacturingBlocked` | ManufacturingBlocked | ManufacturingBlocked |  |
| `manufacturingInProgress` | ManufacturingInProgress | (activity flag — not stored) |  |
| `miningInProgress` | MiningInProgress | (activity flag — not stored) |  |
| `refiningInProgress` | RefiningInProgress | (activity flag — not stored) |  |
| `remoteAccess` | RemoteAccess | (used for flow control only) |  |
| `researchInProgress` | ResearchInProgress | (activity flag — not stored) |  |
| `surfaceVariation` | SurfaceVariation | SurfaceVariation |  |
| `systemId` | SystemId | SystemId |  |
| `systemName` | SystemName | SystemName |  |
| `systemObjectName` | SystemObjectName | PlanetName |  |
| `systemObjectTypeName` | SystemObjectTypeName | SystemObjectTypeName |  |
| `workerCurrentAttitude` | WorkerCurrentAttitude | WorkerCurrentAttitude |  |

## Buildings Field Comparison

### API Fields (from raw JSON)

| JSON Field | DTO Property | Local Model Property | Notes |
|-----------|-------------|---------------------|-------|
| `blueprintDesignName` | BlueprintDesignName | FlatpackBlueprintUUID (via name lookup) |  |
| `buildingAttributes` | BuildingAttributes | buildingAttributes |  |
| `buildingId` | BuildingId | buildingId |  |
| `buildingOnline` | BuildingOnline | Properties[Online] |  |
| `colonyBuildingTypeId` | ColonyBuildingTypeId | colonyBuildingTypeId |  |
| `constructingBuildingFinish` | ConstructingBuildingFinish | BuildCompletionTime |  |
| `detailsRequired` | DetailsRequired | detailsRequired |  |
| `durabilityCurrent` | DurabilityCurrent | durabilityCurrent |  |
| `durabilityMax` | DurabilityMax | durabilityMax |  |
| `extraProperties` | ExtraProperties | extraProperties |  |
| `industries` | Industries | industries |  |
| `manufactureAmountPerRun` | ManufactureAmountPerRun | manufactureAmountPerRun |  |
| `manufactureNumber` | ManufactureNumber | ManufacturingQuantity |  |
| `maxRate` | MaxRate | (not stored locally) |  |
| `nextFinish` | NextFinish | ProcessCompletionTime |  |
| `opsStatusEffects` | OpsStatusEffects | opsStatusEffects |  |
| `resourceIcon` | ResourceIcon | resourceIcon |  |
| `resourceId` | ResourceId | resourceId |  |
| `resourceName` | ResourceName | MiningSurveyResource |  |
| `statusId` | StatusId | Properties[Built] (derived) |  |
| `supportDetailsRequired` | SupportDetailsRequired | supportDetailsRequired |  |

### Local-Only Fields (ColonyStructure properties not in API)

- AssignedWorkers
- BuildCompletionTime
- BuildQueueSequence
- ContentmentIndex
- CurrentAttitude
- DisplaySequence
- FlatpackBlueprintUUID
- IsBuiltAndOnline
- ManufacturingBlueprintUUID
- ManufacturingCommodityName
- ManufacturingCompleted
- ManufacturingQuantity
- MiningLeftOvers
- MiningSurvey
- MiningSurveyResource
- ProcessCompletionTime
- Properties
- RefiningResource
- RefiningResourcePurity
- ResearchingBlueprintUUID
- StagingResources
- StatusDelta
- Statuses
- UUID
- WageLevel

## Warehouse Field Comparison

### API Fields (from raw JSON)

| JSON Field | DTO Property | Local Model Property | Notes |
|-----------|-------------|---------------------|-------|
| `amount` | Amount | Quantity |  |
| `evolution` | Evolution | evolution |  |
| `healthPercentage` | HealthPercentage | healthPercentage |  |
| `icon` | Icon | (not stored locally) |  |
| `Id` | Id | GameItemId |  |
| `jobDeliveryLoc` | JobDeliveryLoc | jobDeliveryLoc |  |
| `jobName` | JobName | jobName |  |
| `jobRef` | JobRef | jobRef |  |
| `jobTrack` | JobTrack | jobTrack |  |
| `lastRepairHealthPercentage` | LastRepairHealthPercentage | lastRepairHealthPercentage |  |
| `mass` | Mass | mass |  |
| `properties` | Properties | ItemProperties |  |
| `resourceName` | ResourceName | Name |  |
| `shipPartType` | ShipPartType | shipPartType |  |
| `typeC` | TypeC | ItemType (mapped) |  |
| `typeId` | TypeId | BaseItemTypeID |  |
| `volume` | Volume | volume |  |

### Local-Only Fields (Item properties not in API)

- BaseItemTypeID
- Contents
- CurrentHP
- Description
- ExtendedName
- MaxHP
- MaxRepairPercent
- NickName
- ResourcePurity
- UUID

