import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { externalCharactersApi } from '../endpoints/external-characters';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { ExternalCharacter } from '../types/domain';

export function useContacts(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'ExternalCharacters'),
    queryFn: () => externalCharactersApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useContactDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'ExternalCharacters', entityUUID ?? ''),
    queryFn: () => externalCharactersApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useContactMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: Partial<ExternalCharacter> }) =>
      entityUUID
        ? externalCharactersApi.update(charUUID!, entityUUID, data)
        : externalCharactersApi.create(charUUID!, data as Omit<ExternalCharacter, 'uuid'>),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'ExternalCharacters') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      externalCharactersApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'ExternalCharacters') });
    },
  });

  return { save, remove };
}
