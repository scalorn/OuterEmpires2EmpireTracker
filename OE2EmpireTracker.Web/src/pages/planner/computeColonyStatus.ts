/**
 * Pure function for colony status computation.
 * Uses "Ideal" mode — all worker slots filled when Online.
 *
 * Rules:
 * - Online: contributes Power, Habitation, Entertainment, Warehouse provisions + workers
 * - Built: contributes workers only (toward Hab/Food/Ent Required) + food provision
 * - Staged: contributes Food Provision only (food accumulates regardless of state)
 * - Workers from Built + Online structures count toward Hab/Food/Ent Required
 * - Entertainment Required = total workers × 2
 * - Warehouse Required = 0 (no inventory in web planner)
 * - Empty list returns null (neutral/empty state)
 */

import type { BlueprintProperties } from '../../utils/blueprintHelpers';

export type { BlueprintProperties } from '../../utils/blueprintHelpers';

export type StructureState = 'Staged' | 'Built' | 'Online';

export interface PlannedStructure {
  id: string;
  blueprintUUID: string;
  name: string;
  subType: string;
  state: StructureState;
  buildQueuePosition: number;
  properties: BlueprintProperties;
}

export interface ColonyStatus {
  powerProvided: number;
  powerRequired: number;
  habitationProvision: number;
  habitationRequired: number;
  foodProvision: number;
  foodRequired: number;
  entertainmentProvided: number;
  entertainmentRequired: number;
  warehouseCapacity: number;
  warehouseRequired: number;
}

/**
 * Computes colony status from a list of planned structures.
 * Returns null for an empty list (indicating the neutral/empty state).
 */
export function computeColonyStatus(
  structures: PlannedStructure[]
): ColonyStatus | null {
  if (structures.length === 0) return null;

  let powerProvided = 0;
  let powerRequired = 0;
  let habitationProvision = 0;
  let foodProvision = 0;
  let entertainmentProvided = 0;
  let warehouseCapacity = 0;
  let totalWorkers = 0;

  for (const structure of structures) {
    const { properties, state } = structure;

    // Food provision accumulates regardless of state
    foodProvision += properties.foodProvision;

    if (state === 'Online') {
      powerProvided += properties.powerProvided;
      powerRequired += properties.powerRequired;
      habitationProvision += properties.habitationProvision;
      entertainmentProvided += properties.entertainmentProvided;
      warehouseCapacity += properties.warehouseCapacity;
      totalWorkers += properties.workerSlots;
    } else if (state === 'Built') {
      totalWorkers += properties.workerSlots;
    }
    // Staged: only food (already added above), no workers, no other provisions
  }

  return {
    powerProvided,
    powerRequired,
    habitationProvision,
    habitationRequired: totalWorkers,
    foodProvision,
    foodRequired: totalWorkers,
    entertainmentProvided,
    entertainmentRequired: totalWorkers * 2,
    warehouseCapacity,
    warehouseRequired: 0,
  };
}
