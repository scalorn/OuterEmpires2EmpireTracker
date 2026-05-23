import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { InfiniteScrollList } from '../InfiniteScrollList';

interface TestItem {
  id: string;
  name: string;
}

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
}

function renderWithQuery(ui: React.ReactElement) {
  const queryClient = createQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
  );
}

function makeItems(count: number, pageOffset = 0): TestItem[] {
  return Array.from({ length: count }, (_, i) => ({
    id: `item-${pageOffset * 50 + i}`,
    name: `Item ${pageOffset * 50 + i}`,
  }));
}

describe('InfiniteScrollList', () => {
  let mockFetchPage: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockFetchPage = vi.fn();
  });

  it('shows loading state initially', () => {
    mockFetchPage.mockReturnValue(new Promise(() => {}));
    renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-items']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
      />,
    );
    expect(screen.getByRole('status')).toBeInTheDocument();
    expect(screen.getByText('Loading items...')).toBeInTheDocument();
  });

  it('renders initial page of items', async () => {
    const items = makeItems(3);
    mockFetchPage.mockResolvedValue(items);

    renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-items']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('Item 0')).toBeInTheDocument();
    });
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
    expect(mockFetchPage).toHaveBeenCalledWith(0);
  });

  it('shows empty state when no items returned', async () => {
    mockFetchPage.mockResolvedValue([]);

    renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-empty']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('No items found.')).toBeInTheDocument();
    });
  });

  it('shows error state with retry button on fetch failure', async () => {
    mockFetchPage.mockRejectedValue(new Error('Network error'));

    renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-error']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('Failed to load items.')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('does not request next page when last page is smaller than pageSize', async () => {
    const items = makeItems(10);
    mockFetchPage.mockResolvedValue(items);

    renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-no-next']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
        pageSize={50}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('Item 0')).toBeInTheDocument();
    });
    // Only one call should have been made (page 0)
    expect(mockFetchPage).toHaveBeenCalledTimes(1);
  });

  it('includes filterValue in query key for cache separation', async () => {
    const items = makeItems(2);
    mockFetchPage.mockResolvedValue(items);

    const { rerender } = renderWithQuery(
      <InfiniteScrollList
        queryKey={['test-filter']}
        fetchPage={mockFetchPage}
        renderItem={(item: TestItem) => <span>{item.name}</span>}
        keyExtractor={(item: TestItem) => item.id}
        filterValue="alpha"
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('Item 0')).toBeInTheDocument();
    });

    const queryClient = createQueryClient();
    mockFetchPage.mockResolvedValue(makeItems(1));

    rerender(
      <QueryClientProvider client={queryClient}>
        <InfiniteScrollList
          queryKey={['test-filter']}
          fetchPage={mockFetchPage}
          renderItem={(item: TestItem) => <span>{item.name}</span>}
          keyExtractor={(item: TestItem) => item.id}
          filterValue="beta"
        />
      </QueryClientProvider>,
    );

    // Changing filterValue triggers a new query
    await waitFor(() => {
      expect(mockFetchPage).toHaveBeenCalledTimes(2);
    });
  });
});
