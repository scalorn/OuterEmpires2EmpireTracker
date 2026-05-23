import { apiClient } from '../client';
import type { MarketListing } from '../types/domain';

export type { MarketListing };

export interface MarketListingCreateRequest {
  stationName: string;
  itemName: string;
  quantity: number;
  price: number;
  condition?: number;
  maxRepair?: number;
}

export interface MarketListingUpdateRequest {
  stationName?: string;
  itemName?: string;
  quantity?: number;
  price?: number;
  condition?: number;
  maxRepair?: number;
}

/**
 * Typed API module for Market Listing entities.
 */
export const marketListingsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-listings`).json<MarketListing[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-listings/${entityUUID}`).json<MarketListing>(),

  create: (charUUID: string, data: MarketListingCreateRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-listings`, { json: data }).json<MarketListing>(),

  update: (charUUID: string, entityUUID: string, data: MarketListingUpdateRequest) =>
    apiClient.put(`api/v1/characters/${charUUID}/market-listings/${entityUUID}`, { json: data }).json<MarketListing>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/market-listings/${entityUUID}`).json<void>(),

  recordSale: (charUUID: string, listingUUID: string, quantity: number) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-listings/${listingUUID}/record-sale`, { json: { quantity } }).json<void>(),
};
