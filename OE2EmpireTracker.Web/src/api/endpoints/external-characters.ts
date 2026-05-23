import { apiClient } from '../client';
import type { ExternalCharacter } from '../types/domain';

export type { ExternalCharacter };

/**
 * Typed API module for External Character (Contacts) entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const externalCharactersApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/external-characters`).json<ExternalCharacter[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/external-characters/${entityUUID}`).json<ExternalCharacter>(),

  create: (charUUID: string, data: Omit<ExternalCharacter, 'uuid'>) =>
    apiClient.post(`api/v1/characters/${charUUID}/external-characters`, { json: data }).json<ExternalCharacter>(),

  update: (charUUID: string, entityUUID: string, data: Partial<ExternalCharacter>) =>
    apiClient.put(`api/v1/characters/${charUUID}/external-characters/${entityUUID}`, { json: data }).json<ExternalCharacter>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/external-characters/${entityUUID}`).json<void>(),
};
