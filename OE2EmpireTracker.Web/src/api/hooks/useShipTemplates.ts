import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { shipTemplatesApi } from '../endpoints/ship-templates';
import type { ShipTemplateCreateRequest, ShipTemplateUpdateRequest } from '../endpoints/ship-templates';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useShipTemplates(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'ShipTemplates'),
    queryFn: () => shipTemplatesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useTemplateDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'ShipTemplates', entityUUID ?? ''),
    queryFn: () => shipTemplatesApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useTemplateMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: ShipTemplateCreateRequest | ShipTemplateUpdateRequest }) =>
      entityUUID
        ? shipTemplatesApi.update(charUUID!, entityUUID, data as ShipTemplateUpdateRequest)
        : shipTemplatesApi.create(charUUID!, data as ShipTemplateCreateRequest),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'ShipTemplates') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      shipTemplatesApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'ShipTemplates') });
    },
  });

  const orderBuild = useMutation({
    mutationFn: (templateUUID: string) =>
      shipTemplatesApi.orderBuild(charUUID!, templateUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'ShipTemplates') });
    },
  });

  return { save, remove, orderBuild };
}
