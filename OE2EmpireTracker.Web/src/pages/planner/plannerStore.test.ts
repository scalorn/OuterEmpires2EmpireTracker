import { describe, it, expect, beforeEach } from 'vitest';
import { usePlannerStore } from './plannerStore';
import type { PlannedStructure, BlueprintProperties } from './plannerStore';

function makeStructure(overrides?: Partial<PlannedStructure>): PlannedStructure {
  return {
    id: crypto.randomUUID(),
    blueprintUUID: 'bp-uuid-1',
    name: 'Test Mining Rig',
    subType: 'MiningRig',
    state: 'Staged',
    buildQueuePosition: 1,
    properties: {
      powerProvided: 100,
      powerRequired: 50,
      habitationProvision: 20,
      foodProvision: 15,
      entertainmentProvided: 10,
      warehouseCapacity: 500,
      workerSlots: 10,
    },
    ...overrides,
  };
}

describe('plannerStore', () => {
  beforeEach(() => {
    usePlannerStore.setState({ structures: [], blueprintCache: {}, status: null });
  });

  describe('addStructure', () => {
    it('adds a structure to the list and computes status', () => {
      const structure = makeStructure({ state: 'Online' });
      usePlannerStore.getState().addStructure(structure);

      const state = usePlannerStore.getState();
      expect(state.structures).toHaveLength(1);
      expect(state.structures[0]).toEqual(structure);
      expect(state.status).not.toBeNull();
    });

    it('grows the list with multiple additions and status reflects all', () => {
      const s1 = makeStructure({ state: 'Online', name: 'Rig A' });
      const s2 = makeStructure({ state: 'Built', name: 'Rig B' });

      usePlannerStore.getState().addStructure(s1);
      usePlannerStore.getState().addStructure(s2);

      const state = usePlannerStore.getState();
      expect(state.structures).toHaveLength(2);
      // Status reflects both: Online contributes power, Built contributes workers
      expect(state.status!.powerProvided).toBe(s1.properties.powerProvided);
      expect(state.status!.habitationRequired).toBe(
        s1.properties.workerSlots + s2.properties.workerSlots
      );
    });
  });

  describe('removeStructure', () => {
    it('removes by id and preserves others (Property 9)', () => {
      const s1 = makeStructure({ name: 'Keep A' });
      const s2 = makeStructure({ name: 'Remove Me' });
      const s3 = makeStructure({ name: 'Keep B' });

      usePlannerStore.getState().addStructure(s1);
      usePlannerStore.getState().addStructure(s2);
      usePlannerStore.getState().addStructure(s3);

      usePlannerStore.getState().removeStructure(s2.id);

      const state = usePlannerStore.getState();
      expect(state.structures).toHaveLength(2);
      expect(state.structures[0].id).toBe(s1.id);
      expect(state.structures[1].id).toBe(s3.id);
      expect(state.status).not.toBeNull();
    });

    it('sets status to null when last item is removed', () => {
      const s1 = makeStructure();
      usePlannerStore.getState().addStructure(s1);
      usePlannerStore.getState().removeStructure(s1.id);

      const state = usePlannerStore.getState();
      expect(state.structures).toHaveLength(0);
      expect(state.status).toBeNull();
    });
  });

  describe('setStructureState', () => {
    it('changes state of specific structure and recomputes status', () => {
      const s1 = makeStructure({ state: 'Staged' });
      usePlannerStore.getState().addStructure(s1);

      // Staged: no power provided
      expect(usePlannerStore.getState().status!.powerProvided).toBe(0);

      usePlannerStore.getState().setStructureState(s1.id, 'Online');

      const state = usePlannerStore.getState();
      expect(state.structures[0].state).toBe('Online');
      // Online: power provided kicks in
      expect(state.status!.powerProvided).toBe(s1.properties.powerProvided);
    });
  });

  describe('cacheBlueprint', () => {
    it('stores blueprint properties by UUID (Property 6)', () => {
      const props: BlueprintProperties = {
        powerProvided: 200,
        powerRequired: 80,
        habitationProvision: 30,
        foodProvision: 25,
        entertainmentProvided: 15,
        warehouseCapacity: 1000,
        workerSlots: 20,
      };

      usePlannerStore.getState().cacheBlueprint('bp-123', props);

      const state = usePlannerStore.getState();
      expect(state.blueprintCache['bp-123']).toEqual(props);
    });

    it('does not overwrite an existing cache entry (Property 6)', () => {
      const original: BlueprintProperties = {
        powerProvided: 200,
        powerRequired: 80,
        habitationProvision: 30,
        foodProvision: 25,
        entertainmentProvided: 15,
        warehouseCapacity: 1000,
        workerSlots: 20,
      };
      const duplicate: BlueprintProperties = {
        powerProvided: 999,
        powerRequired: 999,
        habitationProvision: 999,
        foodProvision: 999,
        entertainmentProvided: 999,
        warehouseCapacity: 999,
        workerSlots: 999,
      };

      usePlannerStore.getState().cacheBlueprint('bp-123', original);
      usePlannerStore.getState().cacheBlueprint('bp-123', duplicate);

      const state = usePlannerStore.getState();
      expect(state.blueprintCache['bp-123']).toEqual(original);
    });
  });

  describe('clearPlan', () => {
    it('empties structures, resets status to null, preserves cache', () => {
      const props: BlueprintProperties = {
        powerProvided: 100,
        powerRequired: 50,
        habitationProvision: 20,
        foodProvision: 15,
        entertainmentProvided: 10,
        warehouseCapacity: 500,
        workerSlots: 10,
      };

      usePlannerStore.getState().cacheBlueprint('bp-cached', props);
      usePlannerStore.getState().addStructure(makeStructure({ state: 'Online' }));

      // Verify pre-conditions
      expect(usePlannerStore.getState().structures.length).toBeGreaterThan(0);
      expect(usePlannerStore.getState().status).not.toBeNull();

      usePlannerStore.getState().clearPlan();

      const state = usePlannerStore.getState();
      expect(state.structures).toHaveLength(0);
      expect(state.status).toBeNull();
      expect(state.blueprintCache['bp-cached']).toEqual(props);
    });
  });
});
