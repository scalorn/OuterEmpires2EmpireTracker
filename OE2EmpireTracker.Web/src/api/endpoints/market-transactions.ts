import { apiClient } from '../client';
import type { MarketTransaction } from '../types/domain';

export type { MarketTransaction };

export interface RecordPurchaseRequest {
  itemName: string;
  itemType?: string;
  quantity: number;
  pricePerUnit: number;
  counterparty?: string;
  counterpartyFaction?: string;
  stationUUID?: string;
}

/**
 * Typed API module for Market Transaction entities.
 * Calls the validated typed endpoints instead of raw data endpoints.
 */
export const marketTransactionsApi = {
  getAll: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-transactions`).json<MarketTransaction[]>(),

  get: (charUUID: string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`).json<MarketTransaction>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`).json<void>(),

  recordPurchase: (charUUID: string, data: RecordPurchaseRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-transactions/record-purchase`, { json: data }).json<MarketTransaction>(),
};
