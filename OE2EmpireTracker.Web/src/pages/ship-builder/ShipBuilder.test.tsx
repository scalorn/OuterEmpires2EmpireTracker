import { vi, describe, it, expect, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import ShipBuilder from './ShipBuilder';
import { useShipBuilderStore } from './shipBuilderStore';

// Mock the public API
vi.mock('../../api/endpoints/public', () => ({
  publicApi: {
    getPublicBlueprints: vi.fn(),
    getBlueprintDetail: vi.fn(),
  },
}));

import { publicApi } from '../../api/endpoints/public';
const mockGetBlueprints = vi.mocked(publicApi.getPublicBlueprints);
const mockGetDetail = vi.mocked(publicApi.getBlueprintDetail);

// Mock data
const HULL_UUID = '00000000-0000-0000-0000-000000000001';
const REACTOR_UUID = '00000000-0000-0000-0000-000000000002';

const mockBlueprintList = {
  items: [
    { uuid: HULL_UUID, name: 'Corvette', extendedName: 'Corvette Mk II', bluePrintType: 'Hull', class: 4 },
    { uuid: REACTOR_UUID, name: 'Reactor', extendedName: 'Reactor Alpha', bluePrintType: 'Reactor', class: 4 },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 10000,
};

const mockHullDetail = {
  uuid: HULL_UUID,
  name: 'Corvette',
  extendedName: 'Corvette Mk II',
  bluePrintType: 'Hull',
  class: 4,
  properties: {
    'Mass': '500',
    'Reactor Slots': '2',
    'Shield Slots': '1',
    'Eng Capacity Available': '100',
  },
};

const mockReactorDetail = {
  uuid: REACTOR_UUID,
  name: 'Reactor',
  extendedName: 'Reactor Alpha',
  bluePrintType: 'Reactor',
  class: 4,
  properties: {
    'Mass': '50',
    'Power Provided': '200',
    'Power Regeneration Rate': '10',
    'Eng Capacity Required': '20',
  },
};


beforeEach(() => {
  // Reset store to initial state
  useShipBuilderStore.setState({
    blueprintList: [],
    blueprintCache: {},
    blueprintListLoading: false,
    blueprintListError: null,
    selectedHullUUID: null,
    hullDetail: null,
    slots: [],
    stats: null,
    loadingUUIDs: new Set(),
    slotErrors: {},
  });
  vi.clearAllMocks();
  // Reset URL hash
  window.history.replaceState(null, '', window.location.pathname);
});

describe('ShipBuilder integration', () => {
  it('renders "Ship Builder" heading and hull selector on mount', async () => {
    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);

    render(<ShipBuilder />);

    expect(screen.getByText('Ship Builder')).toBeInTheDocument();
    // Wait for blueprint list to load
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });
  });

  it('shows "Select a hull to begin" when no hull selected', async () => {
    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);

    render(<ShipBuilder />);

    await waitFor(() => {
      expect(screen.getByText('Select a hull to begin')).toBeInTheDocument();
    });
  });

  it('after selecting hull: slots appear grouped by type', async () => {
    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);
    mockGetDetail.mockResolvedValue(mockHullDetail as never);

    render(<ShipBuilder />);

    // Wait for blueprint list to load
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    // Open the hull dropdown and select the hull
    const combobox = screen.getByRole('combobox');
    fireEvent.focus(combobox);

    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    // Click the hull option
    const hullOption = screen.getByRole('option', { name: /Corvette Mk II/i });
    fireEvent.click(hullOption);

    // Wait for slots to appear — hull has Reactor Slots: 2 and Shield Slots: 1
    await waitFor(() => {
      // SlotGroup headings use h3 elements
      const headings = screen.getAllByRole('heading', { level: 3 });
      const headingTexts = headings.map((h) => h.textContent);
      expect(headingTexts).toContain('Core');
      expect(headingTexts).toContain('Defence');
    });

    // Verify slot labels appear
    expect(screen.getByText('Reactor 0')).toBeInTheDocument();
    expect(screen.getByText('Reactor 1')).toBeInTheDocument();
    expect(screen.getByText('Shield 0')).toBeInTheDocument();

    // "Select a hull to begin" should be gone
    expect(screen.queryByText('Select a hull to begin')).not.toBeInTheDocument();
  });

  it('after installing component: stats update', async () => {
    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);
    mockGetDetail.mockImplementation(async (uuid: string) => {
      if (uuid === HULL_UUID) return mockHullDetail as never;
      if (uuid === REACTOR_UUID) return mockReactorDetail as never;
      throw new Error(`Unknown UUID: ${uuid}`);
    });

    render(<ShipBuilder />);

    // Wait for blueprint list to load
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    // Select hull via store directly (simpler for this test)
    await useShipBuilderStore.getState().selectHull(HULL_UUID);

    // Wait for slots to render
    await waitFor(() => {
      expect(screen.getByText('Reactor 0')).toBeInTheDocument();
    });

    // Stats panel should show hull-only stats (Mass = 500)
    await waitFor(() => {
      expect(screen.getByText('Ship Stats')).toBeInTheDocument();
    });

    // Install a reactor component via store
    await useShipBuilderStore.getState().installComponent('Reactor', 0, REACTOR_UUID);

    // Stats should update — eng capacity should now show 20 / 100
    await waitFor(() => {
      expect(screen.getByText('20 / 100')).toBeInTheDocument();
    });
  });

  it('clear build: resets to initial state', async () => {
    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);
    mockGetDetail.mockResolvedValue(mockHullDetail as never);

    render(<ShipBuilder />);

    // Wait for blueprint list to load
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    // Select hull via store
    await useShipBuilderStore.getState().selectHull(HULL_UUID);

    // Wait for Clear Build button to appear
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /clear build/i })).toBeInTheDocument();
    });

    // Click Clear Build
    fireEvent.click(screen.getByRole('button', { name: /clear build/i }));

    // Should return to empty state
    await waitFor(() => {
      expect(screen.getByText('Select a hull to begin')).toBeInTheDocument();
    });

    // Clear Build button should be gone
    expect(screen.queryByRole('button', { name: /clear build/i })).not.toBeInTheDocument();
  });

  it('error state: shows error when blueprint list fetch fails', async () => {
    mockGetBlueprints.mockRejectedValue(new Error('Network error'));

    render(<ShipBuilder />);

    // Wait for error to appear
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });

    // Retry button should be present
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('URL restore: restores hull from URL hash on load', async () => {
    // Mock window.location.hash to return our build URL
    const hashValue = `#build=${HULL_UUID}:,`;
    Object.defineProperty(window, 'location', {
      value: { ...window.location, hash: hashValue, pathname: '/' },
      writable: true,
    });

    mockGetBlueprints.mockResolvedValue(mockBlueprintList as never);
    mockGetDetail.mockResolvedValue(mockHullDetail as never);

    render(<ShipBuilder />);

    // Wait for hull to be restored — slots should appear
    await waitFor(() => {
      expect(screen.getByText('Reactor 0')).toBeInTheDocument();
      expect(screen.getByText('Reactor 1')).toBeInTheDocument();
      expect(screen.getByText('Shield 0')).toBeInTheDocument();
    });

    // Hull should be selected in the store
    expect(useShipBuilderStore.getState().selectedHullUUID).toBe(HULL_UUID);
  });
});
