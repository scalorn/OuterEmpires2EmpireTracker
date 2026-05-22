import { apiClient } from '../client';

export interface DeliveryItem {
  itemType: string;
  baseItemTypeID: string;
  name: string;
  resourcePurity: string;
  quantity: number;
  delivered: boolean;
}

export interface DeliveryPlanStop {
  colonyUUID: string;
  sequence: number;
  stopCompleted: boolean;
  dropOff: DeliveryItem[];
  pickUp: DeliveryItem[];
  destinationType: string;
  destinationUUID: string;
}

export interface DeliveryPlan {
  uuid: string;
  name: string;
  ownerUUID: string;
  routeUUID: string;
  shipUUID: string;
  completed: boolean;
  stops: DeliveryPlanStop[];
}

export interface DeliveryPlanCreateRequest {
  name: string;
  routeUUID: string;
  shipUUID: string;
  stops: DeliveryPlanStop[];
}

export interface DeliveryPlanUpdateRequest {
  name?: string;
  routeUUID?: string;
  shipUUID?: string;
  completed?: boolean;
  stops?: DeliveryPlanStop[];
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
};
