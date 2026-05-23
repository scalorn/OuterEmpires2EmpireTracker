import { apiClient } from '../client';
import type { SharedDataSummary } from '../types/domain';

/**
 * Typed API module for Shared Data (read-only).
 * Provides access to data shared by other faction members.
 */
export const sharedDataApi = {
  /** List characters who have shared data with the current player. */
  getSharedCharacters: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/shared-data`).json<SharedDataSummary[]>(),

  /** Get shared data detail for a specific sharer and data type. */
  getSharedData: (charUUID: string, sharerUUID: string, dataType: string) =>
    apiClient
      .get(`api/v1/characters/${charUUID}/shared-data/${sharerUUID}/${dataType}`)
      .json<unknown[]>(),
};
