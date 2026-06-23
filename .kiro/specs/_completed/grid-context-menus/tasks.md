# Implementation Plan: Grid Context Menus

## Overview

Add right-click context menus to all DataGridView grids that have command-bar buttons, following the established pattern from FormBlueprintV2 and FormBuildPlanner. Each form receives: Designer declarations (ContextMenuStrip + ToolStripMenuItems), event wiring in the constructor (Click handlers delegating to existing button handlers), CellMouseClick handlers for row selection on right-click, and Opening handlers for enable/disable state management.

Implementation proceeds form-by-form, starting with the simplest cases (single grid, Add/Remove only) and progressing to more complex forms (multiple grids, positional constraints). FormDeliveryRoute is implemented last among the multi-grid forms because it has the most complex Opening logic (Move Up/Down positional constraints).

## Tasks

- [x] 1. Implement FormColonyV2 context menus (3 grids)
  - [x] 1.1 Add Designer declarations for cmsItems, cmsCommodityRequests, cmsOverflowRules
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormColonyV2.Designer.cs
    - Assign each ContextMenuStrip to its grid's ContextMenuStrip property
    - Components: cmsItems (tsmiAddItem, tsmiRemoveItem), cmsCommodityRequests (tsmiAddCommodityRequest, tsmiRemoveCommodityRequest), cmsOverflowRules (tsmiAddOverflowRule, tsmiRemoveOverflowRule)
    - _Requirements: 1.1, 1.2, 1.7, 2.1, 3.1, 4.1_
  - [x] 1.2 Wire event handlers in FormColonyV2 constructor and add handler methods
    - Wire tsmi Click events to existing button handlers (cmdAddItem, cmdRemoveItem, etc.)
    - Add CellMouseClick handlers for dgvItems, dgvCommodityRequests, dgvOverflowRules (row selection on right-click)
    - Add Opening handlers for cmsItems, cmsCommodityRequests, cmsOverflowRules (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 2.2, 2.3, 2.4, 3.2, 3.3, 3.4, 4.2, 4.3, 4.4, 19.1, 19.2, 19.3, 20.1, 20.2_
  - [ ]* 1.3 Write unit tests for FormColonyV2 Opening handlers
    - Test that Remove items are disabled when no row is selected
    - Test that all items are enabled when a row is selected
    - _Requirements: 2.4, 3.4, 4.4, 20.1, 20.2_

- [x] 2. Implement FormSurvey context menu (1 grid)
  - [x] 2.1 Add Designer declarations for cmsResources
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormSurvey.Designer.cs
    - Assign cmsResources to dgvResources.ContextMenuStrip
    - Components: cmsResources (tsmiAddResource, tsmiRemoveResource)
    - _Requirements: 1.1, 1.2, 1.7, 9.1_
  - [x] 2.2 Wire event handlers in FormSurvey constructor and add handler methods
    - Wire tsmiAddResource.Click and tsmiRemoveResource.Click to existing button handlers
    - Add CellMouseClick handler for dgvResources (row selection on right-click)
    - Add Opening handler for cmsResources (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 9.2, 9.3, 9.4, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 3. Implement FormStockTargets context menu (1 grid)
  - [x] 3.1 Add Designer declarations for cmsTargets
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormStockTargets.Designer.cs
    - Assign cmsTargets to dgvTargets.ContextMenuStrip
    - Components: cmsTargets (tsmiAddTarget, tsmiRemoveTarget)
    - _Requirements: 1.1, 1.2, 1.7, 10.1_
  - [x] 3.2 Wire event handlers in FormStockTargets constructor and add handler methods
    - Wire tsmiAddTarget.Click and tsmiRemoveTarget.Click to existing button handlers
    - Add CellMouseClick handler for dgvTargets (row selection on right-click)
    - Add Opening handler for cmsTargets (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 10.2, 10.3, 10.4, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 4. Checkpoint - Verify simple forms compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement FormDeliveryExecution context menu (1 grid, no selection constraint)
  - [x] 5.1 Add Designer declarations for cmsLoadList
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormDeliveryExecution.Designer.cs
    - Assign cmsLoadList to dgvLoadList.ContextMenuStrip
    - Components: cmsLoadList (tsmiMarkAllDelivered, tsmiMarkAllUndelivered)
    - _Requirements: 1.1, 1.2, 1.7, 8.1_
  - [x] 5.2 Wire event handlers in FormDeliveryExecution constructor and add handler methods
    - Wire tsmiMarkAllDelivered.Click and tsmiMarkAllUndelivered.Click to existing button handlers
    - Add CellMouseClick handler for dgvLoadList (row selection on right-click)
    - Note: these actions operate on all items, not a single row, so no Opening disable logic needed
    - _Requirements: 1.3, 1.4, 1.6, 8.2, 8.3, 19.1, 19.2, 19.3_

- [x] 6. Implement FormMarket context menus (2 grids)
  - [x] 6.1 Add Designer declarations for cmsListings and cmsTransactions
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormMarket.Designer.cs
    - Assign each ContextMenuStrip to its grid's ContextMenuStrip property
    - Components: cmsListings (tsmiRecordSale, tsmiEditListing, tsmiDeleteListing), cmsTransactions (tsmiViewDetails)
    - _Requirements: 1.1, 1.2, 1.7, 11.1, 12.1_
  - [x] 6.2 Wire event handlers in FormMarket constructor and add handler methods
    - Wire tsmi Click events to existing button handlers (cmdRecordSale, cmdListingEdit, cmdListingDelete, view details handler)
    - Add CellMouseClick handlers for dgvListings and dgvTransactions (row selection on right-click)
    - Add Opening handlers for cmsListings (disable all 3 items when no selection) and cmsTransactions (disable View Details when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 11.2, 11.3, 11.4, 11.5, 12.2, 12.3, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 7. Implement FormShipTemplate context menu (1 grid)
  - [x] 7.1 Add Designer declarations for cmsSlots
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormShipTemplate.Designer.cs
    - Assign cmsSlots to dgvSlots.ContextMenuStrip
    - Components: cmsSlots (tsmiClearSlot)
    - _Requirements: 1.1, 1.2, 1.7, 13.1_
  - [x] 7.2 Wire event handlers in FormShipTemplate constructor and add handler methods
    - Wire tsmiClearSlot.Click to existing clear slot button handler
    - Add CellMouseClick handler for dgvSlots (row selection on right-click)
    - Add Opening handler for cmsSlots (disable Clear Slot when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 13.2, 13.3, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 8. Implement FormShipInstance context menus (2 grids)
  - [x] 8.1 Add Designer declarations for cmsComponents and cmsCargo
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormShipInstance.Designer.cs
    - Assign each ContextMenuStrip to its grid's ContextMenuStrip property
    - Components: cmsComponents (tsmiClearSlotComponent), cmsCargo (tsmiAddCargo, tsmiRemoveCargo)
    - _Requirements: 1.1, 1.2, 1.7, 14.1, 15.1_
  - [x] 8.2 Wire event handlers in FormShipInstance constructor and add handler methods
    - Wire tsmi Click events to existing button handlers
    - Add CellMouseClick handlers for dgvComponents and dgvCargo (row selection on right-click)
    - Add Opening handlers for cmsComponents (disable Clear Slot when no selection) and cmsCargo (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 14.2, 14.3, 15.2, 15.3, 15.4, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 9. Checkpoint - Verify mid-implementation forms compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Implement FormStation context menus (3 grids)
  - [x] 10.1 Add Designer declarations for cmsHold, cmsStationComponents, cmsMunitions
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormStation.Designer.cs
    - Assign each ContextMenuStrip to its grid's ContextMenuStrip property
    - Components: cmsHold (tsmiAddHold, tsmiRemoveHold), cmsStationComponents (tsmiClearSlotStation), cmsMunitions (tsmiAddMunition, tsmiRemoveMunition)
    - _Requirements: 1.1, 1.2, 1.7, 16.1, 17.1, 18.1_
  - [x] 10.2 Wire event handlers in FormStation constructor and add handler methods
    - Wire tsmi Click events to existing button handlers (cmdHoldAdd, cmdHoldRemove, clear slot handler, cmdMunAdd, cmdMunRemove)
    - Add CellMouseClick handlers for dgvHold, dgvComponents, dgvMunitions (row selection on right-click)
    - Add Opening handlers for cmsHold (disable Remove when no selection), cmsStationComponents (disable Clear Slot when no selection), cmsMunitions (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 16.2, 16.3, 16.4, 17.2, 17.3, 18.2, 18.3, 18.4, 19.1, 19.2, 19.3, 20.1, 20.2_

- [x] 11. Implement FormDeliveryRoute context menus (3 grids, complex enable/disable)
  - [x] 11.1 Add Designer declarations for cmsStops, cmsDropOff, cmsPickUp
    - Declare ContextMenuStrip and ToolStripMenuItem components in FormDeliveryRoute.Designer.cs
    - Assign each ContextMenuStrip to its grid's ContextMenuStrip property
    - Components: cmsStops (tsmiAddStop, tsmiMoveUpStop, tsmiMoveDownStop, tsmiRemoveStop), cmsDropOff (tsmiAddDropOff, tsmiRemoveDropOff), cmsPickUp (tsmiAddPickUp, tsmiRemovePickUp)
    - _Requirements: 1.1, 1.2, 1.7, 5.1, 6.1, 7.1_
  - [x] 11.2 Wire event handlers in FormDeliveryRoute constructor and add handler methods
    - Wire tsmi Click events to existing button handlers (cmdAddStop, cmdUp, cmdDown, cmdRemoveStop, cmdAddDropOff, cmdRemoveDropOff, cmdAddPickUp, cmdRemovePickUp)
    - Add CellMouseClick handlers for dgvStops, dgvDropOff, dgvPickUp (row selection on right-click)
    - Add Opening handler for cmsStops with positional constraints: disable Move Up on first row, disable Move Down on last row, disable Move Up/Down/Remove when no selection
    - Add Opening handlers for cmsDropOff and cmsPickUp (disable Remove when no selection)
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 6.2, 6.3, 6.4, 7.2, 7.3, 7.4, 19.1, 19.2, 19.3, 20.1, 20.2, 20.3_
  - [ ]* 11.3 Write unit tests for FormDeliveryRoute CmsStops_Opening handler
    - Test that Move Up is disabled when first row is selected
    - Test that Move Down is disabled when last row is selected
    - Test that Move Up, Move Down, and Remove Stop are disabled when no row is selected
    - Test that all items are enabled when a middle row is selected
    - _Requirements: 5.6, 5.7, 5.8, 20.1, 20.2, 20.3_

- [x] 12. Final checkpoint - Full build and test verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Designer declarations should be added using the WinForms Designer pattern (component instantiation in InitializeComponent, property assignment, field declarations)
- All forms follow the identical pattern: Designer declarations -> constructor wiring -> CellMouseClick handler -> Opening handler
- No property-based tests are included because this feature is pure UI wiring with no algorithmic logic
- The existing pattern in FormBlueprintV2 (cmsStatistics, cmsResources) serves as the reference implementation
