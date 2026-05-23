import { apiClient } from '../client';
import type { Station } from '../types/domain';

export type { Station };

/**
 * Typed API module for Station entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const stationsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stations`).json<Station[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stations/${entityUUID}`).json<Station>(),

  create: (charUUID: string, data: Omit<Station, 'uuid'>) =>
    apiClient.post(`api/v1/characters/${charUUID}/stations`, { json: data }).json<Station>(),

  update: (charUUID: string, entityUUID: string, data: Partial<Station>) =>
    apiClient.put(`api/v1/characters/${charUUID}/stations/${entityUUID}`, { json: data }).json<Station>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/stations/${entityUUID}`).json<void>(),
};
