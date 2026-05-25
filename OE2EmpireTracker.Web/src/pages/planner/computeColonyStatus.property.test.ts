/**
 * Property-based tests for computeColonyStatus.
 * Uses fast-check to verify correctness properties hold across all valid inputs.
 *
 * Validates: Correctness Properties 1-5, 10
 */
import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { computeColonyStatus } from './computeColonyStatus';
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

const arbStructureState = fc.constantFrom('Staged' as const, 'Built' as const, 'Online' as const);

const arbPlannedStructure: fc.Arbitrary<PlannedStructure> = fc.record({
  id: fc.uuid(),
  blueprintUUID: fc.uuid(),
  name: fc.string({ minLength: 1, maxLength: 50 }),
  subType: fc.constantFrom('MiningRig', 'Refinery', 'ResearchLaboratory', 'Manufactory', 'ColonyCommandCentre'),
  state: arbStructureState,
  buildQueuePosition: fc.nat({ max: 100 }),
  properties: arbBlueprintProperties,
});

// --- Properties ---

describe('computeColonyStatus property-based tests', () => {
  /**
   * Property 1: Pure function (deterministic)
   * Same input produces same output regardless of call order or timing.
   *
   * **Validates: Requirements 4.1, 9.1, 9.2**
   */
  it('produces identical output for identical input (pure function)', () => {
    fc.assert(
      fc.property(
        fc.array(arbPlannedStructure, { maxLength: 20 }),
        (structures) => {
          const result1 = computeColonyStatus(structures);
          const result2 = computeColonyStatus(structures);
          expect(result1).toEqual(result2);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 2: Staged contributes only food provision
   * A single Staged structure contributes only foodProvision; all other fields are zero.
   *
   * **Validates: Requirements 5.3, 5.6**
   */
  it('staged structures contribute only food provision', () => {
    fc.assert(
      fc.property(
        arbPlannedStructure.map((s) => ({ ...s, state: 'Staged' as const })),
        (stagedStructure) => {
          const result = computeColonyStatus([stagedStructure])!;
          expect(result.powerProvided).toBe(0);
          expect(result.powerRequired).toBe(0);
          expect(result.habitationProvision).toBe(0);
          expect(result.entertainmentProvided).toBe(0);
          expect(result.warehouseCapacity).toBe(0);
          expect(result.habitationRequired).toBe(0);
          expect(result.entertainmentRequired).toBe(0);
          expect(result.foodProvision).toBe(stagedStructure.properties.foodProvision);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 3: Online contributes all provisions plus workers
   * A single Online structure contributes all provision fields and its workers
   * count toward habitationRequired, foodRequired, and entertainmentRequired.
   *
   * **Validates: Requirements 5.2**
   */
  it('online structures contribute all provisions plus workers', () => {
    fc.assert(
      fc.property(
        arbPlannedStructure.map((s) => ({ ...s, state: 'Online' as const })),
        (onlineStructure) => {
          const result = computeColonyStatus([onlineStructure])!;
          const props = onlineStructure.properties;
          expect(result.powerProvided).toBe(props.powerProvided);
          expect(result.powerRequired).toBe(props.powerRequired);
          expect(result.habitationProvision).toBe(props.habitationProvision);
          expect(result.foodProvision).toBe(props.foodProvision);
          expect(result.entertainmentProvided).toBe(props.entertainmentProvided);
          expect(result.warehouseCapacity).toBe(props.warehouseCapacity);
          expect(result.habitationRequired).toBe(props.workerSlots);
          expect(result.foodRequired).toBe(props.workerSlots);
          expect(result.entertainmentRequired).toBe(props.workerSlots * 2);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 4: Built contributes workers + food, no other provisions
   * A single Built structure contributes workers toward Required fields and
   * food provision, but no other provisions.
   *
   * **Validates: Requirements 5.4, 5.6**
   */
  it('built structures contribute workers and food, no other provisions', () => {
    fc.assert(
      fc.property(
        arbPlannedStructure.map((s) => ({ ...s, state: 'Built' as const })),
        (builtStructure) => {
          const result = computeColonyStatus([builtStructure])!;
          const props = builtStructure.properties;
          // No provisions except food
          expect(result.powerProvided).toBe(0);
          expect(result.habitationProvision).toBe(0);
          expect(result.entertainmentProvided).toBe(0);
          expect(result.warehouseCapacity).toBe(0);
          expect(result.powerRequired).toBe(0);
          // Food provision accumulates regardless of state
          expect(result.foodProvision).toBe(props.foodProvision);
          // Workers count toward required fields
          expect(result.habitationRequired).toBe(props.workerSlots);
          expect(result.foodRequired).toBe(props.workerSlots);
          expect(result.entertainmentRequired).toBe(props.workerSlots * 2);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 5: Entertainment required = total workers × 2
   * For any non-empty list, entertainmentRequired equals the sum of workerSlots
   * from Built + Online structures multiplied by 2.
   *
   * **Validates: Requirements 4.5**
   */
  it('entertainment required equals total workers times two', () => {
    fc.assert(
      fc.property(
        fc.array(arbPlannedStructure, { minLength: 1, maxLength: 20 }),
        (structures) => {
          const result = computeColonyStatus(structures)!;
          const totalWorkers = structures
            .filter((s) => s.state === 'Built' || s.state === 'Online')
            .reduce((sum, s) => sum + s.properties.workerSlots, 0);
          expect(result.entertainmentRequired).toBe(totalWorkers * 2);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 10: Empty list returns null
   * computeColonyStatus([]) returns null (neutral/empty state).
   *
   * **Validates: Requirements 4.8**
   */
  it('returns null for empty structure list', () => {
    expect(computeColonyStatus([])).toBeNull();
  });
});
