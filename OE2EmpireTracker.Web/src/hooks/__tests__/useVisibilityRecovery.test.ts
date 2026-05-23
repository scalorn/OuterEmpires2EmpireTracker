import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useVisibilityRecovery } from '../useVisibilityRecovery';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const Wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: queryClient }, children);
  return { Wrapper, queryClient };
}

describe('useVisibilityRecovery', () => {
  let originalVisibilityState: PropertyDescriptor | undefined;

  beforeEach(() => {
    originalVisibilityState = Object.getOwnPropertyDescriptor(document, 'visibilityState');
  });

  afterEach(() => {
    if (originalVisibilityState) {
      Object.defineProperty(document, 'visibilityState', originalVisibilityState);
    } else {
      // Reset to default
      Object.defineProperty(document, 'visibilityState', {
        configurable: true,
        get: () => 'visible',
      });
    }
  });

  it('invalidates default timers query key when tab becomes visible', () => {
    const { Wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderHook(() => useVisibilityRecovery(), { wrapper: Wrapper });

    // Simulate tab becoming visible
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'visible',
    });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['timers'] });
  });

  it('invalidates custom query keys when provided', () => {
    const { Wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
    const customKeys = [['colony-timers'], ['build-timers']];

    renderHook(() => useVisibilityRecovery(customKeys), { wrapper: Wrapper });

    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'visible',
    });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['colony-timers'] });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['build-timers'] });
  });

  it('does not invalidate when tab becomes hidden', () => {
    const { Wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderHook(() => useVisibilityRecovery(), { wrapper: Wrapper });

    // Simulate tab becoming hidden
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'hidden',
    });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(invalidateSpy).not.toHaveBeenCalled();
  });

  it('cleans up event listener on unmount', () => {
    const { Wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { unmount } = renderHook(() => useVisibilityRecovery(), { wrapper: Wrapper });
    unmount();

    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'visible',
    });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(invalidateSpy).not.toHaveBeenCalled();
  });

  it('recalculates from end timestamp not last displayed value', () => {
    // This test verifies the design intent: by invalidating queries,
    // components re-fetch timer data and recompute remaining time from
    // the end timestamp (server data), not from a stale local countdown.
    const { Wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    renderHook(() => useVisibilityRecovery(), { wrapper: Wrapper });

    // Simulate tab was hidden then becomes visible (background period)
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => 'visible',
    });
    document.dispatchEvent(new Event('visibilitychange'));

    // Invalidation triggers refetch which forces recalculation from end timestamp
    expect(invalidateSpy).toHaveBeenCalledTimes(1);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['timers'] });
  });
});
