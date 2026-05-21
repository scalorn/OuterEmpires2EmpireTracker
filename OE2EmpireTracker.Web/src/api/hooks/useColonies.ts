import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dataApi } from '../endpoints/data';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useColonies(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Colonies'),
    queryFn: () => dataApi.getData(charUUID!, 'Colonies'),
    enabled: !!charUUID,
  });
}

export function useColonyMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const createOrUpdate = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID: string; data: unknown }) =>
      dataApi.putEntity(charUUID!, 'Colonies', entityUUID, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Colonies') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      dataApi.deleteEntity(charUUID!, 'Colonies', entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Colonies') });
    },
  });

  return { createOrUpdate, remove };
}
