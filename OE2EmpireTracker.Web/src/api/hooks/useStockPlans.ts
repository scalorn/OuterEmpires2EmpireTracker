import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { stockPlansApi, type StockPlanCreateRequest, type StockPlanUpdateRequest } from '../endpoints/stock-plans';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useStockPlans(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'StockPlans'),
    queryFn: () => stockPlansApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useStockPlanDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'StockPlans', entityUUID ?? ''),
    queryFn: () => stockPlansApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useStockPlanMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: StockPlanCreateRequest | StockPlanUpdateRequest }) => {
      if (entityUUID) {
        return stockPlansApi.update(charUUID!, entityUUID, data as StockPlanUpdateRequest);
      }
      return stockPlansApi.create(charUUID!, data as StockPlanCreateRequest);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'StockPlans') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      stockPlansApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'StockPlans') });
    },
  });

  return { save, remove };
}
