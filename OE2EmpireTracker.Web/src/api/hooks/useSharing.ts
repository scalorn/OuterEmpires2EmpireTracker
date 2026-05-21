import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sharingApi } from '../endpoints/sharing';
import { queryKeys } from './queryKeys';
import { useAuthStore } from '../../auth/store';
import type { SharingRule } from '../types/generated';

export function useSharing(charUUID?: string | null) {
  return useQuery({
    queryKey: queryKeys.sharing(charUUID ?? ''),
    queryFn: () => sharingApi.getSharingRules(charUUID!),
    enabled: !!charUUID,
  });
}

export function useSharingMutation() {
  const queryClient = useQueryClient();
  const charUUID = useAuthStore((s) => s.characterUUID);

  return useMutation({
    mutationFn: (rules: SharingRule[]) =>
      sharingApi.putSharingRules(charUUID!, rules),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.sharing(charUUID!) });
    },
  });
}
