# Requirements Document

## Introduction

The Ship Template form currently computes and displays basic aggregated stats (mass, power, cargo, fuel, health, defence, acceleration, thrust, jump distance, mining, scanning). The game's ship info panel shows additional derived stats that require formulas combining multiple blueprint properties with total mass. This feature adds those derived/computed stats to match the in-game display, providing players with accurate performance metrics for their ship builds.

## Glossary

- **Stats_Calculator**: The service component (ShipBuildService.ComputeStats) that computes ship statistics from hull and component blueprints
- **Ship_Stats_Model**: The ShipStats class that holds all computed stat values for a ship build
- **Stats_Display**: The rich text box (rtbStats) in FormShipTemplate and FormShipInstance that renders computed stats
- **Hull_Blueprint**: The ReadOnlyBlueprint for the ship's hull, containing identity (Name, Class, BluePrintType) and capacity properties (Eng Capacity Available, Fuel Capacity, Cargo Capacity)
- **Component_Blueprint**: A ReadOnlyBlueprint for an installed component (drive, thruster, jump drive, nav comp, reactor, etc.)
- **Total_Mass**: The sum of Mass properties from the hull blueprint and all installed component blueprints
- **Engineering_Capacity**: A hull resource measured in units; components consume it (Eng Capacity Required) and the hull provides it (Eng Capacity Available)
- **JAS**: Jump Astral Seconds — the in-game unit of jump distance
- **Acceleration_Factor**: A derived value representing max acceleration, computed as drive Acceleration Rate divided by Total_Mass
- **Turn_Rate**: A derived value representing rotational speed in degrees per second, computed as thruster Rotational Thrust divided by Total_Mass
- **Jump_Fuel_Range**: The total jump distance achievable on a full fuel tank, computed from Fuel Capacity, fuel consumption rate, and Total_Mass
- **Power_Regeneration_Rate**: The rate at which reactors generate power (MW), sourced from the "Power Regeneration Rate" blueprint property


## Requirements

### Requirement 1: Ship Identity Stats

**User Story:** As a player, I want to see the ship type and class derived from the hull blueprint, so that I can quickly identify what kind of ship a template produces.

#### Acceptance Criteria

1. WHEN a hull blueprint is assigned to a template, THE Stats_Calculator SHALL populate the ship type from the hull blueprint Name property
2. WHEN a hull blueprint is assigned to a template, THE Stats_Calculator SHALL populate the ship class from the hull blueprint Class property
3. THE Stats_Display SHALL show the ship type and class as the first line of the stats output

### Requirement 2: Engineering Capacity

**User Story:** As a player, I want to see engineering capacity used versus available, so that I can tell if my build exceeds the hull's engineering budget.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL sum the "Eng Capacity Required" property from all installed component blueprints to compute engineering capacity used
2. THE Stats_Calculator SHALL read the "Eng Capacity Available" property from the hull blueprint to determine total engineering capacity
3. THE Stats_Display SHALL show engineering capacity as "used / available" format
4. IF the engineering capacity used exceeds the engineering capacity available, THEN THE Stats_Display SHALL visually indicate the over-budget condition

### Requirement 3: Max Acceleration Factor

**User Story:** As a player, I want to see the effective acceleration factor for my ship build, so that I can compare propulsion performance across different configurations.

#### Acceptance Criteria

1. WHEN a drive component with an Acceleration property is installed, THE Stats_Calculator SHALL compute the max acceleration factor as Acceleration divided by Total_Mass
2. IF no drive component is installed, THEN THE Stats_Calculator SHALL set the max acceleration factor to zero
3. THE Ship_Stats_Model SHALL store the max acceleration factor as a decimal value


### Requirement 4: Turn Rate

**User Story:** As a player, I want to see the effective turn rate for my ship build, so that I can evaluate maneuverability.

#### Acceptance Criteria

1. WHEN a thruster component with a Rotational Thrust property is installed, THE Stats_Calculator SHALL compute the turn rate as Rotational Thrust divided by Total_Mass
2. IF no thruster component is installed, THEN THE Stats_Calculator SHALL set the turn rate to zero
3. THE Ship_Stats_Model SHALL store the turn rate as a decimal value representing degrees per second

### Requirement 5: Jump Fuel Range and Fuel Per JAS

**User Story:** As a player, I want to see how far my ship can jump on a full fuel tank and how much fuel each JAS costs, so that I can plan long-distance travel and compare jump drive efficiency.

#### Acceptance Criteria

1. WHEN a jump drive with a "Fuel Used / JAS / Mass" property is installed, THE Stats_Calculator SHALL compute Jump Fuel Per JAS as the property value multiplied by Total_Mass
2. WHEN fuel capacity and Jump Fuel Per JAS are both non-zero, THE Stats_Calculator SHALL compute Jump Fuel Range as Fuel Capacity divided by Jump Fuel Per JAS
3. IF no jump drive is installed or fuel capacity is zero, THEN THE Stats_Calculator SHALL set jump fuel range and fuel per JAS to zero
4. THE Ship_Stats_Model SHALL store both Jump Fuel Per JAS (decimal) and Jump Fuel Range (decimal, in JAS units)
5. THE Stats_Display SHALL show Jump Fuel Range, Jump Single Hop Range, and Jump Fuel Per JAS

### Requirement 6: Jump Charge Time

**User Story:** As a player, I want to see the jump charge time for my ship, so that I can evaluate how quickly I can initiate jumps.

#### Acceptance Criteria

1. WHEN a nav comp component is installed, THE Stats_Calculator SHALL read the "Jump Charge Time" property from the nav comp blueprint
2. IF no nav comp is installed, THEN THE Stats_Calculator SHALL set the jump charge time to zero
3. THE Ship_Stats_Model SHALL store the jump charge time as a decimal value in seconds


### Requirement 7: Power Model (Capacitor and Regeneration)

**User Story:** As a player, I want to see both my power capacitor size and regeneration rate, so that I can understand both burst capacity and sustained power output.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL sum the "Power Provided" property from all reactor blueprints to compute total power capacitor (stored energy bank)
2. THE Stats_Calculator SHALL sum the "Power Regeneration Rate" property from all reactor blueprints to compute total power regeneration (MW/s refill rate)
3. THE Ship_Stats_Model SHALL store both Power Provided (capacitor, decimal MW) and Power Regeneration Rate (decimal MW/s) as separate values
4. THE Stats_Display SHALL show both values: "Power: X MW capacitor, Y MW/s regen"
5. THE power sustainability calculations (Requirements 12, 13, 14) SHALL use Power Provided as the energy bank that depletes when draw exceeds regen, and Power Regeneration Rate as the continuous refill rate

### Requirement 8: Enhanced Stats Display Format

**User Story:** As a player, I want the stats display to show all derived stats in a clear, organized format matching the in-game ship info panel layout.

#### Acceptance Criteria

1. THE Stats_Display SHALL organize stats into logical groups: Identity, Engineering, Capacity, Defence, Propulsion, Jump, Power Sustainability, Mining, Weapons, and Scanning
2. THE Stats_Display SHALL show derived values (acceleration factor, turn rate, jump fuel range, sustainability counts) alongside their raw component values
3. WHEN any component is added or removed, THE Stats_Display SHALL recalculate and refresh all derived stats immediately
4. THE Stats_Display SHALL use consistent decimal formatting for derived values (two decimal places for rates, factors, and sustainability counts)
5. THE enhanced stats SHALL be displayed on BOTH the Ship Template form (FormShipTemplate) AND the Ship form (FormShipInstance)

### Requirement 12: Shield Power Sustainability

**User Story:** As a player, I want to know how long my shields will stay up under continuous use, so that I can evaluate whether my reactor can sustain my shield configuration.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL compute the total shield power draw as the sum of "Power Draw Per Second" from all installed shield components
2. IF Power Regeneration Rate >= total shield power draw, THEN THE Stats_Display SHALL indicate shields are sustainable indefinitely
3. IF Power Regeneration Rate < total shield power draw, THEN THE Stats_Calculator SHALL compute shield uptime as Power Provided (capacitor) divided by (total shield power draw minus Power Regeneration Rate)
4. THE Stats_Display SHALL show shield uptime in seconds (to 2 decimal places) when not sustainable, or "Sustainable" when regen covers the draw
5. THE Ship_Stats_Model SHALL store shield power draw (decimal, MW/s) and shield uptime (decimal, seconds; -1 for sustainable)

### Requirement 13: Mining Laser Power Sustainability

**User Story:** As a player, I want to know how many mining lasers I can run continuously on my power regen, so that I can optimize my mining ship configuration.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL read the "Power Draw Per Second" property from each installed mining laser component
2. THE Stats_Calculator SHALL compute sustainable mining laser count as Power Regeneration Rate divided by single mining laser Power Draw Per Second (to 2 decimal places)
3. IF multiple mining lasers with different power draws are installed, THE Stats_Calculator SHALL compute sustainability per laser type
4. THE Stats_Display SHALL show "X.XX lasers sustainable" for each mining laser type installed
5. IF no mining lasers are installed, THE Stats_Display SHALL omit the mining sustainability section

### Requirement 14: Weapon Power Sustainability

**User Story:** As a player, I want to know how many weapons of each type I can fire continuously and how long my full loadout can sustain fire, so that I can balance firepower against power budget.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL read the "Power Draw Per Second" property from each installed weapon component
2. FOR EACH weapon type installed (e.g., Beamer, Coilgun, Railgun, Missile Launcher, Torpedo Launcher), THE Stats_Calculator SHALL compute sustainable weapon count as Power Regeneration Rate divided by that type's Power Draw Per Second (to 2 decimal places)
3. THE Stats_Calculator SHALL compute total weapon power draw as the sum of Power Draw Per Second from ALL installed weapons
4. IF Power Regeneration Rate >= total weapon power draw, THEN THE Stats_Display SHALL indicate all weapons are sustainable indefinitely
5. IF Power Regeneration Rate < total weapon power draw, THEN THE Stats_Calculator SHALL compute weapon sustain time as Power Provided (capacitor) divided by (total weapon power draw minus Power Regeneration Rate)
6. THE Stats_Display SHALL show per-type sustainability ("X.XX beamers sustainable") AND aggregate sustain time
7. IF no weapons are installed, THE Stats_Display SHALL omit the weapon sustainability section

### Requirement 9: Stats Computation Correctness

**User Story:** As a developer, I want the stats computation to be verifiable through property-based testing, so that formula correctness is maintained as the codebase evolves.

#### Acceptance Criteria

1. FOR ALL valid hull and component blueprint combinations, THE Stats_Calculator SHALL produce a Total_Mass equal to the sum of all individual Mass properties (additive invariant)
2. FOR ALL valid builds, THE Stats_Calculator SHALL produce an acceleration factor that is inversely proportional to Total_Mass when Acceleration is held constant
3. FOR ALL valid builds, THE Stats_Calculator SHALL produce a turn rate that is inversely proportional to Total_Mass when Rotational Thrust is held constant
4. FOR ALL valid builds with a jump drive, THE Stats_Calculator SHALL produce a jump fuel range that increases when Fuel Capacity increases and decreases when Total_Mass increases


### Requirement 10: Extensibility

**User Story:** As a developer, I want the enhanced stats system to be easily extensible, so that additional derived stats can be added in the future without restructuring.

#### Acceptance Criteria

1. THE Ship_Stats_Model SHALL accommodate new derived stat properties without breaking existing consumers
2. THE Stats_Calculator SHALL read new blueprint property keys from BlueprintPropertyKeys constants, not hardcoded strings
3. THE Stats_Display SHALL be structured so that adding a new stat line requires only appending to the format string and adding the corresponding value

### Requirement 11: Backward Compatibility

**User Story:** As a player, I want existing ship templates to continue working correctly after the enhanced stats are added, so that no data migration is required.

#### Acceptance Criteria

1. THE Stats_Calculator SHALL produce identical values for all previously-existing stats (TotalMass, PowerGenerated, PowerConsumed, CargoCapacity, FuelCapacity, TotalHealth, Acceleration, RotationalThrust, MaxJumpDistance, FuelPerJump) when given the same inputs
2. WHEN a ship template has no nav comp or jump drive installed, THE Stats_Calculator SHALL set all jump-related derived stats to zero without error
3. THE Ship_Stats_Model SHALL default all new derived stat properties to zero, ensuring no null reference exceptions for existing templates
