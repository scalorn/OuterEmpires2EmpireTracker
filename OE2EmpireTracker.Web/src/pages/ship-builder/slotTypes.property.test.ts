import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { generateSlotsFromHull, HULL_PROPERTY_TO_SLOT_TYPE } from './slotTypes';

/**
 * **Validates: Requirements 2.4**
 *
 * Property 9: Slot generation matches hull properties
 * For any hull with property "X Slots": N (N > 0), generateSlotsFromHull produces
 * exactly N slots of the corresponding type. Properties with value 0 or missing
 * produce no slots.
 */
describe('Property 9: Slot generation matches hull properties', () => {
  const hullPropertyKeys = Object.keys(HULL_PROPERTY_TO_SLOT_TYPE);

  it('produces exactly N slots for each property with value N > 0, and no slots for value 0 or missing', () => {
    const arbHullProperties = fc.dictionary(
      fc.constantFrom(...hullPropertyKeys),
      fc.integer({ min: 0, max: 10 }).map(String)
    );

    fc.assert(fc.property(arbHullProperties, (properties) => {
      const slots = generateSlotsFromHull(properties);

      for (const [propKey, slotType] of Object.entries(HULL_PROPERTY_TO_SLOT_TYPE)) {
        const expectedCount = Math.floor(Number(properties[propKey] ?? '0'));
        const actualCount = slots.filter(s => s.slotType === slotType).length;

        if (expectedCount > 0) {
          expect(actualCount).toBe(expectedCount);
        } else {
          expect(actualCount).toBe(0);
        }
      }
    }), { numRuns: 100 });
  });

  it('all generated slots have sequential 0-based slotIndex within their type', () => {
    const arbHullProperties = fc.dictionary(
      fc.constantFrom(...hullPropertyKeys),
      fc.integer({ min: 0, max: 10 }).map(String)
    );

    fc.assert(fc.property(arbHullProperties, (properties) => {
      const slots = generateSlotsFromHull(properties);

      // Group slots by type and verify indices are 0..N-1
      const byType = new Map<string, number[]>();
      for (const slot of slots) {
        if (!byType.has(slot.slotType)) {
          byType.set(slot.slotType, []);
        }
        byType.get(slot.slotType)!.push(slot.slotIndex);
      }

      for (const [, indices] of byType) {
        const sorted = [...indices].sort((a, b) => a - b);
        for (let i = 0; i < sorted.length; i++) {
          expect(sorted[i]).toBe(i);
        }
      }
    }), { numRuns: 100 });
  });

  it('all generated slots have blueprintUUID set to null', () => {
    const arbHullProperties = fc.dictionary(
      fc.constantFrom(...hullPropertyKeys),
      fc.integer({ min: 1, max: 10 }).map(String)
    );

    fc.assert(fc.property(arbHullProperties, (properties) => {
      const slots = generateSlotsFromHull(properties);
      for (const slot of slots) {
        expect(slot.blueprintUUID).toBeNull();
      }
    }), { numRuns: 50 });
  });

  it('total slot count equals sum of all property values > 0', () => {
    const arbHullProperties = fc.dictionary(
      fc.constantFrom(...hullPropertyKeys),
      fc.integer({ min: 0, max: 10 }).map(String)
    );

    fc.assert(fc.property(arbHullProperties, (properties) => {
      const slots = generateSlotsFromHull(properties);

      let expectedTotal = 0;
      for (const propKey of hullPropertyKeys) {
        const val = Math.floor(Number(properties[propKey] ?? '0'));
        if (val > 0) {
          expectedTotal += val;
        }
      }

      expect(slots.length).toBe(expectedTotal);
    }), { numRuns: 100 });
  });
});
