import { apiClient } from '../client';

/**
 * Typed API module for Market Transaction entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const marketTransactionsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-transactions`).json<unknown[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`).json<unknown>(),

  create: (charUUID: string, data: unknown) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-transactions`, { json: data }).json<unknown>(),

  update: (charUUID: string, entityUUID: string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`, { json: data }).json<unknown>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`).json<void>(),
};
