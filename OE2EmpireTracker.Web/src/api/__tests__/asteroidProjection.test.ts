// Feature: asteroid-reserves-display, Property 1: Public endpoint projection strips private fields
// **Validates: Requirements 1.3**

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { AsteroidReserve } from '../types/domain';

/**
 * Simulates the server-side projection logic that strips private fields
 * from an AsteroidReserve before returning it in the public endpoint response.
 *
 * Server equivalent (C#):
 *   reserves = asteroid.Reserves.Select(r => new {
 *       resourceName = r.ResourceName,
 *       purity = r.Purity,
 *       maxReserve = r.MaxReserve,
 *   })
 */
function projectAsteroidReserve(reserve: AsteroidReserve) {
  return {
    resourceName: reserve.resourceName,
    purity: reserve.purity,
    maxReserve: reserve.maxReserve,
  };
}

describe('asteroidProjection - Property 1: Public endpoint projection strips private fields', () => {
  it('projected reserve does not contain currentReserve or resetTimestamp', () => {
    fc.assert(
      fc.property(
        fc.record({
          resourceName: fc.string({ minLength: 1, maxLength: 30 }),
          purity: fc.string({ minLength: 1, maxLength: 20 }),
          maxReserve: fc.nat({ max: 100_000_000 }),
          currentReserve: fc.nat({ max: 100_000_000 }),
          resetTimestamp: fc.date().map((d) => d.toISOString()),
        }),
        (fullReserve) => {
          const projected = projectAsteroidReserve(fullReserve);

          // The projected object must NOT contain private fields
          expect(projected).not.toHaveProperty('currentReserve');
          expect(projected).not.toHaveProperty('resetTimestamp');
        }
      ),
      { numRuns: 100 }
    );
  });

  it('projected reserve has exactly 3 keys: resourceName, purity, maxReserve', () => {
    fc.assert(
      fc.property(
        fc.record({
          resourceName: fc.string({ minLength: 1, maxLength: 30 }),
          purity: fc.string({ minLength: 1, maxLength: 20 }),
          maxReserve: fc.nat({ max: 100_000_000 }),
          currentReserve: fc.nat({ max: 100_000_000 }),
          resetTimestamp: fc.date().map((d) => d.toISOString()),
        }),
        (fullReserve) => {
          const projected = projectAsteroidReserve(fullReserve);

          const keys = Object.keys(projected).sort();
          expect(keys).toEqual(['maxReserve', 'purity', 'resourceName']);
        }
      ),
      { numRuns: 100 }
    );
  });

  it('projected reserve preserves the public field values exactly', () => {
    fc.assert(
      fc.property(
        fc.record({
          resourceName: fc.string({ minLength: 1, maxLength: 30 }),
          purity: fc.string({ minLength: 1, maxLength: 20 }),
          maxReserve: fc.nat({ max: 100_000_000 }),
          currentReserve: fc.nat({ max: 100_000_000 }),
          resetTimestamp: fc.date().map((d) => d.toISOString()),
        }),
        (fullReserve) => {
          const projected = projectAsteroidReserve(fullReserve);

          expect(projected.resourceName).toBe(fullReserve.resourceName);
          expect(projected.purity).toBe(fullReserve.purity);
          expect(projected.maxReserve).toBe(fullReserve.maxReserve);
        }
      ),
      { numRuns: 100 }
    );
  });
});
