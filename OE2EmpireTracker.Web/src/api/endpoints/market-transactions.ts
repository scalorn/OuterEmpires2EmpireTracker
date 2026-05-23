import { apiClient } from '../client';
import type { MarketTransaction } from '../types/domain';

export type { MarketTransaction };

export interface RecordPurchaseRequest {
  itemName: string;
  quantity: number;
  price: number;
  stationName: string;
  counterparty?: string;
  faction?: string;
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

  create: (charUUID: string, data: Omit<MarketTransaction, 'uuid'>) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-transactions`, { json: data }).json<MarketTransaction>(),

  update: (charUUID: string, entityUUID: string, data: Partial<MarketTransaction>) =>
    apiClient.put(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`, { json: data }).json<MarketTransaction>(),

  delete: (charUUID: string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/market-transactions/${entityUUID}`).json<void>(),

  recordPurchase: (charUUID: string, data: RecordPurchaseRequest) =>
    apiClient.post(`api/v1/characters/${charUUID}/market-transactions`, { json: { ...data, type: 'Buy', transactionDate: new Date().toISOString() } }).json<MarketTransaction>(),
};
