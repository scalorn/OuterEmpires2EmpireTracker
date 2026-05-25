import { describe, it, expect } from 'vitest';
import { isFlatpack, extractSubType, extractBlueprintProperties, parsePropertyValue } from './blueprintHelpers';

/**
 * Unit tests for blueprintHelpers utility functions.
 * Validates: Correctness Properties 7-8
 */

describe('isFlatpack', () => {
  it('returns true for Flatpacks/MiningRig', () => {
    expect(isFlatpack({ bluePrintType: 'Flatpacks/MiningRig' })).toBe(true);
  });

  it('returns true for Flatpacks/CommodityFactory/Agridome', () => {
    expect(isFlatpack({ bluePrintType: 'Flatpacks/CommodityFactory/Agridome' })).toBe(true);
  });

  it('returns false for Ships/Fighter', () => {
    expect(isFlatpack({ bluePrintType: 'Ships/Fighter' })).toBe(false);
  });

  it('returns true for lowercase flatpacks/Refinery (case-insensitive)', () => {
    expect(isFlatpack({ bluePrintType: 'flatpacks/Refinery' })).toBe(true);
  });

  it('returns true for FLATPACKS/ColonyCommandCentre (case-insensitive)', () => {
    expect(isFlatpack({ bluePrintType: 'FLATPACKS/ColonyCommandCentre' })).toBe(true);
  });

  it('returns false for empty string', () => {
    expect(isFlatpack({ bluePrintType: '' })).toBe(false);
  });

  it('returns false for "Flatpack" without trailing slash', () => {
    expect(isFlatpack({ bluePrintType: 'Flatpack' })).toBe(false);
  });
});

describe('extractSubType', () => {
  it('extracts MiningRig from Flatpacks/MiningRig', () => {
    expect(extractSubType('Flatpacks/MiningRig')).toBe('MiningRig');
  });

  it('extracts CommodityFactory/Agridome from Flatpacks/CommodityFactory/Agridome', () => {
    expect(extractSubType('Flatpacks/CommodityFactory/Agridome')).toBe('CommodityFactory/Agridome');
  });

  it('extracts Refinery from flatpacks/Refinery (case-insensitive prefix)', () => {
    expect(extractSubType('flatpacks/Refinery')).toBe('Refinery');
  });

  it('returns as-is when no Flatpacks prefix (Ships/Fighter)', () => {
    expect(extractSubType('Ships/Fighter')).toBe('Ships/Fighter');
  });
});

describe('extractBlueprintProperties', () => {
  it('extracts valid properties correctly', () => {
    const detail = {
      properties: {
        'Power Provided': '100',
        'Power Required': '50',
        'Habitation Provision': '20',
        'Food Provision': '15',
        'Entertainment Provided': '10',
        'Warehouse Capacity': '500',
        'Blue Collar Detail': '5',
        'White Collar Detail': '3',
        'Specialist Detail': '2',
      },
    };
    const result = extractBlueprintProperties(detail);
    expect(result.powerProvided).toBe(100);
    expect(result.powerRequired).toBe(50);
    expect(result.habitationProvision).toBe(20);
    expect(result.foodProvision).toBe(15);
    expect(result.entertainmentProvided).toBe(10);
    expect(result.warehouseCapacity).toBe(500);
    expect(result.workerSlots).toBe(10); // 5 + 3 + 2
  });

  it('defaults all values to 0 when properties is empty', () => {
    const result = extractBlueprintProperties({ properties: {} });
    expect(result.powerProvided).toBe(0);
    expect(result.powerRequired).toBe(0);
    expect(result.habitationProvision).toBe(0);
    expect(result.foodProvision).toBe(0);
    expect(result.entertainmentProvided).toBe(0);
    expect(result.warehouseCapacity).toBe(0);
    expect(result.workerSlots).toBe(0);
  });

  it('defaults non-numeric values to 0 without affecting others', () => {
    const detail = {
      properties: {
        'Power Provided': 'abc',
        'Food Provision': 'NaN',
        'Warehouse Capacity': '250',
      },
    };
    const result = extractBlueprintProperties(detail);
    expect(result.powerProvided).toBe(0);
    expect(result.foodProvision).toBe(0);
    expect(result.warehouseCapacity).toBe(250);
  });

  it('defaults all values to 0 when no properties key exists', () => {
    const result = extractBlueprintProperties({});
    expect(result.powerProvided).toBe(0);
    expect(result.powerRequired).toBe(0);
    expect(result.habitationProvision).toBe(0);
    expect(result.foodProvision).toBe(0);
    expect(result.entertainmentProvided).toBe(0);
    expect(result.warehouseCapacity).toBe(0);
    expect(result.workerSlots).toBe(0);
  });
});

describe('parsePropertyValue', () => {
  it('returns number as-is for numeric input', () => {
    expect(parsePropertyValue(100)).toBe(100);
  });

  it('parses numeric string "50" to 50', () => {
    expect(parsePropertyValue('50')).toBe(50);
  });

  it('parses decimal string "3.14" to 3.14', () => {
    expect(parsePropertyValue('3.14')).toBe(3.14);
  });

  it('returns 0 for null', () => {
    expect(parsePropertyValue(null)).toBe(0);
  });

  it('returns 0 for undefined', () => {
    expect(parsePropertyValue(undefined)).toBe(0);
  });

  it('returns 0 for non-numeric string "abc"', () => {
    expect(parsePropertyValue('abc')).toBe(0);
  });

  it('returns 0 for NaN', () => {
    expect(parsePropertyValue(NaN)).toBe(0);
  });

  it('returns 0 for Infinity', () => {
    expect(parsePropertyValue(Infinity)).toBe(0);
  });

  it('returns 0 for empty string', () => {
    expect(parsePropertyValue('')).toBe(0);
  });
});
