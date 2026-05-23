import { apiClient } from '../client';
import type { DeliveryPlan, StopItemSet, DeliveryItem } from '../types/domain';

export type { DeliveryPlan, StopItemSet, DeliveryItem };

export interface DeliveryPlanCreateRequest {
  name: string;
  routeUUID: string;
  stopItems?: Record<string, StopItemSet>;
}

export interface DeliveryPlanUpdateRequest {
  name?: string;
  routeUUID?: string;
  isCompleted?: boolean;
  stopItems?: Record<string, StopItemSet>;
}

export interface AutoFillRequest {
  routeUUID: string;
}

export interface AutoFillResponse {
  stopItems: Record<string, StopItemSet>;
}

export const deliveryPlansApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/delivery-plans`).json<DeliveryPlan[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/delivery-plans/${entityUUID}`).json<DeliveryPlan>(),

  create: (charUUID: string, data: DeliveryPlanCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/delivery-plans`, { json: data }).json<DeliveryPlan>(),

  update: (charUUID: string, entityUUID: string, data: DeliveryPlanUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/delivery-plans/${entityUUID}`, { json: data }).json<DeliveryPlan>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/delivery-plans/${entityUUID}`).json<void>(),

  autoFill: (charUUID: string, planUUID: string, data: AutoFillRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/delivery-plans/${planUUID}/auto-fill`, { json: data }).json<AutoFillResponse>(),
};
