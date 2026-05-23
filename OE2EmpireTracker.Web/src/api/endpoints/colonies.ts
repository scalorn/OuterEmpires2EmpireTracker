import { apiClient } from '../client';
import type {
  Colony,
  ColonyStructure,
  WarehouseItem,
  CommodityRequest,
} from '../types/domain';
import type { ColonyPlannerRequest, BuildOrderResult } from '../types/generated';

/**
 * Typed API module for Colony entities and sub-resources.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const coloniesApi = {
  // ─── Colony CRUD ────────────────────────────────────────────────────────────

  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/colonies`).json<Colony[]>(),

  get: (charUUID: string, colonyUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/colonies/${colonyUUID}`).json<Colony>(),

  create: (charUUID: string, data: Partial<Colony>) =>
    apiClient.post(`api/v1/characters/${charUUID}/colonies`, { json: data }).json<Colony>(),

  update: (charUUID: string, colonyUUID: string, data: Partial<Colony>) =>
    apiClient.put(`api/v1/characters/${charUUID}/colonies/${colonyUUID}`, { json: data }).json<Colony>(),

  delete: (charUUID: string, colonyUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/colonies/${colonyUUID}`).json<void>(),

  // ─── Structures sub-resource ────────────────────────────────────────────────

  addStructure: (charUUID: string, colonyUUID: string, flatpackBlueprintUUID: string) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/structures`, {
        json: { flatpackBlueprintUUID },
      })
      .json<ColonyStructure>(),

  removeStructure: (charUUID: string, colonyUUID: string, structureUUID: string) =>
    apiClient
      .delete(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/structures/${structureUUID}`)
      .json<void>(),

  // ─── Items sub-resource ─────────────────────────────────────────────────────

  addItem: (charUUID: string, colonyUUID: string, item: Omit<WarehouseItem, 'uuid'>) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/items`, { json: item })
      .json<WarehouseItem>(),

  removeItem: (charUUID: string, colonyUUID: string, itemUUID: string) =>
    apiClient
      .delete(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/items/${itemUUID}`)
      .json<void>(),

  updateItem: (charUUID: string, colonyUUID: string, itemUUID: string, quantity: number) =>
    apiClient
      .put(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/items/${itemUUID}`, {
        json: { quantity },
      })
      .json<WarehouseItem>(),

  // ─── Commodity Requests sub-resource ────────────────────────────────────────

  addCommodityRequest: (
    charUUID: string,
    colonyUUID: string,
    dto: Omit<CommodityRequest, 'isFulfilled'>,
  ) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/commodity-requests`, {
        json: dto,
      })
      .json<CommodityRequest>(),

  removeCommodityRequest: (charUUID: string, colonyUUID: string, commodityName: string) =>
    apiClient
      .delete(
        `api/v1/characters/${charUUID}/colonies/${colonyUUID}/commodity-requests/${commodityName}`,
      )
      .json<void>(),

  updateCommodityRequest: (
    charUUID: string,
    colonyUUID: string,
    commodityName: string,
    dto: Partial<CommodityRequest>,
  ) =>
    apiClient
      .put(
        `api/v1/characters/${charUUID}/colonies/${colonyUUID}/commodity-requests/${commodityName}`,
        { json: dto },
      )
      .json<CommodityRequest>(),

  // ─── Colony actions ─────────────────────────────────────────────────────────

  bootstrap: (charUUID: string, colonyUUID: string) =>
    apiClient
      .post(`api/v1/characters/${charUUID}/colonies/${colonyUUID}/bootstrap`)
      .json<Colony>(),

  optimizeBuildOrder: (request: ColonyPlannerRequest) =>
    apiClient
      .post('api/v1/colony-planner/build-order', { json: request })
      .json<BuildOrderResult>(),
};
