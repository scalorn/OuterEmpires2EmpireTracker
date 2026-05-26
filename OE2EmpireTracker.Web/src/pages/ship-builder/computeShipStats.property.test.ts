import { describe, it, expect } from 'vitest';
import fc from 'fast-check';
import { computeShipStats, BlueprintDetail } from './computeShipStats';

// --- Shared Arbitraries ---

const ADDITIVE_PROPERTY_KEYS = [
  'Mass', 'Power Generated', 'Power Consumed', 'Cargo Capacity', 'Fuel Capacity',
  'Raw Material Capacity', 'Health', 'Energy Defence', 'Kinetic Defence', 'Missile Defence',
  'Shield Hitpoints', 'Shield Regen', 'Acceleration', 'Rotational Thrust', 'Jump Distance',
  'Fuel Per Jump', 'Mining Yield', 'Mining Cycle Time',
];

const ALL_PROPERTY_KEYS = [
  ...ADDITIVE_PROPERTY_KEYS,
  'Scan Level', 'Eng Capacity Available', 'Eng Capacity Required',
  'Power Provided', 'Power Regeneration Rate', 'Power Draw Per Second',
  'Jump Charge Time', 'Fuel Used / JAS / Mass',
];

const COMPONENT_TYPES = [
  'Reactor', 'Shield', 'CargoPod', 'MainDrive', 'Thruster',
  'Beamer/Small', 'CoilGun/Medium', 'Railgun/Large', 'MiningLaser',
  'NavComp', 'JumpDrive', 'SystemObjectScanner', 'FuelTank', 'HullPlating',
];

const arbProperties = fc.dictionary(
  fc.constantFrom(...ALL_PROPERTY_KEYS),
  fc.nat({ max: 10000 }).map(String)
);

const arbBlueprintDetail: fc.Arbitrary<BlueprintDetail> = fc.record({
  uuid: fc.uuid(),
  name: fc.string({ minLength: 1, maxLength: 30 }),
  bluePrintType: fc.constantFrom(...COMPONENT_TYPES),
  class: fc.integer({ min: 2, max: 8 }),
  properties: arbProperties,
});

const arbHullDetail: fc.Arbitrary<BlueprintDetail> = fc.record({
  uuid: fc.uuid(),
  name: fc.string({ minLength: 1, maxLength: 30 }),
  bluePrintType: fc.constant('Hull'),
  class: fc.integer({ min: 2, max: 8 }),
  properties: arbProperties,
});

describe('computeShipStats property tests', () => {
  // Property 1: Pure function determinism
  // **Validates: Requirements 9.1**
  it('produces identical output for identical inputs (pure function)', () => {
    fc.assert(
      fc.property(
        arbHullDetail,
        fc.array(fc.oneof(arbBlueprintDetail, fc.constant(null)), { maxLength: 15 }),
        (hull, components) => {
          const result1 = computeShipStats(hull, components);
          const result2 = computeShipStats(hull, components);
          expect(result1).toEqual(result2);
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 2: Additive commutativity
  // **Validates: Requirements 9.2**
  it('component order does not affect additive stats', () => {
    fc.assert(
      fc.property(
        arbHullDetail,
        fc.array(arbBlueprintDetail, { minLength: 2, maxLength: 10 }),
        (hull, components) => {
          const result1 = computeShipStats(hull, components);
          const shuffled = [...components].reverse();
          const result2 = computeShipStats(hull, shuffled);
          expect(result1.totalMass).toBeCloseTo(result2.totalMass);
          expect(result1.cargoCapacity).toBeCloseTo(result2.cargoCapacity);
          expect(result1.totalHealth).toBeCloseTo(result2.totalHealth);
          expect(result1.powerGenerated).toBeCloseTo(result2.powerGenerated);
          expect(result1.powerConsumed).toBeCloseTo(result2.powerConsumed);
          expect(result1.fuelCapacity).toBeCloseTo(result2.fuelCapacity);
          expect(result1.hopperCapacity).toBeCloseTo(result2.hopperCapacity);
          expect(result1.energyDefence).toBeCloseTo(result2.energyDefence);
          expect(result1.kineticDefence).toBeCloseTo(result2.kineticDefence);
          expect(result1.missileDefence).toBeCloseTo(result2.missileDefence);
          expect(result1.shieldHitpoints).toBeCloseTo(result2.shieldHitpoints);
          expect(result1.shieldRegen).toBeCloseTo(result2.shieldRegen);
          expect(result1.acceleration).toBeCloseTo(result2.acceleration);
          expect(result1.rotationalThrust).toBeCloseTo(result2.rotationalThrust);
          expect(result1.engCapacityUsed).toBeCloseTo(result2.engCapacityUsed);
          expect(result1.scanLevel).toBe(result2.scanLevel);
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 3: ScanLevel maximum
  // **Validates: Requirements 9.3**
  it('scanLevel equals the maximum across all blueprints, not the sum', () => {
    fc.assert(
      fc.property(
        fc.nat({ max: 10 }),
        fc.array(fc.nat({ max: 10 }), { minLength: 2, maxLength: 8 }),
        (hullScan, componentScans) => {
          const hull: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000001',
            name: 'TestHull',
            bluePrintType: 'Hull',
            class: 4,
            properties: { 'Scan Level': String(hullScan) },
          };
          const components: BlueprintDetail[] = componentScans.map((scan, i) => ({
            uuid: `00000000-0000-0000-0000-00000000000${i + 2}`,
            name: `Scanner${i}`,
            bluePrintType: 'SystemObjectScanner',
            class: 4,
            properties: { 'Scan Level': String(scan) },
          }));
          const stats = computeShipStats(hull, components);
          const expectedMax = Math.max(hullScan, ...componentScans);
          expect(stats.scanLevel).toBe(expectedMax);
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 4: Engineering capacity separation
  // **Validates: Requirements 9.4**
  it('engCapacityAvailable comes from hull only, engCapacityUsed from components only', () => {
    fc.assert(
      fc.property(
        fc.nat({ max: 500 }),
        fc.nat({ max: 200 }),
        fc.array(fc.nat({ max: 100 }), { minLength: 1, maxLength: 8 }),
        (hullEngAvailable, hullEngRequired, componentEngRequired) => {
          const hull: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000001',
            name: 'TestHull',
            bluePrintType: 'Hull',
            class: 4,
            properties: {
              'Eng Capacity Available': String(hullEngAvailable),
              'Eng Capacity Required': String(hullEngRequired),
            },
          };
          const components: BlueprintDetail[] = componentEngRequired.map((eng, i) => ({
            uuid: `00000000-0000-0000-0000-00000000000${i + 2}`,
            name: `Comp${i}`,
            bluePrintType: 'CargoPod',
            class: 4,
            properties: { 'Eng Capacity Required': String(eng) },
          }));
          const stats = computeShipStats(hull, components);
          // engCapacityAvailable comes exclusively from hull
          expect(stats.engCapacityAvailable).toBe(hullEngAvailable);
          // engCapacityUsed is sum of components only (hull excluded)
          const expectedUsed = componentEngRequired.reduce((sum, v) => sum + v, 0);
          expect(stats.engCapacityUsed).toBe(expectedUsed);
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 5: Derived stats consistency
  // **Validates: Requirements 5.4, 9.7**
  it('accelerationFactor * totalMass equals acceleration, turnRate * totalMass equals rotationalThrust', () => {
    fc.assert(
      fc.property(
        arbHullDetail,
        fc.array(arbBlueprintDetail, { maxLength: 10 }),
        (hull, components) => {
          const stats = computeShipStats(hull, components);
          if (stats.totalMass > 0) {
            expect(stats.accelerationFactor * stats.totalMass)
              .toBeCloseTo(stats.acceleration, 5);
            expect(stats.turnRate * stats.totalMass)
              .toBeCloseTo(stats.rotationalThrust, 5);
          } else {
            expect(stats.accelerationFactor).toBe(0);
            expect(stats.turnRate).toBe(0);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 6: Shield uptime infinite
  // **Validates: Requirements 5.4**
  it('shieldUptime is -1 when draw <= regen, otherwise powerProvided / (draw - regen)', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 5000 }),
        fc.integer({ min: 1, max: 5000 }),
        fc.integer({ min: 1, max: 5000 }),
        fc.boolean(),
        (powerProvided, powerRegen, shieldDraw, drawExceedsRegen) => {
          // Control whether draw > regen or draw <= regen
          const actualRegen = drawExceedsRegen ? Math.min(powerRegen, shieldDraw - 1) : shieldDraw + powerRegen;
          const hull: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000001',
            name: 'TestHull',
            bluePrintType: 'Hull',
            class: 4,
            properties: {},
          };
          const reactor: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000002',
            name: 'Reactor',
            bluePrintType: 'Reactor',
            class: 4,
            properties: {
              'Power Provided': String(powerProvided),
              'Power Regeneration Rate': String(actualRegen),
            },
          };
          const shield: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000003',
            name: 'Shield',
            bluePrintType: 'Shield',
            class: 4,
            properties: {
              'Power Draw Per Second': String(shieldDraw),
            },
          };
          const stats = computeShipStats(hull, [reactor, shield]);
          if (shieldDraw <= actualRegen) {
            expect(stats.shieldUptime).toBe(-1);
          } else {
            const expected = powerProvided / (shieldDraw - actualRegen);
            expect(stats.shieldUptime).toBeCloseTo(expected, 5);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 7: Weapon sustain infinite
  // **Validates: Requirements 5.4**
  it('weaponSustainTime is -1 when draw <= regen, otherwise powerProvided / (draw - regen)', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 5000 }),
        fc.integer({ min: 1, max: 5000 }),
        fc.integer({ min: 1, max: 5000 }),
        fc.boolean(),
        (powerProvided, powerRegen, weaponDraw, drawExceedsRegen) => {
          // Control whether draw > regen or draw <= regen
          const actualRegen = drawExceedsRegen ? Math.min(powerRegen, weaponDraw - 1) : weaponDraw + powerRegen;
          const hull: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000001',
            name: 'TestHull',
            bluePrintType: 'Hull',
            class: 4,
            properties: {},
          };
          const reactor: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000002',
            name: 'Reactor',
            bluePrintType: 'Reactor',
            class: 4,
            properties: {
              'Power Provided': String(powerProvided),
              'Power Regeneration Rate': String(actualRegen),
            },
          };
          const weapon: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000003',
            name: 'Beamer',
            bluePrintType: 'Beamer/Small',
            class: 4,
            properties: {
              'Power Draw Per Second': String(weaponDraw),
            },
          };
          const stats = computeShipStats(hull, [reactor, weapon]);
          if (weaponDraw <= actualRegen) {
            expect(stats.weaponSustainTime).toBe(-1);
          } else {
            const expected = powerProvided / (weaponDraw - actualRegen);
            expect(stats.weaponSustainTime).toBeCloseTo(expected, 5);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  // Property 12: Missing properties default to zero
  // **Validates: Requirements 9.2**
  it('missing properties default to 0, present properties retain their values', () => {
    fc.assert(
      fc.property(
        fc.record({
          mass: fc.nat({ max: 10000 }),
          health: fc.nat({ max: 10000 }),
          cargo: fc.nat({ max: 10000 }),
          includeMass: fc.boolean(),
          includeHealth: fc.boolean(),
          includeCargo: fc.boolean(),
        }),
        ({ mass, health, cargo, includeMass, includeHealth, includeCargo }) => {
          const properties: Record<string, string> = {};
          if (includeMass) properties['Mass'] = String(mass);
          if (includeHealth) properties['Health'] = String(health);
          if (includeCargo) properties['Cargo Capacity'] = String(cargo);

          const hull: BlueprintDetail = {
            uuid: '00000000-0000-0000-0000-000000000001',
            name: 'TestHull',
            bluePrintType: 'Hull',
            class: 4,
            properties,
          };
          const stats = computeShipStats(hull, []);

          // Present properties retain their values
          if (includeMass) {
            expect(stats.totalMass).toBe(mass);
          } else {
            expect(stats.totalMass).toBe(0);
          }
          if (includeHealth) {
            expect(stats.totalHealth).toBe(health);
          } else {
            expect(stats.totalHealth).toBe(0);
          }
          if (includeCargo) {
            expect(stats.cargoCapacity).toBe(cargo);
          } else {
            expect(stats.cargoCapacity).toBe(0);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
