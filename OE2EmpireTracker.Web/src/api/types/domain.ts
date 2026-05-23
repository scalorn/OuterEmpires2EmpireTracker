// Domain type interfaces for the web UI.
// These mirror the server-side Common_Library models and provide
// full domain model coverage for React components.

// =============================================================================
// Colony domain
// =============================================================================

export interface Colony {
  uuid: string;
  colonyName: string;
  planetName: string;
  systemName: string;
  structures: ColonyStructure[];
  items: WarehouseItem[];
  commodityRequests: CommodityRequest[];
  lastImportUtc?: string;
}

export interface ColonyStructure {
  uuid: string;
  flatpackBlueprintUUID: string;
  blueprintType: string;
  status: 'staged' | 'building' | 'built' | 'online';
  buildQueueSequence: number;
  assignedWorkers: Record<string, boolean>;
  processingEndUtc?: string;
}

export interface WarehouseItem {
  uuid: string;
  name: string;
  itemType: string;
  purity?: string;
  quantity: number;
}

export interface CommodityRequest {
  commodityName: string;
  quantity: number;
  needByDate?: string;
  isFulfilled: boolean;
}

// =============================================================================
// Blueprint domain
// =============================================================================

export interface Blueprint {
  uuid: string;
  name: string;
  blueprintType: string;
  shipClass?: string;
  techLevel: number;
  evolution: number;
  nickName?: string;
  isGlobal: boolean;
  properties: Record<string, number>;
  resources: BlueprintResource[];
}

export interface BlueprintResource {
  resourceName: string;
  quantity: number;
  purity?: string;
}

// =============================================================================
// Survey domain
// =============================================================================

export interface Survey {
  uuid: string;
  planetName: string;
  systemName: string;
  surveyType: 'Planet' | 'Asteroid';
  nickName?: string;
  scannedBy?: string;
  scanDate?: string;
  sensorAbundance?: number;
  purityModifier?: number;
  scanLevel?: number;
  scannerBlueprint?: string;
  resources: SurveyResource[];
  assignedRigCount?: number;
}

export interface SurveyResource {
  resourceName: string;
  purity: string;
  amount: number;
  maxReserve?: number;
}

// =============================================================================
// Player Profile domain
// =============================================================================

export interface PlayerProfile {
  uuid: string;
  name: string;
  faction?: string;
  totalCredits: number;
  skillPoints: number;
  ranks: ProfileRanks;
  skillGroups: SkillGroup[];
}

export interface ProfileRanks {
  public: RankInfo;
  private: RankInfo;
  military: RankInfo;
}

export interface RankInfo {
  level: number;
  currentXP: number;
  xpToNext: number;
}

export interface SkillGroup {
  name: string;
  enabled: boolean;
  skills: Skill[];
}

export interface Skill {
  name: string;
  level: number;
  isTraining: boolean;
}


// ============================================================
// Shared Data domain
// ============================================================

export interface SharedDataSummary {
  characterUUID: string;
  characterName: string;
  faction: string;
  sharedTypes: ('blueprints' | 'surveys' | 'colonies')[];
}

// ============================================================
// Supporting entities
// ============================================================

export interface ExternalCharacter {
  uuid: string;
  name: string;
  faction?: string;
  notes?: string;
}

export interface Station {
  uuid: string;
  name: string;
  systemName: string;
  stationType?: string;
}

export interface Asteroid {
  uuid: string;
  name: string;
  systemName: string;
  linkedSurveyUUID?: string;
}

// =============================================================================
// Baseline data (reference data from server)
// =============================================================================

export interface BaselineData {
  blueprintTypes: string[];
  shipClasses: ShipClassDef[];
  techLevels: TechLevelDef[];
  commodities: CommodityDef[];
  resources: string[];
  purities: string[];
}

export interface ShipClassDef {
  name: string;
  slots: SlotDefinition[];
}

export interface SlotDefinition {
  slotType: string;
  count: number;
}

export interface TechLevelDef {
  level: number;
  name: string;
}

export interface CommodityDef {
  name: string;
  category: string;
}


// ============================================================
// Baseline data (reference data from server)
// ============================================================

export interface BaselineData {
  blueprintTypes: string[];
  shipClasses: ShipClassDef[];
  techLevels: TechLevelDef[];
  commodities: CommodityDef[];
  resources: string[];
  purities: string[];
}

export interface ShipClassDef {
  name: string;
  slots: SlotDefinition[];
}

export interface SlotDefinition {
  slotType: string;
  count: number;
}

export interface TechLevelDef {
  level: number;
  name: string;
}

export interface CommodityDef {
  name: string;
  category: string;
}

// =============================================================================
// Build Plan domain
// =============================================================================

export interface BuildPlan {
  uuid: string;
  name: string;
  items: BuildPlanItem[];
}

export interface BuildPlanItem {
  uuid: string;
  blueprintUUID: string;
  blueprintName: string;
  quantity: number;
  status: 'pending' | 'allocated' | 'complete';
  assignedColonyUUID?: string;
}

// =============================================================================
// Supply Chain domain
// =============================================================================

export interface SupplyChain {
  uuid: string;
  name: string;
  sourceColonyUUID: string;
  sourceColonyName: string;
  destinationColonyUUID: string;
  destinationColonyName: string;
  steps: SupplyChainStep[];
}

export interface SupplyChainStep {
  uuid: string;
  resourceOrCommodity: string;
  quantity: number;
  processingType: string;
  sequence: number;
}

// =============================================================================
// Stock Target domain
// =============================================================================

export interface StockProfile {
  uuid: string;
  name: string;
  assignedColonyUUID?: string;
  assignedColonyName?: string;
  items: StockTargetItem[];
}

export interface StockTargetItem {
  uuid: string;
  itemType: string;
  name: string;
  purity?: string;
  targetQuantity: number;
  currentQuantity?: number;
}

// =============================================================================
// Pricing Plan domain
// =============================================================================

export interface PricingPlan {
  uuid: string;
  name: string;
  items: PricingPlanItem[];
}

export interface PricingPlanItem {
  uuid: string;
  itemName: string;
  itemType: string;
  unitPrice: number;
}

// =============================================================================
// Shared Data domain
// =============================================================================

export interface SharedDataSummary {
  characterUUID: string;
  characterName: string;
  faction: string;
  sharedTypes: ('blueprints' | 'surveys' | 'colonies')[];
}

// =============================================================================
// Supporting entities
// =============================================================================

export interface ExternalCharacter {
  uuid: string;
  name: string;
  faction?: string;
  notes?: string;
}

export interface Station {
  uuid: string;
  name: string;
  systemName: string;
  stationType?: string;
}

export interface Asteroid {
  uuid: string;
  name: string;
  systemName: string;
  linkedSurveyUUID?: string;
}

// =============================================================================
// Baseline data (reference data from server)
// =============================================================================

export interface BaselineData {
  blueprintTypes: string[];
  shipClasses: ShipClassDef[];
  techLevels: TechLevelDef[];
  commodities: CommodityDef[];
  resources: string[];
  purities: string[];
}

export interface ShipClassDef {
  name: string;
  slots: SlotDefinition[];
}

export interface SlotDefinition {
  slotType: string;
  count: number;
}

export interface TechLevelDef {
  level: number;
  name: string;
}

export interface CommodityDef {
  name: string;
  category: string;
}
