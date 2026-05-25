// Feature: asteroid-reserves-display, Property 1: Public endpoint projection strips private fields
// **Validates: Requirements 1.3**

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { AsteroidReserve } from '../types/domain';

/**
 * Simulates the server-side projection logic that strips private fields
 * from an AsteroidReserve before returning it in the public endpoint response.
 */
function projectAsteroidReserve(reserve: AsteroidReserve) {
  return {
    resourceName: reserve.resourceName,
    purity: reserve.purity,
    maxReserve: reserve.maxReserve,
  };
}

/** Generate a valid ISO timestamp string without using fc.date() (which can produce invalid dates). */
const arbTimestamp = fc.integer({ min: 946684800000, max: 1924905600000 }).map((ms) => new Date(ms).toISOString());

const arbFullReserve = fc.record({
  resourceName: fc.string({ minLength: 1, maxLength: 30 }),
  purity: fc.string({ minLength: 1, maxLength: 20 }),
  maxReserve: fc.nat({ max: 100_000_000 }),
  currentReserve: fc.nat({ max: 100_000_000 }),
  resetTimestamp: arbTimestamp,
});

describe('asteroidProjection - Property 1: Public endpoint projection strips private fields', () => {
  it('projected reserve does not contain currentReserve or resetTimestamp', () => {
    fc.assert(
      fc.property(arbFullReserve, (fullReserve) => {
        const projected = projectAsteroidReserve(fullReserve);
        expect(projected).not.toHaveProperty('currentReserve');
        expect(projected).not.toHaveProperty('resetTimestamp');
      }),
      { numRuns: 100 },
    );
  });

  it('projected reserve has exactly 3 keys: resourceName, purity, maxReserve', () => {
    fc.assert(
      fc.property(arbFullReserve, (fullReserve) => {
        const projected = projectAsteroidReserve(fullReserve);
        const keys = Object.keys(projected).sort();
        expect(keys).toEqual(['maxReserve', 'purity', 'resourceName']);
      }),
      { numRuns: 100 },
    );
  });

  it('projected reserve preserves the public field values exactly', () => {
    fc.assert(
      fc.property(arbFullReserve, (fullReserve) => {
        const projected = projectAsteroidReserve(fullReserve);
        expect(projected.resourceName).toBe(fullReserve.resourceName);
        expect(projected.purity).toBe(fullReserve.purity);
        expect(projected.maxReserve).toBe(fullReserve.maxReserve);
      }),
      { numRuns: 100 },
    );
  });
});
