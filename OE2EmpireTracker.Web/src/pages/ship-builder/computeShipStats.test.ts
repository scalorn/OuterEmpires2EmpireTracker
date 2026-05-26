import { describe, it, expect } from 'vitest';
import { computeShipStats, BlueprintDetail } from './computeShipStats';

/** Helper to create a minimal hull blueprint detail */
function makeHull(properties: Record<string, string> = {}): BlueprintDetail {
  return {
    uuid: 'hull-001',
    name: 'Test Hull',
    bluePrintType: 'Hull',
    class: 4,
    properties,
  };
}

/** Helper to create a component blueprint detail */
function makeComponent(
  type: string,
  properties: Record<string, string> = {},
  uuid = 'comp-001'
): BlueprintDetail {
  return {
    uuid,
    name: `Test ${type}`,
    bluePrintType: type,
    class: 4,
    properties,
  };
}

describe('computeShipStats', () => {
  // Test 1: Hull only (no components)
  it('returns stats from hull properties alone when no components installed', () => {
    const hull = makeHull({
      'Mass': '500',
      'Health': '1000',
      'Cargo Capacity': '200',
      'Fuel Capacity': '100',
      'Eng Capacity Available': '50',
      'Scan Level': '2',
    });

    const stats = computeShipStats(hull, []);

    expect(stats.totalMass).toBe(500);
    expect(stats.totalHealth).toBe(1000);
    expect(stats.cargoCapacity).toBe(200);
    expect(stats.fuelCapacity).toBe(100);
    expect(stats.engCapacityAvailable).toBe(50);
    expect(stats.engCapacityUsed).toBe(0);
    expect(stats.scanLevel).toBe(2);
  });

  // Test 2: Additive sums across hull + components
  it('sums additive properties across hull and all components', () => {
    const hull = makeHull({
      'Mass': '500',
      'Health': '1000',
      'Cargo Capacity': '200',
      'Power Generated': '10',
      'Power Consumed': '5',
    });
    const comp1 = makeComponent('CargoPod', {
      'Mass': '50',
      'Health': '100',
      'Cargo Capacity': '300',
      'Power Consumed': '2',
    }, 'comp-001');
    const comp2 = makeComponent('CargoPod', {
      'Mass': '50',
      'Health': '100',
      'Cargo Capacity': '300',
      'Power Consumed': '2',
    }, 'comp-002');

    const stats = computeShipStats(hull, [comp1, comp2]);

    expect(stats.totalMass).toBe(600);
    expect(stats.totalHealth).toBe(1200);
    expect(stats.cargoCapacity).toBe(800);
    expect(stats.powerGenerated).toBe(10);
    expect(stats.powerConsumed).toBe(9);
    expect(stats.powerBalance).toBe(1);
  });

  // Test 3: ScanLevel uses maximum (not sum)
  it('uses maximum ScanLevel across all blueprints, not sum', () => {
    const hull = makeHull({ 'Scan Level': '2' });
    const scanner = makeComponent('SystemObjectScanner', { 'Scan Level': '5' });

    const stats = computeShipStats(hull, [scanner]);

    expect(stats.scanLevel).toBe(5);
  });

  it('ScanLevel from hull wins when it is higher than components', () => {
    const hull = makeHull({ 'Scan Level': '7' });
    const scanner = makeComponent('SystemObjectScanner', { 'Scan Level': '3' });

    const stats = computeShipStats(hull, [scanner]);

    expect(stats.scanLevel).toBe(7);
  });

  // Test 4: EngCapacityAvailable from hull only, EngCapacityUsed from components only
  it('reads EngCapacityAvailable from hull and sums EngCapacityUsed from components', () => {
    const hull = makeHull({
      'Eng Capacity Available': '100',
      'Eng Capacity Required': '999', // hull's eng required should be ignored
    });
    const comp1 = makeComponent('Reactor', {
      'Eng Capacity Required': '20',
      'Eng Capacity Available': '50', // component's eng available should be ignored
    }, 'comp-001');
    const comp2 = makeComponent('Shield', {
      'Eng Capacity Required': '30',
    }, 'comp-002');

    const stats = computeShipStats(hull, [comp1, comp2]);

    expect(stats.engCapacityAvailable).toBe(100);
    expect(stats.engCapacityUsed).toBe(50);
  });

  // Test 5: AccelerationFactor = Acceleration / TotalMass (0 when mass is 0)
  it('computes AccelerationFactor as Acceleration / TotalMass', () => {
    const hull = makeHull({
      'Mass': '200',
      'Acceleration': '100',
    });

    const stats = computeShipStats(hull, []);

    expect(stats.accelerationFactor).toBeCloseTo(0.5, 10);
  });

  it('AccelerationFactor is 0 when TotalMass is 0', () => {
    const hull = makeHull({
      'Mass': '0',
      'Acceleration': '100',
    });

    const stats = computeShipStats(hull, []);

    expect(stats.accelerationFactor).toBe(0);
  });

  // Test 6: TurnRate = RotationalThrust / TotalMass (0 when mass is 0)
  it('computes TurnRate as RotationalThrust / TotalMass', () => {
    const hull = makeHull({
      'Mass': '400',
      'Rotational Thrust': '200',
    });

    const stats = computeShipStats(hull, []);

    expect(stats.turnRate).toBeCloseTo(0.5, 10);
  });

  it('TurnRate is 0 when TotalMass is 0', () => {
    const hull = makeHull({
      'Mass': '0',
      'Rotational Thrust': '200',
    });

    const stats = computeShipStats(hull, []);

    expect(stats.turnRate).toBe(0);
  });

  // Test 7: JumpFuelPerJAS = FuelPerJASPerMass × TotalMass
  it('computes JumpFuelPerJAS as FuelPerJASPerMass * TotalMass', () => {
    const hull = makeHull({ 'Mass': '1000' });
    const jumpDrive = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0.5',
      'Mass': '100',
    });

    const stats = computeShipStats(hull, [jumpDrive]);

    // TotalMass = 1000 + 100 = 1100, FuelPerJASPerMass = 0.5
    expect(stats.jumpFuelPerJAS).toBeCloseTo(550, 10);
  });

  // Test 8: JumpFuelRange = FuelCapacity / JumpFuelPerJAS (0 when JumpFuelPerJAS is 0)
  it('computes JumpFuelRange as FuelCapacity / JumpFuelPerJAS', () => {
    const hull = makeHull({ 'Mass': '1000', 'Fuel Capacity': '500' });
    const jumpDrive = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0.5',
      'Mass': '0',
    });

    const stats = computeShipStats(hull, [jumpDrive]);

    // TotalMass = 1000, JumpFuelPerJAS = 0.5 * 1000 = 500
    // JumpFuelRange = 500 / 500 = 1
    expect(stats.jumpFuelRange).toBeCloseTo(1, 10);
  });

  it('JumpFuelRange is 0 when JumpFuelPerJAS is 0', () => {
    const hull = makeHull({ 'Mass': '1000', 'Fuel Capacity': '500' });

    const stats = computeShipStats(hull, []);

    expect(stats.jumpFuelPerJAS).toBe(0);
    expect(stats.jumpFuelRange).toBe(0);
  });

  // Test 9: ShieldUptime = -1 (infinite) when shieldPowerDraw ≤ powerRegenRate
  it('ShieldUptime is -1 (infinite) when shieldPowerDraw <= powerRegenRate', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '10',
    }, 'reactor-001');
    const shield = makeComponent('Shield', {
      'Power Draw Per Second': '5',
    }, 'shield-001');

    const stats = computeShipStats(hull, [reactor, shield]);

    expect(stats.shieldUptime).toBe(-1);
  });

  // Test 10: ShieldUptime = powerProvided / (shieldPowerDraw - powerRegenRate) when draw > regen
  it('ShieldUptime is powerProvided / (draw - regen) when draw > regen', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '5',
    }, 'reactor-001');
    const shield = makeComponent('Shield', {
      'Power Draw Per Second': '15',
    }, 'shield-001');

    const stats = computeShipStats(hull, [reactor, shield]);

    // ShieldUptime = 1000 / (15 - 5) = 100
    expect(stats.shieldUptime).toBeCloseTo(100, 10);
  });

  it('ShieldUptime is 0 when no shields installed', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '10',
    }, 'reactor-001');

    const stats = computeShipStats(hull, [reactor]);

    expect(stats.shieldUptime).toBe(0);
  });

  // Test 11: WeaponSustainTime = -1 (infinite) when totalWeaponPowerDraw ≤ powerRegenRate
  it('WeaponSustainTime is -1 (infinite) when totalWeaponPowerDraw <= powerRegenRate', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '20',
    }, 'reactor-001');
    const weapon = makeComponent('Beamer/Small', {
      'Power Draw Per Second': '10',
    }, 'weapon-001');

    const stats = computeShipStats(hull, [reactor, weapon]);

    expect(stats.weaponSustainTime).toBe(-1);
  });

  // Test 12: WeaponSustainTime = powerProvided / (totalWeaponPowerDraw - powerRegenRate) when draw > regen
  it('WeaponSustainTime is powerProvided / (draw - regen) when draw > regen', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '500',
      'Power Regeneration Rate': '5',
    }, 'reactor-001');
    const weapon1 = makeComponent('Beamer/Small', {
      'Power Draw Per Second': '10',
    }, 'weapon-001');
    const weapon2 = makeComponent('CoilGun/Medium', {
      'Power Draw Per Second': '15',
    }, 'weapon-002');

    const stats = computeShipStats(hull, [reactor, weapon1, weapon2]);

    // totalWeaponPowerDraw = 10 + 15 = 25, regen = 5
    // WeaponSustainTime = 500 / (25 - 5) = 25
    expect(stats.weaponSustainTime).toBeCloseTo(25, 10);
  });

  it('WeaponSustainTime is 0 when no weapons installed', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '10',
    }, 'reactor-001');

    const stats = computeShipStats(hull, [reactor]);

    expect(stats.weaponSustainTime).toBe(0);
  });

  // Test 13: Weapon count by size (small/medium/large)
  it('counts weapons by slot size', () => {
    const hull = makeHull({ 'Mass': '100' });
    const w1 = makeComponent('Beamer/Small', {}, 'w1');
    const w2 = makeComponent('Railgun/Small', {}, 'w2');
    const w3 = makeComponent('CoilGun/Medium', {}, 'w3');
    const w4 = makeComponent('MissileLauncher/Large', {}, 'w4');
    const w5 = makeComponent('TorpedoLauncher/Large', {}, 'w5');

    const stats = computeShipStats(hull, [w1, w2, w3, w4, w5]);

    expect(stats.smallWeaponsInstalled).toBe(2);
    expect(stats.mediumWeaponsInstalled).toBe(1);
    expect(stats.largeWeaponsInstalled).toBe(2);
  });

  // Test 14: Per-type weapon sustainability
  it('computes per-type weapon sustainability entries', () => {
    const hull = makeHull({ 'Mass': '100' });
    const reactor = makeComponent('Reactor', {
      'Power Provided': '1000',
      'Power Regeneration Rate': '20',
    }, 'reactor-001');
    const w1 = makeComponent('Beamer/Small', {
      'Power Draw Per Second': '5',
    }, 'w1');
    const w2 = makeComponent('Beamer/Small', {
      'Power Draw Per Second': '5',
    }, 'w2');
    const w3 = makeComponent('CoilGun/Medium', {
      'Power Draw Per Second': '10',
    }, 'w3');

    const stats = computeShipStats(hull, [reactor, w1, w2, w3]);

    expect(stats.weaponSustainByType).toHaveLength(2);

    const beamerEntry = stats.weaponSustainByType.find(e => e.weaponType === 'Beamer/Small');
    expect(beamerEntry).toBeDefined();
    expect(beamerEntry!.count).toBe(2);
    expect(beamerEntry!.powerDrawPerSecond).toBe(5);
    expect(beamerEntry!.sustainableCount).toBeCloseTo(4, 10); // 20 / 5 = 4

    const coilEntry = stats.weaponSustainByType.find(e => e.weaponType === 'CoilGun/Medium');
    expect(coilEntry).toBeDefined();
    expect(coilEntry!.count).toBe(1);
    expect(coilEntry!.powerDrawPerSecond).toBe(10);
    expect(coilEntry!.sustainableCount).toBeCloseTo(2, 10); // 20 / 10 = 2
  });

  // Test 15: Empty component array (all nulls)
  it('handles component array with all nulls', () => {
    const hull = makeHull({
      'Mass': '500',
      'Health': '1000',
      'Cargo Capacity': '200',
    });

    const stats = computeShipStats(hull, [null, null, null, null]);

    expect(stats.totalMass).toBe(500);
    expect(stats.totalHealth).toBe(1000);
    expect(stats.cargoCapacity).toBe(200);
    expect(stats.engCapacityUsed).toBe(0);
    expect(stats.powerProvided).toBe(0);
    expect(stats.weaponSustainByType).toHaveLength(0);
  });

  // Test 16: NavComp JumpChargeTime — last wins
  it('uses last NavComp JumpChargeTime (last wins)', () => {
    const hull = makeHull({ 'Mass': '100' });
    const nav1 = makeComponent('NavComp', {
      'Jump Charge Time': '10',
    }, 'nav-001');
    const nav2 = makeComponent('NavComp', {
      'Jump Charge Time': '5',
    }, 'nav-002');

    const stats = computeShipStats(hull, [nav1, nav2]);

    expect(stats.jumpChargeTime).toBe(5);
  });

  it('NavComp JumpChargeTime first wins when last has 0', () => {
    const hull = makeHull({ 'Mass': '100' });
    const nav1 = makeComponent('NavComp', {
      'Jump Charge Time': '10',
    }, 'nav-001');
    const nav2 = makeComponent('NavComp', {
      'Jump Charge Time': '0',
    }, 'nav-002');

    const stats = computeShipStats(hull, [nav1, nav2]);

    // Only overwrite when > 0, so nav2 with 0 doesn't overwrite
    expect(stats.jumpChargeTime).toBe(10);
  });

  // Test 17: JumpDrive FuelPerJASPerMass — last wins
  it('uses last JumpDrive FuelPerJASPerMass (last wins)', () => {
    const hull = makeHull({ 'Mass': '1000' });
    const jd1 = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0.3',
    }, 'jd-001');
    const jd2 = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0.7',
    }, 'jd-002');

    const stats = computeShipStats(hull, [jd1, jd2]);

    // Last wins: 0.7 * 1000 = 700
    expect(stats.jumpFuelPerJAS).toBeCloseTo(700, 10);
  });

  it('JumpDrive FuelPerJASPerMass first wins when last has 0', () => {
    const hull = makeHull({ 'Mass': '1000' });
    const jd1 = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0.5',
    }, 'jd-001');
    const jd2 = makeComponent('JumpDrive', {
      'Fuel Used / JAS / Mass': '0',
    }, 'jd-002');

    const stats = computeShipStats(hull, [jd1, jd2]);

    // Only overwrite when > 0, so jd2 with 0 doesn't overwrite
    expect(stats.jumpFuelPerJAS).toBeCloseTo(500, 10);
  });
});
