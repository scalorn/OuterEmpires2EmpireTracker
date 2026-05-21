import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dataApi } from '../endpoints/data';
import { publicApi, type BlueprintFilters } from '../endpoints/public';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useBlueprints(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Blueprints'),
    queryFn: () => dataApi.getData(charUUID!, 'Blueprints'),
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
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: unknown }) =>
      dataApi.putEntity(charUUID!, 'Blueprints', entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Blueprints') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      dataApi.deleteEntity(charUUID!, 'Blueprints', entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Blueprints') });
    },
  });

  return { createOrUpdate, remove };
}
