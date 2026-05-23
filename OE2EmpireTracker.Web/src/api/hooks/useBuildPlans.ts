import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { buildPlansApi } from '../endpoints/build-plans';
import type { BuildPlanCreateRequest, BuildPlanUpdateRequest } from '../endpoints/build-plans';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useBuildPlans(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'BuildPlans'),
    queryFn: () => buildPlansApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useBuildPlanDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'BuildPlans', entityUUID ?? ''),
    queryFn: () => buildPlansApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useBuildPlanMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: BuildPlanCreateRequest | BuildPlanUpdateRequest }) =>
      entityUUID
        ? buildPlansApi.update(charUUID!, entityUUID, data as BuildPlanUpdateRequest)
        : buildPlansApi.create(charUUID!, data as BuildPlanCreateRequest),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'BuildPlans') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      buildPlansApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'BuildPlans') });
    },
  });

  return { save, remove };
}
