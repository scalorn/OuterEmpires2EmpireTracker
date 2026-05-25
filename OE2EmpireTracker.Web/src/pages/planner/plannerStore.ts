import { create } from 'zustand';
import { computeColonyStatus } from './computeColonyStatus';
import type { PlannedStructure, ColonyStatus, StructureState } from './computeColonyStatus';
import type { BlueprintProperties } from '../../utils/blueprintHelpers';

export type { PlannedStructure, ColonyStatus, StructureState } from './computeColonyStatus';
export type { BlueprintProperties } from '../../utils/blueprintHelpers';

export interface PlannerState {
  structures: PlannedStructure[];
  blueprintCache: Record<string, BlueprintProperties>;
  status: ColonyStatus | null;
  addStructure: (structure: PlannedStructure) => void;
  removeStructure: (id: string) => void;
  setStructureState: (id: string, state: StructureState) => void;
  cacheBlueprint: (uuid: string, properties: BlueprintProperties) => void;
  clearPlan: () => void;
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
}));
