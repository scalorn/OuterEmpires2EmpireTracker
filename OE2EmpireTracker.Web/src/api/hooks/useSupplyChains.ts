import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { supplyChainsApi, type SupplyChainCreateRequest, type SupplyChainUpdateRequest } from '../endpoints/supply-chains';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { SupplyChain } from '../types/domain';

export function useSupplyChains(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'SupplyChains'),
    queryFn: () => supplyChainsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useSupplyChainDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'SupplyChains', entityUUID ?? ''),
    queryFn: () => supplyChainsApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useSupplyChainMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: SupplyChainCreateRequest | SupplyChainUpdateRequest }) => {
      if (entityUUID) {
        return supplyChainsApi.update(charUUID!, entityUUID, data as SupplyChainUpdateRequest);
      }
      return supplyChainsApi.create(charUUID!, data as SupplyChainCreateRequest);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'SupplyChains') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      supplyChainsApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'SupplyChains') });
    },
  });

  return { save, remove };
}
