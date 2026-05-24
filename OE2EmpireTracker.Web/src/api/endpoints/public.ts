import { apiClient } from '../client';

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StarSystem {
  id: number;
  name: string;
  x: number;
  y: number;
  quadrant: string;
  sector: string;
  region: string;
  locality: string;
  spectralClass: string;
  factionId: number;
  factionName: string;
  factionColor: string;
  hasOrbital: boolean;
  hasSpaceport: boolean;
  hasStarbase: boolean;
}

export interface Planet {
  id: number;
  name: string;
  systemId: number;
}

export interface AsteroidSummary {
  uuid: string;
  name: string;
  systemId: number;
}

export interface ColonySummary {
  colonyName: string;
  size: number;
  planetName: string;
}

export interface BlueprintFilters {
  type?: string;
  techLevel?: string;
  shipClass?: string;
  evolution?: string;
  search?: string;
}

export interface SurveyFilters {
  system?: string;
  resourceType?: string;
  purityLevel?: string;
  surveyType?: string;
  minAmount?: string;
  search?: string;
}

export const publicApi = {
  getPublicBlueprints: (filters?: BlueprintFilters, page = 1, pageSize = 20) =>
    apiClient.get('api/v1/public/blueprints', {
      searchParams: {
        page,
        pageSize,
        ...(filters?.type && { type: filters.type }),
        ...(filters?.techLevel && { techLevel: filters.techLevel }),
        ...(filters?.shipClass && { shipClass: filters.shipClass }),
        ...(filters?.evolution && { evolution: filters.evolution }),
        ...(filters?.search && { search: filters.search }),
      },
    }).json<PaginatedResponse<unknown>>(),

  getPublicSurveys: (filters?: SurveyFilters, page = 1, pageSize = 20) =>
    apiClient.get('api/v1/public/surveys', {
      searchParams: {
        page,
        pageSize,
        ...(filters?.system && { system: filters.system }),
        ...(filters?.resourceType && { resourceType: filters.resourceType }),
        ...(filters?.purityLevel && { purityLevel: filters.purityLevel }),
        ...(filters?.search && { search: filters.search }),
      },
    }).json<PaginatedResponse<unknown>>(),

  /** Fetch global/baseline data by type (e.g. BlueprintType, ShipClass, TechLevel). */
  getGlobalData: <T = unknown>(dataType: string) =>
    apiClient.get(`api/v1/public/global/${dataType}`).json<T[]>(),

  /** Fetch all star systems in the galaxy. */
  getSystems: () =>
    apiClient.get('api/v1/public/systems').json<StarSystem[]>(),

  /** Fetch all planets in a specific star system. */
  getSystemPlanets: (systemId: number) =>
    apiClient.get(`api/v1/public/systems/${systemId}/planets`).json<Planet[]>(),

  /** Fetch all asteroids in a specific star system. */
  getSystemAsteroids: (systemId: number) =>
    apiClient.get(`api/v1/public/systems/${systemId}/asteroids`).json<AsteroidSummary[]>(),

  /** Fetch colony summaries (name, size, planet) for a specific star system. */
  getSystemColonies: (systemId: number) =>
    apiClient.get(`api/v1/public/systems/${systemId}/colonies`).json<ColonySummary[]>(),

  /** Fetch a single blueprint's full detail by UUID. */
  getBlueprintDetail: (uuid: string) =>
    apiClient.get(`api/v1/public/blueprints/${uuid}`).json<Record<string, unknown>>(),
};
