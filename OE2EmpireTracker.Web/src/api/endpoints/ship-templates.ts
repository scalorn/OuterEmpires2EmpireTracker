import { apiClient } from '../client';
import type { ShipComponentSlot } from './ships';

export interface ShipTemplate {
  uuid: string;
  name: string;
  ownerUUID: string;
  hullBlueprintUUID: string;
  components: ShipComponentSlot[];
}

export interface ShipTemplateCreateRequest {
  name: string;
  hullBlueprintUUID: string;
  components?: ShipComponentSlot[];
}

export interface ShipTemplateUpdateRequest {
  name?: string;
  hullBlueprintUUID?: string;
  components?: ShipComponentSlot[];
}

export const shipTemplatesApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/ship-templates`).json<ShipTemplate[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/ship-templates/${entityUUID}`).json<ShipTemplate>(),

  create: (charUUID: string, data: ShipTemplateCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/ship-templates`, { json: data }).json<ShipTemplate>(),

  update: (charUUID: string, entityUUID: string, data: ShipTemplateUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/ship-templates/${entityUUID}`, { json: data }).json<ShipTemplate>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/ship-templates/${entityUUID}`).json<void>(),
};
