import { vi, describe, it, expect, beforeEach } from 'vitest';
import fc from 'fast-check';
import { useShipBuilderStore } from './shipBuilderStore';
import { BLUEPRINT_TYPE_TO_SLOT_TYPE, getCompatibleBlueprintTypes } from './slotTypes';
import type { BlueprintSummary } from './shipBuilderStore';

// Mock the public API module
vi.mock('../../api/endpoints/public', () => ({
  publicApi: {
    getPublicBlueprints: vi.fn(),
    getBlueprintDetail: vi.fn(),
  },
}));

import { publicApi } from '../../api/endpoints/public';
const mockGetDetail = vi.mocked(publicApi.getBlueprintDetail);

// --- Shared constants ---

const ALL_SLOT_TYPES = [
  'Reactor', 'MainDrive', 'Thruster', 'JumpDrive', 'NavComp', 'Scanner',
  'Shield', 'CargoPod', 'FuelTank', 'Coupler', 'GERTY',
  'HullPlating', 'HullReinforcement', 'HullSealant',
  'MiningLaser', 'MiningGrapple', 'OreHopper',
  'WeaponSmall', 'WeaponMedium', 'WeaponLarge',
];

const ALL_BLUEPRINT_TYPES = Object.keys(BLUEPRINT_TYPE_TO_SLOT_TYPE);

describe('shipBuilderStore property tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset the store state between tests
    useShipBuilderStore.setState({
      blueprintList: [],
      blueprintCache: {},
      blueprintListLoading: false,
      blueprintListError: null,
      selectedHullUUID: null,
      hullDetail: null,
      slots: [],
      stats: null,
      loadingUUIDs: new Set<string>(),
      slotErrors: {},
    });
  });

  // Property 8: Blueprint cache prevents redundant fetches
  // **Validates: Requirements 6.2, 6.3**
  it('cached blueprint does not trigger additional API call on installComponent', () => {
    fc.assert(
      fc.property(
        fc.uuid(),
        fc.constantFrom(...ALL_SLOT_TYPES),
        fc.nat({ max: 5 }),
        fc.integer({ min: 2, max: 8 }),
        (uuid, slotType, slotIndex, shipClass) => {
          // Reset mocks for each property run
          mockGetDetail.mockClear();

          // Pre-populate the cache with a blueprint detail for this UUID
          useShipBuilderStore.setState({
            blueprintCache: {
              [uuid]: {
                uuid,
                name: 'Cached Component',
                bluePrintType: 'Reactor',
                class: shipClass,
                properties: { 'Mass': '100', 'Power Provided': '200' },
              },
            },
            // Provide a slot that matches so installComponent can proceed
            slots: [{ slotType, slotIndex, blueprintUUID: null }],
            hullDetail: {
              uuid: '00000000-0000-0000-0000-000000000000',
              name: 'TestHull',
              bluePrintType: 'Hull',
              class: shipClass,
              properties: { 'Mass': '500' },
            },
            selectedHullUUID: '00000000-0000-0000-0000-000000000000',
          });

          // Call installComponent — should use cache, NOT call API
          useShipBuilderStore.getState().installComponent(slotType, slotIndex, uuid);

          // The API should NOT have been called since the UUID is already cached
          expect(mockGetDetail).not.toHaveBeenCalled();
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 10: Component filtering respects slot type AND class
  // **Validates: Requirements 4.2, 4.3**
  it('filtering blueprints by slot type and class includes only matching items and excludes all non-matching', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(...ALL_SLOT_TYPES),
        fc.integer({ min: 2, max: 8 }),
        fc.array(
          fc.record({
            uuid: fc.uuid(),
            name: fc.string({ minLength: 1, maxLength: 30 }),
            bluePrintType: fc.constantFrom(...ALL_BLUEPRINT_TYPES),
            class: fc.integer({ min: 2, max: 8 }),
          }),
          { minLength: 1, maxLength: 20 }
        ),
        (slotType, hullClass, blueprints: BlueprintSummary[]) => {
          // Get compatible blueprint types for this slot type
          const compatibleTypes = getCompatibleBlueprintTypes(slotType);

          // Filter blueprints the same way the UI would
          const filtered = blueprints.filter(
            (bp) => compatibleTypes.includes(bp.bluePrintType) && bp.class === hullClass
          );

          // Every included item must have correct slot type mapping AND matching class
          for (const bp of filtered) {
            expect(BLUEPRINT_TYPE_TO_SLOT_TYPE[bp.bluePrintType]).toBe(slotType);
            expect(bp.class).toBe(hullClass);
          }

          // Every excluded item must fail at least one condition
          const excluded = blueprints.filter(
            (bp) => !compatibleTypes.includes(bp.bluePrintType) || bp.class !== hullClass
          );
          for (const bp of excluded) {
            const matchesSlot = BLUEPRINT_TYPE_TO_SLOT_TYPE[bp.bluePrintType] === slotType;
            const matchesClass = bp.class === hullClass;
            expect(matchesSlot && matchesClass).toBe(false);
          }

          // Filtered + excluded = original list (partition property)
          expect(filtered.length + excluded.length).toBe(blueprints.length);
        }
      ),
      { numRuns: 100 }
    );
  });
});
