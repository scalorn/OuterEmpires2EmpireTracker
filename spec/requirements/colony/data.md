# Colony Data

**REQ-COL-001** A Colony SHALL have a UUID, PlanetName, ColonyName, an ItemBag, a list of ColonyStructures, and a list of CommodityRequested.  
**REQ-COL-001a** ColonyStructure SHALL NOT have redundant resource fields (Power, Habitation, Food, Entertainment, WarehouseCapacity, WorkersAssigned). All calculated resource values SHALL be accessed via `structure.Statuses["Actual"]` and `structure.Statuses["Ideal"]`.  
**REQ-COL-001b** ColonyStructure MAY retain incomplete-feature fields (CurrentAttitude, ContentmentIndex, WageLevel, WageAdjustmentTime) for future use.  
**REQ-COL-001c** ColonyStructure.buildQueueSequence SHALL record the explicit position of the structure in the colony's build queue, independent of its display order in the Structures list. This value SHALL be set when a structure is added to the build queue and SHALL be used by the Flatpack Building Form (REQ-ARCH-065) to determine build priority. The current implementation uses list order as a proxy; explicit assignment is a future task.  
**REQ-COL-002** Colony UUID SHALL be generated as a new GUID when first saved if not already set.  
**REQ-COL-003** Colony SHALL serialize to and deserialize from JSON, preserving all fields including nested structures and items.
