import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
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

export function useBlueprintDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Blueprints', entityUUID ?? ''),
    queryFn: () => blueprintsApi.getOne(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function usePublicBlueprints(filters?: BlueprintFilters) {
  return useQuery({
    queryKey: queryKeys.publicBlueprints(filters),
    queryFn: () => publicApi.getPublicBlueprints(filters, 1, 10000),
    placeholderData: keepPreviousData,
  });
}

/** Fetch global/baseline data (BlueprintType, ShipClass, TechLevel, etc.) from the public API. */
export function useGlobalData<T = unknown>(dataType: string) {
  return useQuery({
    queryKey: ['globalData', dataType],
    queryFn: () => publicApi.getGlobalData<T>(dataType),
    staleTime: Infinity, // Baseline data rarely changes
  });
}

export function useBlueprintMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const create = useMutation({
    mutationFn: (data: Omit<Blueprint, 'uuid'>) =>
      blueprintsApi.create(charUUID!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Blueprints') });
    },
  });

  const save = useMutation({
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

  return { create, save, remove };
}
