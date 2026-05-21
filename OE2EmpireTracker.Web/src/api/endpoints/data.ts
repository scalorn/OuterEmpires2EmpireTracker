import { apiClient } from '../client';
import type { DataType } from '../types/generated';

export const dataApi = {
  getData: (charUUID: string, dataType: DataType) =>
    apiClient.get(`api/v1/characters/${charUUID}/data/${dataType}`).json<unknown>(),

  putData: (charUUID: string, dataType: DataType, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/data/${dataType}`, { json: data }).json<void>(),

  getEntity: (charUUID: string, dataType: DataType, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/data/${dataType}/${entityUUID}`).json<unknown>(),

  putEntity: (charUUID: string, dataType: DataType, entityUUID: string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/data/${dataType}/${entityUUID}`, { json: data }).json<void>(),

  deleteEntity: (charUUID: string, dataType: DataType, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/data/${dataType}/${entityUUID}`).json<void>(),
};
