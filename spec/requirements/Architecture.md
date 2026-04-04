# Architecture Requirements

## Persistence

**REQ-ARCH-001** All player data (profiles, blueprints, surveys, colonies) SHALL be persisted to a single JSON file via PlayerContext.writeContext().  
**REQ-ARCH-002** The data file path SHALL be configurable; the default SHALL be `..\..\PlayerData.json` relative to the executable.  
**REQ-ARCH-003** PlayerContext SHALL be a singleton; EmpireContext.PlayerContext SHALL be the single access point.  
**REQ-ARCH-004** On load, if the data file does not exist, PlayerContext SHALL initialize with empty lists and not throw.  
**REQ-ARCH-005** All lists (profiles, blueprints, surveys, colonies) SHALL be sorted on load: profiles by Name, blueprints by Name, surveys by PlanetName then DateTime, colonies by PlanetName.

## MVVM Pattern

**REQ-ARCH-010** UI forms SHALL NOT directly access PropertyBag methods (getBoolean, setProperty, etc.) on data objects. All such access SHALL go through a ViewModel.  
**REQ-ARCH-011** UI forms SHALL NOT directly manipulate Colony.Structures, Colony.Items, or Colony.Commodities lists. All such access SHALL go through ColonyViewModel.  
**REQ-ARCH-012** ViewModels SHALL be plain C# classes with no WinForms dependencies.  
**REQ-ARCH-013** ViewModels SHALL expose typed properties (bool IsBuilt, string PlanetName) rather than raw PropertyBag access.  
**REQ-ARCH-014** The MVVM pattern established in FormColony/ColonyStructure SHALL serve as the template for applying the same pattern to other forms.

## Unit Testing

**REQ-ARCH-020** All data classes SHALL have unit tests covering their public API.  
**REQ-ARCH-021** Tests SHALL NOT depend on file I/O, EmpireContext singleton initialization, or WinForms message loops.  
**REQ-ARCH-022** Tests that involve time-sensitive values (CountDownTime) SHALL use a tolerance of at least 2 seconds.  
**REQ-ARCH-023** JSON round-trip tests SHALL verify that serializing then deserializing produces an object equal to the original.  
**REQ-ARCH-024** ColonyStatusCalculator SHALL have unit tests using mock IColonyStructureWorkers implementations, without requiring a full EmpireContext.

## Logging

**REQ-ARCH-030** The application SHALL use NLog for all logging.  
**REQ-ARCH-031** Log files SHALL be written to a `logs/` subdirectory next to the executable, rolling daily, retaining 7 days.  
**REQ-ARCH-032** Debug and above SHALL be written to the Visual Studio debugger output window.  
**REQ-ARCH-033** Info and above SHALL be written to the rolling log file.  
**REQ-ARCH-034** All exceptions caught in form event handlers SHALL be logged at Error level.  
**REQ-ARCH-035** PlayerContext load and save operations SHALL be logged at Info level.

## Serialization

**REQ-ARCH-040** Newtonsoft.Json SHALL be used for all JSON serialization.  
**REQ-ARCH-041** Computed properties SHALL be decorated with [JsonIgnore] and not appear in serialized JSON. ExtendedName is a computed UI display property present on Item, Blueprint, Survey, Commodity, and SurveyResource — all SHALL be [JsonIgnore]. ExtendedName concatenates identifying fields (e.g. Class, Evolution, Name, TechLevel, NickName for Blueprint) into a human-readable string for display in combo boxes and grids.  
**REQ-ARCH-042** Custom JsonConverters SHALL be used for PropertyBag, ItemBag, and LockTracking to control the exact JSON format.  
**REQ-ARCH-043** JSON files SHALL be written with Formatting.Indented for human readability.

## Correctness Invariants

**REQ-ARCH-050** For any colony, finalActualStatus SHALL equal the last entry in the per-structure Actual status chain.  
**REQ-ARCH-051** For any colony, Ideal resource values SHALL be >= Actual resource values for all provided resources (Power, Habitation, Food, Entertainment, Warehouse).  
**REQ-ARCH-052** ItemBag.CountByType(t, id) - LockTracking.GetLockedQuantity(t, id) SHALL represent the available (unlocked) quantity of that item type.  
**REQ-ARCH-053** WorkerDetail IDs SHALL match the PropertyBag keys used in ColonyStatusCalculator for worker slot parsing at all times.

## Future Work (Approved but not yet implemented)

**REQ-ARCH-060** LockTracking SHALL be added to Colony and serialized as part of the colony JSON.  
**REQ-ARCH-061** The dgvItems Locked Amount column SHALL display the real locked quantity from LockTracking.  
**REQ-ARCH-062** ActualColonyStructureWorkers.IsUnassignedWorkerAvailable(workerKey) SHALL check whether the colony warehouse (ItemBag) contains at least 1 unlocked WorkDetail item whose BaseItemTypeID equals workerKey (the full WorkerDetail ID: "BlueCollarDetail", "WhiteCollarDetail", or "SpecialistDetail"). Unlocked means CountByType minus GetLockedQuantity > 0.  
**REQ-ARCH-062a** One unassigned worker of a given type supports all structures in the colony that need that type — it is not consumed per structure.  
**REQ-ARCH-062b** When a worker is assigned to a specific structure slot (e.g. BlueCollar1), the corresponding WorkDetail item in the warehouse SHALL be locked via LockTracking using the structure's UUID as the process key and the WorkerDetail ID as the item key.  
**REQ-ARCH-062c** When a worker is unassigned from a structure slot, the corresponding lock SHALL be cleared via LockTracking.ClearLocksForProcess or by removing the specific lock entry.  
**REQ-ARCH-063** A Timed Event Review form SHALL display all active CountDownTime instances sorted by TimeRemaining, with double-click navigation to the owning form.  
**REQ-ARCH-064** A Commodity Delivery form SHALL summarize all CommodityRequested entries across all colonies.  
**REQ-ARCH-065** A Flatpack Building form SHALL list colonies with staged but unbuilt structures.  
**REQ-ARCH-066** A Worker Delivery form SHALL list colonies with built structures that have unassigned workers.

## Colony Timed Processing (Future — partial implementation exists for Mining)

**REQ-ARCH-080** Colony.ProcessColony() SHALL process structures in the following order per cycle:
1. Structure Building (BuildCompletionTime expires → set Built=true)
2. Mining (MiningRig structures → add resources to warehouse)
3. Refining Base Resources
4. Refining S1 Synthetics
5. Refining S2 Synthetics
6. Manufacturing
7. Research

**REQ-ARCH-081** Only Mining (step 2) is currently implemented. Steps 1, 3–7 are planned features.  
**REQ-ARCH-082** Structure Building (step 1): when BuildCompletionTime.IntervalsPassed > 0, the structure SHALL be marked Built=true and BuildCompletionTime SHALL be cleared.  
**REQ-ARCH-083** Refining, Manufacturing, and Research processing types require blueprint definitions that specify inputs, outputs, and cycle times. These SHALL be designed before implementation.

**REQ-ARCH-070** The application SHALL support multiple PlayerProfiles. Each Colony SHALL have an OwnerUUID field identifying the PlayerProfile that owns it.  
**REQ-ARCH-071** PlayerContext SHALL maintain a currently selected player (CurrentPlayerUUID). All colony, blueprint, and survey data SHALL be filterable by the currently selected player.  
**REQ-ARCH-072** Colony.ProcessColony() SHALL look up the owning player's Extraction Focus skill level via the OwnerUUID and apply a `(1 + level * 0.01)` multiplier to mined quantity per interval (REQ-COL-056b).  
**REQ-ARCH-073** The Colony Form SHALL only display colonies owned by the currently selected player.  
**REQ-ARCH-074** Colony.OwnerUUID SHALL be serialized to JSON and SHALL default to empty string for backward compatibility with existing save files.
