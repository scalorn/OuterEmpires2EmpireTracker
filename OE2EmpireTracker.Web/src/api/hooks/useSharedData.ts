import { useQuery } from '@tanstack/react-query';
import { sharedDataApi, type SharedWithMeGroup } from '../endpoints/shared-data';
import { queryKeys } from './queryKeys';

/**
 * Fetches data shared with the current character for a specific data type.
 * Returns groups of entities organized by owner character.
 */
export function useSharedWithMe(charUUID?: string | null, dataType?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', `SharedWithMe:${dataType ?? ''}`),
    queryFn: () => sharedDataApi.getSharedWithMe(charUUID!, dataType!),
    enabled: !!charUUID && !!dataType,
  });
}

export type { SharedWithMeGroup };
