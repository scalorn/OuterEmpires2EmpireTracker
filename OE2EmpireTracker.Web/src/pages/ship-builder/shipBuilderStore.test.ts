import { vi, describe, it, expect, beforeEach } from 'vitest';
import { useShipBuilderStore } from './shipBuilderStore';

// Mock the public API module
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
const mockHullResponse = {
  uuid: 'hull-001',
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

const mockReactorResponse = {
  uuid: 'reactor-001',
  name: 'Reactor Alpha',
  extendedName: 'Reactor Alpha Mk I',
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
});

describe('shipBuilderStore', () => {
  describe('loadBlueprintList', () => {
    it('sets blueprintList on success', async () => {
      mockGetBlueprints.mockResolvedValue({
        items: [
          { uuid: 'hull-001', name: 'Corvette', extendedName: 'Corvette Mk II', bluePrintType: 'Hull', class: 4 },
          { uuid: 'reactor-001', name: 'Reactor', extendedName: 'Reactor Alpha', bluePrintType: 'Reactor', class: 4 },
        ],
        totalCount: 2,
        page: 1,
        pageSize: 10000,
      } as never);

      await useShipBuilderStore.getState().loadBlueprintList();

      const state = useShipBuilderStore.getState();
      expect(state.blueprintList).toHaveLength(2);
      expect(state.blueprintList[0]).toEqual({
        uuid: 'hull-001',
        name: 'Corvette Mk II',
        bluePrintType: 'Hull',
        class: 4,
      });
      expect(state.blueprintListLoading).toBe(false);
      expect(state.blueprintListError).toBeNull();
    });

    it('sets error message on failure', async () => {
      mockGetBlueprints.mockRejectedValue(new Error('Network error'));

      await useShipBuilderStore.getState().loadBlueprintList();

      const state = useShipBuilderStore.getState();
      expect(state.blueprintListLoading).toBe(false);
      expect(state.blueprintListError).toBe('Network error');
      expect(state.blueprintList).toHaveLength(0);
    });
  });


  describe('selectHull', () => {
    it('sets hullDetail, generates slots, and computes stats on success', async () => {
      mockGetDetail.mockResolvedValue(mockHullResponse as never);

      await useShipBuilderStore.getState().selectHull('hull-001');

      const state = useShipBuilderStore.getState();
      expect(state.selectedHullUUID).toBe('hull-001');
      expect(state.hullDetail).not.toBeNull();
      expect(state.hullDetail!.uuid).toBe('hull-001');
      expect(state.hullDetail!.name).toBe('Corvette Mk II');
      // Hull has 2 Reactor slots + 1 Shield slot = 3 total slots
      expect(state.slots).toHaveLength(3);
      expect(state.slots.filter(s => s.slotType === 'Reactor')).toHaveLength(2);
      expect(state.slots.filter(s => s.slotType === 'Shield')).toHaveLength(1);
      expect(state.stats).not.toBeNull();
    });

    it('uses cache if hull was already fetched', async () => {
      mockGetDetail.mockResolvedValue(mockHullResponse as never);

      // First call fetches from API
      await useShipBuilderStore.getState().selectHull('hull-001');
      expect(mockGetDetail).toHaveBeenCalledTimes(1);

      // Second call should use cache
      await useShipBuilderStore.getState().selectHull('hull-001');
      expect(mockGetDetail).toHaveBeenCalledTimes(1);
    });

    it('sets error when fetch fails', async () => {
      mockGetDetail.mockRejectedValue(new Error('Not found'));

      await useShipBuilderStore.getState().selectHull('hull-001');

      const state = useShipBuilderStore.getState();
      expect(state.hullDetail).toBeNull();
      expect(state.blueprintListError).toBe('Failed to load hull detail for hull-001');
    });
  });


  describe('installComponent', () => {
    beforeEach(async () => {
      // Set up a hull first
      mockGetDetail.mockResolvedValueOnce(mockHullResponse as never);
      await useShipBuilderStore.getState().selectHull('hull-001');
      vi.clearAllMocks();
    });

    it('updates slot and recomputes stats on success', async () => {
      mockGetDetail.mockResolvedValue(mockReactorResponse as never);

      await useShipBuilderStore.getState().installComponent('Reactor', 0, 'reactor-001');

      const state = useShipBuilderStore.getState();
      const reactorSlot = state.slots.find(s => s.slotType === 'Reactor' && s.slotIndex === 0);
      expect(reactorSlot!.blueprintUUID).toBe('reactor-001');
      expect(state.stats).not.toBeNull();
      // Stats should include reactor mass contribution
      expect(state.stats!.totalMass).toBeGreaterThan(500);
    });

    it('clears slot and recomputes stats when uuid is null', async () => {
      // Install a component first
      mockGetDetail.mockResolvedValue(mockReactorResponse as never);
      await useShipBuilderStore.getState().installComponent('Reactor', 0, 'reactor-001');

      const statsBefore = useShipBuilderStore.getState().stats;
      expect(statsBefore!.totalMass).toBeGreaterThan(500);

      // Clear the slot
      await useShipBuilderStore.getState().installComponent('Reactor', 0, null);

      const state = useShipBuilderStore.getState();
      const reactorSlot = state.slots.find(s => s.slotType === 'Reactor' && s.slotIndex === 0);
      expect(reactorSlot!.blueprintUUID).toBeNull();
      expect(state.stats!.totalMass).toBe(500);
    });

    it('uses cache for previously fetched component', async () => {
      mockGetDetail.mockResolvedValue(mockReactorResponse as never);

      // Install in first slot
      await useShipBuilderStore.getState().installComponent('Reactor', 0, 'reactor-001');
      expect(mockGetDetail).toHaveBeenCalledTimes(1);

      // Install same component in second slot — should use cache
      await useShipBuilderStore.getState().installComponent('Reactor', 1, 'reactor-001');
      expect(mockGetDetail).toHaveBeenCalledTimes(1);
    });

    it('sets slotError without losing build state on failure', async () => {
      // Install a working component first
      mockGetDetail.mockResolvedValueOnce(mockReactorResponse as never);
      await useShipBuilderStore.getState().installComponent('Reactor', 0, 'reactor-001');

      // Fail on second slot
      mockGetDetail.mockRejectedValueOnce(new Error('Server error'));
      await useShipBuilderStore.getState().installComponent('Shield', 0, 'shield-bad');

      const state = useShipBuilderStore.getState();
      // First slot still has its component
      const reactorSlot = state.slots.find(s => s.slotType === 'Reactor' && s.slotIndex === 0);
      expect(reactorSlot!.blueprintUUID).toBe('reactor-001');
      // Error set on the failed slot
      expect(state.slotErrors['Shield-0']).toBe('Failed to load component shield-bad');
      // Hull still selected
      expect(state.selectedHullUUID).toBe('hull-001');
    });
  });


  describe('clearBuild', () => {
    it('resets hull, slots, stats, and errors but preserves blueprintList and cache', async () => {
      // Set up a full build
      mockGetDetail.mockResolvedValueOnce(mockHullResponse as never);
      await useShipBuilderStore.getState().selectHull('hull-001');
      mockGetDetail.mockResolvedValueOnce(mockReactorResponse as never);
      await useShipBuilderStore.getState().installComponent('Reactor', 0, 'reactor-001');

      // Set some blueprint list data
      useShipBuilderStore.setState({
        blueprintList: [{ uuid: 'hull-001', name: 'Corvette Mk II', bluePrintType: 'Hull', class: 4 }],
      });

      const cacheBefore = { ...useShipBuilderStore.getState().blueprintCache };
      expect(Object.keys(cacheBefore).length).toBeGreaterThan(0);

      // Clear the build
      useShipBuilderStore.getState().clearBuild();

      const state = useShipBuilderStore.getState();
      expect(state.selectedHullUUID).toBeNull();
      expect(state.hullDetail).toBeNull();
      expect(state.slots).toHaveLength(0);
      expect(state.stats).toBeNull();
      expect(state.slotErrors).toEqual({});
      // Preserved
      expect(state.blueprintList).toHaveLength(1);
      expect(Object.keys(state.blueprintCache).length).toBeGreaterThan(0);
    });
  });

  describe('blueprint cache behavior', () => {
    it('prevents redundant API calls for the same UUID', async () => {
      mockGetDetail.mockResolvedValue(mockHullResponse as never);

      // Fetch hull-001 twice
      await useShipBuilderStore.getState().selectHull('hull-001');
      await useShipBuilderStore.getState().selectHull('hull-001');

      // Only one API call should have been made
      expect(mockGetDetail).toHaveBeenCalledTimes(1);

      // Cache should contain the detail
      const cache = useShipBuilderStore.getState().blueprintCache;
      expect(cache['hull-001']).toBeDefined();
      expect(cache['hull-001'].uuid).toBe('hull-001');
    });
  });
});
