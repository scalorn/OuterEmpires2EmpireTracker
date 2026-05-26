import { describe, it, expect } from 'vitest';
import {
  generateSlotsFromHull,
  getCompatibleBlueprintTypes,
  HULL_PROPERTY_TO_SLOT_TYPE,
  BLUEPRINT_TYPE_TO_SLOT_TYPE,
  SLOT_TYPE_DISPLAY_NAMES,
  SLOT_GROUPS,
} from './slotTypes';

describe('generateSlotsFromHull', () => {
  it('generates correct slots for positive counts', () => {
    const properties = { 'Reactor Slots': '2', 'Shield Slots': '1' };
    const slots = generateSlotsFromHull(properties);

    const reactorSlots = slots.filter(s => s.slotType === 'Reactor');
    const shieldSlots = slots.filter(s => s.slotType === 'Shield');

    expect(reactorSlots).toHaveLength(2);
    expect(reactorSlots[0]).toEqual({ slotType: 'Reactor', slotIndex: 0, blueprintUUID: null });
    expect(reactorSlots[1]).toEqual({ slotType: 'Reactor', slotIndex: 1, blueprintUUID: null });
    expect(shieldSlots).toHaveLength(1);
    expect(shieldSlots[0]).toEqual({ slotType: 'Shield', slotIndex: 0, blueprintUUID: null });
  });

  it('produces no slots for properties with value "0"', () => {
    const properties = { 'Reactor Slots': '0' };
    const slots = generateSlotsFromHull(properties);
    const reactorSlots = slots.filter(s => s.slotType === 'Reactor');
    expect(reactorSlots).toHaveLength(0);
  });

  it('produces no slots for missing properties', () => {
    const properties = {};
    const slots = generateSlotsFromHull(properties);
    expect(slots).toHaveLength(0);
  });

  it('treats non-numeric values as 0 (no slots)', () => {
    const properties = { 'Reactor Slots': 'abc', 'Shield Slots': 'NaN' };
    const slots = generateSlotsFromHull(properties);
    expect(slots).toHaveLength(0);
  });

  it('floors decimal values', () => {
    const properties = { 'Reactor Slots': '2.9' };
    const slots = generateSlotsFromHull(properties);
    const reactorSlots = slots.filter(s => s.slotType === 'Reactor');
    expect(reactorSlots).toHaveLength(2);
  });

  it('ignores properties not in HULL_PROPERTY_TO_SLOT_TYPE', () => {
    const properties = { 'Unknown Slots': '5', 'Reactor Slots': '1' };
    const slots = generateSlotsFromHull(properties);
    expect(slots).toHaveLength(1);
    expect(slots[0].slotType).toBe('Reactor');
  });
});

describe('getCompatibleBlueprintTypes', () => {
  it('returns ["Reactor"] for Reactor slot type', () => {
    expect(getCompatibleBlueprintTypes('Reactor')).toEqual(['Reactor']);
  });

  it('returns all 5 small weapon types for WeaponSmall', () => {
    const types = getCompatibleBlueprintTypes('WeaponSmall');
    expect(types).toHaveLength(5);
    expect(types).toContain('Beamer/Small');
    expect(types).toContain('Railgun/Small');
    expect(types).toContain('CoilGun/Small');
    expect(types).toContain('MissileLauncher/Small');
    expect(types).toContain('TorpedoLauncher/Small');
  });

  it('returns all 5 medium weapon types for WeaponMedium', () => {
    const types = getCompatibleBlueprintTypes('WeaponMedium');
    expect(types).toHaveLength(5);
    expect(types).toContain('Beamer/Medium');
    expect(types).toContain('Railgun/Medium');
    expect(types).toContain('CoilGun/Medium');
    expect(types).toContain('MissileLauncher/Medium');
    expect(types).toContain('TorpedoLauncher/Medium');
  });

  it('returns all 5 large weapon types for WeaponLarge', () => {
    const types = getCompatibleBlueprintTypes('WeaponLarge');
    expect(types).toHaveLength(5);
    expect(types).toContain('Beamer/Large');
    expect(types).toContain('Railgun/Large');
    expect(types).toContain('CoilGun/Large');
    expect(types).toContain('MissileLauncher/Large');
    expect(types).toContain('TorpedoLauncher/Large');
  });

  it('returns empty array for unknown slot type', () => {
    expect(getCompatibleBlueprintTypes('NonExistent')).toEqual([]);
  });

  it('returns single type for non-weapon slot types', () => {
    expect(getCompatibleBlueprintTypes('MainDrive')).toEqual(['MainDrive']);
    expect(getCompatibleBlueprintTypes('Scanner')).toEqual(['SystemObjectScanner']);
    expect(getCompatibleBlueprintTypes('Coupler')).toEqual(['UniversalCoupler']);
    expect(getCompatibleBlueprintTypes('GERTY')).toEqual(['GERTYDroneRack']);
    expect(getCompatibleBlueprintTypes('HullSealant')).toEqual(['HullSealantInjectionUnit']);
    expect(getCompatibleBlueprintTypes('MiningGrapple')).toEqual(['AsteroidGrapple']);
  });
});

describe('display name completeness', () => {
  it('every slot type in HULL_PROPERTY_TO_SLOT_TYPE has a display name', () => {
    const slotTypes = new Set(Object.values(HULL_PROPERTY_TO_SLOT_TYPE));
    for (const slotType of slotTypes) {
      expect(SLOT_TYPE_DISPLAY_NAMES[slotType], `Missing display name for "${slotType}"`).toBeDefined();
    }
  });

  it('every slot type in SLOT_GROUPS is covered by SLOT_TYPE_DISPLAY_NAMES', () => {
    for (const group of SLOT_GROUPS) {
      for (const slotType of group.slotTypes) {
        expect(SLOT_TYPE_DISPLAY_NAMES[slotType], `Missing display name for "${slotType}" in group "${group.label}"`).toBeDefined();
      }
    }
  });

  it('every slot type in BLUEPRINT_TYPE_TO_SLOT_TYPE values has a display name', () => {
    const slotTypes = new Set(Object.values(BLUEPRINT_TYPE_TO_SLOT_TYPE));
    for (const slotType of slotTypes) {
      expect(SLOT_TYPE_DISPLAY_NAMES[slotType], `Missing display name for "${slotType}"`).toBeDefined();
    }
  });
});
