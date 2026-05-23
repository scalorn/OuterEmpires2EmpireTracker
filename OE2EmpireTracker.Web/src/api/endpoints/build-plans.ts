import { apiClient } from '../client';
import type { BuildPlan, BuildPlanItem } from '../types/domain';

export type { BuildPlan, BuildPlanItem };

export interface BuildPlanCreateRequest {
  name: string;
  items?: Omit<BuildPlanItem, 'uuid'>[];
}

export interface BuildPlanUpdateRequest {
  name?: string;
  items?: Omit<BuildPlanItem, 'uuid'>[];
}

/**
 * Typed API module for Build Plan entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const buildPlansApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/build-plans`).json<BuildPlan[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/build-plans/${entityUUID}`).json<BuildPlan>(),

  create: (charUUID: string, data: BuildPlanCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/build-plans`, { json: data }).json<BuildPlan>(),

  update: (charUUID: string, entityUUID: string, data: BuildPlanUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/build-plans/${entityUUID}`, { json: data }).json<BuildPlan>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/build-plans/${entityUUID}`).json<void>(),
};
