# Colony Status Calculation

**REQ-COL-010** ColonyStatusCalculator.CalculateBuilt() SHALL compute cumulative resource status (Power, Habitation, Food, Entertainment, Warehouse) across all structures in order, storing the result per structure in `Statuses["Actual"]` and the final total in `finalActualStatus`.  
**REQ-COL-011** ColonyStatusCalculator.CalculateIdeal() SHALL perform the same calculation assuming all worker slots are filled and all structures are Built=true, Online=true, Staged=false, storing results in `Statuses["Ideal"]` and `finalIdealStatus`.  
**REQ-COL-012** Each resource accumulator SHALL be seeded from the previous structure's status, not from zero, so that status values are cumulative across the structure list.  
**REQ-COL-013** EntertainmentRequired accumulator SHALL be seeded from the previous structure's EntertainmentRequired (not EntertainmentProvided).  
**REQ-COL-014** WarehouseRequired accumulator SHALL be seeded from the previous structure's WarehouseRequired (not WarehouseCapacity).  
**REQ-COL-015** Power, Habitation, Entertainment, and Warehouse resources SHALL only be counted when the structure is Online.  
**REQ-COL-016** Food provision SHALL be counted regardless of Online status.  
**REQ-COL-017** Each structure's assigned workers SHALL contribute to HabitationRequired, FoodRequired, and EntertainmentRequired for that structure's status entry. A structure with N assigned workers adds N to HabitationRequired and FoodRequired, and N*2 to EntertainmentRequired (entertainment costs 2 per worker).  
**REQ-COL-017b** Unallocated workers (UnassignedBlueCollarDetail, UnassignedWhiteCollarDetail, UnassignedSpecialistDetail) are counted once per colony per type. The first structure in the list that requires an unallocated worker of a given type adds 1 to HabitationRequired, 1 to FoodRequired, and 2 to EntertainmentRequired (consistent with REQ-COL-017's 2x entertainment rule). Subsequent structures that also require the same unallocated worker type do not add again — the `UnallocatedXxxPresent` flag propagates through the status chain to prevent double-counting.  
**REQ-COL-017a** WarehouseRequired SHALL be calculated as the sum of `item.Quantity * item.Volume` across all items in the colony's ItemBag. This value is independent of structure order and SHALL be set on the final status only.  
**REQ-COL-018** Unallocated worker types (UnassignedBlueCollarDetail etc.) SHALL be counted once per colony, not once per structure — superseded by REQ-COL-017b.  
**REQ-COL-019** IColonyStructureWorkers.ActualColonyStructureWorkers SHALL read worker state from the structure's AssignedWorkers PropertyBag.  
**REQ-COL-020** IColonyStructureWorkers.IdealColonyStructureWorkers SHALL return true for all worker slots, return Built=true/Staged=false/Online=true for structure state, and be a no-op for SetWorkerAssigned.

**REQ-COL-110** ColonyStatusCalculator SHALL compute colony-wide resource totals by iterating all structures and accumulating provided/required values for Power, Habitation, Food, Entertainment, and Warehouse capacity.  
**REQ-COL-111** ColonyStructureStatus SHALL track provided vs required pairs for each resource category, plus unallocated worker flags per worker type (BlueCollar, WhiteCollar, Specialist).  
**REQ-COL-112** StructureStatusDelta SHALL represent the incremental resource contribution of a single structure, enabling O(1) recalculation when a single structure changes state.  
**REQ-COL-113** ColonyWorker SHALL represent a worker slot on a structure with Structure reference, WorkerType key, and Assigned flag.  
**REQ-COL-114** ResearchTimeEntry SHALL map evolution levels (0-14) to research durations in seconds. Data SHALL be loaded from BaselineData.json with hardcoded fallbacks in ResearchTimeLookup.  