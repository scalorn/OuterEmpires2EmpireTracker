import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { blueprintsApi } from '../endpoints/blueprints';
import { publicApi, type BlueprintFilters } from '../endpoints/public';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Blueprint } from '../types/domain';

export function useBlueprints(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Blueprints'),
    queryFn: () => blueprintsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function usePublicBlueprints(filters?: BlueprintFilters) {
  return useQuery({
    queryKey: queryKeys.publicBlueprints(filters),
    queryFn: () => publicApi.getPublicBlueprints(filters),
  });
}

export function useBlueprintMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const createOrUpdate = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: Partial<Blueprint> }) =>
      blueprintsApi.update(charUUID!, entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Blueprints') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      blueprintsApi.remove(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Blueprints') });
    },
  });

  return { createOrUpdate, remove };
}
