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
