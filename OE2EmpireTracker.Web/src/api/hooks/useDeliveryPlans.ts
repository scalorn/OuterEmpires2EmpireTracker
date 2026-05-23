import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { deliveryPlansApi } from '../endpoints/delivery-plans';
import type { DeliveryPlanCreateRequest, DeliveryPlanUpdateRequest, AutoFillRequest } from '../endpoints/delivery-plans';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { DeliveryPlan } from '../types/domain';

export function usePlans(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'DeliveryPlans'),
    queryFn: () => deliveryPlansApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function usePlanDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'DeliveryPlans', entityUUID ?? ''),
    queryFn: () => deliveryPlansApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function usePlanMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: DeliveryPlanCreateRequest | DeliveryPlanUpdateRequest }) =>
      entityUUID
        ? deliveryPlansApi.update(charUUID!, entityUUID, data as DeliveryPlanUpdateRequest)
        : deliveryPlansApi.create(charUUID!, data as DeliveryPlanCreateRequest),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'DeliveryPlans') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      deliveryPlansApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'DeliveryPlans') });
    },
  });

  const autoFill = useMutation({
    mutationFn: ({ planUUID, data }: { planUUID: string; data: AutoFillRequest }) =>
      deliveryPlansApi.autoFill(charUUID!, planUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'DeliveryPlans') });
    },
  });

  return { save, remove, autoFill };
}
