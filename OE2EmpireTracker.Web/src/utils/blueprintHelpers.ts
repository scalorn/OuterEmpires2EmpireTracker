/**
 * Blueprint helper utilities for the Colony Planner.
 * Provides flatpack filtering, sub-type extraction, and property parsing.
 */

export interface BlueprintProperties {
  powerProvided: number;
  powerRequired: number;
  habitationProvision: number;
  foodProvision: number;
  entertainmentProvided: number;
  warehouseCapacity: number;
  workerSlots: number;
}

const FLATPACK_PREFIX = 'flatpacks/';

/**
 * Filter blueprints to only flatpacks (bluePrintType starts with "Flatpacks/").
 * Comparison is case-insensitive.
 */
export function isFlatpack(blueprint: { bluePrintType: string }): boolean {
  return blueprint.bluePrintType.toLowerCase().startsWith(FLATPACK_PREFIX);
}

/**
 * Extract sub-type from bluePrintType (e.g. "Flatpacks/MiningRig" → "MiningRig").
 * Returns everything after the first "Flatpacks/" prefix.
 */
export function extractSubType(bluePrintType: string): string {
  const lowerType = bluePrintType.toLowerCase();
  const prefixIndex = lowerType.indexOf(FLATPACK_PREFIX);
  if (prefixIndex === -1) return bluePrintType;
  return bluePrintType.slice(prefixIndex + FLATPACK_PREFIX.length);
}

/**
 * Parse a numeric property value, returning 0 if missing or non-numeric.
 */
export function parsePropertyValue(value: unknown): number {
  if (value === null || value === undefined) return 0;
  const num = Number(value);
  if (isNaN(num) || !isFinite(num)) return 0;
  return num;
}

/**
 * Extract BlueprintProperties from a blueprint detail response.
 * Reads the `properties` dictionary and parses known keys into typed values.
 * Missing or non-numeric property values default to 0.
 */
export function extractBlueprintProperties(
  detail: Record<string, unknown>
): BlueprintProperties {
  const properties = (detail.properties ?? {}) as Record<string, unknown>;

  const blueCollar = parsePropertyValue(properties['Blue Collar Detail']);
  const whiteCollar = parsePropertyValue(properties['White Collar Detail']);
  const specialist = parsePropertyValue(properties['Specialist Detail']);

  return {
    powerProvided: parsePropertyValue(properties['Power Provided']),
    powerRequired: parsePropertyValue(properties['Power Required']),
    habitationProvision: parsePropertyValue(properties['Habitation Provision']),
    foodProvision: parsePropertyValue(properties['Food Provision']),
    entertainmentProvided: parsePropertyValue(properties['Entertainment Provided']),
    warehouseCapacity: parsePropertyValue(properties['Warehouse Capacity']),
    workerSlots: blueCollar + whiteCollar + specialist,
  };
}
