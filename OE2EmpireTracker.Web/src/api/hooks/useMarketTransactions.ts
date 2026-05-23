import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { marketTransactionsApi } from '../endpoints/market-transactions';
import type { RecordPurchaseRequest } from '../endpoints/market-transactions';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';

export function useTransactions(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterData(charUUID ?? '', 'MarketTransactions'),
    queryFn: () => marketTransactionsApi.getAll(charUUID!),
    enabled: !!charUUID,
  });
}

export function useTransactionDetail(charUUID?: string | null, entityUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.characterEntity(charUUID ?? '', 'MarketTransactions', entityUUID ?? ''),
    queryFn: () => marketTransactionsApi.get(charUUID!, entityUUID!),
    enabled: !!charUUID && !!entityUUID,
  });
}

export function useRecordPurchase() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  return useMutation({
    mutationFn: (data: RecordPurchaseRequest) =>
      marketTransactionsApi.recordPurchase(charUUID!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.characterData(charUUID!, 'MarketTransactions') });
    },
  });
}
