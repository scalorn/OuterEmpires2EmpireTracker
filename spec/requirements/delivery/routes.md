# Delivery Routes

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
**REQ-DEL-014** PlayerContext SHALL maintain a `DeliveryRouteList` (BindingList) loaded/saved alongside other player data.
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

## Phase 9: Station, Asteroid, and Ship Stops

**REQ-DEL-080** RouteStop SHALL support DestinationType (Colony/Station/Asteroid/Ship) and a Purpose enum (Cargo/Refuel/CargoAndRefuel).  
**REQ-DEL-081** DeliveryPlanStop SHALL support DestinationType to match the route stop's destination type.  
**REQ-DEL-082** Stations SHALL be valid route stops for cargo pickup and dropoff.  
**REQ-DEL-083** Asteroids SHALL be valid route stops for resource pickup (ship mining operations).  
**REQ-DEL-084** Delivery execution at station and asteroid stops SHALL follow the same checkbox-per-item pattern as colony stops.

## Route Distance and Fuel Estimation

**REQ-DEL-085** The stops grid SHALL display a "JAS" column showing the Jump Arc Seconds distance between consecutive stops. The first stop SHALL show empty (no previous stop). Same-system legs SHALL show 0.  
**REQ-DEL-086** JAS distance SHALL be computed using `DistanceCalculator.CalculateJas()` from the SystemRepository, resolving system names from the stop's destination (colony, station, or asteroid).  
**REQ-DEL-087** The form SHALL provide a ship picker (FilteredTextComboSet) above the tab control, allowing the user to select a ship for fuel estimation.  
**REQ-DEL-088** When a ship is selected, the "Fuel Est." column SHALL display the estimated fuel consumption per leg, computed as `ShipStats.JumpFuelPerJAS × JAS distance`. Empty when no ship is selected or JAS is 0.  
**REQ-DEL-089** When the selected ship's data or template is modified externally, the form SHALL recompute fuel estimates automatically by subscribing to `ShipDataChanged` and `ShipTemplateDataChanged` events.  
