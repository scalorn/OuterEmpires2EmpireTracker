import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { marketListingsApi } from '../endpoints/market-listings';
import type { MarketListingCreateRequest, MarketListingUpdateRequest } from '../endpoints/market-listings';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useListings(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'MarketListings'),
    queryFn: () => marketListingsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useListingDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'MarketListings', entityUUID ?? ''),
    queryFn: () => marketListingsApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useListingMutations() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  const save = useMutation({
    mutationFn: ({ entityUUID, data }: { entityUUID?: string; data: MarketListingCreateRequest | MarketListingUpdateRequest }) => {
      if (entityUUID) {
        return marketListingsApi.update(charUUID!, entityUUID, data as MarketListingUpdateRequest);
      }
      return marketListingsApi.create(charUUID!, data as MarketListingCreateRequest);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'MarketListings') });
    },
  });

  const remove = useMutation({
    mutationFn: (entityUUID: string) =>
      marketListingsApi.delete(charUUID!, entityUUID),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'MarketListings') });
    },
  });

  const recordSale = useMutation({
    mutationFn: ({ listingUUID, quantity }: { listingUUID: string; quantity: number }) =>
      marketListingsApi.recordSale(charUUID!, listingUUID, quantity),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'MarketListings') });
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'MarketTransactions') });
    },
  });

  return { save, remove, recordSale };
}
