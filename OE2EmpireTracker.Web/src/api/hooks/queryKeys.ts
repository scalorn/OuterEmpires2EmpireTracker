import type { BlueprintFilters, SurveyFilters } from '../endpoints/public';

export const queryKeys = {
  // Characters
  characters: ['characters'] as const,
  character: (uuid: string) => ['characters', uuid] as const,
  characterData: (uuid: string, dataType: string) =>
    ['characters', uuid, 'data', dataType] as const,
  characterEntity: (uuid: string, dataType: string, entityUUID: string) =>
    ['characters', uuid, 'data', dataType, entityUUID] as const,

  // Factions
  factions: ['factions'] as const,
  faction: (uuid: string) => ['factions', uuid] as const,
  factionMembers: (uuid: string) => ['factions', uuid, 'members'] as const,
  factionShared: (uuid: string, dataType: string) =>
    ['factions', uuid, 'shared', dataType] as const,

  // Sharing
  sharing: (charUUID: string) => ['sharing', charUUID] as const,

  // Public
  publicBlueprints: (filters?: BlueprintFilters) =>
    ['public', 'blueprints', filters] as const,
  publicSurveys: (filters?: SurveyFilters) =>
    ['public', 'surveys', filters] as const,

  // Colony Planner
  colonyPlannerStatus: ['planner', 'status'] as const,
  colonyPlannerBuildOrder: ['planner', 'build-order'] as const,
  colonyPlannerEligibility: ['planner', 'eligibility'] as const,

  // Colonies
  colonies: (charUUID: string) =>
    ['characters', charUUID, 'colonies'] as const,
  colonyDetail: (charUUID: string, colonyUUID: string) =>
    ['characters', charUUID, 'colonies', colonyUUID] as const,

  // Blueprints
  blueprints: (charUUID: string) =>
    ['characters', charUUID, 'blueprints'] as const,
  blueprintDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'blueprints', entityUUID] as const,

  // Surveys
  surveys: (charUUID: string) =>
    ['characters', charUUID, 'surveys'] as const,
  surveyDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'surveys', entityUUID] as const,

  // Profiles
  profiles: (charUUID: string) =>
    ['characters', charUUID, 'profiles'] as const,
  profileDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'profiles', entityUUID] as const,

  // Delivery Routes
  deliveryRoutes: (charUUID: string) =>
    ['characters', charUUID, 'delivery-routes'] as const,
  deliveryRouteDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'delivery-routes', entityUUID] as const,

  // Delivery Plans
  deliveryPlans: (charUUID: string) =>
    ['characters', charUUID, 'delivery-plans'] as const,
  deliveryPlanDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'delivery-plans', entityUUID] as const,

  // Ship Templates
  shipTemplates: (charUUID: string) =>
    ['characters', charUUID, 'ship-templates'] as const,
  shipTemplateDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'ship-templates', entityUUID] as const,

  // Market Listings
  marketListings: (charUUID: string) =>
    ['characters', charUUID, 'market-listings'] as const,
  marketListingDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'market-listings', entityUUID] as const,

  // Market Transactions
  marketTransactions: (charUUID: string) =>
    ['characters', charUUID, 'market-transactions'] as const,
  marketTransactionDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'market-transactions', entityUUID] as const,

  // Build Plans
  buildPlans: (charUUID: string) =>
    ['characters', charUUID, 'build-plans'] as const,
  buildPlanDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'build-plans', entityUUID] as const,

  // Supply Chains
  supplyChains: (charUUID: string) =>
    ['characters', charUUID, 'supply-chains'] as const,
  supplyChainDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'supply-chains', entityUUID] as const,

  // Stock Profiles
  stockProfiles: (charUUID: string) =>
    ['characters', charUUID, 'stock-profiles'] as const,
  stockProfileDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'stock-profiles', entityUUID] as const,

  // Stock Plans
  stockPlans: (charUUID: string) =>
    ['characters', charUUID, 'stock-plans'] as const,
  stockPlanDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'stock-plans', entityUUID] as const,

  // Pricing Plans
  pricingPlans: (charUUID: string) =>
    ['characters', charUUID, 'pricing-plans'] as const,
  pricingPlanDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'pricing-plans', entityUUID] as const,

  // Shared Data
  sharedData: (charUUID: string) =>
    ['characters', charUUID, 'shared-data'] as const,
  sharedDataDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'shared-data', entityUUID] as const,

  // Contacts (External Characters)
  contacts: (charUUID: string) =>
    ['characters', charUUID, 'external-characters'] as const,
  contactDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'external-characters', entityUUID] as const,

  // Stations
  stations: (charUUID: string) =>
    ['characters', charUUID, 'stations'] as const,
  stationDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'stations', entityUUID] as const,

  // Asteroids
  asteroids: (charUUID: string) =>
    ['characters', charUUID, 'asteroids'] as const,
  asteroidDetail: (charUUID: string, entityUUID: string) =>
    ['characters', charUUID, 'asteroids', entityUUID] as const,

  // Baseline (global reference data)
  baseline: ['baseline'] as const,

  // Timers
  timers: ['timers'] as const,
};
