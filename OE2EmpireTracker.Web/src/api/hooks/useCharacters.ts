import { useQuery } from '@tanstack/react-query';
import { charactersApi } from '../endpoints/characters';
import { queryKeys } from './queryKeys';

export function useCharacters() {
  return useQuery({
    queryKey: queryKeys.characters,
    queryFn: () => charactersApi.getAll(),
  });
}

export function useCharacter(uuid: string | null) {
  return useQuery({
    queryKey: queryKeys.character(uuid ?? ''),
    queryFn: () => charactersApi.get(uuid!),
    enabled: !!uuid,
  });
}
