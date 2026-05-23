import { apiClient } from '../client';
import type { PlayerProfile } from '../types/domain';

export type { PlayerProfile };

/**
 * Typed API module for PlayerProfile entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const profilesApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/profiles`).json<PlayerProfile[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/profiles/${entityUUID}`).json<PlayerProfile>(),

  create: (charUUID: string, data: Omit<PlayerProfile, 'uuid'>) =>
    apiClient.post(`api/v1/characters/${charUUID}/profiles`, { json: data }).json<PlayerProfile>(),

  update: (charUUID: string, entityUUID: string, data: Partial<PlayerProfile>) =>
    apiClient.put(`api/v1/characters/${charUUID}/profiles/${entityUUID}`, { json: data }).json<PlayerProfile>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/profiles/${entityUUID}`).json<void>(),
};
