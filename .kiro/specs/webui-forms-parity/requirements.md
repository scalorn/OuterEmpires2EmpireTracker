# Requirements Document

## Introduction

This feature brings the OE2EmpireTracker web UI (React/TypeScript/Vite) to feature parity with the desktop WinForms application's forms. The current web UI has minimal CRUD list pages with basic text inputs. The goal is to implement rich, interactive forms matching the desktop app's functionality for colony management, blueprints, surveys, player profiles, delivery routes, delivery execution, ship templates, market, supply chains, stock targets, pricing plans, shared data viewing, and supporting forms.

## Glossary

- **Web_UI**: The React/TypeScript single-page application at OE2EmpireTracker.Web, served via Vite
- **Desktop_App**: The .NET Framework 4.8.1 WinForms application at OE2EmpireTracker/
- **Server**: The ASP.NET Core net8.0 API server at OE2EmpireTracker.Server
- **Baseline_Data**: Shared game reference data (flatpack types, ship classes, tech levels, commodities) served via GET /api/v1/global/baseline
- **Colony_Form**: The web UI page for managing colonies, structures, warehousing, and workers
- **Blueprint_Form**: The web UI page for managing blueprints, properties, evolution, and manufacturing
- **Survey_Form**: The web UI page for managing planet and asteroid resource surveys
- **Profile_Form**: The web UI page for managing player profiles, skills, and ranks
- **Route_Form**: The web UI page for managing delivery routes and stops
- **Execution_Form**: The web UI page for executing delivery plans step by step
- **Ship_Template_Form**: The web UI page for designing ship configurations with component slots
- **Market_Form**: The web UI page for managing market listings and transactions
- **Supply_Chain_Form**: The web UI page for managing supply chains between colonies
- **Stock_Target_Form**: The web UI page for managing stock profiles and warehouse target levels
- **Pricing_Plan_Form**: The web UI page for managing pricing plans with per-item prices
- **Shared_Data_View**: The web UI page for viewing data shared by other faction members
- **DataTable**: A reusable React component for displaying sortable, filterable tabular data
- **Detail_Panel**: A right-side panel showing detailed information for a selected entity
- **Filter_Bar**: A set of filter controls (text search, dropdowns) for narrowing entity lists


## Requirements

### Requirement 1: Colony Management Form

**User Story:** As a player, I want to manage my colonies in the web UI with the same capabilities as the desktop app, so that I can view structures, warehouse contents, worker assignments, and colony status without needing the desktop application.

#### Acceptance Criteria

1. THE Colony_Form SHALL display a searchable, sortable list of all colonies for the current player in a left panel
2. WHEN a colony is selected from the list, THE Colony_Form SHALL display colony details (planet name, colony name, system name) and tabbed content (Structures, Warehousing, Commodity Requests, Administration) in a Detail_Panel. THE Colony_Form SHALL hide the tabbed content area when no colony is selected
3. THE Colony_Form SHALL display each structure with its blueprint type, status (staged/building/built/online), worker assignments, and processing timer on the Structures tab
4. WHEN the user selects a flatpack from a filtered dropdown and clicks Add, THE Colony_Form SHALL add a new structure to the colony
5. WHEN the user clicks Optimize, THE Colony_Form SHALL call POST /api/v1/colony-planner/build-order to compute the optimal build sequence, then automatically persist the reordered structures back to the colony via PUT
6. THE Colony_Form SHALL display warehouse inventory grouped by item type with columns for name, purity, and quantity on the Warehousing tab
7. THE Colony_Form SHALL display commodity requests with commodity name, quantity, need-by date, and fulfilled status on the Commodity Requests tab
8. THE Colony_Form SHALL display a colony status summary showing power, habitation, food, entertainment, warehouse capacity, and worker allocation on the Structures tab
9. WHEN the user clicks Bootstrap, THE Colony_Form SHALL call POST /api/v1/characters/{uuid}/colonies/{colonyUuid}/bootstrap to add a standard set of starter structures to the colony server-side
10. THE Colony_Form SHALL display an Administration tab with import staleness indicators (no color for fresh, yellow for 5-6 days, red for 6+ days) and a colony status report



### Requirement 2: Blueprint Management Form

**User Story:** As a player, I want to manage my blueprints in the web UI with filtering, property editing, and evolution tracking, so that I can organize my blueprint collection and track evolution chains.

#### Acceptance Criteria

1. THE Blueprint_Form SHALL display a filterable list of blueprints with columns for type, name, tech level, evolution, nick name, and reference count
2. THE Blueprint_Form SHALL provide filter controls for text search, blueprint type, ship class, tech level, and evolution level
3. WHEN a blueprint is selected, THE Blueprint_Form SHALL display editable detail fields (name, type, ship class, tech level, evolution, nick name, global flag) in a Detail_Panel
4. THE Blueprint_Form SHALL display a Statistics tab showing the blueprint's numeric properties as an editable grid
5. THE Blueprint_Form SHALL display a Resources tab showing manufacturing resource requirements as an editable grid
6. THE Blueprint_Form SHALL display an Evolution Graph tab showing property percentage changes across evolution levels as a line chart
7. WHEN the user clicks Save, THE Blueprint_Form SHALL persist all changes (details, properties, resources) to the server via the blueprints API endpoint
8. WHEN the user clicks New, THE Blueprint_Form SHALL clear the form and prepare for creating a new blueprint
9. WHEN the user clicks Delete, THE Blueprint_Form SHALL prompt for confirmation and remove the blueprint via the API



### Requirement 3: Survey Management Form

**User Story:** As a player, I want to manage planet and asteroid resource surveys in the web UI, so that I can view resource data, filter surveys, and track mining potential.

#### Acceptance Criteria

1. THE Survey_Form SHALL display a filterable list of surveys with columns for planet name, system name, survey type, and scan date
2. THE Survey_Form SHALL provide filter controls for text search, survey type (Planet/Asteroid/All), purity level, and minimum yield amount
3. WHEN a survey is selected, THE Survey_Form SHALL display detail fields (planet name, system name, survey ID, nick name, scanned by, scan date, sensor abundance, purity modifier, scan level, scanner blueprint) in a Detail_Panel
4. THE Survey_Form SHALL display a resource grid showing resource name, purity, amount, and max reserve (for asteroids) for the selected survey
5. WHEN the user edits resource grid rows, THE Survey_Form SHALL allow adding, editing, and removing resource entries
6. WHEN the user clicks Save, THE Survey_Form SHALL persist all changes to the server via the surveys API endpoint
7. WHEN the user clicks New, THE Survey_Form SHALL clear the form for creating a new survey manually
8. WHEN the user clicks Delete, THE Survey_Form SHALL prompt for confirmation and remove the survey via the API
9. IF a survey is assigned to mining rigs, THEN THE Survey_Form SHALL disable the Delete button entirely with a tooltip explaining that the survey cannot be deleted while assigned to mining rigs



### Requirement 4: Player Profile Form

**User Story:** As a player, I want to manage my character profile in the web UI including skills, ranks, and basic information, so that I can track skill progression and character stats.

#### Acceptance Criteria

1. THE Profile_Form SHALL display a list of player profiles in a left panel with the ability to select one
2. WHEN a profile is selected, THE Profile_Form SHALL display editable fields for name, faction, total credits, and skill points
3. THE Profile_Form SHALL display three rank sections (Public, Private, Military) each showing rank level, current XP, and XP to next level
4. THE Profile_Form SHALL display skill groups as collapsible sections, each with a toggle to enable/disable the group
5. THE Profile_Form SHALL display individual skills within each group showing skill name, current level, and training status
6. WHEN the user changes a skill level or training status, THE Profile_Form SHALL update the profile data immediately
7. WHEN the user clicks Save, THE Profile_Form SHALL persist all profile changes to the server via the profiles API endpoint
8. WHEN the user clicks New, THE Profile_Form SHALL clear the form for creating a new profile
9. WHEN the user clicks Delete, THE Profile_Form SHALL prompt for confirmation and remove the profile via the API



### Requirement 5: Delivery Route Form

**User Story:** As a player, I want to create and manage delivery routes in the web UI, so that I can plan logistics between colonies.

#### Acceptance Criteria

1. THE Route_Form SHALL display a filterable list of delivery routes for the current player in a left panel
2. WHEN a route is selected, THE Route_Form SHALL display the route name and an ordered list of stops (colony name, planet name, system name) in a Detail_Panel
3. WHEN the user selects a colony from a dropdown and clicks Add Stop, THE Route_Form SHALL append the colony as a new stop on the route. THE Route_Form SHALL allow adding stops to an empty route (no minimum stop count required)
4. THE Route_Form SHALL allow reordering stops via Up/Down controls
5. THE Route_Form SHALL allow removing individual stops from the route
6. WHEN the user clicks Save, THE Route_Form SHALL persist the route and its stops to the server via the delivery-routes API endpoint
7. WHEN the user clicks New, THE Route_Form SHALL clear the form for creating a new route
8. WHEN the user clicks Delete, THE Route_Form SHALL prompt for confirmation and remove the route via the API


### Requirement 6: Delivery Plan Management

**User Story:** As a player, I want to create and manage delivery plans within routes, so that I can specify what items to pick up and drop off at each stop.

#### Acceptance Criteria

1. WHEN a route is selected, THE Route_Form SHALL display a Plan tab with a dropdown to select or create delivery plans
2. WHEN the user clicks New Plan, THE Route_Form SHALL create a plan with a default name based on route name and current date
3. WHEN a stop is selected and a plan is active, THE Route_Form SHALL display drop-off and pick-up item lists for that stop
4. THE Route_Form SHALL allow adding items to drop-off and pick-up lists with item type, name, purity (for resources), and quantity fields
5. THE Route_Form SHALL allow removing items from drop-off and pick-up lists
6. WHEN the user clicks Auto-Fill, THE Route_Form SHALL populate the plan based on colony commodity requests, needed flatpacks, and manufacturing resources
7. WHEN the user clicks Save, THE Route_Form SHALL persist the plan and all item assignments to the server via the delivery-plans API endpoint



### Requirement 7: Delivery Execution Form

**User Story:** As a player, I want to execute delivery plans step by step in the web UI, so that I can track progress as I deliver items in-game.

#### Acceptance Criteria

1. THE Execution_Form SHALL display route and plan selection dropdowns with text filters
2. WHEN a route and plan are selected, THE Execution_Form SHALL display a consolidated load list showing all items to load with total quantity and volume
3. THE Execution_Form SHALL display each stop as a section with checkable drop-off and pick-up items
4. WHEN the user checks a commodity drop-off item, THE Execution_Form SHALL mark the corresponding commodity request on the target colony as fulfilled via the API
5. WHEN the user checks a flatpack drop-off item, THE Execution_Form SHALL mark the corresponding colony structure as staged via the API
6. WHEN all items at a stop are checked, THE Execution_Form SHALL display a Complete Stop button
7. WHEN the user clicks Complete Stop, THE Execution_Form SHALL mark the stop as done and only then show visual completion indication (the visual completion state is shown only after the Complete Stop button is clicked)
8. WHEN all stops are complete, THE Execution_Form SHALL mark the plan as completed via the API


### Requirement 8: Ship Template Form

**User Story:** As a player, I want to design ship configurations in the web UI by selecting hulls and filling component slots, so that I can plan ship builds and compare loadouts.

#### Acceptance Criteria

1. THE Ship_Template_Form SHALL display a list of ship templates for the current player in a left panel
2. WHEN a template is selected, THE Ship_Template_Form SHALL display a hull dropdown populated from Baseline_Data ship classes
3. WHEN a hull is selected, THE Ship_Template_Form SHALL display the available component slots grouped by slot type (reactors, drives, weapons, cargo, shields)
4. THE Ship_Template_Form SHALL provide a filtered dropdown for each slot showing only compatible blueprints from the player's collection
5. THE Ship_Template_Form SHALL display a live-updating stats panel showing mass, power balance, cargo capacity, defence ratings, and propulsion
6. THE Ship_Template_Form SHALL display a pricing section with a pricing plan dropdown that shows the total estimated build cost
7. WHEN the user clicks Save, THE Ship_Template_Form SHALL persist the template configuration to the server via the ship-templates API endpoint
8. WHEN the user clicks New, THE Ship_Template_Form SHALL clear the form for creating a new template
9. WHEN the user clicks Order Build, THE Ship_Template_Form SHALL generate manufacturing items for the hull and all components via the build-plans API



### Requirement 9: Market Form

**User Story:** As a player, I want to manage market listings and transactions in the web UI, so that I can track trading activity and profitability.

#### Acceptance Criteria

1. THE Market_Form SHALL display a Listings tab showing all active market listings with station, item, quantity, price, and condition columns
2. THE Market_Form SHALL allow creating new listings with station, item, quantity, price, and optional condition/max-repair fields
3. WHEN the user clicks Record Sale on a listing, THE Market_Form SHALL decrement the listing quantity, create a transaction record, and remove the listing when quantity reaches zero
4. THE Market_Form SHALL display a Transactions tab with a filterable history of all buy and sell transactions
5. THE Market_Form SHALL provide transaction filters for type (Buy/Sell), item, counterparty, faction, station, and date range
6. THE Market_Form SHALL allow recording purchases with item, quantity, price, station, and counterparty fields
7. THE Market_Form SHALL display a Summary tab showing profit/loss totals and per-item breakdown for the filtered period


### Requirement 10: Shared UI Components and Navigation

**User Story:** As a player, I want consistent navigation and reusable UI patterns across all web forms, so that the interface is predictable and easy to use.

#### Acceptance Criteria

1. THE Web_UI SHALL provide navigation links to all form pages (Colonies, Blueprints, Surveys, Profiles, Delivery Routes, Delivery Execution, Ship Templates, Market, Supply Chains, Stock Targets, Pricing Plans, Shared Data, Colony Activity, Daily Build, Build Planner, Contacts, Stations, Asteroids) in the authenticated section
2. THE Web_UI SHALL use a consistent master-detail layout pattern with a filterable list on the left and a detail panel on the right for entity management forms
3. THE Web_UI SHALL provide a reusable Filter_Bar component supporting text search, dropdown filters, and checkbox filters
4. THE Web_UI SHALL provide a reusable editable grid component for tabular data entry (resources, properties, warehouse items) using inline cell editing (click a cell to edit in place, changes saved on blur or Enter)
5. THE Web_UI SHALL display loading states, error states with retry, and empty states consistently across all forms
6. THE Web_UI SHALL use Baseline_Data from the server to populate dropdowns for blueprint types, ship classes, tech levels, resources, and commodities
7. THE Web_UI SHALL keep unsaved-changes detection always active. WHEN detection is unavailable (e.g., component error), THE Web_UI SHALL block all navigation until detection is restored.
8. THE Web_UI SHALL use infinite scroll for entity lists exceeding the initial page size (50 items)
9. THE Web_UI SHALL be responsive, adapting the master-detail layout for desktop browsers, tablets, and mobile devices (collapsing to single-panel navigation on small screens)



### Requirement 11: Colony Activity and Daily Build Forms

**User Story:** As a player, I want to view colony activity timers and daily build optimization in the web UI, so that I can monitor processing status and plan daily operations.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Colony Activity page displaying active timers across all colonies grouped by activity type (mining, refining, manufacturing, research, building)
2. THE Web_UI SHALL only display countdown timers when processing timers are actually active. WHEN a timer reaches zero, THE Web_UI SHALL keep showing zero until the user navigates away from the page.
3. THE Web_UI SHALL provide an inactivity mode toggle showing idle structures that need attention
4. THE Web_UI SHALL provide a Colony Daily Build page showing the optimized daily build order for a selected colony
5. WHEN the user selects a colony on the Daily Build page, THE Web_UI SHALL only display resource requirements and time estimates after the user selects a colony, which triggers the build sequence computation via the colony-planner API


### Requirement 12: Build Planner Form

**User Story:** As a player, I want to manage build plans in the web UI, so that I can track manufacturing orders for colony structures and ship components.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Build Planner page displaying a list of build plans for the current player
2. WHEN a build plan is selected, THE Web_UI SHALL display the plan's items with blueprint name, quantity, status (pending/allocated/complete), and assigned colony
3. THE Web_UI SHALL allow adding items to a build plan by selecting a blueprint and quantity
4. THE Web_UI SHALL allow marking items as allocated or complete
5. THE Web_UI SHALL display resource requirements for pending items aggregated across the plan

### Requirement 13: Supporting Entity Forms

**User Story:** As a player, I want to manage contacts, stations, and asteroids in the web UI, so that all entity types from the desktop app are accessible.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Contacts page with a list of faction contacts showing name, faction, and notes, with create/edit/delete capabilities
2. THE Web_UI SHALL provide a Stations page with a list of stations showing name, system, and type, with create/edit/delete capabilities
3. THE Web_UI SHALL provide an Asteroids page with a list of asteroids showing name, system, and linked survey, with create/edit/delete capabilities
4. WHEN the user edits a contact, station, or asteroid, THE Web_UI SHALL display an inline edit form with all relevant fields
5. WHEN the user clicks Save on a supporting entity form, THE Web_UI SHALL persist changes to the server via the corresponding API endpoint



### Requirement 14: Supply Chain Management Form

**User Story:** As a player, I want to manage supply chains in the web UI, so that I can plan resource flows between colonies and track production dependencies.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Supply Chains page displaying a filterable list of supply chains for the current player
2. WHEN a supply chain is selected, THE Web_UI SHALL display the chain's name, source colony, destination colony, and an ordered list of steps
3. THE Web_UI SHALL allow creating new supply chains with name, source colony, and destination colony fields
4. THE Web_UI SHALL allow adding steps to a supply chain specifying resource/commodity, quantity, and processing type
5. THE Web_UI SHALL allow reordering, editing, and removing steps from a supply chain
6. WHEN the user clicks Save, THE Web_UI SHALL persist the supply chain to the server via the supply-chains API endpoint
7. WHEN the user clicks Delete, THE Web_UI SHALL prompt for confirmation and remove the supply chain via the API



### Requirement 15: Stock Target Management Form

**User Story:** As a player, I want to manage stock targets and stock profiles in the web UI, so that I can define warehouse target levels for colonies and track inventory goals.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Stock Targets page displaying stock profiles for the current player
2. WHEN a stock profile is selected, THE Web_UI SHALL display the profile name and a list of stock target items with item name, target quantity, and current quantity
3. THE Web_UI SHALL allow creating new stock profiles with a name and assigning them to colonies
4. THE Web_UI SHALL allow adding stock target items to a profile specifying item type, name, purity (for resources), and target quantity
5. THE Web_UI SHALL allow editing and removing stock target items from a profile
6. WHEN the user clicks Save, THE Web_UI SHALL persist the stock profile and targets to the server via the stock-profiles and stock-plans API endpoints
7. WHEN the user clicks Delete, THE Web_UI SHALL prompt for confirmation and remove the stock profile via the API



### Requirement 16: Pricing Plan Management Form

**User Story:** As a player, I want to manage pricing plans in the web UI, so that I can define per-item prices for ship build cost estimation and market valuation.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Pricing Plans page displaying a list of pricing plans for the current player
2. WHEN a pricing plan is selected, THE Web_UI SHALL display the plan name and a list of item prices with item name, item type, and unit price
3. THE Web_UI SHALL allow creating new pricing plans with a name
4. THE Web_UI SHALL allow adding item prices to a plan by selecting an item from a filtered dropdown and entering a unit price
5. THE Web_UI SHALL allow editing and removing item prices from a plan
6. WHEN the user clicks Save, THE Web_UI SHALL persist the pricing plan to the server via the pricing-plans API endpoint
7. WHEN the user clicks Delete, THE Web_UI SHALL prompt for confirmation and remove the pricing plan via the API



### Requirement 17: Shared/External Character Data View

**User Story:** As a player, I want to view data shared with me by other players in my faction, so that I can see shared blueprints, surveys, and other entities without switching accounts.

#### Acceptance Criteria

1. THE Web_UI SHALL provide a Shared Data page accessible from the navigation
2. THE Web_UI SHALL display a list of characters who have shared data with the current player
3. WHEN a sharing character is selected, THE Web_UI SHALL display the shared data types (blueprints, surveys, colonies) as tabs
4. THE Web_UI SHALL display shared entities in read-only mode using the same list/detail layout as the owning forms
5. THE Web_UI SHALL clearly indicate shared data with a visual badge distinguishing it from owned data



### Requirement 18: Real-Time Updates

**User Story:** As a player, I want the web UI to reflect changes made by background processing or other clients in real time, so that timer countdowns and status changes appear without manual refresh.

#### Acceptance Criteria

1. THE Web_UI SHALL receive real-time entity update notifications via the existing WebSocket connection
2. WHEN a WebSocket notification indicates an entity has changed, THE Web_UI SHALL invalidate the relevant React Query cache and refetch the updated data
3. WHILE a colony structure has an active processing timer, THE Web_UI SHALL display a live countdown computed client-side from the known end time, decrementing each second
4. WHEN the browser tab is backgrounded, THE Web_UI SHALL recalculate the correct remaining time when the tab regains focus (compensating for throttled timers)
5. WHEN a processing timer reaches zero, THE Web_UI SHALL keep displaying zero until the user navigates away, then update the structure status on next data fetch
6. WHEN the WebSocket connection is lost, THE Web_UI SHALL display a connection status banner and continue showing cached data until reconnection
