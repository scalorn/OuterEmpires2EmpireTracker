import { apiClient } from '../client';
import type { DataType } from '../types/generated';

function toLowerDataType(dataType: DataType | string): string {
  return String(dataType).toLowerCase();
}

export const dataApi = {
  /** Get ALL character data as a combined object (all data types merged). */
  getAllData: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/data`).json<Record<string, unknown>>(),

  getData: (charUUID: string, dataType: DataType | string) =>
    apiClient.get(`api/v1/characters/${charUUID}/data/${toLowerDataType(dataType)}`).json<unknown>(),

  putData: (charUUID: string, dataType: DataType | string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/data/${toLowerDataType(dataType)}`, { json: data }).json<void>(),

  getEntity: (charUUID: string, dataType: DataType | string, entityUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/data/${toLowerDataType(dataType)}/${entityUUID}`).json<unknown>(),

  putEntity: (charUUID: string, dataType: DataType | string, entityUUID: string, data: unknown) =>
    apiClient.put(`api/v1/characters/${charUUID}/data/${toLowerDataType(dataType)}/${entityUUID}`, { json: data }).json<void>(),

  deleteEntity: (charUUID: string, dataType: DataType | string, entityUUID: string) =>
    apiClient.delete(`api/v1/characters/${charUUID}/data/${toLowerDataType(dataType)}/${entityUUID}`).json<void>(),
};
