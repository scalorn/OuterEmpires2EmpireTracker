import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { coloniesApi } from '../endpoints/colonies';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Colony, WarehouseItem, CommodityRequest } from '../types/domain';
import type { ColonyPlannerRequest } from '../types/generated';

export function useColonies(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Colonies'),
    queryFn: () => coloniesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useColonyDetail(charUUID?: string | null, colonyUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Colonies', colonyUUID ?? ''),
    queryFn: () => coloniesApi.get(charUUID!, colonyUUID!),
    enabled: !!charUUID && !!colonyUUID,
  });
}

export function useColonyMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const invalidateColonies = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Colonies') });
  };

  const createOrUpdate = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: Partial<Colony> }) =>
      coloniesApi.update(charUUID!, entityUUID, data),
    onSuccess: invalidateColonies,
  });

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: Partial<Colony> }) =>
      entityUUID
        ? coloniesApi.update(charUUID!, entityUUID, data)
        : coloniesApi.create(charUUID!, data),
    onSuccess: invalidateColonies,
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) => coloniesApi.delete(charUUID!, entityUUID),
    onSuccess: invalidateColonies,
  });

  const addStructure = useMutation({
    mutationFn: ({
      colonyUUID,
      flatpackBlueprintUUID,
    }: {
      colonyUUID: string;
      flatpackBlueprintUUID: string;
    }) => coloniesApi.addStructure(charUUID!, colonyUUID, flatpackBlueprintUUID),
    onSuccess: invalidateColonies,
  });

  const removeStructure = useMutation({
    mutationFn: ({ colonyUUID, structureUUID }: { colonyUUID: string; structureUUID: string }) =>
      coloniesApi.removeStructure(charUUID!, colonyUUID, structureUUID),
    onSuccess: invalidateColonies,
  });

  const addItem = useMutation({
    mutationFn: ({
      colonyUUID,
      item,
    }: {
      colonyUUID: string;
      item: Omit<WarehouseItem, 'uuid'>;
    }) => coloniesApi.addItem(charUUID!, colonyUUID, item),
    onSuccess: invalidateColonies,
  });

  const removeItem = useMutation({
    mutationFn: ({ colonyUUID, itemUUID }: { colonyUUID: string; itemUUID: string }) =>
      coloniesApi.removeItem(charUUID!, colonyUUID, itemUUID),
    onSuccess: invalidateColonies,
  });

  const updateItem = useMutation({
    mutationFn: ({
      colonyUUID,
      itemUUID,
      quantity,
    }: {
      colonyUUID: string;
      itemUUID: string;
      quantity: number;
    }) => coloniesApi.updateItem(charUUID!, colonyUUID, itemUUID, quantity),
    onSuccess: invalidateColonies,
  });

  const addCommodityRequest = useMutation({
    mutationFn: ({
      colonyUUID,
      dto,
    }: {
      colonyUUID: string;
      dto: Omit<CommodityRequest, 'isFulfilled'>;
    }) => coloniesApi.addCommodityRequest(charUUID!, colonyUUID, dto),
    onSuccess: invalidateColonies,
  });

  const removeCommodityRequest = useMutation({
    mutationFn: ({
      colonyUUID,
      commodityName,
    }: {
      colonyUUID: string;
      commodityName: string;
    }) => coloniesApi.removeCommodityRequest(charUUID!, colonyUUID, commodityName),
    onSuccess: invalidateColonies,
  });

  const updateCommodityRequest = useMutation({
    mutationFn: ({
      colonyUUID,
      commodityName,
      dto,
    }: {
      colonyUUID: string;
      commodityName: string;
      dto: Partial<CommodityRequest>;
    }) => coloniesApi.updateCommodityRequest(charUUID!, colonyUUID, commodityName, dto),
    onSuccess: invalidateColonies,
  });

  const bootstrap = useMutation({
    mutationFn: (colonyUUID: string) => coloniesApi.bootstrap(charUUID!, colonyUUID),
    onSuccess: invalidateColonies,
  });

  const optimizeBuildOrder = useMutation({
    mutationFn: (request: ColonyPlannerRequest) => coloniesApi.optimizeBuildOrder(request),
    onSuccess: invalidateColonies,
  });

  return {
    createOrUpdate,
    save,
    remove,
    addStructure,
    removeStructure,
    addItem,
    removeItem,
    updateItem,
    addCommodityRequest,
    removeCommodityRequest,
    updateCommodityRequest,
    bootstrap,
    optimizeBuildOrder,
  };
}
