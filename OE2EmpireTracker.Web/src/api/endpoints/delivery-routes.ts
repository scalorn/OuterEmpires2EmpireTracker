import { apiClient } from '../client';
import type { DeliveryRoute, RouteStop } from '../types/domain';

export type { DeliveryRoute, RouteStop };

export interface DeliveryRouteCreateRequest {
  name: string;
  stops: Omit<RouteStop, 'uuid'>[];
}

export interface DeliveryRouteUpdateRequest {
  name?: string;
  stops?: Omit<RouteStop, 'uuid'>[];
}

export interface ReorderStopsRequest {
  stops: { uuid: string; sequence: number }[];
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

  reorderStops: (charUUID: string, routeUUID: string, data: ReorderStopsRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/delivery-routes/${routeUUID}/reorder`, { json: data }).json<DeliveryRoute>(),
};
