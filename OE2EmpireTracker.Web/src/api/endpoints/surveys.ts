import { apiClient } from '../client';
import type { Survey } from '../types/domain';

/**
 * Typed API module for Survey entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const surveysApi = {
  getAll: (charUUID: string, limit?: number, offset?: number) => {
    const searchParams: Record<string, string> = {};
    if (limit !== undefined) searchParams.limit = String(limit);
    if (offset !== undefined) searchParams.offset = String(offset);
    return apiClient
      .get(`api/v1/characters/${charUUID}/surveys`, { searchParams })
      .json<Survey[]>();
  },

  getOne: (charUUID: string, entityUUID: string) =>
    apiClient
      .get(`api/v1/characters/${charUUID}/surveys/${entityUUID}`)
      .json<Survey>(),

  create: (charUUID: string, data: Omit<Survey, 'uuid'>) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/surveys`, { json: data })
      .json<Survey>(),

  update: (charUUID: string, entityUUID: string, data: Partial<Survey>) =>
    apiClient
      .put(`api/v1/characters/${charUUID}/surveys/${entityUUID}`, { json: data })
      .json<Survey>(),

  remove: (charUUID: string, entityUUID: string) =>
    apiClient
      .delete(`api/v1/characters/${charUUID}/surveys/${entityUUID}`)
      .json<void>(),
};
