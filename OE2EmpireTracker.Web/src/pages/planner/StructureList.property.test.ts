/**
 * Property-based tests for StructureList disable logic.
 * Uses fast-check to verify disable flag correctness across all valid inputs.
 *
 * Feature: colony-planner-reorder, Property 2: Disable logic correctness
 *
 * Validates: Requirements 1.3, 1.4, 1.5, 1.6, 1.7
 */
import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { computeDisableFlags } from './computeDisableFlags';
import type { PlannedStructure } from './computeColonyStatus';

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
 * Generates a list of 1+ non-CC planned structures with unique sequential positions.
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
    { minLength: 1, maxLength: 20 }
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
      { minLength: 1, maxLength: 19 }
    )
  )
  .map(([cc, rest]) => [
    { ...cc, buildQueuePosition: 1 },
    ...rest.map((item, idx) => ({ ...item, buildQueuePosition: idx + 2 })),
  ]);

/**
 * Generates a list with a CC at a random (non-first) position and non-CC structures elsewhere.
 */
const arbStructureListWithCCNotFirst = fc
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
      { minLength: 2, maxLength: 19 }
    )
  )
  .map(([cc, rest]) => {
    // Place CC somewhere other than first
    const ccIdx = 1 + Math.floor(Math.random() * (rest.length - 1));
    const all: PlannedStructure[] = rest.map((item, idx) => ({
      ...item,
      buildQueuePosition: idx < ccIdx ? idx + 1 : idx + 2,
    }));
    all.splice(ccIdx, 0, { ...cc, buildQueuePosition: ccIdx + 1 } as PlannedStructure);
    return all;
  });


// --- Properties ---

describe('StructureList property-based tests', () => {
  // Feature: colony-planner-reorder, Property 2: Disable logic correctness
  /**
   * Property 2: Disable logic correctness
   *
   * For any list of planned structures (with or without a Colony Command Centre),
   * the move-up button SHALL be disabled if and only if: the structure is the CC,
   * OR the structure is at position 1, OR the structure is at position 2 with CC
   * at position 1, OR the list has only one item.
   * The move-down button SHALL be disabled if and only if: the structure is the CC,
   * OR the structure is at the last position, OR the list has only one item.
   *
   * **Validates: Requirements 1.3, 1.4, 1.5, 1.6, 1.7**
   */
  describe('Property 2: Disable logic correctness', () => {
    it('move-up disabled iff: CC, first, second-with-CC-first, or single item (no CC in list)', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          const flags = computeDisableFlags(structures as PlannedStructure[]);
          const sorted = [...structures].sort((a, b) => a.buildQueuePosition - b.buildQueuePosition);

          for (let i = 0; i < sorted.length; i++) {
            const isFirst = i === 0;
            const isSingleItem = sorted.length === 1;
            // No CC in this list, so isCC and isSecondWithCCFirst are always false
            const expectedUp = isFirst || isSingleItem;
            expect(flags[i].isMoveUpDisabled).toBe(expectedUp);
          }
        }),
        { numRuns: 100 }
      );
    });

    it('move-down disabled iff: CC, last, or single item (no CC in list)', () => {
      fc.assert(
        fc.property(arbNonCCStructureList, (structures) => {
          const flags = computeDisableFlags(structures as PlannedStructure[]);
          const sorted = [...structures].sort((a, b) => a.buildQueuePosition - b.buildQueuePosition);

          for (let i = 0; i < sorted.length; i++) {
            const isLast = i === sorted.length - 1;
            const isSingleItem = sorted.length === 1;
            // No CC in this list
            const expectedDown = isLast || isSingleItem;
            expect(flags[i].isMoveDownDisabled).toBe(expectedDown);
          }
        }),
        { numRuns: 100 }
      );
    });
