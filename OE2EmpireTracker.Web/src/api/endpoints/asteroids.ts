import { apiClient } from '../client';

/**
 * Typed API module for Asteroid entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const asteroidsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/asteroids`).json<unknown[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`).json<unknown>(),

  create: (charUUID: string, data: unknown) =>
    apiClient.post(`api/v1/characters/${charUUID}/asteroids`, { json: data }).json<unknown>(),

  update: (charUUID: string, entityUUID: string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`, { json: data }).json<unknown>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/asteroids/${entityUUID}`).json<void>(),
};
