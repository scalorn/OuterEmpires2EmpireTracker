# Implementation Plan: Ship Enhanced Stats

## Overview

Extend the ship stats computation pipeline to calculate derived performance metrics (acceleration factor, turn rate, jump fuel range, power sustainability) and display them in an organized format matching the in-game ship info panel. Implementation follows the existing architecture: constants → model → service → forms.

## Tasks

- [ ] 1. Add BlueprintPropertyKeys constants and new model types
  - [x] 1.1 Add new BlueprintPropertyKeys constants
    - Add 7 new string constants to `OE2EmpireTracker.Common/Constants/BlueprintPropertyKeys.cs`: EngCapacityRequired, EngCapacityAvailable, PowerProvided, PowerRegenRate, PowerDrawPerSecond, JumpChargeTime, FuelPerJASPerMass
    - Use the exact property name strings from the design document
    - _Requirements: 10.2_

  - [ ] 1.2 Create WeaponSustainEntry POCO
    - Create `OE2EmpireTracker.Common/Models/WeaponSustainEntry.cs`
    - Properties: WeaponType (string), PowerDrawPerSecond (decimal), Count (int), SustainableCount (decimal)
    - All properties default to zero/empty
    - _Requirements: 14.2, 14.6_

  - [ ] 1.3 Create MiningSustainEntry POCO
    - Create `OE2EmpireTracker.Common/Models/MiningSustainEntry.cs`
    - Properties: LaserType (string), PowerDrawPerSecond (decimal), Count (int), SustainableCount (decimal)
    - All properties default to zero/empty
    - _Requirements: 13.2, 13.3, 13.4_

  - [ ] 1.4 Add new properties to ShipStats model
    - Extend `OE2EmpireTracker.Common/Models/ShipStats.cs` with 15 new properties plus 2 list types
    - Add: ShipType (string), ShipClass (int), AccelerationFactor (decimal), TurnRate (decimal), JumpFuelPerJAS (decimal), JumpFuelRange (decimal), JumpChargeTime (decimal), PowerProvided (decimal), PowerRegenRate (decimal), ShieldPowerDraw (decimal), ShieldUptime (decimal), TotalWeaponPowerDraw (decimal), WeaponSustainTime (decimal), WeaponSustainByType (List<WeaponSustainEntry>), MiningSustainByType (List<MiningSustainEntry>)
    - All numeric properties default to zero, lists default to empty, strings default to string.Empty
    - _Requirements: 1.1, 1.2, 3.3, 4.3, 5.4, 6.3, 7.3, 11.3, 12.5_


- [ ] 2. Extend ShipBuildService Phase 1 (power draw collection)
  - [ ] 2.1 Add power draw collection by component type during Phase 1 aggregation
    - Extend `AddBlueprintStats` in `OE2EmpireTracker.Common/Services/ShipBuildService.cs` to accumulate:
      - EngCapacityRequired from components → stats.EngCapacityUsed
      - EngCapacityAvailable from hull → stats.EngCapacityAvailable
      - PowerProvided from reactors → stats.PowerProvided
      - PowerRegenRate from reactors → stats.PowerRegenRate
      - JumpChargeTime from nav comp → stats.JumpChargeTime
      - FuelPerJASPerMass from jump drive → stored for Phase 2
    - Use BlueprintPropertyKeys constants (not hardcoded strings)
    - _Requirements: 2.1, 2.2, 6.1, 7.1, 7.2, 10.2_

  - [ ] 2.2 Collect per-component power draws into typed lists
    - Identify component types via `ReadOnlyBlueprint.BluePrintType` using `BlueprintTypes` constants
    - Collect shield PowerDrawPerSecond → stats.ShieldPowerDraw (sum) 
    - Collect weapon PowerDrawPerSecond → stats.TotalWeaponPowerDraw (sum) + WeaponSustainByType entries grouped by BluePrintType
    - Collect mining laser PowerDrawPerSecond → MiningSustainByType entries grouped by BluePrintType
    - _Requirements: 12.1, 13.1, 14.1, 14.3_

  - [ ] 2.3 Populate ship identity from hull blueprint
    - Set stats.ShipType from hull blueprint Name property
    - Set stats.ShipClass from hull blueprint Class property
    - _Requirements: 1.1, 1.2_


- [ ] 3. Implement ShipBuildService Phase 2 (derived stats computation)
  - [ ] 3.1 Implement propulsion derived stats
    - Compute AccelerationFactor = Acceleration / TotalMass (guard: 0 when TotalMass == 0)
    - Compute TurnRate = RotationalThrust / TotalMass (guard: 0 when TotalMass == 0)
    - _Requirements: 3.1, 3.2, 4.1, 4.2_

  - [ ] 3.2 Implement jump derived stats
    - Compute JumpFuelPerJAS = FuelPerJASPerMass × TotalMass
    - Compute JumpFuelRange = FuelCapacity / JumpFuelPerJAS (guard: 0 when JumpFuelPerJAS == 0)
    - When no jump drive installed, both remain zero
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3.3 Implement shield sustainability
    - When PowerRegenRate >= ShieldPowerDraw → ShieldUptime = -1 (sustainable)
    - When PowerRegenRate < ShieldPowerDraw → ShieldUptime = PowerProvided / (ShieldPowerDraw - PowerRegenRate)
    - _Requirements: 12.2, 12.3, 7.5_

  - [ ] 3.4 Implement weapon sustainability
    - Aggregate: When PowerRegenRate >= TotalWeaponPowerDraw → WeaponSustainTime = -1 (sustainable)
    - Aggregate: When PowerRegenRate < TotalWeaponPowerDraw → WeaponSustainTime = PowerProvided / (TotalWeaponPowerDraw - PowerRegenRate)
    - Per-type: For each WeaponSustainEntry, SustainableCount = PowerRegenRate / PowerDrawPerSecond
    - _Requirements: 14.2, 14.4, 14.5_

  - [ ] 3.5 Implement mining sustainability
    - Per-type: For each MiningSustainEntry, SustainableCount = PowerRegenRate / PowerDrawPerSecond
    - Guard: SustainableCount = 0 when PowerDrawPerSecond == 0
    - _Requirements: 13.2, 13.3_


- [ ] 4. Checkpoint - Core computation logic
  - Ensure all tests pass, ask the user if questions arise.
  - Verify ShipBuildService compiles cleanly with zero warnings
  - Verify backward compatibility: existing stats unchanged for same inputs

- [ ] 5. Property-based tests for computation correctness
  - [ ]* 5.1 Write property test: Additive stats are sums of component values
    - **Property 1: Additive stats are sums of component values**
    - **Validates: Requirements 2.1, 7.1, 7.2, 9.1, 12.1, 14.3**
    - Generate random hull + component blueprints, verify TotalMass, EngCapacityUsed, PowerProvided, PowerRegenRate, ShieldPowerDraw, TotalWeaponPowerDraw equal sums of individual values
    - Test file: `OE2EmpireTracker.Tests/Services/ShipStatsPropertyTests.cs`

  - [ ]* 5.2 Write property test: Acceleration factor formula
    - **Property 2: Acceleration factor formula**
    - **Validates: Requirements 3.1, 3.2, 9.2**
    - For any build with TotalMass > 0 and Acceleration > 0, AccelerationFactor == Acceleration / TotalMass
    - When Acceleration is zero, AccelerationFactor is zero regardless of TotalMass

  - [ ]* 5.3 Write property test: Turn rate formula
    - **Property 3: Turn rate formula**
    - **Validates: Requirements 4.1, 4.2, 9.3**
    - For any build with TotalMass > 0 and RotationalThrust > 0, TurnRate == RotationalThrust / TotalMass
    - When RotationalThrust is zero, TurnRate is zero regardless of TotalMass

  - [ ]* 5.4 Write property test: Jump fuel per JAS formula
    - **Property 4: Jump fuel per JAS formula**
    - **Validates: Requirements 5.1, 5.3**
    - For any build with FuelPerJump > 0 and TotalMass > 0, JumpFuelPerJAS == FuelPerJump × TotalMass
    - When no jump drive (FuelPerJump = 0), JumpFuelPerJAS is zero

  - [ ]* 5.5 Write property test: Jump fuel range formula
    - **Property 5: Jump fuel range formula**
    - **Validates: Requirements 5.2, 5.3, 9.4**
    - For any build where JumpFuelPerJAS > 0 and FuelCapacity > 0, JumpFuelRange == FuelCapacity / JumpFuelPerJAS
    - When JumpFuelPerJAS or FuelCapacity is zero, JumpFuelRange is zero

  - [ ]* 5.6 Write property test: Shield uptime formula
    - **Property 6: Shield uptime formula**
    - **Validates: Requirements 12.2, 12.3, 7.5**
    - When PowerRegenRate >= ShieldPowerDraw, ShieldUptime == -1
    - When PowerRegenRate < ShieldPowerDraw, ShieldUptime == PowerProvided / (ShieldPowerDraw - PowerRegenRate)

  - [ ]* 5.7 Write property test: Mining sustainability formula
    - **Property 7: Mining sustainability formula**
    - **Validates: Requirements 13.2, 13.3**
    - For each mining laser type with PowerDrawPerSecond > 0, SustainableCount == PowerRegenRate / PowerDrawPerSecond

  - [ ]* 5.8 Write property test: Weapon sustainability formula
    - **Property 8: Weapon sustainability formula**
    - **Validates: Requirements 14.2, 14.4, 14.5**
    - Per-type SustainableCount == PowerRegenRate / PowerDrawPerSecond
    - Aggregate WeaponSustainTime == -1 when PowerRegenRate >= TotalWeaponPowerDraw
    - Aggregate WeaponSustainTime == PowerProvided / (TotalWeaponPowerDraw - PowerRegenRate) otherwise


- [ ] 6. Unit tests for specific scenarios
  - [ ]* 6.1 Write unit test: Zero-component build (hull only)
    - Hull only → all derived stats are zero except mass and identity
    - Verify AccelerationFactor, TurnRate, JumpFuelPerJAS, JumpFuelRange, ShieldUptime, WeaponSustainTime all zero
    - Test file: `OE2EmpireTracker.Tests/Services/ShipStatsUnitTests.cs`
    - _Requirements: 3.2, 4.2, 5.3, 6.2, 11.2_

  - [ ]* 6.2 Write unit test: Full combat build
    - Hull + reactor + shields + weapons → verify sustainability values computed correctly
    - Verify ShieldUptime and WeaponSustainTime match expected formulas
    - _Requirements: 12.2, 12.3, 14.4, 14.5_

  - [ ]* 6.3 Write unit test: Mining build
    - Hull + reactor + mining lasers → verify mining sustainability per type
    - Verify SustainableCount = PowerRegenRate / LaserPowerDraw
    - _Requirements: 13.2, 13.3, 13.4_

  - [ ]* 6.4 Write unit test: Jump build
    - Hull + reactor + jump drive + nav comp + fuel tank → verify jump range and charge time
    - Verify JumpFuelPerJAS, JumpFuelRange, JumpChargeTime all populated correctly
    - _Requirements: 5.1, 5.2, 6.1_

  - [ ]* 6.5 Write unit test: Over-budget engineering
    - Components exceed hull eng capacity → verify EngCapacityUsed > EngCapacityAvailable
    - _Requirements: 2.1, 2.2_

  - [ ]* 6.6 Write unit test: Sustainable shields
    - High regen reactor + low-draw shield → ShieldUptime == -1
    - _Requirements: 12.2_

  - [ ]* 6.7 Write unit test: Unsustainable weapons
    - Low regen + many weapons → WeaponSustainTime > 0 (finite)
    - _Requirements: 14.5_

- [ ] 7. Checkpoint - Computation and tests
  - Ensure all tests pass, ask the user if questions arise.
  - Verify property tests run with 100+ iterations each
  - Verify backward compatibility: existing ShipBuildServiceTests still pass


- [ ] 8. Update stats display in FormShipTemplate
  - [ ] 8.1 Update RefreshStats in FormShipTemplate
    - Rewrite `RefreshStats()` in `OE2EmpireTracker/OE2EmpireTracker/Forms/ShipTemplate/FormShipTemplate.cs`
    - Organize stats into logical groups: Identity, Engineering, Capacity, Defence, Propulsion, Jump, Power, Mining, Weapons, Scanning
    - Show derived values alongside raw values (e.g., "Accel Factor: X.XX (Raw: X)")
    - Use consistent 2 decimal places for rates, factors, and sustainability counts
    - Show "Sustainable" or "X.XXs uptime" for shield/weapon sustainability
    - Show per-type sustainability lines for weapons and mining lasers
    - Omit mining sustainability section when no mining lasers installed
    - Omit weapon sustainability section when no weapons installed
    - Show over-budget warning indicator when EngCapacityUsed > EngCapacityAvailable
    - _Requirements: 1.3, 2.3, 2.4, 5.5, 8.1, 8.2, 8.3, 8.4, 8.5, 12.4, 13.4, 13.5, 14.6, 14.7_

- [ ] 9. Update stats display in FormShipInstance
  - [ ] 9.1 Update RefreshStats in FormShipInstance
    - Rewrite `RefreshStats()` in `OE2EmpireTracker/OE2EmpireTracker/Forms/ShipInstance/FormShipInstance.cs`
    - Use the same organized format as FormShipTemplate (identical grouping and formatting)
    - Ensure both forms display the same stats in the same format
    - _Requirements: 8.5_


- [ ] 10. Final verification and audit
  - [ ] 10.1 Verify backward compatibility
    - Run existing ShipBuildServiceTests — all must pass unchanged
    - Verify ComputeStats method signature is unchanged (no new parameters)
    - Verify all previously-existing stats produce identical values for same inputs
    - _Requirements: 11.1, 11.2, 11.3_

  - [ ] 10.2 Verify extensibility
    - Confirm new properties don't break existing consumers
    - Confirm all blueprint property lookups use BlueprintPropertyKeys constants
    - Confirm display format is structured for easy addition of new stat lines
    - _Requirements: 10.1, 10.2, 10.3_

  - [ ] 10.3 Build and audit
    - Full solution build with zero errors and zero warnings
    - Run `node .kiro/tools/audit.js` — no new findings
    - All tests pass via vstest.console
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (Properties 1-8)
- Unit tests validate specific example scenarios and edge cases
- The implementation language is C# (.NET Framework 4.8.1) as specified in the design
- FsCheck 2.16.6 with NUnit is used for property-based tests (already in the test project)
- All new model files go in `OE2EmpireTracker.Common/Models/`
- All new constants go in `OE2EmpireTracker.Common/Constants/BlueprintPropertyKeys.cs`
- Service logic stays in `OE2EmpireTracker.Common/Services/ShipBuildService.cs`

## Requirements Traceability Matrix

| Requirement | Criteria | Covered By Task(s) |
|-------------|----------|---------------------|
| Req 1 (Ship Identity) | 1.1, 1.2, 1.3 | 2.3, 1.4, 8.1 |
| Req 2 (Engineering Capacity) | 2.1, 2.2, 2.3, 2.4 | 2.1, 8.1 |
| Req 3 (Acceleration Factor) | 3.1, 3.2, 3.3 | 3.1, 1.4 |
| Req 4 (Turn Rate) | 4.1, 4.2, 4.3 | 3.1, 1.4 |
| Req 5 (Jump Fuel Range) | 5.1, 5.2, 5.3, 5.4, 5.5 | 3.2, 1.4, 8.1 |
| Req 6 (Jump Charge Time) | 6.1, 6.2, 6.3 | 2.1, 1.4 |
| Req 7 (Power Model) | 7.1, 7.2, 7.3, 7.4, 7.5 | 2.1, 1.4, 8.1, 3.3 |
| Req 8 (Display Format) | 8.1, 8.2, 8.3, 8.4, 8.5 | 8.1, 9.1 |
| Req 9 (Correctness) | 9.1, 9.2, 9.3, 9.4 | 5.1-5.8, 10.3 |
| Req 10 (Extensibility) | 10.1, 10.2, 10.3 | 1.1, 1.4, 10.2 |
| Req 11 (Backward Compat) | 11.1, 11.2, 11.3 | 10.1, 1.4 |
| Req 12 (Shield Sustain) | 12.1, 12.2, 12.3, 12.4, 12.5 | 2.2, 3.3, 8.1, 1.4 |
| Req 13 (Mining Sustain) | 13.1, 13.2, 13.3, 13.4, 13.5 | 2.2, 3.5, 8.1, 1.3 |
| Req 14 (Weapon Sustain) | 14.1-14.7 | 2.2, 3.4, 8.1, 1.2 |

