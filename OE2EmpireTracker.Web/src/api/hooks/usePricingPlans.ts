import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { pricingPlansApi } from '../endpoints/pricing-plans';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { PricingPlan } from '../types/domain';

export function usePricingPlans(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'PricingPlans'),
    queryFn: () => pricingPlansApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function usePricingPlanDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'PricingPlans', entityUUID ?? ''),
    queryFn: () => pricingPlansApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function usePricingPlanMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: Partial<PricingPlan> }) =>
      entityUUID
        ? pricingPlansApi.update(charUUID!, entityUUID, data)
        : pricingPlansApi.create(charUUID!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'PricingPlans') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      pricingPlansApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'PricingPlans') });
    },
  });

  return { save, remove };
}
