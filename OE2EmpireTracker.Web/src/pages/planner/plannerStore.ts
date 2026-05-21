import { create } from 'zustand';
import type { PlannerStructure, ColonyStatusResult, BuildOrderResult } from '../../api/types/generated';

interface PlannerState {
  structures: PlannerStructure[];
  status: ColonyStatusResult | null;
  buildOrder: BuildOrderResult | null;
  isComputing: boolean;
  addStructure: (structure: PlannerStructure) => void;
  removeStructure: (index: number) => void;
  updateStructure: (index: number, structure: PlannerStructure) => void;
  setStructures: (structures: PlannerStructure[]) => void;
  setStatus: (status: ColonyStatusResult | null) => void;
  setBuildOrder: (buildOrder: BuildOrderResult | null) => void;
  setIsComputing: (isComputing: boolean) => void;
  reset: () => void;
}

const initialState = {
  structures: [] as PlannerStructure[],
  status: null as ColonyStatusResult | null,
  buildOrder: null as BuildOrderResult | null,
  isComputing: false,
};

export const usePlannerStore = create<PlannerState>()((set) => ({
  ...initialState,

  addStructure: (structure) =>
    set((state) => ({ structures: [...state.structures, structure] })),

  removeStructure: (index) =>
    set((state) => ({
      structures: state.structures.filter((_, i) => i !== index),
    })),

  updateStructure: (index, structure) =>
    set((state) => ({
      structures: state.structures.map((s, i) => (i === index ? structure : s)),
    })),

  setStructures: (structures) => set({ structures }),
  setStatus: (status) => set({ status }),
  setBuildOrder: (buildOrder) => set({ buildOrder }),
  setIsComputing: (isComputing) => set({ isComputing }),
  reset: () => set(initialState),
}));
