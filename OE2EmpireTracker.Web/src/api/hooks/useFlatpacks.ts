import { useQuery } from '@tanstack/react-query';
import { publicApi } from '../endpoints/public';
import { isFlatpack, extractSubType } from '../../utils/blueprintHelpers';

export interface FlatpackOption {
  uuid: string;
  name: string;
  subType: string;
}

export function useFlatpacks() {
  return useQuery({
    queryKey: ['public', 'flatpacks'],
    queryFn: () => publicApi.getPublicBlueprints({ type: 'Flatpacks' }, 1, 10000),
    staleTime: 5 * 60 * 1000,
    retry: 3,
    retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 10000),
    refetchOnWindowFocus: false,
    select: (data): FlatpackOption[] =>
      data.items
        .filter((item): item is { uuid: string; extendedName: string; bluePrintType: string } =>
          typeof item === 'object' &&
          item !== null &&
          'bluePrintType' in item &&
          typeof (item as Record<string, unknown>).bluePrintType === 'string' &&
          isFlatpack(item as { bluePrintType: string })
        )
        .map((item) => ({
          uuid: item.uuid,
          name: item.extendedName,
          subType: extractSubType(item.bluePrintType),
        })),
  });
}
