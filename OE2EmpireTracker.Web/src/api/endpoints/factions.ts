import { apiClient } from '../client';
import type { ServerFaction, ServerCharacter } from '../types/generated';

export const factionsApi = {
  getFaction: (uuid: string) =>
    apiClient.get(`api/v1/factions/${uuid}`).json<ServerFaction>(),

  getMembers: (factionUUID: string) =>
    apiClient.get(`api/v1/factions/${factionUUID}/members`).json<ServerCharacter[]>(),

  getSharedData: (factionUUID: string, dataType: string) =>
    apiClient.get(`api/v1/factions/${factionUUID}/shared/${dataType}`).json<unknown>(),
};
