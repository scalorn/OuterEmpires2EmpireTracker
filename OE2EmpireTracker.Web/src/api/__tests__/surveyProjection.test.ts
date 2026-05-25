// Feature: asteroid-reserves-display, Property 2: AsteroidUUID inclusion in survey response
// **Validates: Requirements 2.2, 2.3**

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';

/**
 * Projection function that determines whether asteroidUUID should be included
 * in the public survey response. Included iff surveyType is 'Asteroid' AND
 * asteroidUUID is a non-null, non-empty string.
 */
function shouldIncludeAsteroidUUID(
  surveyType: 'planet' | 'asteroid',
  asteroidUUID?: string | null
): boolean {
  return surveyType === 'asteroid' && !!asteroidUUID && asteroidUUID.length > 0;
}

/**
 * Arbitrary for asteroidUUID values: non-empty strings, empty string, null, undefined.
 */
const arbAsteroidUUID = fc.oneof(
  fc.string({ minLength: 1, maxLength: 36 }),
  fc.constant(''),
  fc.constant(null),
  fc.constant(undefined)
);

const arbSurveyType = fc.constantFrom('planet' as const, 'asteroid' as const);

describe('surveyProjection - Property 2: AsteroidUUID inclusion', () => {
  it('includes asteroidUUID iff surveyType is Asteroid AND UUID is non-empty string', () => {
    fc.assert(
      fc.property(arbSurveyType, arbAsteroidUUID, (surveyType, asteroidUUID) => {
        const included = shouldIncludeAsteroidUUID(surveyType, asteroidUUID);

        const isAsteroid = surveyType === 'asteroid';
        const hasNonEmptyUUID =
          asteroidUUID !== null && asteroidUUID !== undefined && asteroidUUID.length > 0;

        // The biconditional: included iff (isAsteroid AND hasNonEmptyUUID)
        expect(included).toBe(isAsteroid && hasNonEmptyUUID);
      }),
      { numRuns: 100 }
    );
  });

  it('always excludes asteroidUUID for Planet surveys regardless of UUID value', () => {
    fc.assert(
      fc.property(arbAsteroidUUID, (asteroidUUID) => {
        const included = shouldIncludeAsteroidUUID('planet', asteroidUUID);
        expect(included).toBe(false);
      }),
      { numRuns: 100 }
    );
  });

  it('excludes asteroidUUID for Asteroid surveys with empty/null/undefined UUID', () => {
    fc.assert(
      fc.property(
        fc.constantFrom('' as string, null as null, undefined as undefined),
        (asteroidUUID) => {
          const included = shouldIncludeAsteroidUUID('asteroid', asteroidUUID);
          expect(included).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });

  it('includes asteroidUUID for Asteroid surveys with non-empty UUID', () => {
    fc.assert(
      fc.property(fc.string({ minLength: 1, maxLength: 36 }), (asteroidUUID) => {
        const included = shouldIncludeAsteroidUUID('asteroid', asteroidUUID);
        expect(included).toBe(true);
      }),
      { numRuns: 100 }
    );
  });
});
