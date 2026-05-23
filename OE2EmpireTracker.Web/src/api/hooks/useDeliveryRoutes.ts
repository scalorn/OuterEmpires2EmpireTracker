import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { deliveryRoutesApi } from '../endpoints/delivery-routes';
import type { DeliveryRouteCreateRequest, DeliveryRouteUpdateRequest } from '../endpoints/delivery-routes';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useDeliveryRoutes(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'DeliveryRoutes'),
    queryFn: () => deliveryRoutesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useRouteDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'DeliveryRoutes', entityUUID ?? ''),
    queryFn: () => deliveryRoutesApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useRouteMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: DeliveryRouteCreateRequest | DeliveryRouteUpdateRequest }) => {
      if (entityUUID) {
        return deliveryRoutesApi.update(charUUID!, entityUUID, data as DeliveryRouteUpdateRequest);
      }
      return deliveryRoutesApi.create(charUUID!, data as DeliveryRouteCreateRequest);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'DeliveryRoutes') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      deliveryRoutesApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'DeliveryRoutes') });
    },
  });

  return { save, remove };
}
