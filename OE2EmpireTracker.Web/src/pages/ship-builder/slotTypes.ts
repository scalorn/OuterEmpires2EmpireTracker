/**
 * Slot type constants and mappings for the Ship Template Builder.
 * Maps hull blueprint properties to slot types, blueprint types to compatible slots,
 * and provides display names and grouping for the UI.
 */

export interface ComponentSlot {
  slotType: string;
  slotIndex: number;
  blueprintUUID: string | null;
}

/** Maps hull blueprint property names to slot type identifiers */
export const HULL_PROPERTY_TO_SLOT_TYPE: Record<string, string> = {
  'Reactor Slots': 'Reactor',
  'Main Drive Slots': 'MainDrive',
  'Thruster Slots': 'Thruster',
  'Jump Drive Slots': 'JumpDrive',
  'Nav Comp Slots': 'NavComp',
  'Scanner Slots': 'Scanner',
  'Shield Slots': 'Shield',
  'Cargo Pod Slots': 'CargoPod',
  'Fuel Tank Slots': 'FuelTank',
  'Coupler Slots': 'Coupler',
  'GERTY Slots': 'GERTY',
  'Small Weapon Mounts': 'WeaponSmall',
  'Medium Weapon Mounts': 'WeaponMedium',
  'Large Weapon Mounts': 'WeaponLarge',
  'Max Hull Plating': 'HullPlating',
  'Max Hull Reinforcement': 'HullReinforcement',
  'Max Hull Sealant Units': 'HullSealant',
  'Max Mining Lasers': 'MiningLaser',
  'Max Mining Grapples': 'MiningGrapple',
  'Max Ore Hoppers': 'OreHopper',
};

/** Maps blueprint type strings to their compatible slot type */
export const BLUEPRINT_TYPE_TO_SLOT_TYPE: Record<string, string> = {
  'Reactor': 'Reactor',
  'MainDrive': 'MainDrive',
  'Thruster': 'Thruster',
  'JumpDrive': 'JumpDrive',
  'NavComp': 'NavComp',
  'SystemObjectScanner': 'Scanner',
  'Shield': 'Shield',
  'CargoPod': 'CargoPod',
  'FuelTank': 'FuelTank',
  'UniversalCoupler': 'Coupler',
  'GERTYDroneRack': 'GERTY',
  'HullPlating': 'HullPlating',
  'HullReinforcement': 'HullReinforcement',
  'HullSealantInjectionUnit': 'HullSealant',
  'MiningLaser': 'MiningLaser',
  'AsteroidGrapple': 'MiningGrapple',
  'OreHopper': 'OreHopper',
  // Weapons — size suffix determines slot type
  'Beamer/Small': 'WeaponSmall',
  'Railgun/Small': 'WeaponSmall',
  'CoilGun/Small': 'WeaponSmall',
  'MissileLauncher/Small': 'WeaponSmall',
  'TorpedoLauncher/Small': 'WeaponSmall',
  'Beamer/Medium': 'WeaponMedium',
  'Railgun/Medium': 'WeaponMedium',
  'CoilGun/Medium': 'WeaponMedium',
  'MissileLauncher/Medium': 'WeaponMedium',
  'TorpedoLauncher/Medium': 'WeaponMedium',
  'Beamer/Large': 'WeaponLarge',
  'Railgun/Large': 'WeaponLarge',
  'CoilGun/Large': 'WeaponLarge',
  'MissileLauncher/Large': 'WeaponLarge',
  'TorpedoLauncher/Large': 'WeaponLarge',
};

/** Display names for slot types */
export const SLOT_TYPE_DISPLAY_NAMES: Record<string, string> = {
  'Reactor': 'Reactor',
  'MainDrive': 'Main Drive',
  'Thruster': 'Thruster',
  'JumpDrive': 'Jump Drive',
  'NavComp': 'Nav Comp',
  'Scanner': 'Scanner',
  'Shield': 'Shield',
  'CargoPod': 'Cargo Pod',
  'FuelTank': 'Fuel Tank',
  'Coupler': 'Coupler',
  'GERTY': 'GERTY',
  'HullPlating': 'Hull Plating',
  'HullReinforcement': 'Hull Reinforcement',
  'HullSealant': 'Hull Sealant',
  'MiningLaser': 'Mining Laser',
  'MiningGrapple': 'Mining Grapple',
  'OreHopper': 'Ore Hopper',
  'WeaponSmall': 'Small Weapon',
  'WeaponMedium': 'Medium Weapon',
  'WeaponLarge': 'Large Weapon',
};

/** Slot group definitions for display ordering */
export const SLOT_GROUPS: { label: string; slotTypes: string[] }[] = [
  { label: 'Core', slotTypes: ['Reactor', 'MainDrive', 'Thruster', 'JumpDrive', 'NavComp', 'Scanner'] },
  { label: 'Defence', slotTypes: ['Shield', 'HullPlating', 'HullReinforcement', 'HullSealant'] },
  { label: 'Capacity', slotTypes: ['CargoPod', 'FuelTank', 'OreHopper', 'Coupler', 'GERTY'] },
  { label: 'Weapons', slotTypes: ['WeaponSmall', 'WeaponMedium', 'WeaponLarge'] },
  { label: 'Mining', slotTypes: ['MiningLaser', 'MiningGrapple'] },
];

/** Get compatible blueprint types for a given slot type */
export function getCompatibleBlueprintTypes(slotType: string): string[] {
  return Object.entries(BLUEPRINT_TYPE_TO_SLOT_TYPE)
    .filter(([, st]) => st === slotType)
    .map(([bpType]) => bpType);
}

/** Generate slots from hull properties */
export function generateSlotsFromHull(properties: Record<string, string>): ComponentSlot[] {
  const slots: ComponentSlot[] = [];
  for (const [propKey, slotType] of Object.entries(HULL_PROPERTY_TO_SLOT_TYPE)) {
    const count = Math.floor(Number(properties[propKey] ?? '0'));
    if (count > 0) {
      for (let i = 0; i < count; i++) {
        slots.push({ slotType, slotIndex: i, blueprintUUID: null });
      }
    }
  }
  return slots;
}
