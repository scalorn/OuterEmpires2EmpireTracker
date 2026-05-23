import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { stockProfilesApi, type StockProfileCreateRequest, type StockProfileUpdateRequest } from '../endpoints/stock-profiles';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { StockProfile } from '../types/domain';

export function useStockProfiles(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'StockProfiles'),
    queryFn: () => stockProfilesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useStockProfileDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'StockProfiles', entityUUID ?? ''),
    queryFn: () => stockProfilesApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useStockProfileMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: StockProfileCreateRequest | StockProfileUpdateRequest }) => {
      if (entityUUID) {
        return stockProfilesApi.update(charUUID!, entityUUID, data as StockProfileUpdateRequest);
      }
      return stockProfilesApi.create(charUUID!, data as StockProfileCreateRequest);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'StockProfiles') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      stockProfilesApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'StockProfiles') });
    },
  });

  return { save, remove };
}
