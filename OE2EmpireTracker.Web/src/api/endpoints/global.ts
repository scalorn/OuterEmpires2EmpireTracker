import { apiClient } from '../client';
import type { DataType } from '../types/generated';

export const globalApi = {
  getGlobalData: (dataType: DataType) =>
    apiClient.get(`api/v1/global/${dataType}`).json<unknown>(),
};
