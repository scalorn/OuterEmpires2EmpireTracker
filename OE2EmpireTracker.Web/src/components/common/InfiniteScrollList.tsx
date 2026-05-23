import { useRef, useCallback, useEffect, type ReactNode } from 'react';
import { useInfiniteQuery, type QueryKey } from '@tanstack/react-query';
import { LoadingSpinner } from './LoadingSpinner';

export interface InfiniteScrollListProps<T> {
  queryKey: QueryKey;
  fetchPage: (pageParam: number) => Promise<T[]>;
  renderItem: (item: T) => ReactNode;
  keyExtractor: (item: T) => string;
  pageSize?: number;
  filterValue?: string;
}

export function InfiniteScrollList<T>({
  queryKey,
  fetchPage,
  renderItem,
  keyExtractor,
  pageSize = 50,
  filterValue,
}: InfiniteScrollListProps<T>) {
  const sentinelRef = useRef<HTMLDivElement>(null);

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
    refetch,
  } = useInfiniteQuery({
    queryKey: [...queryKey, filterValue],
    queryFn: ({ pageParam }) => fetchPage(pageParam),
    initialPageParam: 0,
    getNextPageParam: (lastPage, _allPages, lastPageParam) => {
      if (lastPage.length < pageSize) return undefined;
      return lastPageParam + 1;
    },
  });

  const handleIntersect = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      const entry = entries[0];
      if (entry?.isIntersecting && hasNextPage && !isFetchingNextPage) {
        fetchNextPage();
      }
    },
    [fetchNextPage, hasNextPage, isFetchingNextPage],
  );

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(handleIntersect, {
      rootMargin: '200px',
    });
    observer.observe(sentinel);

    return () => {
      observer.disconnect();
    };
  }, [handleIntersect]);

  if (isLoading) {
    return <LoadingSpinner message="Loading items..." />;
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-8">
        <p className="text-sm text-red-400">Failed to load items.</p>
        <button
          onClick={() => refetch()}
          className="mt-2 rounded bg-blue-600 px-3 py-1 text-sm text-white hover:bg-blue-500"
        >
          Retry
        </button>
      </div>
    );
  }

  const allItems = data?.pages.flat() ?? [];

  if (allItems.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-gray-500">No items found.</p>
    );
  }

  return (
    <div className="flex flex-col">
      {allItems.map((item) => (
        <div key={keyExtractor(item)}>{renderItem(item)}</div>
      ))}
      <div ref={sentinelRef} className="h-1" aria-hidden="true" />
      {isFetchingNextPage && (
        <div className="flex justify-center py-4" role="status">
          <div className="h-5 w-5 animate-spin rounded-full border-2 border-gray-600 border-t-blue-500" />
          <span className="sr-only">Loading more items</span>
        </div>
      )}
    </div>
  );
}
