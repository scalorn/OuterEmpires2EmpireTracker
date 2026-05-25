// Feature: asteroid-reserves-display, Property 4: Resource-to-reserve matching
// **Validates: Requirements 3.3, 3.6, 4.3**

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { matchReserveToResource } from '../reserveMatching';
import { AsteroidReserve } from '../../api/types/domain';

/**
 * Generates a non-empty alphanumeric string (1-20 chars) suitable for
 * resource names and purity values.
 */
const arbName = fc.stringMatching(/^[a-z][a-z0-9]{0,19}$/);

describe('matchReserveToResource - Property 4', () => {
  it('matches when resourceName AND purity are equal (case-insensitive)', () => {
    fc.assert(
      fc.property(
        arbName,
        arbName,
        fc.nat({ max: 10_000_000 }),
        (baseName, basePurity, maxReserve) => {
          // Create a reserve with the base (lowercase) name and purity
          const reserve: AsteroidReserve = {
            resourceName: baseName,
            purity: basePurity,
            maxReserve,
          };

          // Create a resource with uppercased name/purity to test case-insensitivity
          const resource = {
            resourceName: baseName.toUpperCase(),
            purity: basePurity.toUpperCase(),
          };

          const result = matchReserveToResource(resource, [reserve]);
          expect(result).toBe(reserve);
        }
      ),
      { numRuns: 100 }
    );
  });

  it('matches with arbitrary case variations on both sides', () => {
    fc.assert(
      fc.property(
        arbName,
        arbName,
        fc.nat({ max: 10_000_000 }),
        fc.array(fc.boolean(), { minLength: 20, maxLength: 20 }),
        fc.array(fc.boolean(), { minLength: 20, maxLength: 20 }),
        (baseName, basePurity, maxReserve, nameFlags, purityFlags) => {
          // Apply random case to resource name
          const resourceName = baseName
            .split('')
            .map((ch, i) => (nameFlags[i % nameFlags.length] ? ch.toUpperCase() : ch.toLowerCase()))
            .join('');

          // Apply different random case to reserve name
          const reserveName = baseName
            .split('')
            .map((ch, i) => (!nameFlags[i % nameFlags.length] ? ch.toUpperCase() : ch.toLowerCase()))
            .join('');

          // Apply random case to purity on both sides
          const resourcePurity = basePurity
            .split('')
            .map((ch, i) => (purityFlags[i % purityFlags.length] ? ch.toUpperCase() : ch.toLowerCase()))
            .join('');

          const reservePurity = basePurity
            .split('')
            .map((ch, i) => (!purityFlags[i % purityFlags.length] ? ch.toUpperCase() : ch.toLowerCase()))
            .join('');

          const reserve: AsteroidReserve = {
            resourceName: reserveName,
            purity: reservePurity,
            maxReserve,
          };

          const resource = { resourceName, purity: resourcePurity };
          const result = matchReserveToResource(resource, [reserve]);
          expect(result).toBe(reserve);
        }
      ),
      { numRuns: 100 }
    );
  });

  it('returns undefined when resourceName differs', () => {
    fc.assert(
      fc.property(
        arbName,
        arbName,
        arbName,
        fc.nat({ max: 10_000_000 }),
        (resourceName, reserveName, purity, maxReserve) => {
          // Only test when names are actually different (case-insensitive)
          fc.pre(resourceName.toLowerCase() !== reserveName.toLowerCase());

          const reserve: AsteroidReserve = {
            resourceName: reserveName,
            purity,
            maxReserve,
          };

          const resource = { resourceName, purity };
          const result = matchReserveToResource(resource, [reserve]);
          expect(result).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  it('returns undefined when purity differs', () => {
    fc.assert(
      fc.property(
        arbName,
        arbName,
        arbName,
        fc.nat({ max: 10_000_000 }),
        (name, resourcePurity, reservePurity, maxReserve) => {
          // Only test when purities are actually different (case-insensitive)
          fc.pre(resourcePurity.toLowerCase() !== reservePurity.toLowerCase());

          const reserve: AsteroidReserve = {
            resourceName: name,
            purity: reservePurity,
            maxReserve,
          };

          const resource = { resourceName: name, purity: resourcePurity };
          const result = matchReserveToResource(resource, [reserve]);
          expect(result).toBeUndefined();
        }
      ),
      { numRuns: 100 }
    );
  });

  it('returns undefined when reserves array is empty', () => {
    fc.assert(
      fc.property(arbName, arbName, (name, purity) => {
        const resource = { resourceName: name, purity };
        const result = matchReserveToResource(resource, []);
        expect(result).toBeUndefined();
      }),
      { numRuns: 100 }
    );
  });
});
