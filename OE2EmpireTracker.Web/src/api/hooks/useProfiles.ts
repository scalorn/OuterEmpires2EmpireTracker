import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profilesApi } from '../endpoints/profiles';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { PlayerProfile } from '../types/domain';

export function useProfiles(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Profiles'),
    queryFn: () => profilesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useProfileDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Profiles', entityUUID ?? ''),
    queryFn: () => profilesApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useProfileMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: Partial<PlayerProfile> }) =>
      profilesApi.update(charUUID!, entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Profiles') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      profilesApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Profiles') });
    },
  });

  return { save, remove };
}
