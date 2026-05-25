/**
 * Pure function for computing ship stats from hull and component blueprint properties.
 * Mirrors the C# ShipBuildService.ComputeStats algorithm.
 */

export interface BlueprintDetail {
  uuid: string;
  name: string;
  bluePrintType: string;
  class: number;
  properties: Record<string, string>;
}

export interface ShipStats {
  // Core
  totalMass: number;
  powerGenerated: number;
  powerConsumed: number;
  powerBalance: number;
  engCapacityUsed: number;
  engCapacityAvailable: number;

  // Capacity
  cargoCapacity: number;
  fuelCapacity: number;
  hopperCapacity: number;

  // Defence
  totalHealth: number;
  energyDefence: number;
  kineticDefence: number;
  missileDefence: number;
  shieldHitpoints: number;
  shieldRegen: number;

  // Propulsion (raw + derived)
  acceleration: number;
  rotationalThrust: number;
  accelerationFactor: number;
  turnRate: number;

  // Jump (raw + derived)
  maxJumpDistance: number;
  fuelPerJump: number;
  jumpFuelPerJAS: number;
  jumpFuelRange: number;
  jumpChargeTime: number;

  // Power model
  powerProvided: number;
  powerRegenRate: number;
  shieldPowerDraw: number;
  shieldUptime: number;        // -1 means infinite
  totalWeaponPowerDraw: number;
  weaponSustainTime: number;   // -1 means infinite

  // Per-type sustainability
  weaponSustainByType: WeaponSustainEntry[];
  miningSustainByType: MiningSustainEntry[];

  // Mining
  miningYield: number;
  miningCycleTime: number;

  // Scanning
  scanLevel: number;

  // Weapons installed count
  smallWeaponsInstalled: number;
  mediumWeaponsInstalled: number;
  largeWeaponsInstalled: number;
}

export interface WeaponSustainEntry {
  weaponType: string;
  powerDrawPerSecond: number;
  count: number;
  sustainableCount: number;
}

export interface MiningSustainEntry {
  laserType: string;
  powerDrawPerSecond: number;
  count: number;
  sustainableCount: number;
}

/** Weapon type prefixes — a blueprint is a weapon if its type starts with one of these */
const WEAPON_PREFIXES = ['Beamer', 'CoilGun', 'Railgun', 'MissileLauncher', 'TorpedoLauncher'];

function isWeaponType(blueprintType: string): boolean {
  return WEAPON_PREFIXES.some((prefix) => blueprintType.startsWith(prefix));
}

/** Parse a numeric property value, defaulting to 0 for missing or non-numeric values */
function prop(properties: Record<string, string>, key: string): number {
  const raw = properties[key];
  if (raw === undefined || raw === null) return 0;
  const val = Number(raw);
  return Number.isFinite(val) ? val : 0;
}

/** Determine weapon slot size from blueprint type (e.g. "Beamer/Small" → "WeaponSmall") */
function getWeaponSlotSize(blueprintType: string): 'WeaponSmall' | 'WeaponMedium' | 'WeaponLarge' | null {
  if (blueprintType.endsWith('/Small')) return 'WeaponSmall';
  if (blueprintType.endsWith('/Medium')) return 'WeaponMedium';
  if (blueprintType.endsWith('/Large')) return 'WeaponLarge';
  return null;
}

/**
 * Pure function: computes ship stats from hull properties and installed component properties.
 * Mirrors ShipBuildService.ComputeStats algorithm.
 *
 * @param hullDetail - The hull blueprint detail (properties + type info)
 * @param componentDetails - Array of installed component blueprint details (may contain nulls for empty slots)
 * @returns Computed ShipStats
 */
export function computeShipStats(
  hullDetail: BlueprintDetail,
  componentDetails: (BlueprintDetail | null)[]
): ShipStats {
  // Collect all blueprints (hull + non-null components)
  const allBlueprints: BlueprintDetail[] = [hullDetail, ...componentDetails.filter((c): c is BlueprintDetail => c !== null)];

  // --- Phase 1: Accumulate raw stats ---

  // Additive sums across hull + all components
  let totalMass = 0;
  let powerGenerated = 0;
  let powerConsumed = 0;
  let cargoCapacity = 0;
  let fuelCapacity = 0;
  let hopperCapacity = 0;
  let totalHealth = 0;
  let energyDefence = 0;
  let kineticDefence = 0;
  let missileDefence = 0;
  let shieldHitpoints = 0;
  let shieldRegen = 0;
  let acceleration = 0;
  let rotationalThrust = 0;
  let maxJumpDistance = 0;
  let fuelPerJump = 0;
  let miningYield = 0;
  let miningCycleTime = 0;
  let scanLevel = 0;

  for (const bp of allBlueprints) {
    const p = bp.properties;
    totalMass += prop(p, 'Mass');
    powerGenerated += prop(p, 'Power Generated');
    powerConsumed += prop(p, 'Power Consumed');
    cargoCapacity += prop(p, 'Cargo Capacity');
    fuelCapacity += prop(p, 'Fuel Capacity');
    hopperCapacity += prop(p, 'Raw Material Capacity');
    totalHealth += prop(p, 'Health');
    energyDefence += prop(p, 'Energy Defence');
    kineticDefence += prop(p, 'Kinetic Defence');
    missileDefence += prop(p, 'Missile Defence');
    shieldHitpoints += prop(p, 'Shield Hitpoints');
    shieldRegen += prop(p, 'Shield Regen');
    acceleration += prop(p, 'Acceleration');
    rotationalThrust += prop(p, 'Rotational Thrust');
    maxJumpDistance += prop(p, 'Jump Distance');
    fuelPerJump += prop(p, 'Fuel Per Jump');
    miningYield += prop(p, 'Mining Yield');
    miningCycleTime += prop(p, 'Mining Cycle Time');
    // ScanLevel: maximum, not sum
    scanLevel = Math.max(scanLevel, prop(p, 'Scan Level'));
  }


  // Hull-only: Eng Capacity Available
  const engCapacityAvailable = prop(hullDetail.properties, 'Eng Capacity Available');

  // Component-only accumulations
  let engCapacityUsed = 0;
  let powerProvided = 0;
  let powerRegenRate = 0;
  let shieldPowerDraw = 0;
  let totalWeaponPowerDraw = 0;
  let jumpChargeTime = 0;
  let fuelPerJASPerMass = 0;

  // Per-type tracking
  const weaponDraws = new Map<string, number[]>();
  const miningDraws = new Map<string, number[]>();

  // Weapon count by size
  let smallWeaponsInstalled = 0;
  let mediumWeaponsInstalled = 0;
  let largeWeaponsInstalled = 0;

  const components = componentDetails.filter((c): c is BlueprintDetail => c !== null);

  for (const comp of components) {
    const p = comp.properties;
    const bpType = comp.bluePrintType;

    // Eng capacity from components only
    engCapacityUsed += prop(p, 'Eng Capacity Required');

    // Power draw per second (used by shields, weapons, mining)
    const drawPerSec = prop(p, 'Power Draw Per Second');

    if (bpType === 'Reactor') {
      powerProvided += prop(p, 'Power Provided');
      powerRegenRate += prop(p, 'Power Regeneration Rate');
    } else if (bpType === 'Shield') {
      shieldPowerDraw += drawPerSec;
    } else if (isWeaponType(bpType)) {
      totalWeaponPowerDraw += drawPerSec;
      // Track per-type draws
      const existing = weaponDraws.get(bpType);
      if (existing) {
        existing.push(drawPerSec);
      } else {
        weaponDraws.set(bpType, [drawPerSec]);
      }
      // Count by size
      const slotSize = getWeaponSlotSize(bpType);
      if (slotSize === 'WeaponSmall') smallWeaponsInstalled++;
      else if (slotSize === 'WeaponMedium') mediumWeaponsInstalled++;
      else if (slotSize === 'WeaponLarge') largeWeaponsInstalled++;
    } else if (bpType === 'MiningLaser') {
      const existing = miningDraws.get(bpType);
      if (existing) {
        existing.push(drawPerSec);
      } else {
        miningDraws.set(bpType, [drawPerSec]);
      }
    } else if (bpType === 'NavComp') {
      // Last wins
      const ct = prop(p, 'Jump Charge Time');
      if (ct > 0) jumpChargeTime = ct;
    } else if (bpType === 'JumpDrive') {
      // Last wins
      const fpm = prop(p, 'Fuel Used / JAS / Mass');
      if (fpm > 0) fuelPerJASPerMass = fpm;
    }
  }


  // --- Phase 2: Compute derived stats ---

  const powerBalance = powerGenerated - powerConsumed;

  // Propulsion
  const accelerationFactor = totalMass > 0 ? acceleration / totalMass : 0;
  const turnRate = totalMass > 0 ? rotationalThrust / totalMass : 0;

  // Jump
  const jumpFuelPerJAS = fuelPerJASPerMass * totalMass;
  const jumpFuelRange = jumpFuelPerJAS > 0 ? fuelCapacity / jumpFuelPerJAS : 0;

  // Shield sustainability
  let shieldUptime = 0;
  if (shieldPowerDraw > 0) {
    shieldUptime = powerRegenRate >= shieldPowerDraw
      ? -1
      : powerProvided / (shieldPowerDraw - powerRegenRate);
  }

  // Weapon sustainability (aggregate)
  let weaponSustainTime = 0;
  if (totalWeaponPowerDraw > 0) {
    weaponSustainTime = powerRegenRate >= totalWeaponPowerDraw
      ? -1
      : powerProvided / (totalWeaponPowerDraw - powerRegenRate);
  }

  // Per-type weapon sustainability
  const weaponSustainByType: WeaponSustainEntry[] = [];
  for (const [weaponType, draws] of weaponDraws) {
    const drawPerSec = draws.length > 0 ? draws[0] : 0;
    weaponSustainByType.push({
      weaponType,
      powerDrawPerSecond: drawPerSec,
      count: draws.length,
      sustainableCount: drawPerSec > 0 ? powerRegenRate / drawPerSec : 0,
    });
  }

  // Per-type mining sustainability
  const miningSustainByType: MiningSustainEntry[] = [];
  for (const [laserType, draws] of miningDraws) {
    const drawPerSec = draws.length > 0 ? draws[0] : 0;
    miningSustainByType.push({
      laserType,
      powerDrawPerSecond: drawPerSec,
      count: draws.length,
      sustainableCount: drawPerSec > 0 ? powerRegenRate / drawPerSec : 0,
    });
  }


  return {
    totalMass,
    powerGenerated,
    powerConsumed,
    powerBalance,
    engCapacityUsed,
    engCapacityAvailable,
    cargoCapacity,
    fuelCapacity,
    hopperCapacity,
    totalHealth,
    energyDefence,
    kineticDefence,
    missileDefence,
    shieldHitpoints,
    shieldRegen,
    acceleration,
    rotationalThrust,
    accelerationFactor,
    turnRate,
    maxJumpDistance,
    fuelPerJump,
    jumpFuelPerJAS,
    jumpFuelRange,
    jumpChargeTime,
    powerProvided,
    powerRegenRate,
    shieldPowerDraw,
    shieldUptime,
    totalWeaponPowerDraw,
    weaponSustainTime,
    weaponSustainByType,
    miningSustainByType,
    miningYield,
    miningCycleTime,
    scanLevel,
    smallWeaponsInstalled,
    mediumWeaponsInstalled,
    largeWeaponsInstalled,
  };
}
