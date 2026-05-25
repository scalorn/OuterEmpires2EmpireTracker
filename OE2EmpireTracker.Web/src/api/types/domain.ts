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
  displaySequence: number;
  buildingID: number;
  buildQueueSequence: number;
  properties: Record<string, string>;
  assignedWorkers: Record<string, string>;
  buildCompletionTime?: CountDownTime | null;
  processCompletionTime?: CountDownTime | null;
  miningSurvey?: string | null;
  miningSurveyResource?: string | null;
  miningLeftOvers: number;
  refiningResource?: string | null;
  refiningResourcePurity?: string | null;
  researchingBlueprintUUID?: string | null;
  manufacturingBlueprintUUID?: string | null;
  manufacturingCommodityName?: string | null;
  manufacturingQuantity: number;
  manufacturingCompleted: number;
  stagingResources: boolean;
  currentAttitude: string;
  contentmentIndex: number;
  wageLevel: number;
  // UI-computed fields (derived from properties/blueprint lookup, not on wire)
  blueprintType?: string;
  status?: string;
  processingEndUtc?: string;
}

export interface CountDownTime {
  startTime: string;
  repeatIntervalSeconds: number;
  isRepeating: boolean;
  timeRemaining: number;
  intervalsPassed: number;
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
  bluePrintType: string;
  shipClass?: string;
  techLevel: string;
  evolution: number;
  nickName?: string;
  ownerUUID: string;
  class: number;
  copyCost: number;
  baseBlueprintUUID?: string;
  properties: Record<string, string>;
  resources: Record<string, string>;
}

// =============================================================================
// Survey domain
// =============================================================================

export interface Survey {
  uuid: string;
  planetName: string;
  systemName: string;
  surveyType: 'planet' | 'asteroid';
  nickName?: string;
  scannedBy?: string;
  dateTime?: string;
  scannerBlueprintUUID?: string;
  asteroidUUID?: string;
  properties?: Record<string, string>;
  resources: Record<string, SurveyResource>;
}

export interface SurveyResource {
  resource: string;
  purity: string;
  amount: string;
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

// =============================================================================
// Delivery domain
// =============================================================================

export interface DeliveryRoute {
  uuid: string;
  name: string;
  stops: RouteStop[];
}

export interface RouteStop {
  uuid: string;
  colonyUUID: string;
  colonyName: string;
  planetName: string;
  systemName: string;
  sequence: number;
}

export interface DeliveryPlan {
  uuid: string;
  routeUUID: string;
  name: string;
  isCompleted: boolean;
  stopItems: Record<string, StopItemSet>;
}

export interface StopItemSet {
  dropOff: DeliveryItem[];
  pickUp: DeliveryItem[];
}

export interface DeliveryItem {
  uuid: string;
  itemType: string;
  name: string;
  purity?: string;
  quantity: number;
  isChecked: boolean;
}

// =============================================================================
// Ship Template domain
// =============================================================================

export interface ShipTemplate {
  uuid: string;
  name: string;
  hullShipClass: string;
  slots: TemplateSlot[];
}

export interface TemplateSlot {
  slotType: string;
  slotIndex: number;
  blueprintUUID?: string;
}

// =============================================================================
// Market domain
// =============================================================================

export interface MarketListing {
  uuid: string;
  stationName: string;
  itemName: string;
  quantity: number;
  price: number;
  condition?: number;
  maxRepair?: number;
}

export interface MarketTransaction {
  uuid: string;
  ownerUUID: string;
  transactionType: 'buy' | 'sell';
  itemType: string;
  itemReferenceID: string;
  itemName: string;
  quantity: number;
  pricePerUnit: number;
  totalPrice: number;
  counterparty: string;
  counterpartyFaction: string;
  stationUUID: string;
  timestamp: string;
  notes: string;
  listingUUID: string;
  currentHP: number;
  maxHP: number;
  maxRepairPercent: number;
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
  reserves: AsteroidReserve[];
}

export interface AsteroidReserve {
  resourceName: string;
  purity: string;
  maxReserve: number;
  currentReserve?: number;
  resetTimestamp?: string;
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
