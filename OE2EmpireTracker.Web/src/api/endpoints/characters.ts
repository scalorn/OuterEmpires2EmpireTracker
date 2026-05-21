import { apiClient } from '../client';
import type { ServerCharacter } from '../types/generated';

export const charactersApi = {
  getAll: () =>
    apiClient.get('api/v1/characters').json<ServerCharacter[]>(),

  get: (uuid: string) =>
    apiClient.get(`api/v1/characters/${uuid}`).json<ServerCharacter>(),

  switchCharacter: (uuid: string) =>
    apiClient.post(`api/v1/characters/${uuid}/switch`).json<void>(),
};
