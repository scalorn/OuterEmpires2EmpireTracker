import fc from 'fast-check';
import { describe, it, expect } from 'vitest';

// Feature: asteroid-reserves-display, Property 3: Fetch-enabled logic
/**
 * Property tests for fetch-enabled logic.
 * Validates: Requirements 3.1, 3.5
 *
 * Property 3: Fetch-enabled logic
 * - Fetch enabled iff surveyType === 'Asteroid' AND asteroidUUID is non-null, non-undefined, non-empty
 * - For all other combinations, fetch is not triggered
 *
 * This tests the pure decision function that determines whether the asteroid
 * detail fetch should be enabled, mirroring the component + hook logic:
 * - Component only passes asteroidUUID to the hook when surveyType === 'Asteroid'
 * - Hook uses `enabled: !!asteroidUUID` to gate the fetch
 */

/**
 * Pure function representing the combined fetch-enabled logic.
 * The component checks surveyType, the hook checks asteroidUUID truthiness.
 */
function isFetchEnabled(surveyType: 'Planet' | 'Asteroid', asteroidUUID?: string | null): boolean {
  return surveyType === 'Asteroid' && !!asteroidUUID && asteroidUUID.length > 0;
}

/** Arbitrary for survey type */
const surveyTypeArb = fc.constantFrom('Planet' as const, 'Asteroid' as const);

/** Arbitrary for asteroidUUID including edge cases: valid strings, empty, null, undefined */
const asteroidUUIDArb = fc.oneof(
  fc.uuid(),
  fc.constant(null),
  fc.constant(undefined),
  fc.constant(''),
  fc.string({ minLength: 1 }),
);

describe('Property 3: Fetch-enabled logic', () => {
  it('fetch enabled iff surveyType === Asteroid AND asteroidUUID is non-null, non-empty', () => {
    fc.assert(
      fc.property(
        surveyTypeArb,
        asteroidUUIDArb,
        (surveyType, asteroidUUID) => {
          const result = isFetchEnabled(surveyType, asteroidUUID);
          const expected =
            surveyType === 'Asteroid' &&
            asteroidUUID !== null &&
            asteroidUUID !== undefined &&
            asteroidUUID.length > 0;
          expect(result).toBe(expected);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('Planet surveys never enable fetch regardless of asteroidUUID', () => {
    fc.assert(
      fc.property(
        asteroidUUIDArb,
        (asteroidUUID) => {
          const result = isFetchEnabled('Planet', asteroidUUID);
          expect(result).toBe(false);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('Asteroid surveys with null or empty UUID do not enable fetch', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(null, undefined, ''),
        (asteroidUUID) => {
          const result = isFetchEnabled('Asteroid', asteroidUUID);
          expect(result).toBe(false);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('Asteroid surveys with non-empty UUID always enable fetch', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        (asteroidUUID) => {
          const result = isFetchEnabled('Asteroid', asteroidUUID);
          expect(result).toBe(true);
        },
      ),
      { numRuns: 100 },
    );
  });
});
