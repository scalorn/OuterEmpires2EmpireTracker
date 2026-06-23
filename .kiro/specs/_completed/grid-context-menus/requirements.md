# Requirements Document

## Introduction

All DataGridView grids in the application currently rely exclusively on command-bar buttons for user actions (Add, Remove, Up, Down, Edit, etc.). This forces users to move the mouse away from the grid to the button bar for every action. Right-click context menus provide faster, in-place access to the same actions directly on the grid row the user is interacting with.

FormBlueprintV2 already has context menus on dgvStatistics and dgvResources (cmsStatistics, cmsResources). FormBuildPlanner has cmsBuildItems on dgvBuildItems. This feature extends the pattern to all remaining forms with actionable grids, ensuring consistent right-click behavior across the application.

Each context menu mirrors the existing button actions for its grid. No new functionality is introduced -- the menus provide an alternative access path to existing operations.

## Glossary

- **Context_Menu**: A System.Windows.Forms.ContextMenuStrip instance assigned to a DataGridView via its ContextMenuStrip property, displayed on right-click.
- **Menu_Item**: A System.Windows.Forms.ToolStripMenuItem within a Context_Menu representing a single action.
- **Grid**: A DataGridView or DataEntryGridView control displaying tabular data.
- **Command_Bar**: A FlowLayoutPanel containing Button controls that provide actions for a grid.
- **Form_ColonyV2**: The colony management form (FormColonyV2) with grids on Warehousing, Workers, and Overflow tabs.
- **Form_DeliveryRoute**: The delivery route editor form (FormDeliveryRoute) with grids for stops, drop-off items, and pick-up items.
- **Form_DeliveryExecution**: The delivery execution form (FormDeliveryExecution) with a load list grid and checkbox-based stop items.
- **Form_Survey**: The survey form (FormSurvey) with a resources grid.
- **Form_BlueprintV2**: The blueprint form (FormBlueprintV2) with statistics and resources grids (already has context menus -- included for completeness).
- **Form_StockTargets**: The stock targets form (FormStockTargets) with a targets grid.
- **Form_Market**: The market form (FormMarket) with listings and transactions grids.
- **Form_ShipTemplate**: The ship template form (FormShipTemplate) with a slots grid.
- **Form_ShipInstance**: The ship instance form (FormShipInstance) with components and cargo grids.
- **Form_Station**: The station form (FormStation) with hold, components, and munitions grids.

## Requirements

### Requirement 1: Context Menu Infrastructure Pattern

**User Story:** As a developer, I want a consistent pattern for adding context menus to grids, so that all forms follow the same conventions and the menus behave predictably.

#### Acceptance Criteria

1. THE Context_Menu SHALL be assigned to the Grid via the ContextMenuStrip property in the Designer file.
2. THE Context_Menu SHALL use the naming convention `cms{GridPurpose}` (e.g. cmsStops, cmsDropOff, cmsHold).
3. WHEN the user right-clicks on a Grid row, THE Context_Menu SHALL display at the click location.
4. WHEN the Context_Menu is opened, THE Form SHALL select the row under the mouse cursor so the action targets the correct row.
5. WHEN a Menu_Item action requires a selected row and no row is selected, THE Menu_Item SHALL be disabled.
6. WHEN a Menu_Item invokes an action, THE Form SHALL call the same handler method as the corresponding Command_Bar button.
7. THE Menu_Item SHALL use the naming convention `tsmi{Action}{GridPurpose}` (e.g. tsmiAddStop, tsmiRemoveDropOff).

### Requirement 2: FormColonyV2 Warehousing Tab Context Menu

**User Story:** As a player, I want to right-click on the warehouse items grid to add or remove items, so that I can manage inventory without reaching for the button bar.

#### Acceptance Criteria

1. THE Form_ColonyV2 SHALL provide a Context_Menu on dgvItems with Menu_Items for `Add Item` and `Remove Item`.
2. WHEN the user activates `Add Item` from the Context_Menu, THE Form_ColonyV2 SHALL execute the same logic as cmdAddItem.
3. WHEN the user activates `Remove Item` from the Context_Menu, THE Form_ColonyV2 SHALL remove the selected row from dgvItems using the same logic as the existing remove operation.
4. IF no row is selected in dgvItems, THEN THE `Remove Item` Menu_Item SHALL be disabled.

### Requirement 3: FormColonyV2 Workers Tab Context Menu

**User Story:** As a player, I want to right-click on the commodity requests grid to add or remove requests, so that I can manage worker requests quickly.

#### Acceptance Criteria

1. THE Form_ColonyV2 SHALL provide a Context_Menu on dgvCommodityRequests with Menu_Items for `Add Request` and `Remove Request`.
2. WHEN the user activates `Add Request` from the Context_Menu, THE Form_ColonyV2 SHALL execute the same logic as cmdAddCommodityRequest.
3. WHEN the user activates `Remove Request` from the Context_Menu, THE Form_ColonyV2 SHALL remove the selected row from dgvCommodityRequests.
4. IF no row is selected in dgvCommodityRequests, THEN THE `Remove Request` Menu_Item SHALL be disabled.

### Requirement 4: FormColonyV2 Overflow Tab Context Menu

**User Story:** As a player, I want to right-click on the overflow rules grid to add or remove rules, so that I can manage overflow configuration in place.

#### Acceptance Criteria

1. THE Form_ColonyV2 SHALL provide a Context_Menu on dgvOverflowRules with Menu_Items for `Add Rule` and `Remove Rule`.
2. WHEN the user activates `Add Rule` from the Context_Menu, THE Form_ColonyV2 SHALL execute the same logic as cmdAddOverflowRule.
3. WHEN the user activates `Remove Rule` from the Context_Menu, THE Form_ColonyV2 SHALL execute the same logic as cmdRemoveOverflowRule.
4. IF no row is selected in dgvOverflowRules, THEN THE `Remove Rule` Menu_Item SHALL be disabled.

### Requirement 5: FormDeliveryRoute Stops Grid Context Menu

**User Story:** As a player, I want to right-click on the stops grid to add, remove, or reorder stops, so that I can edit routes without using the button bar.

#### Acceptance Criteria

1. THE Form_DeliveryRoute SHALL provide a Context_Menu on dgvStops with Menu_Items for `Add Stop`, `Move Up`, `Move Down`, and `Remove Stop`.
2. WHEN the user activates `Add Stop` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdAddStop.
3. WHEN the user activates `Move Up` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdUp.
4. WHEN the user activates `Move Down` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdDown.
5. WHEN the user activates `Remove Stop` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdRemoveStop.
6. IF no row is selected in dgvStops, THEN THE `Move Up`, `Move Down`, and `Remove Stop` Menu_Items SHALL be disabled.
7. WHEN the selected row is the first row, THE `Move Up` Menu_Item SHALL be disabled.
8. WHEN the selected row is the last row, THE `Move Down` Menu_Item SHALL be disabled.

### Requirement 6: FormDeliveryRoute Drop-Off Grid Context Menu

**User Story:** As a player, I want to right-click on the drop-off items grid to add or remove items, so that I can edit stop cargo quickly.

#### Acceptance Criteria

1. THE Form_DeliveryRoute SHALL provide a Context_Menu on dgvDropOff with Menu_Items for `Add Item` and `Remove Item`.
2. WHEN the user activates `Add Item` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdAddDropOff.
3. WHEN the user activates `Remove Item` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdRemoveDropOff.
4. IF no row is selected in dgvDropOff, THEN THE `Remove Item` Menu_Item SHALL be disabled.

### Requirement 7: FormDeliveryRoute Pick-Up Grid Context Menu

**User Story:** As a player, I want to right-click on the pick-up items grid to add or remove items, so that I can edit stop cargo quickly.

#### Acceptance Criteria

1. THE Form_DeliveryRoute SHALL provide a Context_Menu on dgvPickUp with Menu_Items for `Add Item` and `Remove Item`.
2. WHEN the user activates `Add Item` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdAddPickUp.
3. WHEN the user activates `Remove Item` from the Context_Menu, THE Form_DeliveryRoute SHALL execute the same logic as cmdRemovePickUp.
4. IF no row is selected in dgvPickUp, THEN THE `Remove Item` Menu_Item SHALL be disabled.

### Requirement 8: FormDeliveryExecution Load List Context Menu

**User Story:** As a player, I want to right-click on the load list grid to mark all items at a stop as delivered or undelivered, so that I can quickly update delivery status.

#### Acceptance Criteria

1. THE Form_DeliveryExecution SHALL provide a Context_Menu on dgvLoadList with Menu_Items for `Mark All Delivered` and `Mark All Undelivered`.
2. WHEN the user activates `Mark All Delivered` from the Context_Menu, THE Form_DeliveryExecution SHALL set all delivery item checkboxes to checked.
3. WHEN the user activates `Mark All Undelivered` from the Context_Menu, THE Form_DeliveryExecution SHALL set all delivery item checkboxes to unchecked.

### Requirement 9: FormSurvey Resources Grid Context Menu

**User Story:** As a player, I want to right-click on the survey resources grid to add or remove resource rows, so that I can edit survey data in place.

#### Acceptance Criteria

1. THE Form_Survey SHALL provide a Context_Menu on dgvResources with Menu_Items for `Add Resource` and `Remove Resource`.
2. WHEN the user activates `Add Resource` from the Context_Menu, THE Form_Survey SHALL add a new empty row to dgvResources.
3. WHEN the user activates `Remove Resource` from the Context_Menu, THE Form_Survey SHALL remove the selected row from dgvResources.
4. IF no row is selected in dgvResources, THEN THE `Remove Resource` Menu_Item SHALL be disabled.

### Requirement 10: FormStockTargets Targets Grid Context Menu

**User Story:** As a player, I want to right-click on the targets grid to add or remove stock targets, so that I can manage targets without using the button bar.

#### Acceptance Criteria

1. THE Form_StockTargets SHALL provide a Context_Menu on dgvTargets with Menu_Items for `Add Target` and `Remove Target`.
2. WHEN the user activates `Add Target` from the Context_Menu, THE Form_StockTargets SHALL execute the same logic as cmdAddTarget.
3. WHEN the user activates `Remove Target` from the Context_Menu, THE Form_StockTargets SHALL execute the same logic as cmdRemoveTarget.
4. IF no row is selected in dgvTargets, THEN THE `Remove Target` Menu_Item SHALL be disabled.

### Requirement 11: FormMarket Listings Grid Context Menu

**User Story:** As a player, I want to right-click on the market listings grid to record a sale or edit a listing, so that I can manage market data quickly.

#### Acceptance Criteria

1. THE Form_Market SHALL provide a Context_Menu on dgvListings with Menu_Items for `Record Sale`, `Edit Listing`, and `Delete Listing`.
2. WHEN the user activates `Record Sale` from the Context_Menu, THE Form_Market SHALL execute the same logic as cmdRecordSale.
3. WHEN the user activates `Edit Listing` from the Context_Menu, THE Form_Market SHALL execute the same logic as cmdListingEdit.
4. WHEN the user activates `Delete Listing` from the Context_Menu, THE Form_Market SHALL execute the same logic as cmdListingDelete.
5. IF no row is selected in dgvListings, THEN THE `Record Sale`, `Edit Listing`, and `Delete Listing` Menu_Items SHALL be disabled.

### Requirement 12: FormMarket Transactions Grid Context Menu

**User Story:** As a player, I want to right-click on the transactions grid to view transaction details, so that I can inspect transactions without additional navigation.

#### Acceptance Criteria

1. THE Form_Market SHALL provide a Context_Menu on dgvTransactions with a Menu_Item for `View Details`.
2. WHEN the user activates `View Details` from the Context_Menu, THE Form_Market SHALL display the full details of the selected transaction.
3. IF no row is selected in dgvTransactions, THEN THE `View Details` Menu_Item SHALL be disabled.

### Requirement 13: FormShipTemplate Slots Grid Context Menu

**User Story:** As a player, I want to right-click on the ship template slots grid to clear a slot assignment, so that I can edit templates in place.

#### Acceptance Criteria

1. THE Form_ShipTemplate SHALL provide a Context_Menu on dgvSlots with a Menu_Item for `Clear Slot`.
2. WHEN the user activates `Clear Slot` from the Context_Menu, THE Form_ShipTemplate SHALL clear the component assignment for the selected slot row.
3. IF no row is selected in dgvSlots, THEN THE `Clear Slot` Menu_Item SHALL be disabled.

### Requirement 14: FormShipInstance Components Grid Context Menu

**User Story:** As a player, I want to right-click on the ship components grid to clear a component slot, so that I can edit ship loadouts in place.

#### Acceptance Criteria

1. THE Form_ShipInstance SHALL provide a Context_Menu on dgvComponents with a Menu_Item for `Clear Slot`.
2. WHEN the user activates `Clear Slot` from the Context_Menu, THE Form_ShipInstance SHALL clear the component assignment for the selected slot row.
3. IF no row is selected in dgvComponents, THEN THE `Clear Slot` Menu_Item SHALL be disabled.

### Requirement 15: FormShipInstance Cargo Grid Context Menu

**User Story:** As a player, I want to right-click on the ship cargo grid to add or remove cargo items, so that I can manage ship inventory quickly.

#### Acceptance Criteria

1. THE Form_ShipInstance SHALL provide a Context_Menu on dgvCargo with Menu_Items for `Add Item` and `Remove Item`.
2. WHEN the user activates `Add Item` from the Context_Menu, THE Form_ShipInstance SHALL execute the same logic as cmdAddItem.
3. WHEN the user activates `Remove Item` from the Context_Menu, THE Form_ShipInstance SHALL execute the same logic as cmdRemoveItem.
4. IF no row is selected in dgvCargo, THEN THE `Remove Item` Menu_Item SHALL be disabled.

### Requirement 16: FormStation Hold Grid Context Menu

**User Story:** As a player, I want to right-click on the station hold grid to add or remove hold items, so that I can manage station inventory quickly.

#### Acceptance Criteria

1. THE Form_Station SHALL provide a Context_Menu on dgvHold with Menu_Items for `Add Item` and `Remove Item`.
2. WHEN the user activates `Add Item` from the Context_Menu, THE Form_Station SHALL execute the same logic as cmdHoldAdd.
3. WHEN the user activates `Remove Item` from the Context_Menu, THE Form_Station SHALL execute the same logic as cmdHoldRemove.
4. IF no row is selected in dgvHold, THEN THE `Remove Item` Menu_Item SHALL be disabled.

### Requirement 17: FormStation Components Grid Context Menu

**User Story:** As a player, I want to right-click on the station components grid to clear a component slot, so that I can edit station loadouts in place.

#### Acceptance Criteria

1. THE Form_Station SHALL provide a Context_Menu on dgvComponents with a Menu_Item for `Clear Slot`.
2. WHEN the user activates `Clear Slot` from the Context_Menu, THE Form_Station SHALL clear the component assignment for the selected slot row.
3. IF no row is selected in dgvComponents, THEN THE `Clear Slot` Menu_Item SHALL be disabled.

### Requirement 18: FormStation Munitions Grid Context Menu

**User Story:** As a player, I want to right-click on the station munitions grid to add or remove munitions, so that I can manage station armament quickly.

#### Acceptance Criteria

1. THE Form_Station SHALL provide a Context_Menu on dgvMunitions with Menu_Items for `Add Munition` and `Remove Munition`.
2. WHEN the user activates `Add Munition` from the Context_Menu, THE Form_Station SHALL execute the same logic as cmdMunAdd.
3. WHEN the user activates `Remove Munition` from the Context_Menu, THE Form_Station SHALL execute the same logic as cmdMunRemove.
4. IF no row is selected in dgvMunitions, THEN THE `Remove Munition` Menu_Item SHALL be disabled.

### Requirement 19: Row Selection on Right-Click

**User Story:** As a player, I want right-clicking on a grid row to select that row before showing the context menu, so that the menu action always targets the row I clicked on.

#### Acceptance Criteria

1. WHEN the user right-clicks on a Grid cell, THE Form SHALL select the row containing that cell before displaying the Context_Menu.
2. WHEN the user right-clicks on an area of the Grid with no rows, THE Form SHALL clear the selection and disable row-dependent Menu_Items.
3. THE row selection on right-click SHALL use the CellMouseClick or MouseClick event with MouseButtons.Right detection.

### Requirement 20: Menu Item Enable/Disable State Management

**User Story:** As a player, I want context menu items to be enabled or disabled based on the current selection state, so that I cannot accidentally invoke actions on nothing.

#### Acceptance Criteria

1. WHEN the Context_Menu Opening event fires, THE Form SHALL evaluate the current Grid selection and enable or disable each Menu_Item accordingly.
2. WHEN a Menu_Item requires a selected row and no row is selected, THE Menu_Item SHALL have its Enabled property set to false.
3. WHEN a Menu_Item has positional constraints (e.g. Move Up on first row), THE Menu_Item SHALL have its Enabled property set to false when the constraint is violated.
