import { apiClient } from '../client';
import type { DataType } from '../types/generated';
import type { BaselineData } from '../types/domain';

export const globalApi = {
  getGlobalData: (dataType: DataType) =>
    apiClient.get(`api/v1/global/${dataType}`).json<unknown>(),

  /** Fetch baseline reference data (blueprint types, ship classes, tech levels, commodities, etc.). */
  getBaseline: () =>
    apiClient.get(`api/v1/global/baseline`).json<BaselineData>(),
};
