import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { coloniesApi } from '../endpoints/colonies';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Colony } from '../types/domain';

export function useColonies(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Colonies'),
    queryFn: () => coloniesApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useColonyMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const createOrUpdate = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: Partial<Colony> }) =>
      coloniesApi.update(charUUID!, entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Colonies') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      coloniesApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Colonies') });
    },
  });

  return { createOrUpdate, remove };
}
