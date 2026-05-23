import { apiClient } from '../client';
import type { Blueprint } from '../types/domain';

/**
 * Typed API module for Blueprint entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const blueprintsApi = {
  getAll: (charUUID: string, limit?: number, offset?: number) => {
    const searchParams: Record<string, string> = {};
    if (limit !== undefined) searchParams.limit = String(limit);
    if (offset !== undefined) searchParams.offset = String(offset);
    return apiClient
      .get(`api/v1/characters/${charUUID}/blueprints`, { searchParams })
      .json<Blueprint[]>();
  },

  getOne: (charUUID: string, entityUUID: string) =>
    apiClient
      .get(`api/v1/characters/${charUUID}/blueprints/${entityUUID}`)
      .json<Blueprint>(),

  create: (charUUID: string, data: Omit<Blueprint, 'uuid'>) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/blueprints`, { json: data })
      .json<Blueprint>(),

  update: (charUUID: string, entityUUID: string, data: Partial<Blueprint>) =>
    apiClient
      .put(`api/v1/characters/${charUUID}/blueprints/${entityUUID}`, { json: data })
      .json<Blueprint>(),

  remove: (charUUID: string, entityUUID: string) =>
    apiClient
      .delete(`api/v1/characters/${charUUID}/blueprints/${entityUUID}`)
      .json<void>(),
};
