import { apiClient } from '../client';
import type { StockProfile, StockTargetItem } from '../types/domain';

/**
 * Typed API module for Stock Plan entities.
 * Stock plans represent the target inventory levels (stock targets) within a profile.
 * Provides CRUD operations for managing stock plan items.
 */

export interface StockPlanCreateRequest {
  name: string;
  assignedColonyUUID?: string;
  items?: Omit<StockTargetItem, 'uuid' | 'currentQuantity'>[];
}

export interface StockPlanUpdateRequest {
  name?: string;
  assignedColonyUUID?: string;
  items?: Omit<StockTargetItem, 'uuid' | 'currentQuantity'>[];
}

export const stockPlansApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stock-plans`).json<StockProfile[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stock-plans/${entityUUID}`).json<StockProfile>(),

  create: (charUUID: string, data: StockPlanCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/stock-plans`, { json: data }).json<StockProfile>(),

  update: (charUUID: string, entityUUID: string, data: StockPlanUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/stock-plans/${entityUUID}`, { json: data }).json<StockProfile>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/stock-plans/${entityUUID}`).json<void>(),
};
