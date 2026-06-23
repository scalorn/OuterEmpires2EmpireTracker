# Requirements Document

## Introduction

The Commodity Delivery Loop feature adds two capabilities to the delivery system: (A) auto-filling commodity drop-off items on the Route Builder plan tab based on unfulfilled colony commodity requests, and (B) updating CommodityRequested fulfillment status when commodity delivery items are checked off on the execution form. Together these close the loop between requesting commodities and delivering them.

## Glossary

- **Plan_Tab**: The "Plan" tab on FormDeliveryRoute, used for managing delivery plan items per stop.
- **Auto_Fill_Button**: A button on the Plan_Tab that populates drop-off items from unfulfilled commodity requests.
- **Auto_Fill_Menu**: A popup or panel with checkboxes for request types (Commodities, Flatpacks, Resources, Workers) shown when Auto_Fill_Button is activated.
- **CommodityRequested**: A data object on Colony with Name, Requested, Delivered, NeedBy, and Fulfilled properties representing a colony's commodity need.
- **DeliveryItem**: An item entry in a DeliveryPlanStop's DropOff or PickUp list, with ItemType, BaseItemTypeID, Name, ResourcePurity, Quantity, and Delivered properties.
- **DeliveryPlanStop**: A stop in a DeliveryPlan containing ColonyUUID, Sequence, DropOff list, and PickUp list.
- **DeliveryPlanViewModel**: The ViewModel wrapping DeliveryPlan, providing typed operations for plan manipulation.
- **Execution_Form**: FormDeliveryExecution, the form that displays a delivery plan for stop-by-stop execution with checkboxes per item.
- **Shortfall**: The quantity (Requested - Delivered) for a CommodityRequested entry that has not been fulfilled.
- **Colony**: A player-owned colony containing a Commodities list of CommodityRequested entries.

## Requirements

### Requirement 1: Auto-Fill Button Placement

**User Story:** As a player, I want an Auto-Fill button on the delivery plan tab, so that I can quickly populate drop-off items from colony commodity requests.

#### Acceptance Criteria

1. WHEN a DeliveryPlan is selected on the Plan_Tab, THE Plan_Tab SHALL display the Auto_Fill_Button in the plan command area.
2. WHILE no DeliveryPlan is selected, THE Plan_Tab SHALL hide or disable the Auto_Fill_Button.

### Requirement 2: Auto-Fill Request Type Selection

**User Story:** As a player, I want to choose which request types to auto-fill, so that I can control what gets added to my delivery plan.

#### Acceptance Criteria

1. WHEN the Auto_Fill_Button is clicked, THE Auto_Fill_Menu SHALL display checkboxes for four request types: Commodities, Flatpacks, Resources for Manufacturing, and Workers.
2. THE Auto_Fill_Menu SHALL enable the Commodities checkbox and set it to checked by default.
3. THE Auto_Fill_Menu SHALL display the Flatpacks checkbox as disabled with a "(Future)" label.
4. THE Auto_Fill_Menu SHALL display the Resources for Manufacturing checkbox as disabled with a "(Future)" label.
5. THE Auto_Fill_Menu SHALL display the Workers checkbox as disabled with a "(Future)" label.
6. THE Auto_Fill_Menu SHALL include a confirmation button to execute the auto-fill operation.

### Requirement 3: Commodity Auto-Fill Logic

**User Story:** As a player, I want auto-fill to scan each stop's colony for unfulfilled commodity requests, so that the correct shortfall quantities are added to my plan.

#### Acceptance Criteria

1. WHEN auto-fill is executed with Commodities checked, THE DeliveryPlanViewModel SHALL iterate each DeliveryPlanStop in sequence order.
2. FOR each DeliveryPlanStop, THE DeliveryPlanViewModel SHALL retrieve the Colony referenced by ColonyUUID.
3. FOR each Colony, THE DeliveryPlanViewModel SHALL identify CommodityRequested entries where Fulfilled is false and (Requested - Delivered) is greater than zero.
4. FOR each unfulfilled CommodityRequested, THE DeliveryPlanViewModel SHALL add a DeliveryItem to the stop's DropOff list with ItemType set to Commodity, Name matching the CommodityRequested Name, BaseItemTypeID matching the CommodityRequested Name, and Quantity set to the Shortfall (Requested - Delivered).
5. THE DeliveryPlanViewModel SHALL persist the updated plan after auto-fill completes.
6. IF a Colony referenced by a DeliveryPlanStop cannot be found, THEN THE DeliveryPlanViewModel SHALL skip that stop and continue processing remaining stops.

### Requirement 4: Auto-Fill Additive Behavior

**User Story:** As a player, I want auto-fill to add to my existing plan items rather than replacing them, so that I do not lose manually added entries.

#### Acceptance Criteria

1. WHEN auto-fill adds DeliveryItems to a stop's DropOff list, THE DeliveryPlanViewModel SHALL append new items without removing or modifying existing DeliveryItems in the list.
2. THE DeliveryPlanViewModel SHALL add commodity items regardless of whether a DeliveryItem with the same Name already exists in the stop's DropOff list.

### Requirement 5: Auto-Fill Drop-Off Only

**User Story:** As a player, I want auto-fill to only handle drop-off items in this phase, so that pick-up sourcing can be addressed separately in a future phase.

#### Acceptance Criteria

1. WHEN auto-fill is executed, THE DeliveryPlanViewModel SHALL only add items to the DropOff list of each DeliveryPlanStop.
2. THE DeliveryPlanViewModel SHALL not modify the PickUp list of any DeliveryPlanStop during auto-fill.

### Requirement 6: Commodity Fulfillment on Delivery Check

**User Story:** As a player, I want checking a commodity delivery item on the execution form to automatically update the colony's commodity request as fulfilled, so that I do not have to manually update fulfillment status.

#### Acceptance Criteria

1. WHEN a commodity DeliveryItem checkbox is checked on the Execution_Form, THE Execution_Form SHALL find the matching CommodityRequested on the target Colony by comparing the DeliveryItem Name to the CommodityRequested Name.
2. WHEN a matching CommodityRequested is found, THE Execution_Form SHALL set the CommodityRequested Delivered property equal to the CommodityRequested Requested property.
3. WHEN a matching CommodityRequested is found, THE Execution_Form SHALL set the CommodityRequested Fulfilled property to true.
4. THE Execution_Form SHALL persist the updated Colony data immediately after updating the CommodityRequested.
5. IF no matching CommodityRequested is found on the target Colony, THEN THE Execution_Form SHALL log a warning and continue without error.

### Requirement 7: Commodity Fulfillment All-or-Nothing

**User Story:** As a player, I want commodity delivery to be all-or-nothing per line item, so that partial deliveries do not create confusing intermediate states.

#### Acceptance Criteria

1. WHEN a commodity DeliveryItem is marked as delivered, THE Execution_Form SHALL set CommodityRequested Delivered equal to CommodityRequested Requested regardless of the DeliveryItem Quantity.
2. THE Execution_Form SHALL set CommodityRequested Fulfilled to true in the same operation as setting Delivered equal to Requested.

### Requirement 8: Commodity Unfulfillment on Uncheck

**User Story:** As a player, I want unchecking a commodity delivery item to reverse the fulfillment, so that I can correct mistakes.

#### Acceptance Criteria

1. WHEN a commodity DeliveryItem checkbox is unchecked on the Execution_Form, THE Execution_Form SHALL find the matching CommodityRequested on the target Colony by comparing the DeliveryItem Name to the CommodityRequested Name.
2. WHEN a matching CommodityRequested is found and the checkbox is unchecked, THE Execution_Form SHALL set the CommodityRequested Delivered property to zero.
3. WHEN a matching CommodityRequested is found and the checkbox is unchecked, THE Execution_Form SHALL set the CommodityRequested Fulfilled property to false.
4. THE Execution_Form SHALL persist the updated Colony data immediately after reversing the CommodityRequested fulfillment.

### Requirement 9: Plan Tab UI Refresh After Auto-Fill

**User Story:** As a player, I want the plan tab to refresh after auto-fill runs, so that I can see the newly added items immediately.

#### Acceptance Criteria

1. WHEN auto-fill completes, THE Plan_Tab SHALL refresh the DropOff grid for the currently selected stop to display all items including newly added ones.
2. WHEN auto-fill completes, THE Plan_Tab SHALL retain the currently selected stop in the stops grid.
