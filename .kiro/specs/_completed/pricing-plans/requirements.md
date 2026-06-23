# Requirements Document — BL-016: Pricing Plans

## Introduction

Pricing Plans provide a way to assign monetary values (in credits) to resources, commodities, and manufactured items in OE2. A pricing plan is a named, per-player configuration containing base resource prices for Refined, S1, and S2 purity levels, plus optional time-cost parameters. The system uses these base prices plus the existing bill-of-materials chains (commodity construction recipes, blueprint resource requirements) to compute rolled-up costs for commodities and manufactured items.

Multiple pricing plans support different valuation perspectives: cost basis ("what did it cost me to produce this?"), market value ("what would someone pay?"), replacement cost ("what would it cost to reproduce?"), pessimistic vs optimistic market estimates, self-mined vs market-purchased inputs, etc.

This iteration builds the foundational data model, persistence, input form, and price calculator. Price display integration in other forms (blueprint lists, delivery plans, colony reports) and plan comparison are deferred to a future iteration.

### Iteration Scope

**In scope:**
- Data model for pricing plans and base resource prices
- Persistence to PlayerData.json
- Input form for creating/editing pricing plans and entering resource prices
- Price calculator for commodities (from construction resources) and manufactured items (from blueprint resources + optional time cost)
- Manufactory and Commodity Manufactory items

**Out of scope (future work):**
- Price display in blueprint lists, delivery plans, colony reports
- Plan comparison
- Unrefined resource pricing (High/Medium/Low purities)
- Commodity price overrides
- Blueprint copy cost in pricing
- Refining time cost in resource rollup

## Glossary

- **Pricing_Plan**: A named, per-player, persisted configuration containing base resource prices, optional time-cost settings, and metadata describing its purpose. Each plan produces a complete set of computed prices for all items.
- **Base_Resource_Price**: A user-entered price (in credits) for a single resource at a specific purity (Refined, S1, or S2) within a pricing plan. These are the leaf-level inputs from which all other prices are computed.
- **Computed_Price**: A price derived by the Price_Calculator by rolling up base resource prices through commodity construction recipes or blueprint resource requirements.
- **Incomplete_Price**: A Computed_Price where one or more input resources have no Base_Resource_Price entry in the plan (not even zero). The price is computed using available inputs but flagged as incomplete.
- **Price_Calculator**: The service that traverses the bill-of-materials chain to compute the price of any commodity or manufactured item from base resource prices and optional time cost.
- **Bill_of_Materials**: The chain of inputs required to produce an item — resources combined into commodities, resources consumed by blueprint manufacturing.
- **Fixed_Cost_Per_Item**: An optional flat credit amount added to every manufactured item's price, representing overhead, facility cost, or any per-item charge the player wants to model.
- **Hourly_Cost_Rate**: An optional credits-per-hour rate multiplied by the blueprint's manufacturing time, representing the opportunity cost of facility time.
- **Rollup**: The process of summing input costs through the bill-of-materials chain to arrive at a final computed price.
- **PlayerData**: The JSON file (`PlayerData.json`) where player-specific data including pricing plans is persisted.
- **Manufacturing_Time**: The time required to manufacture an item, as specified by the blueprint. There is no formula to predict this — it is a property of the blueprint itself.

## Resolved Questions

> These questions were raised during initial requirements drafting and resolved with the user before design.

### OQ-1: Resource Purity Granularity — RESOLVED
**Decision:** Only Refined, S1, and S2 purity levels get base prices. Unrefined resources (High/Medium/Low) are excluded from this iteration. The user enters prices directly for each purity level — there is no auto-computation from unrefined to refined.

### OQ-2: Commodity Price Override — RESOLVED
**Decision:** Computed only for this iteration. No overrides. Commodity prices are strictly computed from their construction resource inputs.

### OQ-3: Blueprint/Manufactured Item Inputs — RESOLVED
**Decision:** Only refined resources are inputs to blueprints. Evolution changes the resources listed in the blueprint, so the resources in the Blueprint's `Resources` dictionary ARE the cost basis as-is. Copy cost (`CopyCost`) is a separate concern and excluded from this iteration.

### OQ-4: Time Cost Modeling — RESOLVED
**Decision:** Time cost is composite/additive, not a mode selector. A pricing plan has two optional time cost fields: a fixed cost per item (credits) and an hourly rate (credits per hour). Both default to zero. The manufactured item price formula is: **Resource Cost + Fixed Cost Per Item + (Hourly Rate × Manufacturing Hours)**. All three components stack. Manufacturing time is specified by the blueprint — there is no formula to predict it. Refining time is NOT factored into resource cost. The user can account for refining time in their base resource prices if they choose. This iteration prices Manufactory and Commodity Manufactory items.

### OQ-5: Plan Ownership — RESOLVED
**Decision:** Per-player only. No global plans. Each pricing plan belongs to a specific player profile.

### OQ-6: Price Display Integration — RESOLVED
**Decision:** Price display in other forms is a separate future feature. This iteration builds the foundational data structures and input form only. Requirements 6 (Price Display Integration) and 8 (Plan Comparison) are removed — future work.

### OQ-7: Persistence — RESOLVED
**Decision:** PlayerData.json following existing patterns.

### OQ-8: Missing Prices — RESOLVED
**Decision:** Zero is a valid price (the user may intentionally price a resource at zero). For resources that have NO price entry at all (not even zero), the Price_Calculator flags the computed price as incomplete. The distinction is: an explicit zero means "free"; absence of an entry means "unknown/not priced."


## Requirements

### Requirement 1: Pricing Plan CRUD

**User Story:** As a player, I want to create, edit, and delete pricing plans, so that I can model different valuation scenarios for my empire's economy.

#### Acceptance Criteria

1. THE Pricing_Plan SHALL have a unique identifier (UUID), a name, an optional description, an optional Fixed_Cost_Per_Item (credits, defaults to zero), and an optional Hourly_Cost_Rate (credits per hour, defaults to zero).
2. WHEN the user creates a new Pricing_Plan, THE Application SHALL persist the plan to PlayerData with a generated UUID, associated with the current player profile.
3. WHEN the user edits a Pricing_Plan name, description, or time cost fields, THE Application SHALL persist the changes to PlayerData.
4. WHEN the user deletes a Pricing_Plan, THE Application SHALL remove the plan and all associated Base_Resource_Prices from PlayerData.
5. THE Application SHALL allow multiple Pricing_Plans to exist simultaneously per player profile.
6. IF a Pricing_Plan name is empty or whitespace, THEN THE Application SHALL reject the save and display a validation message.
7. THE Pricing_Plan SHALL belong to a single player profile (per-player ownership).
8. THE Application SHALL accept non-negative decimal values for Fixed_Cost_Per_Item and Hourly_Cost_Rate.

### Requirement 2: Base Resource Price Entry

**User Story:** As a player, I want to set per-resource prices at Refined, S1, and S2 purity levels within a pricing plan, so that the system can compute costs for everything built from those resources.

#### Acceptance Criteria

1. WHEN the user opens a Pricing_Plan for editing, THE Application SHALL display all known resources (from the Resource list) at Refined purity, plus all S1 and S2 synthetic resources, with their current price in the plan (or blank if unset).
2. WHEN the user enters a Base_Resource_Price for a resource and purity combination, THE Application SHALL persist the price within the Pricing_Plan.
3. THE Application SHALL accept Base_Resource_Prices as non-negative decimal values in credits, including zero.
4. IF the user enters a negative price value, THEN THE Application SHALL reject the input and display a validation message.
5. WHEN the user clears a Base_Resource_Price, THE Application SHALL remove the price entry from the Pricing_Plan (treating the resource as unpriced, distinct from a zero price).
6. THE Application SHALL limit priceable purity levels to Refined, S1, and S2 only (no unrefined High/Medium/Low purities).

### Requirement 3: Commodity Price Computation

**User Story:** As a player, I want the system to compute commodity prices from their construction resource inputs, so that I can see the true cost of producing commodities.

#### Acceptance Criteria

1. WHEN a Pricing_Plan has Base_Resource_Prices set, THE Price_Calculator SHALL compute the price of each Commodity by summing (input resource quantity × input resource Refined price) for all entries in the Commodity's ConstructionResources dictionary.
2. IF any input resource in a Commodity's ConstructionResources has no Base_Resource_Price entry in the plan (not even zero), THEN THE Price_Calculator SHALL flag the Computed_Price as incomplete.
3. THE Price_Calculator SHALL use the resource name keys from ConstructionResources to look up the corresponding Refined Base_Resource_Price in the Pricing_Plan.
4. WHEN all input resources in a Commodity's ConstructionResources have a Base_Resource_Price entry (including zero), THE Price_Calculator SHALL mark the Computed_Price as complete.

### Requirement 4: Manufactured Item Price Computation

**User Story:** As a player, I want the system to compute the manufacturing cost of a blueprint's output item, so that I can evaluate whether manufacturing is profitable.

#### Acceptance Criteria

1. WHEN a Pricing_Plan has resource prices available, THE Price_Calculator SHALL compute the price of a manufactured item using the formula: **Resource Cost + Fixed_Cost_Per_Item + (Hourly_Cost_Rate × Manufacturing Hours)**, where Resource Cost is the sum of (input quantity × input resource price) for all entries in the Blueprint's Resources dictionary.
2. THE Price_Calculator SHALL add the plan's Fixed_Cost_Per_Item to the manufactured item's Computed_Price (zero if not set).
3. WHEN the plan's Hourly_Cost_Rate is greater than zero and Manufacturing_Time is known for the blueprint, THE Price_Calculator SHALL add (Hourly_Cost_Rate × manufacturing hours) to the manufactured item's Computed_Price.
4. IF any input in a Blueprint's Resources dictionary has no price entry in the plan (not even zero), THEN THE Price_Calculator SHALL flag the manufactured item's Computed_Price as incomplete.
5. THE Price_Calculator SHALL use the resource names from the Blueprint's Resources dictionary as-is (evolution changes are already reflected in the blueprint's resource list).
6. ALL three cost components (resource cost, fixed per item, hourly rate) SHALL be additive — they always stack.

### Requirement 5: Pricing Plan Serialization

**User Story:** As a player, I want my pricing plans to persist across application sessions, so that I do not lose my pricing configurations.

#### Acceptance Criteria

1. THE Application SHALL serialize Pricing_Plans to JSON using Newtonsoft.Json, following the existing PlayerData persistence pattern.
2. THE Application SHALL deserialize Pricing_Plans from JSON on application load, restoring all Base_Resource_Prices and plan metadata.
3. THE Serializer SHALL format Pricing_Plan JSON as a named object containing the plan metadata (UUID, name, description, time cost settings) and a dictionary of resource prices keyed by resource name and purity.
4. FOR ALL valid Pricing_Plan objects, serializing then deserializing SHALL produce an equivalent object (round-trip property).
5. IF the PlayerData file lacks a PricingPlans section, THEN THE Application SHALL load with an empty pricing plan list (no error).
6. THE Application SHALL persist Pricing_Plans within the player profile section of PlayerData.json (per-player ownership).

### Requirement 6: Incomplete Price Flagging

**User Story:** As a player, I want to see which computed prices are incomplete due to missing resource prices, so that I can identify gaps in my pricing plan.

#### Acceptance Criteria

1. WHEN a Computed_Price has one or more input resources with no Base_Resource_Price entry, THE Price_Calculator SHALL mark the Computed_Price as incomplete.
2. WHEN a resource has an explicit Base_Resource_Price of zero, THE Price_Calculator SHALL treat zero as a valid price (not incomplete).
3. THE Price_Calculator SHALL distinguish between "no entry" (incomplete) and "zero entry" (valid, resource is free) for all price lookups.
4. WHEN a Computed_Price is flagged as incomplete, THE Application SHALL display a visual indicator in the pricing plan form so the user can identify which items have missing inputs.
