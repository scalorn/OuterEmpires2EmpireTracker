import { apiClient } from '../client';
import type { SharingRule } from '../types/generated';

export const sharingApi = {
  getSharingRules: (charUUID: string) =>
    apiClient.get(`api/v1/characters/${charUUID}/sharing`).json<SharingRule[]>(),

  putSharingRules: (charUUID: string, rules: SharingRule[]) =>
    apiClient.put(`api/v1/characters/${charUUID}/sharing`, { json: rules }).json<void>(),
};
