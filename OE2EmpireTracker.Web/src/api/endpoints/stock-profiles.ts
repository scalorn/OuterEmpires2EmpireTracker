import { apiClient } from '../client';
import type { StockProfile, StockTargetItem } from '../types/domain';

/**
 * Typed API module for Stock Profile entities.
 * Provides CRUD operations for stock profiles and their target items.
 */

export interface StockProfileCreateRequest {
  name: string;
  assignedColonyUUID?: string;
  items?: Omit<StockTargetItem, 'uuid' | 'currentQuantity'>[];
}

export interface StockProfileUpdateRequest {
  name?: string;
  assignedColonyUUID?: string;
  items?: Omit<StockTargetItem, 'uuid' | 'currentQuantity'>[];
}

export const stockProfilesApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stock-profiles`).json<StockProfile[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/stock-profiles/${entityUUID}`).json<StockProfile>(),

  create: (charUUID: string, data: StockProfileCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/stock-profiles`, { json: data }).json<StockProfile>(),

  update: (charUUID: string, entityUUID: string, data: StockProfileUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/stock-profiles/${entityUUID}`, { json: data }).json<StockProfile>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/stock-profiles/${entityUUID}`).json<void>(),
};
