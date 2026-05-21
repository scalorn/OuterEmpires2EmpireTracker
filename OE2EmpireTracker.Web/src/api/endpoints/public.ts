import { apiClient } from '../client';

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface BlueprintFilters {
  type?: string;
  techLevel?: string;
  shipClass?: string;
  search?: string;
}

export interface SurveyFilters {
  system?: string;
  resourceType?: string;
  purityLevel?: string;
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
};
