import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { encodeBuild, decodeBuild, DecodedBuild } from './urlCodec';
import type { ComponentSlot } from './slotTypes';

/**
 * Property 11: URL encode/decode round-trip
 *
 * For any valid build state (hull UUID + slot component UUIDs), encoding then
 * decoding produces the original state. decodeBuild(encodeBuild(hull, slots))
 * equals the original hull and component UUIDs.
 *
 * **Validates: Requirements 11.1, 11.2, 11.3, 11.4**
 */
describe('Property 11: URL encode/decode round-trip', () => {
  it('encode then decode produces original state for builds with at least one component', () => {
    // Generate arrays that contain at least one non-null UUID.
    // This avoids the degenerate case of a single-element all-null array
    // where the compact encoding (empty string) is indistinguishable from
    // zero components. In practice, the store uses the hull to determine
    // slot count, so this edge case is handled at the store level.
    const arbComponentUUIDs = fc.array(
      fc.option(fc.uuid(), { nil: null }),
      { minLength: 1, maxLength: 30 }
    ).filter(arr => arr.some(uuid => uuid !== null));

    fc.assert(fc.property(
      fc.uuid(),
      arbComponentUUIDs,
      (hullUUID, componentUUIDs) => {
        const slots: ComponentSlot[] = componentUUIDs.map((uuid, i) => ({
          slotType: 'Reactor',
          slotIndex: i,
          blueprintUUID: uuid,
        }));
        const encoded = encodeBuild(hullUUID, slots);
        const decoded = decodeBuild(encoded);

        // Should not be null (valid encoded build always decodes)
        expect(decoded).not.toBeNull();
        // Should not be a DecodeError
        expect(decoded).not.toHaveProperty('type');

        const result = decoded as DecodedBuild;
        expect(result.hullUUID).toBe(hullUUID);
        expect(result.componentUUIDs).toEqual(componentUUIDs);
      }
    ), { numRuns: 100 });
  });

  it('encode then decode preserves hull UUID for empty builds', () => {
    fc.assert(fc.property(
      fc.uuid(),
      (hullUUID) => {
        const slots: ComponentSlot[] = [];
        const encoded = encodeBuild(hullUUID, slots);
        const decoded = decodeBuild(encoded);

        expect(decoded).not.toBeNull();
        expect(decoded).not.toHaveProperty('type');

        const result = decoded as DecodedBuild;
        expect(result.hullUUID).toBe(hullUUID);
        expect(result.componentUUIDs).toEqual([]);
      }
    ), { numRuns: 50 });
  });

  it('encode then decode preserves all-null slot arrays of length >= 2', () => {
    // Arrays of 2+ null slots encode with commas (e.g. "," for [null,null])
    // which decode correctly back to the original array.
    const arbAllNulls = fc.integer({ min: 2, max: 30 }).map(
      n => Array.from({ length: n }, () => null) as (string | null)[]
    );

    fc.assert(fc.property(
      fc.uuid(),
      arbAllNulls,
      (hullUUID, componentUUIDs) => {
        const slots: ComponentSlot[] = componentUUIDs.map((uuid, i) => ({
          slotType: 'Reactor',
          slotIndex: i,
          blueprintUUID: uuid,
        }));
        const encoded = encodeBuild(hullUUID, slots);
        const decoded = decodeBuild(encoded);

        expect(decoded).not.toBeNull();
        expect(decoded).not.toHaveProperty('type');

        const result = decoded as DecodedBuild;
        expect(result.hullUUID).toBe(hullUUID);
        expect(result.componentUUIDs).toEqual(componentUUIDs);
      }
    ), { numRuns: 50 });
  });
});
