# Implementation Plan: Web Ship Template Builder

## Overview

Implement a public ship template builder page at `/ship-builder` following the Colony Planner architectural pattern: Zustand store, public API data fetching, React components, and client-side stats computation. The implementation is split into vertical slices — slot type constants, pure computation function, Zustand store, URL codec, then UI components — with property-based tests validating correctness properties from the design.

## Tasks

- [ ] 1. Implement slot type constants and mappings
  - [-] 1.1 Create `slotTypes.ts` with all slot type constants, mappings, and helper functions
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/slotTypes.ts`
    - Implement `HULL_PROPERTY_TO_SLOT_TYPE`, `BLUEPRINT_TYPE_TO_SLOT_TYPE`, `SLOT_TYPE_DISPLAY_NAMES`, `SLOT_GROUPS` constants
    - Implement `getCompatibleBlueprintTypes(slotType)` and `generateSlotsFromHull(properties)` functions
    - _Requirements: 2.4, 3.1, 3.4, 4.2, 10.1, 13.1, 13.2_
  - [~] 1.2 Write unit tests for slot type mappings
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/slotTypes.test.ts`
    - Test `generateSlotsFromHull` with various hull properties (zero, positive, missing)
    - Test `getCompatibleBlueprintTypes` for each slot type
    - Test display name completeness (every slot type has a display name)
    - _Requirements: 2.4, 13.1, 13.2_
  - [~] 1.3 Write property test for slot generation (Property 9)
    - **Property 9: Slot generation matches hull properties**
    - **Validates: Requirements 2.4**
    - Add to `slotTypes.test.ts` or create `slotTypes.property.test.ts`
    - For any hull with property "X Slots": N (N > 0), `generateSlotsFromHull` produces exactly N slots of the corresponding type

- [ ] 2. Implement stats computation pure function
  - [-] 2.1 Create `computeShipStats.ts` with the pure computation function
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/computeShipStats.ts`
    - Define `ShipStats`, `WeaponSustainEntry`, `MiningSustainEntry` interfaces
    - Implement Phase 1: accumulate raw stats (additive sums, ScanLevel max, eng capacity, power/shield/weapon draws)
    - Implement Phase 2: compute derived stats (AccelerationFactor, TurnRate, JumpFuelPerJAS, JumpFuelRange, ShieldUptime, WeaponSustainTime, per-type sustainability)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_
  - [~] 2.2 Write unit tests for `computeShipStats`
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/computeShipStats.test.ts`
    - Test additive sums, ScanLevel max, derived stats, edge cases (zero mass, no components, infinite sustain)
    - _Requirements: 5.2, 5.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_
  - [~] 2.3 Write property test: pure function determinism (Property 1)
    - **Property 1: Stats computation is a pure function of inputs**
    - **Validates: Requirements 9.1**
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/computeShipStats.property.test.ts`
  - [~] 2.4 Write property test: additive commutativity (Property 2)
    - **Property 2: Additive properties are commutative**
    - **Validates: Requirements 9.2**
  - [~] 2.5 Write property test: ScanLevel maximum (Property 3)
    - **Property 3: ScanLevel uses maximum, not sum**
    - **Validates: Requirements 9.3**
  - [~] 2.6 Write property test: engineering capacity separation (Property 4)
    - **Property 4: Engineering capacity separates hull from components**
    - **Validates: Requirements 9.4**
  - [~] 2.7 Write property test: derived stats consistency (Property 5)
    - **Property 5: Derived stats are consistent with raw stats**
    - **Validates: Requirements 5.4, 9.7**
  - [~] 2.8 Write property test: shield uptime infinite (Property 6)
    - **Property 6: Shield uptime infinite when draw ≤ regen**
    - **Validates: Requirements 5.4**
  - [~] 2.9 Write property test: weapon sustain infinite (Property 7)
    - **Property 7: Weapon sustain time infinite when draw ≤ regen**
    - **Validates: Requirements 5.4**
  - [~] 2.10 Write property test: missing properties default to zero (Property 12)
    - **Property 12: Missing properties default to zero**
    - **Validates: Requirements 9.2**

- [ ] 3. Implement URL codec
  - [-] 3.1 Create `urlCodec.ts` with encode/decode functions
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/urlCodec.ts`
    - Implement `encodeBuild(hullUUID, slots)` — compact hash fragment format
    - Implement `decodeBuild(hash)` — parse hash back to hull + component UUIDs
    - Define `DecodedBuild` and `DecodeError` interfaces
    - _Requirements: 11.1, 11.4, 11.7_
  - [~] 3.2 Write unit tests for URL codec
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/urlCodec.test.ts`
    - Test encode/decode round-trip, empty slots, malformed input, invalid UUIDs, missing hull
    - _Requirements: 11.1, 11.4, 11.5, 11.6, 11.7_
  - [~] 3.3 Write property test: URL encode/decode round-trip (Property 11)
    - **Property 11: URL encode/decode round-trip**
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.4**

- [~] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Implement Zustand store
  - [~] 5.1 Create `shipBuilderStore.ts` with state management
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/shipBuilderStore.ts`
    - Define `ComponentSlot`, `BlueprintSummary`, `BlueprintDetail`, `ShipBuilderState` interfaces
    - Implement `loadBlueprintList` action (fetch from public API, page=1, pageSize=10000)
    - Implement `selectHull` action (fetch detail, generate slots, compute stats)
    - Implement `installComponent` action (fetch detail if not cached, update slot, recompute stats)
    - Implement `clearBuild` action (reset hull, slots, stats; preserve cache)
    - Implement `restoreFromURL` action (decode URL, restore hull + components)
    - Implement blueprint cache logic and duplicate request prevention via `loadingUUIDs`
    - _Requirements: 2.3, 2.4, 2.5, 4.4, 4.5, 4.6, 4.7, 5.1, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 7.1, 7.2, 7.3, 7.4, 11.2, 11.5, 11.6_
  - [~] 5.2 Write unit tests for the Zustand store
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/shipBuilderStore.test.ts`
    - Test loadBlueprintList, selectHull, installComponent, clearBuild, blueprint cache behavior, error states
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 7.1, 7.2, 7.3, 7.4_
  - [~] 5.3 Write property test: blueprint cache prevents redundant fetches (Property 8)
    - **Property 8: Blueprint cache prevents redundant fetches**
    - **Validates: Requirements 6.2, 6.3**
  - [~] 5.4 Write property test: component filtering respects slot type AND class (Property 10)
    - **Property 10: Component filtering respects slot type AND class**
    - **Validates: Requirements 4.2, 4.3**

- [~] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. Implement UI components - Hull selection and slot display
  - [~] 7.1 Create `HullSelector.tsx` component
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/HullSelector.tsx`
    - Searchable dropdown showing hull name and class
    - Filter by case-insensitive substring match
    - Loading and error states
    - _Requirements: 2.1, 2.2, 8.1, 8.2, 8.3, 8.4, 8.5_
  - [~] 7.2 Create `SlotGrid.tsx`, `SlotGroup.tsx`, and `SlotItem.tsx` components
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/SlotGrid.tsx`
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/SlotGroup.tsx`
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/SlotItem.tsx`
    - SlotGrid groups slots by SLOT_GROUPS, hides empty groups
    - SlotItem uses FilteredDropdown for component selection, filtered by slot type + class
    - Includes empty/none option for clearing a slot
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.6, 4.8, 8.6, 10.1, 10.2_

- [ ] 8. Implement UI components - Stats display
  - [~] 8.1 Create `ShipStatsDisplay.tsx` component
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/ShipStatsDisplay.tsx`
    - Display stat categories: Engineering, Capacity, Defence, Propulsion, Jump, Power, Mining, Weapons, Scanning
    - Engineering capacity overrun visual indicator
    - Shield/weapon uptime "infinite" display when applicable
    - Weapon sustain by type and mining sustain by type
    - _Requirements: 5.3, 5.4, 5.5, 5.6, 10.3_

- [ ] 9. Implement page component and routing
  - [~] 9.1 Create `ShipBuilder.tsx` main page component
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/ShipBuilder.tsx`
    - Compose HullSelector, SlotGrid, ShipStatsDisplay, and Clear Build toolbar
    - Responsive layout: two-column at lg breakpoint, stacked below
    - Trigger `loadBlueprintList` on mount
    - URL state sync: update URL on build change, restore from URL on load
    - _Requirements: 1.1, 1.3, 1.4, 7.4, 11.1, 11.2, 11.3, 12.1, 12.2, 12.3_
  - [~] 9.2 Add route and navigation entry
    - Modify `OE2EmpireTracker.Web/src/App.tsx` — add `/ship-builder` route
    - Modify `OE2EmpireTracker.Web/src/components/layout/Sidebar.tsx` — add "Ship Builder" to publicNavItems after Colony Planner
    - _Requirements: 1.1, 1.2_

- [~] 10. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Integration testing
  - [~] 11.1 Write integration test for ShipBuilder page
    - Create `OE2EmpireTracker.Web/src/pages/ship-builder/ShipBuilder.test.tsx`
    - Test: select hull → slots appear, install component → stats update, clear build → reset
    - Test: URL restore on load, error states for failed fetches
    - _Requirements: 1.1, 2.3, 2.4, 4.5, 5.1, 7.4, 8.1, 11.2, 11.5, 11.6, 11.7_

- [~] 12. Final checkpoint - Ensure all tests pass and build succeeds
  - Ensure all tests pass, ask the user if questions arise.
  - Run `npm run build` from OE2EmpireTracker.Web/ to verify production build

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The implementation follows the Colony Planner pattern (Zustand store, public API, React components)
- All code is TypeScript, running in the existing Vite + React + Vitest setup
- `computeShipStats` is a pure function enabling independent property-based testing
- Blueprint cache and duplicate request prevention are store-level concerns

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "3.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7", "2.8", "2.9", "2.10", "3.2", "3.3"] },
    { "id": 2, "tasks": ["5.1"] },
    { "id": 3, "tasks": ["5.2", "5.3", "5.4"] },
    { "id": 4, "tasks": ["7.1", "7.2", "8.1"] },
    { "id": 5, "tasks": ["9.1"] },
    { "id": 6, "tasks": ["9.2"] },
    { "id": 7, "tasks": ["11.1"] }
  ]
}
```
