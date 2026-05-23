import { apiClient } from '../client';
import type { PricingPlan } from '../types/domain';

/**
 * Typed API module for Pricing Plan entities.
 * Provides CRUD operations for managing pricing plans with per-item prices.
 */
export const pricingPlansApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/pricing-plans`).json<PricingPlan[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`).json<PricingPlan>(),

  create: (charUUID: string, data: Partial<PricingPlan>) =>
    apiClient.post(`api/v1/characters/${charUUID}/pricing-plans`, { json: data }).json<PricingPlan>(),

  update: (charUUID: string, entityUUID: string, data: Partial<PricingPlan>) =>
    apiClient.put(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`, { json: data }).json<PricingPlan>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/pricing-plans/${entityUUID}`).json<void>(),
};
