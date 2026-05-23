import { apiClient } from '../client';

/**
 * Response shape from GET /shared-with-me/{dataType}.
 * Each group represents entities shared by a single owner character.
 */
export interface SharedWithMeGroup {
  ownerCharacterUUID: string;
  ownerCharacterName: string;
  entities: unknown[];
}

/**
 * Typed API module for Shared Data (read-only).
 * Provides access to data shared by other faction members via the
 * /shared-with-me/{dataType} endpoint.
 */
export const sharedDataApi = {
  /** Get data shared with the current character for a specific data type. */
  getSharedWithMe: (charUUID: string, dataType: string) =>
    apiClient
      .get(`api/v1/characters/${charUUID}/shared-with-me/${dataType}`)
      .json<SharedWithMeGroup[]>(),
};
