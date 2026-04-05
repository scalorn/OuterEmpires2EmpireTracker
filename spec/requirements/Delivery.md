# Delivery & Route Requirements

## Background

Deliveries move items between colonies and a central hub (space station, deferred). Types of deliveries:
- Commodities requested by colonies
- Workers needed to meet ideal build plans
- Resources for manufacturing items and commodities
- Pickup of mined/refined resources to prevent warehouse overflow

Colonies are on planets. Planets are in systems. Travel between systems costs time and fuel. Travel between planets within a system costs time. Delivery routes define an ordered sequence of stops to minimize travel.

## Phase 1: System Name Tracking

**REQ-DEL-001** Colony SHALL have a `SystemName` string property, serialized to JSON, defaulting to empty string.
**REQ-DEL-002** Survey SHALL have a `SystemName` string property, serialized to JSON, defaulting to empty string.
**REQ-DEL-003** SurveyParser SHALL extract SystemName from the survey title (format: "PlanetName, SystemName (SurveyID)"). The SystemName is the text between the comma and the opening parenthesis.
**REQ-DEL-004** When a colony is created or edited, the user SHALL be able to enter or edit the SystemName.
**REQ-DEL-005** When a survey is imported, the SystemName SHALL be auto-populated from the parsed survey data.

## Phase 2: Route Builder — Data Model

**REQ-DEL-010** A `DeliveryRoute` SHALL have: UUID, Name, OwnerUUID, and an ordered list of `RouteStop` entries.
**REQ-DEL-011** A `RouteStop` SHALL have: ColonyUUID and a sequence number (int, 0-based).
**REQ-DEL-012** DeliveryRoute SHALL be serialized to JSON as part of PlayerData.json (new `DeliveryRoute[]` array on PlayerRoot).
**REQ-DEL-013** DeliveryRoute.OwnerUUID SHALL match the owning player's UUID. Routes are per-player.
**REQ-DEL-014** PlayerContext SHALL maintain a `deliveryRouteList` (BindingList) loaded/saved alongside other player data.
**REQ-DEL-015** RouteStop.ColonyUUID SHALL reference a colony owned by the same player.

## Phase 3: Route Builder — UI

**REQ-DEL-020** A Route Builder form SHALL be accessible from the MainWindow Edit menu.
**REQ-DEL-021** The form SHALL have a left panel with a filtered list of saved routes (Name column).
**REQ-DEL-022** The form SHALL have a right panel with:
  - Route name text field
  - Ordered grid of stops showing: sequence #, colony name, planet name, system name
  - Up/Down/Delete buttons to reorder or remove stops
  - "Add Stop" section with a colony picker (combo box filtered by current player's colonies)
**REQ-DEL-023** Save SHALL persist the route. Delete SHALL remove it. New SHALL clear the form.
**REQ-DEL-024** The route list SHALL filter by current player and refresh on CurrentPlayerChanged.
**REQ-DEL-025** The colony picker SHALL show colonies as "PlanetName - ColonyName (SystemName)".

## Phase 4: Delivery Planning — Commodity Summary (Future)

**REQ-DEL-030** Given a route, the system SHALL generate a consolidated commodity request list across all colonies in the route.
**REQ-DEL-031** For each stop, the system SHALL show what commodities to drop off.
**REQ-DEL-032** The user SHALL be able to manually add/remove items from the delivery plan.

## Phase 5: Delivery Planning — Workers & Resources (Future)

**REQ-DEL-040** The system SHALL identify colonies with unmet worker needs (actual vs ideal) and include worker deliveries.
**REQ-DEL-041** The system SHALL identify colonies with manufacturing resource shortages and include resource deliveries.
**REQ-DEL-042** The system SHALL identify colonies with warehouse overflow risk and include resource pickups.

## Phase 6: Ship Integration (Future)

**REQ-DEL-050** When ships are modeled, delivery plans SHALL respect cargo capacity.
**REQ-DEL-051** Multi-route deliveries SHALL be supported when cargo exceeds single-route capacity.

## Phase 7: Space Station Hub (Future)

**REQ-DEL-060** Space stations SHALL be modeled as build structures not tied to a planet.
**REQ-DEL-061** Space stations SHALL serve as the central hub for resource storage and delivery staging.
**REQ-DEL-062** Space stations SHALL have no storage limitations.
