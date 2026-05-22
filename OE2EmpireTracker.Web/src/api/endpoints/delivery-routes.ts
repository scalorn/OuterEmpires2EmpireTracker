import { apiClient } from '../client';

export interface RouteStop {
  colonyUUID: string;
  sequence: number;
  destinationType: string;
  destinationUUID: string;
  purpose: string;
  fuelEstimate: number;
}

export interface DeliveryRoute {
  uuid: string;
  name: string;
  ownerUUID: string;
  stops: RouteStop[];
}

export interface DeliveryRouteCreateRequest {
  name: string;
  stops: RouteStop[];
}

export interface DeliveryRouteUpdateRequest {
  name?: string;
  stops?: RouteStop[];
}

export const deliveryRoutesApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/delivery-routes`).json<DeliveryRoute[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/delivery-routes/${entityUUID}`).json<DeliveryRoute>(),

  create: (charUUID: string, data: DeliveryRouteCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/delivery-routes`, { json: data }).json<DeliveryRoute>(),

  update: (charUUID: string, entityUUID: string, data: DeliveryRouteUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/delivery-routes/${entityUUID}`, { json: data }).json<DeliveryRoute>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/delivery-routes/${entityUUID}`).json<void>(),
};
