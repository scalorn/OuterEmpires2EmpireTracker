import { apiClient } from '../client';

/**
 * Typed API module for Pricing Plan entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const pricingPlansApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/pricing-plans`).json<unknown[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`).json<unknown>(),

  create: (charUUID: string, data: unknown) =>
    apiClient.post(`api/v1/characters/${charUUID}/pricing-plans`, { json: data }).json<unknown>(),

  update: (charUUID: string, entityUUID: string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`, { json: data }).json<unknown>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`).json<void>(),
};
