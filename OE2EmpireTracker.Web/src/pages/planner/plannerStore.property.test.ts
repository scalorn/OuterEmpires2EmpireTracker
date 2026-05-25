/**
 * Property-based tests for plannerStore reorder actions.
 * Uses fast-check to verify correctness properties hold across all valid inputs.
 *
 * Validates: Requirements 1.1, 1.2, 1.8, 2.3, 2.4, 2.9
 */
import { describe, it, expect, beforeEach } from 'vitest';
import fc from 'fast-check';
import { usePlannerStore } from './plannerStore';
import { computeColonyStatus } from './computeColonyStatus';
import type { PlannedStructure, OptimizedOrderEntry } from './plannerStore';

// --- Arbitraries ---

const arbBlueprintProperties = fc.record({
  powerProvided: fc.nat({ max: 1000 }),
  powerRequired: fc.nat({ max: 500 }),
  habitationProvision: fc.nat({ max: 200 }),
  foodProvision: fc.nat({ max: 200 }),
  entertainmentProvided: fc.nat({ max: 100 }),
  warehouseCapacity: fc.nat({ max: 5000 }),
  workerSlots: fc.nat({ max: 50 }),
});

const arbStructureState = fc.constantFrom(
  'Staged' as const,
  'Built' as const,
  'Online' as const
);

const arbNonCCSubType = fc.constantFrom(
  'MiningRig',
  'Refinery',
  'ResearchLaboratory',
  'Manufactory',
  'ReactorCore',
  'HabitationBlock'
);

/**
 * Generates a list of 2+ planned structures with unique, sequential buildQueuePositions.
 * All structures are non-CC to allow free movement.
 */
const arbNonCCStructureList = fc
  .array(
    fc.record({
      id: fc.uuid(),
      blueprintUUID: fc.uuid(),
      name: fc.string({ minLength: 1, maxLength: 30 }),
      subType: arbNonCCSubType,
      state: arbStructureState,
      properties: arbBlueprintProperties,
    }),
    { minLength: 2, maxLength: 15 }
  )
  .map((items) =>
    items.map((item, idx) => ({
      ...item,
      buildQueuePosition: idx + 1,
    }))
  );

/**
 * Generates a list with a CC at position 1 and 1+ non-CC structures after it.
 */
const arbStructureListWithCC = fc
  .tuple(
    fc.record({
      id: fc.uuid(),
      blueprintUUID: fc.uuid(),
      name: fc.constant('Colony Command Centre'),
      subType: fc.constant('ColonyCommandCentre'),
      state: arbStructureState,
      properties: arbBlueprintProperties,
    }),
    fc.array(
      fc.record({
        id: fc.uuid(),
        blueprintUUID: fc.uuid(),
        name: fc.string({ minLength: 1, maxLength: 30 }),
        subType: arbNonCCSubType,
        state: arbStructureState,
        properties: arbBlueprintProperties,
      }),
      { minLength: 1, maxLength: 14 }
    )
  )
  .map(([cc, rest]) => [
    { ...cc, buildQueuePosition: 1 },
    ...rest.map((item, idx) => ({ ...item, buildQueuePosition: idx + 2 })),
  ]);

// --- Helper ---

function resetStore(structures: PlannedStructure[]) {
  usePlannerStore.setState({
    structures,
    status: computeColonyStatus(structures),
    blueprintCache: {},
  });
}

// --- Properties ---

describe('plannerStore property-based tests', () => {
  beforeEach(() => {
    usePlannerStore.setState({
      structures: [],
      status: null,
      blueprintCache: {},
    });
  });

  // Feature: colony-planner-reorder, Property 1: Move swap preserves all other positions
  /**
   * Property 1: Move swap preserves all other positions
   *
   * For any list of 2+ planned structures and any valid move operation (up or down)
   * on a non-CC structure that is not at the boundary, the operation SHALL swap the
   * `buildQueuePosition` of exactly two structures (the target and its neighbor)
   * and leave all other structures' positions unchanged.
   *
   * **Validates: Requirements 1.1, 1.2**
   */
  describe('Property 1: Move swap preserves all other positions', () => {
    it('moveStructureUp swaps only target and neighbor positions', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          // Pick a structure that is NOT at position 1 (can move up)
          const moveable = structures.filter((s) => s.buildQueuePosition > 1);
          if (moveable.length === 0) return; // skip degenerate case

          const target = moveable[0];
          resetStore(structures as PlannedStructure[]);

          // Find the neighbor above (highest position below target)
          const neighborAbove = structures
            .filter((s) => s.buildQueuePosition < target.buildQueuePosition)
            .reduce((best, s) =>
              s.buildQueuePosition > best.buildQueuePosition ? s : best
            );

          usePlannerStore.getState().moveStructureUp(target.id);
          const result = usePlannerStore.getState().structures;

          // Target should now have neighbor's old position
          const movedTarget = result.find((s) => s.id === target.id)!;
          expect(movedTarget.buildQueuePosition).toBe(neighborAbove.buildQueuePosition);

          // Neighbor should now have target's old position
          const movedNeighbor = result.find((s) => s.id === neighborAbove.id)!;
          expect(movedNeighbor.buildQueuePosition).toBe(target.buildQueuePosition);

          // All other structures unchanged
          for (const s of structures) {
            if (s.id === target.id || s.id === neighborAbove.id) continue;
            const resultS = result.find((r) => r.id === s.id)!;
            expect(resultS.buildQueuePosition).toBe(s.buildQueuePosition);
          }
        }),
        { numRuns: 100 }
      );
    });

    it('moveStructureDown swaps only target and neighbor positions', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          // Pick a structure that is NOT at the last position (can move down)
          const maxPos = Math.max(...structures.map((s) => s.buildQueuePosition));
          const moveable = structures.filter(
            (s) => s.buildQueuePosition < maxPos
          );
          if (moveable.length === 0) return;

          const target = moveable[0];
          resetStore(structures as PlannedStructure[]);

          // Find the neighbor below (lowest position above target)
          const neighborBelow = structures
            .filter((s) => s.buildQueuePosition > target.buildQueuePosition)
            .reduce((best, s) =>
              s.buildQueuePosition < best.buildQueuePosition ? s : best
            );

          usePlannerStore.getState().moveStructureDown(target.id);
          const result = usePlannerStore.getState().structures;

          // Target should now have neighbor's old position
          const movedTarget = result.find((s) => s.id === target.id)!;
          expect(movedTarget.buildQueuePosition).toBe(neighborBelow.buildQueuePosition);

          // Neighbor should now have target's old position
          const movedNeighbor = result.find((s) => s.id === neighborBelow.id)!;
          expect(movedNeighbor.buildQueuePosition).toBe(target.buildQueuePosition);

          // All other structures unchanged
          for (const s of structures) {
            if (s.id === target.id || s.id === neighborBelow.id) continue;
            const resultS = result.find((r) => r.id === s.id)!;
            expect(resultS.buildQueuePosition).toBe(s.buildQueuePosition);
          }
        }),
        { numRuns: 100 }
      );
    });

    it('moveStructureUp with CC present: CC-protected structure is no-op, all positions unchanged', () => {
      fc.assert(
        fc.property(arbStructureListWithCC, (structures) => {
          // The structure at position 2 should not be able to move up (CC protection)
          const atPos2 = structures.find((s) => s.buildQueuePosition === 2);
          if (!atPos2) return;

          resetStore(structures as PlannedStructure[]);
          usePlannerStore.getState().moveStructureUp(atPos2.id);
          const result = usePlannerStore.getState().structures;

          // All positions unchanged (no-op)
          for (const s of structures) {
            const resultS = result.find((r) => r.id === s.id)!;
            expect(resultS.buildQueuePosition).toBe(s.buildQueuePosition);
          }
        }),
        { numRuns: 100 }
      );
    });
  });

  // Feature: colony-planner-reorder, Property 3: Status invariant after reorder
  /**
   * Property 3: Status invariant after reorder
   *
   * For any reorder operation (moveStructureUp, moveStructureDown, or applyOptimizedOrder),
   * the resulting `status` in the store SHALL equal `computeColonyStatus(resultingStructures)`.
   *
   * **Validates: Requirements 1.8, 2.9**
   */
  describe('Property 3: Status invariant after reorder', () => {
    it('status equals computeColonyStatus after moveStructureUp', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          const moveable = structures.filter((s) => s.buildQueuePosition > 1);
          if (moveable.length === 0) return;

          resetStore(structures as PlannedStructure[]);
          usePlannerStore.getState().moveStructureUp(moveable[0].id);

          const state = usePlannerStore.getState();
          const expected = computeColonyStatus(state.structures);
          expect(state.status).toEqual(expected);
        }),
        { numRuns: 100 }
      );
    });

    it('status equals computeColonyStatus after moveStructureDown', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          const maxPos = Math.max(...structures.map((s) => s.buildQueuePosition));
          const moveable = structures.filter(
            (s) => s.buildQueuePosition < maxPos
          );
          if (moveable.length === 0) return;

          resetStore(structures as PlannedStructure[]);
          usePlannerStore.getState().moveStructureDown(moveable[0].id);

          const state = usePlannerStore.getState();
          const expected = computeColonyStatus(state.structures);
          expect(state.status).toEqual(expected);
        }),
        { numRuns: 100 }
      );
    });

    it('status equals computeColonyStatus after applyOptimizedOrder', () => {
      fc.assert(
        fc.property(
          arbNonCCStructureList,
          fc.array(fc.nat({ max: 50 }), { minLength: 1, maxLength: 10 }),
          (structures, newPositions) => {
            resetStore(structures as PlannedStructure[]);

            // Build an optimizedOrder that maps some structures to new positions
            const optimizedOrder: OptimizedOrderEntry[] = structures
              .slice(0, Math.min(newPositions.length, structures.length))
              .map((s, idx) => ({
                flatpackBlueprintUUID: s.blueprintUUID,
                buildQueueSequence: newPositions[idx % newPositions.length],
              }));

            usePlannerStore.getState().applyOptimizedOrder(optimizedOrder);

            const state = usePlannerStore.getState();
            const expected = computeColonyStatus(state.structures);
            expect(state.status).toEqual(expected);
          }
        ),
        { numRuns: 100 }
      );
    });
  });

  // Feature: colony-planner-reorder, Property 6: Apply optimized order correctness
  /**
   * Property 6: Apply optimized order correctness
   *
   * For any list of planned structures and any `optimizedOrder` response (which may
   * contain entries with UUIDs not present in the local plan), `applyOptimizedOrder`
   * SHALL update the `buildQueuePosition` of each local structure whose `blueprintUUID`
   * matches an entry's `flatpackBlueprintUUID` to the entry's `buildQueueSequence`,
   * and SHALL leave unmatched local structures' positions unchanged.
   *
   * **Validates: Requirements 2.3, 2.4**
   */
  describe('Property 6: Apply optimized order correctness', () => {
    it('matched structures get updated positions, unmatched stay unchanged', () => {
      fc.assert(
        fc.property(
          arbNonCCStructureList,
          fc.array(
            fc.record({
              flatpackBlueprintUUID: fc.uuid(),
              buildQueueSequence: fc.nat({ max: 50 }),
            }),
            { minLength: 0, maxLength: 5 }
          ),
          fc.array(fc.nat({ max: 50 }), { minLength: 1, maxLength: 10 }),
          (structures, extraEntries, newPositions) => {
            resetStore(structures as PlannedStructure[]);

            // Build optimizedOrder: some entries match local structures, some don't
            const matchingEntries: OptimizedOrderEntry[] = structures
              .slice(0, Math.min(newPositions.length, structures.length))
              .map((s, idx) => ({
                flatpackBlueprintUUID: s.blueprintUUID,
                buildQueueSequence: newPositions[idx % newPositions.length],
              }));

            // Extra entries have random UUIDs that won't match local structures
            const optimizedOrder = [...matchingEntries, ...extraEntries];

            usePlannerStore.getState().applyOptimizedOrder(optimizedOrder);
            const result = usePlannerStore.getState().structures;

            // Matched structures should have their new positions
            for (const entry of matchingEntries) {
              const resultS = result.find(
                (s) => s.blueprintUUID === entry.flatpackBlueprintUUID
              )!;
              expect(resultS.buildQueuePosition).toBe(entry.buildQueueSequence);
            }

            // Unmatched local structures should keep original positions
            const matchedUUIDs = new Set(
              matchingEntries.map((e) => e.flatpackBlueprintUUID)
            );
            for (const s of structures) {
              if (matchedUUIDs.has(s.blueprintUUID)) continue;
              const resultS = result.find((r) => r.id === s.id)!;
              expect(resultS.buildQueuePosition).toBe(s.buildQueuePosition);
            }
          }
        ),
        { numRuns: 100 }
      );
    });

    it('entries with no local match are ignored (no crash, no side effects)', () => {
      fc.assert(
        fc.property(
          arbNonCCStructureList,
          fc.array(
            fc.record({
              flatpackBlueprintUUID: fc.uuid(),
              buildQueueSequence: fc.nat({ max: 50 }),
            }),
            { minLength: 1, maxLength: 10 }
          ),
          (structures, unmatchedEntries) => {
            resetStore(structures as PlannedStructure[]);

            // Only pass entries that don't match any local structure
            const localUUIDs = new Set(structures.map((s) => s.blueprintUUID));
            const safeEntries = unmatchedEntries.filter(
              (e) => !localUUIDs.has(e.flatpackBlueprintUUID)
            );
            if (safeEntries.length === 0) return;

            usePlannerStore.getState().applyOptimizedOrder(safeEntries);
            const result = usePlannerStore.getState().structures;

            // All positions unchanged
            for (const s of structures) {
              const resultS = result.find((r) => r.id === s.id)!;
              expect(resultS.buildQueuePosition).toBe(s.buildQueuePosition);
            }
          }
        ),
        { numRuns: 100 }
      );
    });
  });
});
