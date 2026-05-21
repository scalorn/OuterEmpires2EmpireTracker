import type { BlueprintFilters, SurveyFilters } from '../endpoints/public';

export const queryKeys = {
  characters: ['characters'] as const,
  character: (uuid: string) => ['characters', uuid] as const,
  characterData: (uuid: string, dataType: string) =>
    ['characters', uuid, 'data', dataType] as const,
  characterEntity: (uuid: string, dataType: string, entityUUID: string) =>
    ['characters', uuid, 'data', dataType, entityUUID] as const,

  factions: ['factions'] as const,
  faction: (uuid: string) => ['factions', uuid] as const,
  factionMembers: (uuid: string) => ['factions', uuid, 'members'] as const,
  factionShared: (uuid: string, dataType: string) =>
    ['factions', uuid, 'shared', dataType] as const,

  sharing: (charUUID: string) => ['sharing', charUUID] as const,

  publicBlueprints: (filters?: BlueprintFilters) =>
    ['public', 'blueprints', filters] as const,
  publicSurveys: (filters?: SurveyFilters) =>
    ['public', 'surveys', filters] as const,

  colonyPlannerStatus: ['planner', 'status'] as const,
  colonyPlannerBuildOrder: ['planner', 'build-order'] as const,
  colonyPlannerEligibility: ['planner', 'eligibility'] as const,
};
