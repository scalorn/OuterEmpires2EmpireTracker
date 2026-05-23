import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { stationsApi } from '../endpoints/stations';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Station } from '../types/domain';

export function useStations(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Stations'),
    queryFn: () => stationsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useStationDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Stations', entityUUID ?? ''),
    queryFn: () => stationsApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useStationMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: Partial<Station> }) =>
      entityUUID
        ? stationsApi.update(charUUID!, entityUUID, data)
        : stationsApi.create(charUUID!, data as Omit<Station, 'uuid'>),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Stations') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      stationsApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Stations') });
    },
  });

  return { save, remove };
}
