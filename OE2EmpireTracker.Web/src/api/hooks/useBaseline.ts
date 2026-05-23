import { useQuery } from '@tanstack/react-query';
import { globalApi } from '../endpoints/global';
import type { BaselineData } from '../types/domain';

export function useBaseline() {
  return useQuery<BaselineData>({
    queryKey: ['baseline'],
    queryFn: () => globalApi.getBaseline(),
    staleTime: Infinity, // Baseline data never changes during a session
  });
}
