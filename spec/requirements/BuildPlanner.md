# Build Planner Requirements

## Build Plans

**REQ-BPL-001** A BuildPlan SHALL have UUID, Name, OwnerUUID, Description, DeliveryPlanUUID, IsActive flag, and a nested list of BuildItems.  
**REQ-BPL-002** BuildPlan.IsActive SHALL default to true. Inactive plans SHALL be excluded from resource check cascades and delivery generation.  
**REQ-BPL-003** Deleting a BuildPlan SHALL delete all contained BuildItems (cascade delete via nesting).  

## Build Items

**REQ-BPL-010** A BuildItem SHALL have UUID, ItemType (Manufactory/Commodity/ShipTemplate/Mining/Refining/Research), Status (Staged/Delivering/Ready/InProgress/Completed), and quantity.  
**REQ-BPL-011** BuildItem SHALL reference a build location via BuildLocationType (DestinationType enum) and BuildLocationUUID. Default location type is Colony.  
**REQ-BPL-012** BuildItem SHALL support allocation to a specific structure via StructureUUID.  
**REQ-BPL-013** ShipTemplate build items SHALL reference a ShipTemplateUUID and expand into child Manufactory items linked via ParentBuildItemUUID.  
**REQ-BPL-014** ShipTemplate build items SHALL specify an assembly location via AssemblyLocationType and AssemblyLocationUUID.  
**REQ-BPL-015** Mining/Refining build items SHALL store MiningResource, MiningSurveyUUID, RefiningResource, and RefiningPurity fields.  
**REQ-BPL-016** BuildItem.Quantity SHALL represent runs (manufacturing runs, commodity cycles, ships). Output quantity is computed by the service layer from blueprint/commodity metadata.  

## Build Plan Service

**REQ-BPL-020** BuildPlanService.ValidatePlanName SHALL reject empty or duplicate plan names within the same owner.  
**REQ-BPL-021** BuildPlanService.ValidateBuildItem SHALL reject items with missing blueprint/commodity references for their item type.  
**REQ-BPL-022** BuildPlanService.GenerateColonyBuildItems SHALL scan a colony's unstaged structures and create Manufactory build items for each, skipping structures already covered by existing items in the target plan.  

## Resource Check Service

**REQ-BPL-030** ResourceCheckService.ComputeShortfalls SHALL compare required resources for a build item against the allocated colony's warehouse, returning per-resource shortfall quantities.  
**REQ-BPL-031** ResourceCheckService.ComputePlanShortfalls SHALL aggregate shortfalls across all items in a plan.  

## Delivery Generation Service

**REQ-BPL-040** DeliveryGenerationService.GenerateDeliveryPlan SHALL create a delivery plan for resource shortfalls of a single build plan.  
**REQ-BPL-041** DeliveryGenerationService.GenerateConsolidatedDeliveryPlan SHALL merge shortfalls from multiple build plans into a single delivery plan, grouping by manufacturing colony.  
**REQ-BPL-042** DeliveryGenerationService.GenerateFlatpackDeliveryPlan SHALL create a delivery plan for completed build items, grouping flatpacks by destination colony.  

## Queue Calculator

**REQ-BPL-050** QueueCalculator SHALL accept a target duration string (countdown format) and a blueprint or commodity, and compute the number of runs needed to fill that duration.  
**REQ-BPL-051** For manufactories, runs = ceiling(targetSeconds / manufacturingTimeSeconds).  
**REQ-BPL-052** For commodity factories, runs = ceiling(targetSeconds / cycleTimeSeconds), with total output = runs × commoditiesPerCycle.  

## Auto-Assign Service

**REQ-BPL-060** AutoAssignService.ProposeAssignments SHALL match unallocated build items to available structures across colonies, respecting BuildLocationType.  
**REQ-BPL-061** AutoAssignService SHALL support Colony, Station, and Ship location types for future factory ship/station manufacturing.  

## Colony Admin Integration

**REQ-BPL-070** The Colony Administration tab SHALL provide a "Generate Build Plan" button that creates build items for unstaged colony structures.  
**REQ-BPL-071** The user SHALL be prompted to create a new plan or add to an existing plan.  

## Build Planner Form

**REQ-BPL-080** FormBuildPlanner SHALL be an MDI child form with a plan list, plan details panel, and build items grid.  
**REQ-BPL-081** The build items grid SHALL display item type, name, quantity, status, location, and structure allocation.  
**REQ-BPL-082** Build item status SHALL be color-coded in the grid.  
**REQ-BPL-083** The form SHALL provide an IsActive checkbox; inactive plans SHALL be styled with gray italic text.  
**REQ-BPL-084** BuildPlanReferenceCounter SHALL count references from StockPlan.ReplenishmentBuildPlanUUID and display in a Refs column.
