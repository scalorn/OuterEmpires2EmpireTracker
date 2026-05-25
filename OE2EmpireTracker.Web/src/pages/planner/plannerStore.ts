import { create } from 'zustand';
import { computeColonyStatus } from './computeColonyStatus';
import type { PlannedStructure, ColonyStatus, StructureState } from './computeColonyStatus';
import type { BlueprintProperties } from '../../utils/blueprintHelpers';
import type { PlannerStructure } from '../../api/types/generated';

export type { PlannedStructure, ColonyStatus, StructureState } from './computeColonyStatus';
export type { BlueprintProperties } from '../../utils/blueprintHelpers';

export interface OptimizedOrderEntry {
  flatpackBlueprintUUID: string;
  buildQueueSequence: number;
}

/**
 * Maps local PlannedStructure[] to the wire format expected by the build-order endpoint.
 * Pure function extracted for testability.
 */
export function mapToWireFormat(structures: PlannedStructure[]): PlannerStructure[] {
  return structures.map((s) => ({
    flatpackBlueprintUUID: s.blueprintUUID,
    isBuilt: s.state === 'Built',
    isStaged: s.state === 'Staged',
    isOnline: s.state === 'Online',
    buildQueueSequence: s.buildQueuePosition,
    assignedWorkers: {},
  }));
}

export interface PlannerState {
  structures: PlannedStructure[];
  blueprintCache: Record<string, BlueprintProperties>;
  status: ColonyStatus | null;
  addStructure: (structure: PlannedStructure) => void;
  removeStructure: (id: string) => void;
  setStructureState: (id: string, state: StructureState) => void;
  cacheBlueprint: (uuid: string, properties: BlueprintProperties) => void;
  clearPlan: () => void;
  moveStructureUp: (id: string) => void;
  moveStructureDown: (id: string) => void;
  applyOptimizedOrder: (optimizedOrder: OptimizedOrderEntry[]) => void;
  replaceStructures: (structures: PlannedStructure[]) => void;
}

export const usePlannerStore = create<PlannerState>()((set) => ({
  structures: [],
  blueprintCache: {},
  status: null,

  addStructure: (structure) =>
    set((state) => {
      const structures = [...state.structures, structure];
      return { structures, status: computeColonyStatus(structures) };
    }),

  removeStructure: (id) =>
    set((state) => {
      const structures = state.structures.filter((s) => s.id !== id);
      return { structures, status: computeColonyStatus(structures) };
    }),

  setStructureState: (id, newState) =>
    set((state) => {
      const structures = state.structures.map((s) =>
        s.id === id ? { ...s, state: newState } : s
      );
      return { structures, status: computeColonyStatus(structures) };
    }),

  cacheBlueprint: (uuid, properties) =>
    set((state) => {
      if (state.blueprintCache[uuid]) return state;
      return {
        blueprintCache: { ...state.blueprintCache, [uuid]: properties },
      };
    }),

  clearPlan: () => set({ structures: [], status: null }),

  moveStructureUp: (id) =>
    set((state) => {
      const target = state.structures.find((s) => s.id === id);
      if (!target) return state;
      // No-op if target is CC
      if (target.subType === 'ColonyCommandCentre') return state;

      // Find the neighbor with the next-lower buildQueuePosition
      const neighborsAbove = state.structures.filter(
        (s) => s.buildQueuePosition < target.buildQueuePosition
      );
      if (neighborsAbove.length === 0) return state;

      const neighbor = neighborsAbove.reduce((closest, s) =>
        s.buildQueuePosition > closest.buildQueuePosition ? s : closest
      );

      // No-op if neighbor is CC (CC protection: nothing can move above CC)
      if (neighbor.subType === 'ColonyCommandCentre') return state;

      // Swap positions
      const structures = state.structures.map((s) => {
        if (s.id === target.id)
          return { ...s, buildQueuePosition: neighbor.buildQueuePosition };
        if (s.id === neighbor.id)
          return { ...s, buildQueuePosition: target.buildQueuePosition };
        return s;
      });
      return { structures, status: computeColonyStatus(structures) };
    }),

  moveStructureDown: (id) =>
    set((state) => {
      const target = state.structures.find((s) => s.id === id);
      if (!target) return state;
      // No-op if target is CC
      if (target.subType === 'ColonyCommandCentre') return state;

      // Find the neighbor with the next-higher buildQueuePosition
      const neighborsBelow = state.structures.filter(
        (s) => s.buildQueuePosition > target.buildQueuePosition
      );
      if (neighborsBelow.length === 0) return state;

      const neighbor = neighborsBelow.reduce((closest, s) =>
        s.buildQueuePosition < closest.buildQueuePosition ? s : closest
      );

      // Swap positions
      const structures = state.structures.map((s) => {
        if (s.id === target.id)
          return { ...s, buildQueuePosition: neighbor.buildQueuePosition };
        if (s.id === neighbor.id)
          return { ...s, buildQueuePosition: target.buildQueuePosition };
        return s;
      });
      return { structures, status: computeColonyStatus(structures) };
    }),

  applyOptimizedOrder: (optimizedOrder) =>
    set((state) => {
      const structures = state.structures.map((s) => {
        const entry = optimizedOrder.find(
          (e) => e.flatpackBlueprintUUID === s.blueprintUUID
        );
        if (entry) {
          return { ...s, buildQueuePosition: entry.buildQueueSequence };
        }
        return s;
      });
      return { structures, status: computeColonyStatus(structures) };
    }),

  replaceStructures: (structures) =>
    set(() => ({
      structures,
      status: computeColonyStatus(structures),
    })),
}));
