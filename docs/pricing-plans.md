# Pricing Plans

Pricing Plans let you assign credit values to resources and see what your commodities and manufactured items actually cost to produce. Create multiple plans to model different scenarios — market value, cost basis, pessimistic estimates, whatever makes sense for your empire.

## Opening the Form

Go to **Manage → Pricing Plans** from the main menu.

## How It Works

Each pricing plan stores base prices for resources at Refined, S1, and S2 purity levels. The system uses these base prices plus the game's bill-of-materials chains to compute rolled-up costs for commodities and manufactured items.

You can also set optional time-cost parameters:
- **Fixed Cost Per Item** — a flat credit amount added to every manufactured item (overhead, facility cost, etc.)
- **Hourly Rate** — credits per hour multiplied by the blueprint's manufacturing time (opportunity cost of facility time)

The manufactured item price formula is: **Resource Cost + Fixed Cost Per Item + (Hourly Rate × Manufacturing Hours)**.

## Creating a Plan

1. Click **New** in the left panel.
2. Enter a name for the plan.
3. Optionally add a description and set time-cost parameters.
4. Click **Save**.

## Setting Resource Prices

When you select a plan, the right panel shows a grid of all known resources with their purity level. Enter a price in the Price column for each resource you want to price.

- Entering **0** means the resource is free (valid price).
- Leaving the field **blank** means the resource is unpriced — any computed price that depends on it will be flagged as incomplete.

## Incomplete Prices

If a commodity or blueprint requires a resource that has no price entry in your plan, the computed price is flagged as incomplete. This helps you identify gaps in your pricing plan so you can fill them in.

## Multiple Plans

You can create as many plans as you want per player profile. Common scenarios:
- **Cost Basis** — what it actually costs you to produce resources (mining + refining time)
- **Market Value** — what you'd pay to buy resources on the market
- **Replacement Cost** — what it would cost to reproduce from scratch
- **Pessimistic / Optimistic** — bracket your estimates

## Deleting a Plan

Select a plan and click **Delete**. You'll be asked to confirm. All resource prices in that plan are removed.

## Where Pricing Plans Are Used

- **Blueprint form** — Select a plan to see the manufacturing cost of a single blueprint.
- **Ship Template form** — Select a plan to see the total build cost of an entire ship design (hull + all components).
- **Stock Targets** — Pricing data feeds into cost estimates for replenishment planning.

## Player Ownership

Each pricing plan belongs to a specific player profile. When you switch players, the plan list updates to show only that player's plans.
