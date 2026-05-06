# Data Models — Colony

## Colony Status Calculation Models

### ColonyStructureStatus

Accumulator for colony-wide resource totals computed by ColonyStatusCalculator. Each field tracks a provided/required pair for a resource category. Not serialized  recomputed on demand.

Fields:
- PowerProvided / PowerRequired (decimal)  total power generation vs demand
- HabitationProvision / HabitationRequired (decimal)  housing units vs worker count
- FoodProvision / FoodRequired (decimal)  food production vs worker count
- EntertainmentProvided / EntertainmentRequired (decimal)  entertainment vs worker demand (2 per worker)
- WarehouseCapacity / WarehouseRequired (decimal)  storage volume vs current usage
- UnallocatedBlueCollarPresent / UnallocatedWhiteCollarPresent / UnallocatedSpecialistPresent (bool)  whether unassigned workers of each type exist in the warehouse

### StructureStatusDelta

Per-structure incremental resource contribution. Used for O(1) single-structure recalculation: subtract old delta, add new delta, instead of recomputing the entire colony.

Fields:
- PowerProvided, PowerRequired, HabitationProvision, FoodProvision, EntertainmentProvided, WarehouseCapacity (decimal)  same categories as ColonyStructureStatus but for one structure
- WorkerCount (int)  assigned workers on this structure
- UnallocatedCount (int)  unallocated workers contributed by this structure

### ColonyWorker

Represents a single worker slot on a colony structure. Used internally by ColonyStatusCalculator to track labor allocation.

Fields:
- Structure (ColonyStructure)  the structure this worker is assigned to
- WorkerType (string)  role key (e.g. "BlueCollar1", "WhiteCollar1", "Specialist1")
- Assigned (bool)  whether this slot is currently active

### ResearchTimeEntry

Maps a blueprint evolution level to its research duration. Serialized in BaselineData.json under the ResearchTime array. Used by ResearchTimeLookup for research timer calculations.

Fields:
- Evolution (int)  the current evolution level (0-14)
- ResearchTimeSeconds (long)  duration in seconds to research from this level to the next
