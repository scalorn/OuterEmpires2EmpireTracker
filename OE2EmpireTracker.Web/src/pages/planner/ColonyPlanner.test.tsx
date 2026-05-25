import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ColonyPlanner } from './ColonyPlanner';
import { usePlannerStore } from './plannerStore';

vi.mock('../../api/endpoints/public', () => ({
  publicApi: {
    getBlueprintDetail: vi.fn(),
    getPublicBlueprints: vi.fn(),
  },
}));

const mockMutateAsync = vi.fn();
vi.mock('../../api/hooks/useColonyPlanner', () => ({
  useColonyPlannerBuildOrder: () => ({
    mutateAsync: mockMutateAsync,
  }),
}));

vi.mock('../../api/hooks/useFlatpacks', () => ({
  useFlatpacks: () => ({
    data: [
      { uuid: 'uuid-1', name: 'Iron Mining Rig', subType: 'MiningRig' },
      { uuid: 'uuid-2', name: 'Steel Refinery', subType: 'Refinery' },
    ],
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  }),
}));

import { publicApi } from '../../api/endpoints/public';

const mockedGetBlueprintDetail = vi.mocked(publicApi.getBlueprintDetail);

function renderWithProviders() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ColonyPlanner />
    </QueryClientProvider>
  );
}

const validBlueprintDetail = {
  uuid: 'uuid-1',
  name: 'Iron Mining Rig',
  bluePrintType: 'Flatpacks/MiningRig',
  properties: {
    'Power Provided': '0',
    'Power Required': '50',
    'Habitation Provision': '0',
    'Food Provision': '10',
    'Entertainment Provided': '0',
    'Warehouse Capacity': '100',
    'Blue Collar Detail': '5',
    'White Collar Detail': '2',
    'Specialist Detail': '1',
  },
};

describe('ColonyPlanner integration', () => {
  beforeEach(() => {
    usePlannerStore.setState({ structures: [], blueprintCache: {}, status: null });
    vi.clearAllMocks();
  });

  it('adding a structure updates status display', async () => {
    mockedGetBlueprintDetail.mockResolvedValue(validBlueprintDetail);
    renderWithProviders();

    // Select a flatpack from the list
    const option = screen.getByText('Iron Mining Rig');
    fireEvent.click(option);

    // Click Add button
    const addButton = screen.getByRole('button', { name: /add/i });
    fireEvent.click(addButton);

    // Wait for status display to show values (non-empty state)
    await waitFor(() => {
      expect(screen.getByText('Colony Status')).toBeInTheDocument();
      // Status bar shows "50 / 50" for Power (powerRequired=50, powerRequired from Online=50)
      // The structure is added as Staged by default, so workers=0
      // Food provision = 10, food required = 0 (staged contributes no workers)
      expect(screen.getByText('10 / 0')).toBeInTheDocument();
    });
  });

  it('removing a structure updates status display', async () => {
    // Pre-populate store with a structure
    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Online',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });

    renderWithProviders();

    // Verify status is showing (structure is Online, so power required = 50)
    expect(screen.getByText('0 / 50')).toBeInTheDocument();

    // Click Remove on the structure
    const removeButton = screen.getByRole('button', { name: /remove/i });
    fireEvent.click(removeButton);

    // Status should reset to empty state prompt
    await waitFor(() => {
      expect(screen.getByText('Add structures to see colony status.')).toBeInTheDocument();
    });
  });

  it('clear plan resets to empty state', () => {
    // Pre-populate store with structures
    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Online',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });

    // Mock window.confirm to return true
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderWithProviders();

    // Verify structure is displayed in the structure list
    expect(screen.getByRole('listitem', { name: /iron mining rig/i })).toBeInTheDocument();

    // Click Clear Plan button
    const clearButton = screen.getByRole('button', { name: /clear plan/i });
    fireEvent.click(clearButton);

    // Verify structures list is empty and status shows empty state
    expect(screen.getByText('Add structures to see colony status.')).toBeInTheDocument();
    expect(screen.getByText(/no structures added yet/i)).toBeInTheDocument();
  });

  it('confirmation dialog appears when clearing non-empty plan', () => {
    // Pre-populate store with a structure
    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Staged',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

    renderWithProviders();

    // Click Clear Plan
    const clearButton = screen.getByRole('button', { name: /clear plan/i });
    fireEvent.click(clearButton);

    // Verify window.confirm was called
    expect(confirmSpy).toHaveBeenCalledWith(
      'Are you sure you want to clear all structures from your plan?'
    );

    // Since confirm returned false, structure should still be there
    expect(screen.getByRole('listitem', { name: /iron mining rig/i })).toBeInTheDocument();
  });

  it('error display when blueprint detail fetch fails', async () => {
    mockedGetBlueprintDetail.mockRejectedValue(new Error('Network error'));
    renderWithProviders();

    // Select a flatpack from the list
    const option = screen.getByText('Iron Mining Rig');
    fireEvent.click(option);

    // Click Add button
    const addButton = screen.getByRole('button', { name: /add/i });
    fireEvent.click(addButton);

    // Wait for error message to appear
    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });

    // Structure should NOT be added to the list
    expect(screen.getByText(/no structures added yet/i)).toBeInTheDocument();
  });
});


describe('Optimize Build Order button', () => {
  beforeEach(() => {
    usePlannerStore.setState({ structures: [], blueprintCache: {}, status: null });
    vi.clearAllMocks();
  });

  it('displays "Optimize Build Order" button', () => {
    renderWithProviders();
    expect(screen.getByRole('button', { name: /optimize build order/i })).toBeInTheDocument();
  });

  it('button is disabled when structure list is empty', () => {
    renderWithProviders();
    const button = screen.getByRole('button', { name: /optimize build order/i });
    expect(button).toBeDisabled();
  });

  it('button is disabled when structure list has only 1 item', () => {
    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Staged',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });

    renderWithProviders();
    const button = screen.getByRole('button', { name: /optimize build order/i });
    expect(button).toBeDisabled();
  });

  it('button is enabled when structure list has 2+ items', () => {
    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Staged',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });
    usePlannerStore.getState().addStructure({
      id: 'test-id-2',
      blueprintUUID: 'uuid-2',
      name: 'Steel Refinery',
      subType: 'Refinery',
      state: 'Staged',
      buildQueuePosition: 2,
      properties: {
        powerProvided: 0,
        powerRequired: 30,
        habitationProvision: 0,
        foodProvision: 5,
        entertainmentProvided: 0,
        warehouseCapacity: 50,
        workerSlots: 4,
      },
    });

    renderWithProviders();
    const button = screen.getByRole('button', { name: /optimize build order/i });
    expect(button).not.toBeDisabled();
  });

  it('shows spinner and disables button during request', async () => {
    // Use a deferred promise to control when the mutation resolves
    let resolveRequest!: (value: unknown) => void;
    mockMutateAsync.mockImplementation(
      () => new Promise((resolve) => { resolveRequest = resolve; })
    );

    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Staged',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });
    usePlannerStore.getState().addStructure({
      id: 'test-id-2',
      blueprintUUID: 'uuid-2',
      name: 'Steel Refinery',
      subType: 'Refinery',
      state: 'Staged',
      buildQueuePosition: 2,
      properties: {
        powerProvided: 0,
        powerRequired: 30,
        habitationProvision: 0,
        foodProvision: 5,
        entertainmentProvided: 0,
        warehouseCapacity: 50,
        workerSlots: 4,
      },
    });

    renderWithProviders();
    const button = screen.getByRole('button', { name: /optimize build order/i });
    fireEvent.click(button);

    // Button should now show "Optimizing…" and be disabled
    await waitFor(() => {
      expect(screen.getByText('Optimizing…')).toBeInTheDocument();
    });
    const optimizingButton = screen.getByRole('button', { name: /optimizing/i });
    expect(optimizingButton).toBeDisabled();

    // Resolve the request to clean up
    resolveRequest({ optimizedOrder: [] });
    await waitFor(() => {
      expect(screen.getByText('Optimize Build Order')).toBeInTheDocument();
    });
  });

  it('shows error message on API failure without modifying structure list', async () => {
    mockMutateAsync.mockRejectedValue(new Error('Server unavailable'));

    usePlannerStore.getState().addStructure({
      id: 'test-id-1',
      blueprintUUID: 'uuid-1',
      name: 'Iron Mining Rig',
      subType: 'MiningRig',
      state: 'Staged',
      buildQueuePosition: 1,
      properties: {
        powerProvided: 0,
        powerRequired: 50,
        habitationProvision: 0,
        foodProvision: 10,
        entertainmentProvided: 0,
        warehouseCapacity: 100,
        workerSlots: 8,
      },
    });
    usePlannerStore.getState().addStructure({
      id: 'test-id-2',
      blueprintUUID: 'uuid-2',
      name: 'Steel Refinery',
      subType: 'Refinery',
      state: 'Staged',
      buildQueuePosition: 2,
      properties: {
        powerProvided: 0,
        powerRequired: 30,
        habitationProvision: 0,
        foodProvision: 5,
        entertainmentProvided: 0,
        warehouseCapacity: 50,
        workerSlots: 4,
      },
    });

    renderWithProviders();
    const button = screen.getByRole('button', { name: /optimize build order/i });
    fireEvent.click(button);

    // Wait for error message to appear
    await waitFor(() => {
      expect(screen.getByText('Server unavailable')).toBeInTheDocument();
    });

    // Error should be in the red banner
    const errorElement = screen.getByText('Server unavailable');
    expect(errorElement).toHaveClass('text-red-300');

    // Structures should remain unchanged
    const structures = usePlannerStore.getState().structures;
    expect(structures).toHaveLength(2);
    expect(structures[0].buildQueuePosition).toBe(1);
    expect(structures[1].buildQueuePosition).toBe(2);
  });
});
