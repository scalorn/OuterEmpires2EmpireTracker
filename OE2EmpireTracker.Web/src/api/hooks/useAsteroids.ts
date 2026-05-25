import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { asteroidsApi } from '../endpoints/asteroids';
import { publicApi } from '../endpoints/public';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { Asteroid } from '../types/domain';

export function useAsteroids(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'Asteroids'),
    queryFn: () => asteroidsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useAsteroidDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'Asteroids', entityUUID ?? ''),
    queryFn: () => asteroidsApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useAsteroidMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: Partial<Asteroid> }) =>
      entityUUID
        ? asteroidsApi.update(charUUID!, entityUUID, data)
        : asteroidsApi.create(charUUID!, data as Omit<Asteroid, 'uuid'>),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Asteroids') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      asteroidsApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'Asteroids') });
    },
  });

  return { save, remove };
}

export function usePublicAsteroidDetail(asteroidUUID?: string | null) {
  return useQuery({
    queryKey: ['public', 'asteroids', asteroidUUID],
    queryFn: () => publicApi.getAsteroidDetail(asteroidUUID!),
    enabled: !!asteroidUUID,
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes — reserve data changes infrequently
  });
}
