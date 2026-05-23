import { apiClient } from '../client';
import type { SupplyChain, SupplyChainStep } from '../types/domain';

/**
 * Typed API module for Supply Chain entities.
 * Provides CRUD operations plus step management for supply chain workflows.
 */

export interface SupplyChainCreateRequest {
  name: string;
  sourceColonyUUID: string;
  destinationColonyUUID: string;
  steps?: Omit<SupplyChainStep, 'uuid'>[];
}

export interface SupplyChainUpdateRequest {
  name?: string;
  sourceColonyUUID?: string;
  destinationColonyUUID?: string;
  steps?: Omit<SupplyChainStep, 'uuid'>[];
}

export interface SupplyChainStepRequest {
  resourceOrCommodity: string;
  quantity: number;
  processingType: string;
  sequence: number;
}

export const supplyChainsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/supply-chains`).json<SupplyChain[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/supply-chains/${entityUUID}`).json<SupplyChain>(),

  create: (charUUID: string, data: SupplyChainCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/supply-chains`, { json: data }).json<SupplyChain>(),

  update: (charUUID: string, entityUUID: string, data: SupplyChainUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/supply-chains/${entityUUID}`, { json: data }).json<SupplyChain>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/supply-chains/${entityUUID}`).json<void>(),

  // Step management
  addStep: (charUUID: string, chainUUID: string, step: SupplyChainStepRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/supply-chains/${chainUUID}/steps`, { json: step }).json<SupplyChainStep>(),

  updateStep: (charUUID: string, chainUUID: string, stepUUID: string, step: SupplyChainStepRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/supply-chains/${chainUUID}/steps/${stepUUID}`, { json: step }).json<SupplyChainStep>(),

  removeStep: (charUUID: string, chainUUID: string, stepUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/supply-chains/${chainUUID}/steps/${stepUUID}`).json<void>(),

  reorderSteps: (charUUID: string, chainUUID: string, stepUUIDs: string[]) =>
    apiClient.put(`api/v1/characters/${charUUID}/supply-chains/${chainUUID}/steps/reorder`, { json: { stepUUIDs } }).json<SupplyChain>(),
};
