import { useState, useMemo, useCallback } from 'react';
import { useShipTemplates, useTemplateMutations } from '../../api/hooks/useShipTemplates';
import { useBlueprints } from '../../api/hooks/useBlueprints';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { ShipStatsPanel } from '../../components/domain/ShipStatsPanel';
import type { ShipTemplate, TemplateSlot, Blueprint, ShipClassDef, SlotDefinition } from '../../api/types/domain';

/** Slot type to blueprint type mapping for filtering compatible blueprints */
const SLOT_TYPE_TO_BLUEPRINT_TYPE: Record<string, string> = {
  reactors: 'Reactor',
  drives: 'Drive',
  weapons: 'Weapon',
  cargo: 'Cargo',
  shields: 'Shield',
};

/** Compute ship stats from assigned blueprints */
function computeStats(
  slots: TemplateSlot[],
  blueprints: Blueprint[],
): { mass: number; powerBalance: number; cargoCapacity: number; defenceRating: number; propulsion: number } {
  let mass = 0;
  let powerBalance = 0;
  let cargoCapacity = 0;
  let defenceRating = 0;
  let propulsion = 0;

  for (const slot of slots) {
    if (!slot.blueprintUUID) continue;
    const bp = blueprints.find((b) => b.uuid === slot.blueprintUUID);
    if (!bp) continue;

    const props = bp.properties ?? {};
    mass += props['Mass'] ?? props['mass'] ?? 0;
    powerBalance += props['Power'] ?? props['power'] ?? 0;
    cargoCapacity += props['Cargo'] ?? props['cargo'] ?? 0;
    defenceRating += props['Defence'] ?? props['defence'] ?? 0;
    propulsion += props['Propulsion'] ?? props['propulsion'] ?? 0;
  }

  return { mass, powerBalance, cargoCapacity, defenceRating, propulsion };
}
