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
