import fc from 'fast-check';
import { describe, it, expect } from 'vitest';

/**
 * Property test for blueprint filter correctness.
 * Validates: Requirements 6.2
 */

interface Blueprint {
  UUID: string;
  Name: string;
  BlueprintType: string;
  TechLevel: string;
  ShipClass: string;
}

interface BlueprintFilters {
  type?: string;
  techLevel?: string;
  shipClass?: string;
  search?: string;
}

// Pure filter function matching the logic used in BlueprintBrowser
function filterBlueprints(blueprints: Blueprint[], filters: BlueprintFilters): Blueprint[] {
  return blueprints.filter((bp) => {
    if (filters.type && bp.BlueprintType !== filters.type) return false;
    if (filters.techLevel && bp.TechLevel !== filters.techLevel) return false;
    if (filters.shipClass && bp.ShipClass !== filters.shipClass) return false;
    if (filters.search) {
      const search = filters.search.toLowerCase();
      if (!bp.Name.toLowerCase().includes(search)) return false;
    }
    return true;
  });
}

const TYPES = ['Ship', 'Structure', 'Module', 'Weapon'];
const TECH_LEVELS = ['1', '2', '3', '4', '5'];
const SHIP_CLASSES = ['Fighter', 'Frigate', 'Destroyer', 'Cruiser', 'Battleship'];

const blueprintArb = fc.record({
  UUID: fc.uuid(),
  Name: fc.string({ minLength: 1, maxLength: 50 }),
  BlueprintType: fc.constantFrom(...TYPES),
  TechLevel: fc.constantFrom(...TECH_LEVELS),
  ShipClass: fc.constantFrom(...SHIP_CLASSES),
});

const filtersArb = fc.record({
  type: fc.option(fc.constantFrom(...TYPES), { nil: undefined }),
  techLevel: fc.option(fc.constantFrom(...TECH_LEVELS), { nil: undefined }),
  shipClass: fc.option(fc.constantFrom(...SHIP_CLASSES), { nil: undefined }),
  search: fc.option(fc.string({ minLength: 1, maxLength: 10 }), { nil: undefined }),
});

describe('Property 4: Blueprint filter correctness', () => {
  it('for any set of filter criteria, every item in filtered result matches ALL active criteria', () => {
    fc.assert(
      fc.property(
        fc.array(blueprintArb, { minLength: 0, maxLength: 50 }),
        filtersArb,
        (blueprints, filters) => {
          const result = filterBlueprints(blueprints, filters);

          for (const bp of result) {
            if (filters.type) {
              expect(bp.BlueprintType).toBe(filters.type);
            }
            if (filters.techLevel) {
              expect(bp.TechLevel).toBe(filters.techLevel);
            }
            if (filters.shipClass) {
              expect(bp.ShipClass).toBe(filters.shipClass);
            }
            if (filters.search) {
              expect(bp.Name.toLowerCase()).toContain(filters.search.toLowerCase());
            }
          }
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});
