# Supply Chains

Supply chains model your resource pipelines from extraction to delivery. Open from **Manage  Supply Chains**.

## Creating a Supply Chain

1. Click **New** to create a supply chain
2. Give it a name (e.g. "Iron Pipeline")
3. Add stages to define the flow

## Stage Types

Each stage represents a step in your resource pipeline:

- **Mine**  Mining at a colony
- **AsteroidMine**  Mining at an asteroid
- **PickUp**  Collecting accumulated resources at a location
- **Refine**  Refining resources at a colony
- **Research**  Research/evolution at a colony
- **Deliver**  Final delivery to a destination

## Stage Configuration

Each stage has:
- **Location Type**  Colony, Station, Asteroid, or Ship
- **Location**  The specific location for this stage
- **Resource**  What resource this stage handles
- **Purity**  The purity level of the resource
- **Threshold**  For PickUp/Refine/Deliver stages, the quantity that triggers a delivery
- **Rate/hr**  Expected production rate per hour
- **Route**  The delivery route to use when the threshold is exceeded

## Thresholds and Delivery Routes

Stages with an accumulation threshold (PickUp, Refine, Deliver) must have a delivery route assigned. When the background processor detects that inventory at a stage's location exceeds the threshold, it generates a delivery plan on the designated route to move the excess.

Mine and AsteroidMine stages don't need thresholds  they produce continuously.

## Active/Inactive Toggle

Use the **Active** checkbox to pause or resume a chain. Inactive chains appear grayed out in the list and are completely skipped by background processing. This lets you temporarily disable a pipeline without deleting it.

## Flow Summary

The bottom of the form shows a condensed text summary of your pipeline stages, making it easy to see the full flow at a glance (e.g. "Mine@Alpha  PickUp@Station(5000)  Refine@Gamma(3000)  Deliver@Beta").