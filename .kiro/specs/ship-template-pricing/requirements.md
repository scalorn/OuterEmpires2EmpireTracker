# Requirements Document

## Introduction

The Ship Template Pricing feature adds a pricing plan selector and total cost calculation to the FormShipTemplate form. When a pricing plan is selected, the system computes the total cost of building the ship by summing the individual blueprint prices (hull + all component slots) using the selected plan's resource prices and time-cost parameters. This gives the user visibility into the total material and manufacturing cost of a ship design before committing to a build order.

The feature mirrors the existing pricing plan integration on the Blueprint form (FormBlueprintV2), which computes a single blueprint's price. The Ship Template form extends this by aggregating prices across all blueprints that compose the template.

## Glossary

- **Ship_Template_Form**: The FormShipTemplate MDI child window where users design ship loadouts by selecting a hull and component blueprints.
- **Pricing_Plan**: A named, per-player configuration containing base resource prices and time-cost parameters (FixedCostPerItem, HourlyCostRate). Managed via FormPricingPlan.
- **Price_Calculator**: The static PriceCalculator service that computes blueprint and commodity prices from a PricingPlan's resource prices.
- **Template_Price**: The aggregate cost of all blueprints in a ship template (hull + all filled component slots), computed using the selected Pricing_Plan.
- **Component_Slot**: A slot in the ship template grid that references a specific blueprint (reactor, drive, weapon, etc.).
- **Pricing_Row**: The UI row containing the pricing plan dropdown, computed total price label, and optional per-blueprint breakdown indicator.


## Requirements

### Requirement 1: Pricing Plan Selector on Ship Template Form

**User Story:** As a player, I want to select a pricing plan on the Ship Template form, so that I can see the total cost of building a ship design.

#### Acceptance Criteria

1. THE Ship_Template_Form SHALL display a pricing plan dropdown (FilteredTextComboSet) in the detail panel, positioned below the hull selection row and above the component slot grid.
2. THE pricing plan dropdown SHALL be populated with all pricing plans owned by the current player, sorted alphabetically by name, with a "(none)" option as the default selection.
3. WHEN the user selects a pricing plan, THE Ship_Template_Form SHALL compute and display the Template_Price using the selected plan.
4. WHEN "(none)" is selected, THE Ship_Template_Form SHALL display no price (empty label).
5. THE pricing plan dropdown SHALL use the same FilteredTextComboSet control and parallel UUID list pattern used by the Blueprint form's pricing plan selector.
6. THE pricing plan dropdown SHALL be enabled only when a template is loaded (not in the cleared/empty state).

### Requirement 2: Template Price Calculation

**User Story:** As a player, I want the total ship cost calculated from all blueprints in the template, so that I can compare designs by cost.

#### Acceptance Criteria

1. THE Ship_Template_Form SHALL compute Template_Price by summing the individual blueprint prices for the hull blueprint and all filled component slots.
2. FOR EACH blueprint in the template (hull + components), THE Price_Calculator SHALL compute the price using ComputeBlueprintPrice with the selected Pricing_Plan and the blueprint's manufacturing hours.
3. THE manufacturing hours for each blueprint SHALL be derived from the blueprint's ManufactureRunTime property (parsed via EvolutionChainService.ParseTimeToSeconds, converted to hours).
4. IF a blueprint has no ManufactureRunTime property, THEN the manufacturing hours for that blueprint SHALL default to zero.
5. THE Template_Price SHALL include the FixedCostPerItem and HourlyCostRate contributions for each individual blueprint in the template (not once for the whole template).
6. IF any individual blueprint price is incomplete (missing resource prices), THEN the aggregate Template_Price SHALL be flagged as incomplete.


### Requirement 3: Price Display

**User Story:** As a player, I want to see the total ship cost clearly displayed, so that I can quickly assess the expense of a template.

#### Acceptance Criteria

1. THE Ship_Template_Form SHALL display the computed Template_Price in a label adjacent to the pricing plan dropdown, formatted as a number with two decimal places (N2 format).
2. IF the Template_Price is incomplete (one or more blueprints have missing resource prices), THEN the display SHALL append an asterisk (*) indicator after the price.
3. IF no hull is selected or no pricing plan is selected, THEN the price label SHALL be empty.
4. IF the hull blueprint cannot be resolved (deleted or missing), THEN the price label SHALL be empty.

### Requirement 4: Price Recalculation Triggers

**User Story:** As a player, I want the price to update automatically when I change the template or pricing data, so that the displayed cost is always current.

#### Acceptance Criteria

1. WHEN the user changes the selected pricing plan, THE Ship_Template_Form SHALL recompute and display the Template_Price.
2. WHEN the user changes the hull selection, THE Ship_Template_Form SHALL recompute and display the Template_Price.
3. WHEN the user changes any component slot assignment, THE Ship_Template_Form SHALL recompute and display the Template_Price.
4. WHEN a PricingDataChanged event fires (pricing plan modified externally), THE Ship_Template_Form SHALL repopulate the pricing plan dropdown and recompute the Template_Price.
5. WHEN a CurrentPlayerChanged event fires, THE Ship_Template_Form SHALL repopulate the pricing plan dropdown (new player's plans) and clear the price display.

### Requirement 5: Layout and Positioning

**User Story:** As a player, I want the pricing controls positioned logically on the form, so that I can see cost information without disrupting the existing workflow.

#### Acceptance Criteria

1. THE pricing row (label + dropdown + price label) SHALL be contained in a FlowLayoutPanel (flpPricing) with LeftToRight flow direction.
2. THE flpPricing panel SHALL be positioned in the detail panel (flpDetail) between the hull selection row (flpHull) and the component slot grid (dgvSlots).
3. THE pricing row SHALL have a "Pricing Plan" label, followed by the FilteredTextComboSet dropdown, followed by the computed price label.
4. THE pricing row SHALL be AutoSize so it collapses when empty and expands to fit content.


### Requirement 6: Event Subscriptions

**User Story:** As a player, I want the pricing display to stay in sync with external changes, so that I always see accurate cost data.

#### Acceptance Criteria

1. THE Ship_Template_Form SHALL subscribe to the PricingDataChanged event on PlayerContext.
2. THE Ship_Template_Form SHALL unsubscribe from PricingDataChanged in OnFormClosed.
3. WHEN PricingDataChanged fires on a background thread, THE Ship_Template_Form SHALL marshal the handler to the UI thread via BeginInvoke before updating controls.

### Requirement 7: Empty and Edge States

**User Story:** As a player, I want the pricing display to handle edge cases gracefully, so that the form does not crash or show misleading data.

#### Acceptance Criteria

1. IF no pricing plans exist for the current player, THEN the dropdown SHALL show only "(none)" and the price label SHALL be empty.
2. IF the selected pricing plan is deleted externally (PricingDataChanged fires and the plan no longer exists), THEN the dropdown SHALL revert to "(none)" and the price label SHALL be cleared.
3. IF a component slot references a blueprint that cannot be resolved (deleted), THEN that slot SHALL contribute zero to the Template_Price and the price SHALL be flagged as incomplete.
4. IF the template has no filled component slots (hull only), THEN the Template_Price SHALL be the hull blueprint's price alone.
5. WHEN a new template is created (New button), THE pricing plan selection SHALL be preserved (not reset to "(none)") so the user can immediately see cost as they build.

