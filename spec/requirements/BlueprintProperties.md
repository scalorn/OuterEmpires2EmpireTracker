# Blueprint Property Validation Requirements

## Property Types

**REQ-BPV-001** Blueprint properties SHALL be classified into the following value types: Integer, Decimal, Boolean, Time, ComboBox, CheckBox, Unknown.
**REQ-BPV-002** Unknown properties SHALL allow free-form text input with no validation.

## Integer Properties (31 total)

**REQ-BPV-010** The following properties SHALL validate as integers (`^[+-]?\d+$`):
BlueCollarDetail, CargoCapacity, CargoVolumeSize, CrewSupported, EngCapacityRequired, EntertainmentProvided, ExtractionEfficiency, FoodProvision, FuelCapacity, HabitationProvision, Health, LargeWeaponMounts, ManufactureSlot, Mass, MaxAllowedOnShip, MaxPerColony, MediumWeaponMounts, MiningSlot, PowerGenerated, PowerProvided, PowerRequired, RefiningRate, RefiningSlot, RemoteOperations, ResearchSlot, SmallWeaponMounts, SpecialistDetail, StructuralIntegrity, UnassignedSpecialistDetail, UnassignedWhiteCollarDetail, WarehouseCapacity.

## Decimal Properties (18 total)

**REQ-BPV-020** The following properties SHALL validate as decimals (`^[+-]?\d+(\.\d+)?$`):
CooldownTime, EnergyDefence, FuelUsed, HPPercentRestored, HullHPPercentRestored, HullManufactureTimeModification, KineticDamageDefence, ManufactureTimeReduction, MaximumDamageRepairRate, MaxJumpDistance, MiningYieldModification, MissileDamageDefence, PowerRegenerationRate, PurityModifier, RefiningTimeModification, ResearchTimeModification, ScanLevel, SensorAbundanceFactor, WearAndTearRate.

## Boolean/CheckBox Properties (3 total)

**REQ-BPV-030** The following properties SHALL render as checkboxes: CanManufacture, CanResearch, Consumable.

## ComboBox Properties (1 total)

**REQ-BPV-040** CommodityIndustry SHALL render as a combo box populated with CommodityIndustry names.

## Time Properties (1 total)

**REQ-BPV-050** ManufactureTime SHALL validate as a time string (`^(\d+d\s*)?(\d+h\s*)?(\d+m\s*)?(\d+s\s*)?$`).

## Commodity Industries (14 types)

**REQ-BPV-060** The following commodity industry types SHALL be available:
Administration Block, Agridome, Centre Of Economics, Engineering Block, Healthcare Institute, Institute Of Defence, Leisure Industry Centre, Logistics Centre, Manufacturing Industry Centre, Mining Industry Centre, Off World Living Institute, Refining Industry Centre, Science Centre, Technology Institute.
