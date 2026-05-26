/**
 * Zustand store for the Ship Template Builder page.
 * Manages blueprint data, build state (hull + components), computed stats,
 * and URL sharing. All blueprint detail fetches are cached to avoid redundant API calls.
 */

import { create } from 'zustand';
import { type ComponentSlot, generateSlotsFromHull } from './slotTypes';
import { computeShipStats, type ShipStats, type BlueprintDetail } from './computeShipStats';
import { decodeBuild, type DecodeError } from './urlCodec';
import { publicApi, type PaginatedResponse } from '../../api/endpoints/public';

export type { BlueprintDetail } from './computeShipStats';

export interface BlueprintSummary {
  uuid: string;
  name: string;              // extendedName from API
  bluePrintType: string;
  class: number;
}

export interface ShipBuilderState {
  // Blueprint data
  blueprintList: BlueprintSummary[];
  blueprintCache: Record<string, BlueprintDetail>;
  blueprintListLoading: boolean;
  blueprintListError: string | null;

  // Build state
  selectedHullUUID: string | null;
  hullDetail: BlueprintDetail | null;
  slots: ComponentSlot[];
  stats: ShipStats | null;

  // Loading/error per-slot
  loadingUUIDs: Set<string>;
  slotErrors: Record<string, string>;

  // Actions
  loadBlueprintList: () => Promise<void>;
  selectHull: (uuid: string) => Promise<void>;
  installComponent: (slotType: string, slotIndex: number, uuid: string | null) => Promise<void>;
  clearBuild: () => void;
  restoreFromURL: (hash: string) => Promise<void>;
}


/** Helper: fetch blueprint detail with caching and duplicate request prevention */
async function fetchDetail(
  uuid: string,
  get: () => ShipBuilderState,
  set: (partial: Partial<ShipBuilderState> | ((state: ShipBuilderState) => Partial<ShipBuilderState>)) => void,
): Promise<BlueprintDetail | null> {
  // Check cache first
  const cached = get().blueprintCache[uuid];
  if (cached) return cached;

  // Check if already loading (duplicate request prevention)
  if (get().loadingUUIDs.has(uuid)) {
    // Wait for the existing request to complete by polling cache
    return new Promise<BlueprintDetail | null>((resolve) => {
      const interval = setInterval(() => {
        const state = get();
        if (!state.loadingUUIDs.has(uuid)) {
          clearInterval(interval);
          resolve(state.blueprintCache[uuid] ?? null);
        }
      }, 50);
      // Safety timeout after 30s
      setTimeout(() => {
        clearInterval(interval);
        resolve(get().blueprintCache[uuid] ?? null);
      }, 30000);
    });
  }

  // Mark as loading
  set((state) => ({
    loadingUUIDs: new Set([...state.loadingUUIDs, uuid]),
  }));

  try {
    const response = await publicApi.getBlueprintDetail(uuid);
    const detail: BlueprintDetail = {
      uuid: response.uuid as string,
      name: (response.extendedName as string) || (response.name as string),
      bluePrintType: response.bluePrintType as string,
      class: (response.shipClass as number) ?? (response.class as number),
      properties: response.properties as Record<string, string>,
    };

    // Cache the detail
    set((state) => ({
      blueprintCache: { ...state.blueprintCache, [uuid]: detail },
      loadingUUIDs: new Set([...state.loadingUUIDs].filter((id) => id !== uuid)),
    }));

    return detail;
  } catch {
    // Remove from loading set on failure
    set((state) => ({
      loadingUUIDs: new Set([...state.loadingUUIDs].filter((id) => id !== uuid)),
    }));
    return null;
  }
}


/** Helper: recompute stats from current hull and installed components */
function recomputeStats(state: ShipBuilderState): ShipStats | null {
  if (!state.hullDetail) return null;

  const componentDetails: (BlueprintDetail | null)[] = state.slots.map((slot) => {
    if (!slot.blueprintUUID) return null;
    return state.blueprintCache[slot.blueprintUUID] ?? null;
  });

  return computeShipStats(state.hullDetail, componentDetails);
}

export const useShipBuilderStore = create<ShipBuilderState>((set, get) => ({
  // Initial state
  blueprintList: [],
  blueprintCache: {},
  blueprintListLoading: false,
  blueprintListError: null,
  selectedHullUUID: null,
  hullDetail: null,
  slots: [],
  stats: null,
  loadingUUIDs: new Set<string>(),
  slotErrors: {},

  loadBlueprintList: async () => {
    set({ blueprintListLoading: true, blueprintListError: null });
    try {
      const response = await publicApi.getPublicBlueprints(undefined, 1, 10000) as PaginatedResponse<{
        uuid: string;
        name: string;
        extendedName: string;
        bluePrintType: string;
        class: number;
      }>;

      const blueprintList: BlueprintSummary[] = response.items.map((item) => ({
        uuid: item.uuid,
        name: item.extendedName || item.name,
        bluePrintType: item.bluePrintType,
        class: item.class,
      }));

      set({ blueprintList, blueprintListLoading: false, blueprintListError: null });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load blueprint list';
      set({ blueprintListLoading: false, blueprintListError: message });
    }
  },


  selectHull: async (uuid: string) => {
    const detail = await fetchDetail(uuid, get, set);
    if (!detail) {
      set({ blueprintListError: `Failed to load hull detail for ${uuid}` });
      return;
    }

    const slots = generateSlotsFromHull(detail.properties);
    const newState: Partial<ShipBuilderState> = {
      selectedHullUUID: uuid,
      hullDetail: detail,
      slots,
      slotErrors: {},
    };

    set(newState);

    // Compute stats with hull only (no components installed yet)
    const stats = recomputeStats({ ...get(), ...newState } as ShipBuilderState);
    set({ stats });
  },

  installComponent: async (slotType: string, slotIndex: number, uuid: string | null) => {
    const state = get();
    const slotKey = `${slotType}-${slotIndex}`;

    // Clear component from slot
    if (uuid === null) {
      const updatedSlots = state.slots.map((slot) =>
        slot.slotType === slotType && slot.slotIndex === slotIndex
          ? { ...slot, blueprintUUID: null }
          : slot,
      );
      // Clear any error for this slot
      const { [slotKey]: _removed, ...remainingErrors } = state.slotErrors;
      set({ slots: updatedSlots, slotErrors: remainingErrors });
      const stats = recomputeStats({ ...get(), slots: updatedSlots });
      set({ stats });
      return;
    }

    // Fetch detail (uses cache if available)
    const detail = await fetchDetail(uuid, get, set);
    if (!detail) {
      set((s) => ({
        slotErrors: { ...s.slotErrors, [slotKey]: `Failed to load component ${uuid}` },
      }));
      return;
    }

    // Update the slot with the component UUID
    const updatedSlots = get().slots.map((slot) =>
      slot.slotType === slotType && slot.slotIndex === slotIndex
        ? { ...slot, blueprintUUID: uuid }
        : slot,
    );

    // Clear any error for this slot
    const { [slotKey]: _removed, ...remainingErrors } = get().slotErrors;
    set({ slots: updatedSlots, slotErrors: remainingErrors });

    // Recompute stats
    const stats = recomputeStats({ ...get(), slots: updatedSlots });
    set({ stats });
  },


  clearBuild: () => {
    set({
      selectedHullUUID: null,
      hullDetail: null,
      slots: [],
      stats: null,
      slotErrors: {},
    });
  },

  restoreFromURL: async (hash: string) => {
    const result = decodeBuild(hash);

    // null means no build data in hash — do nothing
    if (result === null) return;

    // DecodeError — show error and start empty
    if ('type' in result) {
      const decodeError = result as DecodeError;
      set({ blueprintListError: decodeError.message });
      return;
    }

    // Valid decoded build — restore hull and components
    const { hullUUID, componentUUIDs } = result;

    // Select the hull first
    await get().selectHull(hullUUID);

    // If hull selection failed, stop
    if (!get().hullDetail) return;

    // Install components into their respective slot positions
    const slots = get().slots;
    for (let i = 0; i < componentUUIDs.length && i < slots.length; i++) {
      const compUUID = componentUUIDs[i];
      if (compUUID !== null) {
        const slot = slots[i];
        await get().installComponent(slot.slotType, slot.slotIndex, compUUID);
      }
    }
  },
}));
