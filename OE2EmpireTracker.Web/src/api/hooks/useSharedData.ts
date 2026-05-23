import { useQuery } from '@tanstack/react-query';
import { sharedDataApi } from '../endpoints/shared-data';
import { queryKeys } from './queryKeys';

export function useSharedCharacters(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'SharedData'),
    queryFn: () => sharedDataApi.getSharedCharacters(charUUID!),
    enabled: !!charUUID,
  });
}

export function useSharedEntityData(
  charUUID?: string | null,
  sharerUUID?: string | null,
  dataType?: string | null,
) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'SharedData', `${sharerUUID ?? ''}:${dataType ?? ''}`),
    queryFn: () => sharedDataApi.getSharedData(charUUID!, sharerUUID!, dataType!),
    enabled: !!charUUID && !!sharerUUID && !!dataType,
  });
}
