import { apiClient } from '../client';

export interface ShipComponentSlot {
  slotType: string;
  slotIndex: number;
  blueprintUUID: string;
  currentHP: number;
  maxHP: number;
  maxRepairPercent: number;
}

export interface Ship {
  uuid: string;
  name: string;
  ownerUUID: string;
  templateUUID: string;
  hullBlueprintUUID: string;
  components: ShipComponentSlot[];
  locationType: string;
  locationUUID: string;
  cargo: Record<string, unknown>;
  hopper: Record<string, unknown>;
  hullCurrentHP: number;
  hullMaxHP: number;
  hullMaxRepairPercent: number;
}

export interface ShipCreateRequest {
  name: string;
  templateUUID: string;
  hullBlueprintUUID: string;
  components?: ShipComponentSlot[];
  locationType?: string;
  locationUUID?: string;
}

export interface ShipUpdateRequest {
  name?: string;
  templateUUID?: string;
  hullBlueprintUUID?: string;
  components?: ShipComponentSlot[];
  locationType?: string;
  locationUUID?: string;
  hullCurrentHP?: number;
  hullMaxHP?: number;
  hullMaxRepairPercent?: number;
}

export const shipsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/ships`).json<Ship[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/ships/${entityUUID}`).json<Ship>(),

  create: (charUUID: string, data: ShipCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/ships`, { json: data }).json<Ship>(),

  update: (charUUID: string, entityUUID: string, data: ShipUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/ships/${entityUUID}`, { json: data }).json<Ship>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/ships/${entityUUID}`).json<void>(),
};
