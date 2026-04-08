# Blueprint Property Validation Requirements

## Property Types

**REQ-BPV-001** Blueprint properties SHALL be classified into the following value types: Integer, Decimal, Boolean, Time, ComboBox, CheckBox, Unknown.
**REQ-BPV-002** Unknown properties SHALL allow free-form text input with no validation.

## Integer Properties (57 total)

**REQ-BPV-010** The following properties SHALL validate as integers (`^[+-]?\d+$`):
BlueCollarDetail, CargoCapacity, CargoVolumeSize, CrewSupported, EngCapacityRequired, EntertainmentProvided, ExtractionEfficiency, FoodProvision, FuelCapacity, HabitationProvision, Health, LargeWeaponMounts, MagazineSize, ManufactureSlot, Mass, MaxAllowedOnShip, MaxMiningGrapples, MaxMiningLasers, MaxOreHoppers, MaxPerColony, MaxRange, MediumWeaponMounts, MinRange, MiningCycleTime, MiningSlot, PowerGenerated, PowerProvided, PowerRequired, RawMaterialCapacity, RefiningRate, RefiningSlot, RemoteOperations, ResearchSlot, ShieldHitpoints, SmallWeaponMounts, SpecialistDetail, StructuralIntegrity, UnassignedSpecialistDetail, UnassignedWhiteCollarDetail, WarehouseCapacity, WeaponSlotSize, EngCapacityAvailable, LicenseLevel, MaxHullPlating, MaxHullReinforcement, MaxHullSealantUnits, ReactorSlots, MainDriveSlots, ThrusterSlots, JumpDriveSlots, NavCompSlots, ScannerSlots, ShieldSlots, CargoPodSlots, FuelTankSlots, CouplerSlots, GERTYSlots.

## Decimal Properties (42 total)

**REQ-BPV-020** The following properties SHALL validate as decimals (`^[+-]?\d+(\.\d+)?$`):
AccelerationRate, CooldownTime, DeployTime, EnergyDamageRating, EnergyDefence, EnergyDefenceRating, FuelTransferRate, FuelUsed, FuelUsedPerJASPerMass, HPPercentRestored, HPPercentRestoredForAPart, HullHPPercentRestored, HullManufactureTimeModification, IncreasedHullHPPercent, JumpChargeTime, KineticDamageDefence, KineticDamageRating, KineticDefenceRating, LaunchVelocity, ManufactureTimeReduction, MaxEffectiveRange, MaxJumpDistance, MaximumDamageRepair, MiningYield, MiningYieldIncrease, MiningYieldModification, MissileDamageDefence, MissileDamageRating, MissileDefenceRating, PowerDrawPerSecond, PowerDrawPerShot, PowerRegenerationRate, PurityModifier, RateOfFire, RefiningTimeModification, ReloadTime, ResearchTimeModification, RotationalThrust, ScanLevel, SensorAbundanceFactor, ShieldRegen, WearAndTearRate.

## Boolean/CheckBox Properties (5 total)

**REQ-BPV-030** The following properties SHALL render as checkboxes: CanManufacture, CanResearch, Consumable, MiningCapable, PDTCapable.

## ComboBox Properties (1 total)

**REQ-BPV-040** CommodityIndustry SHALL render as a combo box populated with CommodityIndustry names.

## Time Properties (1 total)

**REQ-BPV-050** ManufactureRunTime SHALL validate as a time string (`^(\d+d\s*)?(\d+h\s*)?(\d+m\s*)?(\d+s\s*)?$`).

## Commodity Industries (14 types)

**REQ-BPV-060** The following commodity industry types SHALL be available:
Administration Block, Agridome, Centre Of Economics, Engineering Block, Healthcare Institute, Institute Of Defence, Leisure Industry Centre, Logistics Centre, Manufacturing Industry Centre, Mining Industry Centre, Off World Living Institute, Refining Industry Centre, Science Centre, Technology Institute.
