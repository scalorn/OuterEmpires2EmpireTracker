// This file is auto-generated. Do not edit manually.
// Regenerate with: npm run generate-types
// Generated from: Models.cs, PermissionModels.cs
// Generated at: 2026-05-25T21:21:10.627Z

export type TokenRole = 'Owner' | 'FactionLeader' | 'Character';

export type MembershipActionType = 'JoinRequest' | 'Invitation';

export type SharingTargetType = 'Faction' | 'Character' | 'Public';

export type GranteeType = 'Character' | 'Faction';

export type PermissionActionType = 'CapabilityGranted' | 'CapabilityRevoked' | 'GroupAssigned' | 'GroupRemoved' | 'ClearanceChanged';

export type DataType = 'Blueprints' | 'Colonies' | 'Surveys' | 'DeliveryRoutes' | 'DeliveryPlans' | 'BuildPlans' | 'Ships' | 'ShipTemplates' | 'Stations' | 'Asteroids' | 'MarketListings' | 'MarketTransactions' | 'PricingPlans' | 'StockPlans' | 'StockProfiles' | 'SupplyChains';

export interface RateLimitConfig {
  requestsPerMinute: number;
}

export interface ApiToken {
  id: string;
  tokenHash: string;
  characterUUID: string | null;
  role: TokenRole;
  factionUUID: string | null;
  createdUtc: string;
  lastUsedUtc: string | null;
  isRevoked: boolean;
  rateLimits: RateLimitConfig;
}

export interface MembershipAction {
  id: string;
  factionUUID: string;
  characterUUID: string;
  type: MembershipActionType;
  createdUtc: string;
  expiresUtc: string;
}

export interface SharingRule {
  id: string;
  ownerCharacterUUID: string;
  targetUUID: string;
  targetType: SharingTargetType;
  dataType: string | null;
  entityUUID: string | null;
}

export interface CharacterPreferences {
  characterUUID: string;
  serverProcessing: boolean;
}

export interface EntityMetadata {
  lastModifiedUtc: string;
  modifiedByTokenId: string | null;
}

export interface ServerFaction {
  uuid: string;
  name: string;
  description: string;
  leaderCharacterUUIDs: string[];
  metadata: EntityMetadata;
}

export interface ServerCharacter {
  uuid: string;
  name: string;
  factionUUID: string | null;
  metadata: EntityMetadata;
}

export interface ColonySummary {
  colonyName: string;
  size: number;
  planetName: string;
}

export interface AsteroidSummary {
  uuid: string;
  name: string;
  systemId: number;
}

export interface BlueprintSummary {
  uuid: string;
  name: string;
  nickName: string;
  bluePrintType: string;
  techLevel: string;
  evolution: number;
  class: number;
  description: string;
}

export interface FactionCapability {
  uuid: string;
  factionUUID: string;
  name: string;
  description: string;
}

export interface FactionClearanceLevel {
  uuid: string;
  factionUUID: string;
  level: number;
  name: string;
  description: string;
}

export interface FactionPermissionGroup {
  uuid: string;
  factionUUID: string;
  name: string;
  description: string;
  defaultClearanceLevelUUID: string;
}

export interface FactionGroupCapability {
  groupUUID: string;
  capabilityUUID: string;
}

export interface FactionGroupSharingRule {
  uuid: string;
  groupUUID: string;
  dataType: string | null;
  entityUUID: string | null;
  minClearanceLevelUUID: string;
}

export interface FactionMemberPermissions {
  characterUUID: string;
  factionUUID: string;
  groupUUID: string | null;
  clearanceLevelUUID: string;
}

export interface FactionMemberCapability {
  characterUUID: string;
  factionUUID: string;
  capabilityUUID: string;
}

export interface CharacterCapability {
  uuid: string;
  ownerCharacterUUID: string;
  name: string;
  description: string;
}

export interface CharacterClearanceLevel {
  uuid: string;
  ownerCharacterUUID: string;
  level: number;
  name: string;
  description: string;
}

export interface CharacterPermissionGroup {
  uuid: string;
  ownerCharacterUUID: string;
  name: string;
  description: string;
  defaultClearanceLevelUUID: string;
}

export interface CharacterGroupCapability {
  groupUUID: string;
  capabilityUUID: string;
}

export interface CharacterGroupSharingRule {
  uuid: string;
  groupUUID: string;
  dataType: string | null;
  entityUUID: string | null;
}

export interface CharacterGranteePermissions {
  ownerCharacterUUID: string;
  granteeType: GranteeType;
  granteeUUID: string;
  groupUUID: string | null;
  clearanceLevelUUID: string | null;
}

export interface CharacterGranteeCapability {
  ownerCharacterUUID: string;
  granteeType: GranteeType;
  granteeUUID: string;
  capabilityUUID: string;
}

export interface IntelComment {
  uuid: string;
  targetCharacterUUID: string;
  submitterCharacterUUID: string;
  text: string;
  createdUtc: string;
}

export interface IntelCommentFactionShare {
  uuid: string;
  intelCommentUUID: string;
  factionUUID: string;
  classificationLevelUUID: string | null;
  classifiedByCharacterUUID: string | null;
  sharedUtc: string;
  classifiedUtc: string | null;
}

export interface PermissionAuditEntry {
  uuid: string;
  timestamp: string;
  actorCharacterUUID: string;
  targetCharacterUUID: string;
  actionType: PermissionActionType;
  oldValue: string;
  newValue: string;
}

// ============================================================
// Supplemental types (endpoint-local DTOs not in model files)
// ============================================================

export interface ColonyPlannerRequest {
  structures: PlannerStructure[];
  items?: Record<string, number>;
  playerSkills?: Record<string, number>;
}

export interface PlannerStructure {
  flatpackBlueprintUUID: string;
  isBuilt: boolean;
  isStaged: boolean;
  isOnline: boolean;
  buildQueueSequence: number;
  assignedWorkers?: Record<string, boolean>;
}

export interface ColonyStatusResult {
  powerProvided: number;
  powerRequired: number;
  habitationProvision: number;
  habitationRequired: number;
  foodProvision: number;
  foodRequired: number;
  entertainmentProvided: number;
  entertainmentRequired: number;
  warehouseCapacity: number;
  warehouseRequired: number;
}

export interface OptimizedOrderEntry {
  flatpackBlueprintUUID: string;
  buildQueueSequence: number;
}

export interface BuildOrderResult {
  optimizedOrder?: OptimizedOrderEntry[];
  steps: BuildOrderStep[];
  totalTimeEstimate: string;
}

export interface BuildOrderStep {
  sequence: number;
  structureName: string;
  blueprintType: string;
  resourcesRequired: ResourceRequirement[];
  timeEstimate: string;
}

export interface ResourceRequirement {
  resourceName: string;
  quantity: number;
}

export interface EligibilityResult {
  eligible: boolean;
  stagedCount: number;
  buildingCount: number;
  firstStagedStructure: PlannerStructure | null;
}
