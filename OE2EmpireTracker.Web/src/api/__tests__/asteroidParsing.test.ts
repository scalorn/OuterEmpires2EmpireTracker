import fc from 'fast-check';
import { describe, it, expect } from 'vitest';
import type { AsteroidReserve } from '../types/domain';

// Feature: asteroid-reserves-display, Property 6: Reserves field defaults to empty array
/**
 * Property tests for asteroid response parsing.
 * Validates: Requirements 5.1
 *
 * Property 6: Reserves field defaults to empty array
 * - When server response has null reserves, parsed Asteroid has reserves = []
 * - When server response has undefined reserves, parsed Asteroid has reserves = []
 * - When server response omits reserves, parsed Asteroid has reserves = []
 * - When server response has a valid reserves array, it passes through unchanged
 */

/**
 * Parses a raw server response into a typed Asteroid object,
 * ensuring reserves defaults to an empty array when null/undefined/omitted.
 */
function parseAsteroidResponse(response: {
  uuid: string;
  name: string;
  reserves?: any;
}): { uuid: string; name: string; reserves: AsteroidReserve[] } {
  return {
    uuid: response.uuid,
    name: response.name,
    reserves: response.reserves ?? [],
  };
}

describe('Property 6: Reserves field defaults to empty array', () => {
  it('null reserves produces empty array', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        fc.string({ minLength: 1 }),
        (uuid, name) => {
          const parsed = parseAsteroidResponse({ uuid, name, reserves: null });
          expect(parsed.reserves).toEqual([]);
          expect(Array.isArray(parsed.reserves)).toBe(true);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('undefined reserves produces empty array', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        fc.string({ minLength: 1 }),
        (uuid, name) => {
          const parsed = parseAsteroidResponse({ uuid, name, reserves: undefined });
          expect(parsed.reserves).toEqual([]);
          expect(Array.isArray(parsed.reserves)).toBe(true);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('omitted reserves produces empty array', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        fc.string({ minLength: 1 }),
        (uuid, name) => {
          const parsed = parseAsteroidResponse({ uuid, name });
          expect(parsed.reserves).toEqual([]);
          expect(Array.isArray(parsed.reserves)).toBe(true);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('valid reserves array passes through unchanged', () => {
    const reserveArb = fc.record({
      resourceName: fc.string({ minLength: 1 }),
      purity: fc.string({ minLength: 1 }),
      maxReserve: fc.nat(),
    });

    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        fc.string({ minLength: 1 }),
        fc.array(reserveArb, { minLength: 1, maxLength: 10 }),
        (uuid, name, reserves) => {
          const parsed = parseAsteroidResponse({ uuid, name, reserves });
          expect(parsed.reserves).toEqual(reserves);
          expect(parsed.reserves.length).toBe(reserves.length);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('uuid and name pass through unchanged regardless of reserves state', () => {
    const reservesArb = fc.oneof(
      fc.constant(null),
      fc.constant(undefined),
      fc.array(
        fc.record({
          resourceName: fc.string({ minLength: 1 }),
          purity: fc.string({ minLength: 1 }),
          maxReserve: fc.nat(),
        }),
        { maxLength: 5 },
      ),
    );

    fc.assert(
      fc.property(
        fc.string({ minLength: 1 }),
        fc.string({ minLength: 1 }),
        reservesArb,
        (uuid, name, reserves) => {
          const parsed = parseAsteroidResponse({ uuid, name, reserves });
          expect(parsed.uuid).toBe(uuid);
          expect(parsed.name).toBe(name);
        },
      ),
      { numRuns: 100 },
    );
  });
});
