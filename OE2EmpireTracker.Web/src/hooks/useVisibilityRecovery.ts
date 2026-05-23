import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';

/**
 * Invalidates timer-related queries when the browser tab regains focus.
 * This ensures countdown timers recalculate from their known end timestamps
 * rather than continuing from a stale last-displayed value (browsers throttle
 * setInterval to 1/sec or less when backgrounded).
 *
 * @param queryKeys - Array of query keys to invalidate on visibility recovery.
 *   If not provided, invalidates all queries containing 'timers' in their key.
 *
 * Validates: Requirements 18.4
 */
export function useVisibilityRecovery(queryKeys?: string[][]): void {
  const queryClient = useQueryClient();

  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        if (queryKeys && queryKeys.length > 0) {
          for (const key of queryKeys) {
            queryClient.invalidateQueries({ queryKey: key });
          }
        } else {
          queryClient.invalidateQueries({ queryKey: ['timers'] });
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [queryClient, queryKeys]);
}
