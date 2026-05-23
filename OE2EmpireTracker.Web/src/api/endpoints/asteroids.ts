import { apiClient } from '../client';
import type { Asteroid } from '../types/domain';

export type { Asteroid };

/**
 * Typed API module for Asteroid entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const asteroidsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/asteroids`).json<Asteroid[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`).json<Asteroid>(),

  create: (charUUID: string, data: Omit<Asteroid, 'uuid'>) =>
    apiClient.post(`api/v1/characters/${charUUID}/asteroids`, { json: data }).json<Asteroid>(),

  update: (charUUID: string, entityUUID: string, data: Partial<Asteroid>) =>
    apiClient.put(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`, { json: data }).json<Asteroid>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`).json<void>(),
};
