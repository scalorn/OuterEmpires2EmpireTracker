# Colony Requirements

## Colony Data

**REQ-COL-001** A Colony SHALL have a UUID, PlanetName, ColonyName, an ItemBag, a list of ColonyStructures, and a list of CommodityRequested.  
**REQ-COL-001a** ColonyStructure SHALL NOT have redundant resource fields (Power, Habitation, Food, Entertainment, WarehouseCapacity, WorkersAssigned). All calculated resource values SHALL be accessed via `structure.Statuses["Actual"]` and `structure.Statuses["Ideal"]`.  
**REQ-COL-001b** ColonyStructure MAY retain incomplete-feature fields (CurrentAttitude, ContentmentIndex, WageLevel, WageAdjustmentTime) for future use.  
**REQ-COL-001c** ColonyStructure.buildQueueSequence SHALL record the explicit position of the structure in the colony's build queue, independent of its display order in the Structures list. This value SHALL be set when a structure is added to the build queue and SHALL be used by the Flatpack Building Form (REQ-ARCH-065) to determine build priority. The current implementation uses list order as a proxy; explicit assignment is a future task.  
**REQ-COL-002** Colony UUID SHALL be generated as a new GUID when first saved if not already set.  
**REQ-COL-003** Colony SHALL serialize to and deserialize from JSON, preserving all fields including nested structures and items.

## Colony Status Calculation

**REQ-COL-010** ColonyStatusCalculator.CalculateBuilt() SHALL compute cumulative resource status (Power, Habitation, Food, Entertainment, Warehouse) across all structures in order, storing the result per structure in `Statuses["Actual"]` and the final total in `finalActualStatus`.  
**REQ-COL-011** ColonyStatusCalculator.CalculateIdeal() SHALL perform the same calculation assuming all worker slots are filled and all structures are Built=true, Online=true, Staged=false, storing results in `Statuses["Ideal"]` and `finalIdealStatus`.  
**REQ-COL-012** Each resource accumulator SHALL be seeded from the previous structure's status, not from zero, so that status values are cumulative across the structure list.  
**REQ-COL-013** EntertainmentRequired accumulator SHALL be seeded from the previous structure's EntertainmentRequired (not EntertainmentProvided).  
**REQ-COL-014** WarehouseRequired accumulator SHALL be seeded from the previous structure's WarehouseRequired (not WarehouseCapacity).  
**REQ-COL-015** Power, Habitation, Entertainment, and Warehouse resources SHALL only be counted when the structure is Online.  
**REQ-COL-016** Food provision SHALL be counted regardless of Online status.  
**REQ-COL-017** Worker counts (BlueCollar, WhiteCollar, Specialist) SHALL contribute to HabitationRequired, FoodRequired, and EntertainmentRequired.  
**REQ-COL-018** Unallocated worker types (UnassignedBlueCollarDetail etc.) SHALL be counted once per colony, not once per structure.  
**REQ-COL-019** IColonyStructureWorkers.ActualColonyStructureWorkers SHALL read worker state from the structure's AssignedWorkers PropertyBag.  
**REQ-COL-020** IColonyStructureWorkers.IdealColonyStructureWorkers SHALL return true for all worker slots, return Built=true/Staged=false/Online=true for structure state, and be a no-op for SetWorkerAssigned.

## Colony Structure Management (UI)

**REQ-COL-030** The colony form SHALL display a list of all saved colonies filtered by a name search field.  
**REQ-COL-031** Selecting a colony from the list SHALL populate the form with that colony's data.  
**REQ-COL-032** The user SHALL be able to add a flatpack structure to the colony by selecting from a filtered list of Flatpack blueprints and clicking Add.  
**REQ-COL-033** Each structure in the colony SHALL be displayed as a ColonyStructure control showing its blueprint name, sequence number, and Actual/Ideal resource status.  
**REQ-COL-034** The user SHALL be able to reorder structures using Up and Down buttons; the display order SHALL update immediately.  
**REQ-COL-035** The user SHALL be able to delete a structure using a Delete button; the structure SHALL be removed from the colony and the display SHALL update immediately.  
**REQ-COL-036** Pressing the Delete key when a structure control is focused SHALL delete that structure.  
**REQ-COL-037** After any structure change (add, reorder, delete, worker assignment, state change), CalculateBuilt() and CalculateIdeal() SHALL be called and all structure controls SHALL be refreshed.  
**REQ-COL-038** The structure background color SHALL indicate state: Yellow=Staged, PaleVioletRed=Built but Offline, LightGreen=Online with missing workers, Green=Online with all workers.

## Structure State

**REQ-COL-040** Each structure SHALL have Built, Staged, and Online boolean state stored in its Properties PropertyBag.  
**REQ-COL-041** Setting Online=true SHALL also set Built=true and Staged=false.  
**REQ-COL-042** Setting Built=true SHALL also set Staged=false.  
**REQ-COL-043** Setting Staged=true SHALL also set Built=false and Online=false.  
**REQ-COL-044** Worker assignment checkboxes SHALL reflect the current AssignedWorkers state when the control is loaded.  
**REQ-COL-045** Changing a worker assignment checkbox SHALL immediately update the AssignedWorkers PropertyBag and trigger recalculation.

## Mining Rig Structures

**REQ-COL-050** When a structure's blueprint type is MiningRig, the structure control SHALL display survey selection and resource selection combos.  
**REQ-COL-051** The survey combo SHALL be filtered to surveys matching the colony's PlanetName.  
**REQ-COL-052** Selecting a survey SHALL populate the resource combo with that survey's resources.  
**REQ-COL-053** Clicking Start SHALL begin a repeating countdown timer for the mining process.  
**REQ-COL-054** The countdown display SHALL update every second while the timer is running.  
**REQ-COL-055** Clicking Done SHALL call Colony.ProcessColony(), stop the timer, and clear the process state.  
**REQ-COL-056** Colony.ProcessColony() SHALL calculate mined quantity as Amount * IntervalsPassed, add it to the matching resource item in the colony warehouse, and consume the processed intervals.

## Colony Items (Warehouse)

**REQ-COL-060** The user SHALL be able to add items of type Resource, Commodity, WorkDetail, Survey, and Blueprint to the colony warehouse.  
**REQ-COL-061** Resource items SHALL require purity selection; the default purity SHALL be Refined.  
**REQ-COL-062** Commodity items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the commodity Name.  
**REQ-COL-063** WorkDetail items SHALL be selected from BlueCollarDetail, WhiteCollarDetail, SpecialistDetail; BaseItemTypeID SHALL be set to the WorkerDetail ID.  
**REQ-COL-064** Survey items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the Survey UUID.  
**REQ-COL-065** Blueprint items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the Blueprint UUID.  
**REQ-COL-066** The items grid SHALL display ItemType, ExtendedName, locked amount, and quantity for each item.  
**REQ-COL-067** Pressing the Delete key when an item row is selected SHALL remove that item from the colony and refresh the grid.

## Commodity Requests

**REQ-COL-070** The user SHALL be able to add a commodity request by selecting a commodity from a filtered combo and clicking Add.  
**REQ-COL-071** A new CommodityRequested SHALL be created with the commodity Name, Requested quantity from the quantity field, NeedBy date from a date field, Delivered=0, and Fulfilled=false.  
**REQ-COL-071a** CommodityRequested.Fulfilled SHALL be set to true when Delivered >= Requested.  
**REQ-COL-072** The commodity requests grid SHALL display Name, Requested amount, Delivered amount, and NeedBy date for each request.  
**REQ-COL-073** Editing the Amount cell in the grid SHALL immediately update the Requested field of the backing CommodityRequested object.  
**REQ-COL-074** Pressing the Delete key when a commodity request row is selected SHALL remove that request from the colony.  
**REQ-COL-075** The commodity requests grid SHALL use FullRowSelect mode so that clicking any cell selects the entire row.

## Colony Persistence

**REQ-COL-080** Clicking Save SHALL write PlanetName and ColonyName from the form fields to the colony, add the colony to the player context if not already present, and call playerContext.writeContext().  
**REQ-COL-081** The colony list SHALL be populated from playerContext.colonyList on form load.  
**REQ-COL-082** After saving, the colony list SHALL reflect the updated PlanetName and ColonyName.

## MVVM Pattern

**REQ-COL-090** All direct PropertyBag access for Built, Staged, Online, and worker assignment SHALL go through ColonyStructureViewModel, not directly from the UI.  
**REQ-COL-091** All direct Colony.Structures, Colony.Items, and Colony.Commodities list manipulation SHALL go through ColonyViewModel, not directly from the UI.  
**REQ-COL-092** ColonyViewModel.RecalculateStatus() SHALL call both CalculateBuilt() and CalculateIdeal().  
**REQ-COL-093** ColonyViewModel.Save() SHALL ensure UUID is set, add to playerContext if absent, and call writeContext().
