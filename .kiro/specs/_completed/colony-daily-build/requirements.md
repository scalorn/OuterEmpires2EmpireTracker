# Requirements Document

## Introduction

The Colony Daily Build feature provides a dedicated form for managing structure building across colonies on a delivery route. Players select a route, and the form displays all colonies along that route that have a staged (but not yet building) structure ready to be built. The player can initiate a build on each colony, which starts a countdown timer representing the build duration. Only one structure can be building at a time per colony, and the base build time for any flatpack is 24 hours, reduced by the Builder skill.

This form follows the same route-selector + scrollable-content pattern established by FormDeliveryExecution.

## Glossary

- **Colony_Daily_Build_Form**: The WinForms form that displays buildable structures across colonies on a selected route.
- **Colony**: A player-owned settlement on a planet containing structures, items, and workers.
- **ColonyStructure**: A single structure within a colony, backed by a flatpack blueprint, with Built/Staged/Online state and a BuildCompletionTime countdown.
- **BuildCompletionTime**: A CountDownTime instance on ColonyStructure representing the time remaining until a structure transitions from building to built.
- **Builder_Skill**: A player skill that reduces build time by 2% per level (minimum 1 second), per REQ-ARCH-072.
- **Staged_Structure**: A ColonyStructure where IsStaged=true and IsBuilt=false, meaning the flatpack has been delivered but construction has not started.
- **Building_Structure**: A ColonyStructure where IsStaged=false, IsBuilt=false, and BuildCompletionTime has time remaining, meaning construction is in progress.
- **DeliveryRoute**: An ordered sequence of RouteStop entries, each referencing a colony UUID.
- **PlayerContext**: The singleton managing all player data, events, and persistence.
- **ExtendedName**: A computed display string on Blueprint combining class, evolution, name, tech level, and nickname.

## Requirements

### Requirement 1: Form Access

**User Story:** As a player, I want to open the Colony Daily Build form from the main menu, so that I can manage structure building across my colonies.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL be accessible from the MainWindow Edit menu as "Colony Daily Build".
2. THE Colony_Daily_Build_Form SHALL implement IProgrammaticUpdateSource with ProgrammaticUpdateGuard pattern.
3. THE Colony_Daily_Build_Form SHALL subscribe to PlayerContext.CurrentPlayerChanged and refresh all data when the current player changes.
4. THE Colony_Daily_Build_Form SHALL subscribe to PlayerContext.ColonyDataChanged and refresh the display when colony data changes externally.
5. THE Colony_Daily_Build_Form SHALL unsubscribe from all PlayerContext events in OnFormClosed.

### Requirement 2: Route Selection

**User Story:** As a player, I want to select a delivery route, so that I can see which colonies on that route have structures ready to build.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL display a route selector dropdown filtered to routes owned by the current player.
2. THE Colony_Daily_Build_Form SHALL display a text filter field that filters the route dropdown by name (case-insensitive substring match).
3. WHEN a route is selected, THE Colony_Daily_Build_Form SHALL display all colonies on that route that are eligible for building.
4. WHEN the route selection is cleared, THE Colony_Daily_Build_Form SHALL clear the buildable structures display.

### Requirement 3: Eligible Colony Filtering

**User Story:** As a player, I want to see only colonies that have a buildable structure and no structure currently building, so that I can focus on actionable items.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL display a colony from the selected route only when that colony has at least one Staged_Structure and has no Building_Structure.
2. A ColonyStructure SHALL be considered a Staged_Structure when IsStaged is true and IsBuilt is false.
3. A ColonyStructure SHALL be considered a Building_Structure when IsStaged is false, IsBuilt is false, and BuildCompletionTime is not null and BuildCompletionTime.TimeRemaining is greater than zero.
4. WHEN a colony has a Building_Structure, THE Colony_Daily_Build_Form SHALL exclude that colony from the buildable list.
5. WHEN a colony has no Staged_Structure, THE Colony_Daily_Build_Form SHALL exclude that colony from the buildable list.

### Requirement 4: Buildable Structure Display

**User Story:** As a player, I want to see the next structure to build and a single build button for each eligible colony, so that I can quickly do my daily builds without extra decisions.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL display each eligible colony as a section showing the colony's PlanetName and ColonyName.
2. FOR each eligible colony, THE Colony_Daily_Build_Form SHALL display only the first Staged_Structure (by colony Structures list order) with the blueprint ExtendedName. The user is responsible for ensuring structures are in the desired build order before using this form.
3. THE Colony_Daily_Build_Form SHALL display exactly one "Build" button per eligible colony, next to the displayed structure name.
4. THE Colony_Daily_Build_Form SHALL NOT display any other staged structures for that colony — only the first one.
5. THE Colony_Daily_Build_Form SHALL use a scrollable FlowLayoutPanel for the colony/structure list, following the same layout pattern as FormDeliveryExecution.

### Requirement 5: Build Time Calculation

**User Story:** As a player, I want the build time to account for my Builder skill level, so that higher skill levels result in faster builds.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL calculate build time as 24 hours (86400 seconds) multiplied by (1 - Builder_Skill level * 0.02).
2. IF the calculated build time is less than 1 second, THEN THE Colony_Daily_Build_Form SHALL use 1 second as the build time.
3. THE Colony_Daily_Build_Form SHALL look up the Builder_Skill level from the PlayerProfile that owns the colony (via Colony.OwnerUUID).

### Requirement 6: Start Building

**User Story:** As a player, I want to click the Build button to start a structure building on a colony, so that the countdown begins and the colony is removed from the buildable list.

#### Acceptance Criteria

1. WHEN the Build button is clicked, THE Colony_Daily_Build_Form SHALL set the Staged_Structure's IsStaged to false using ColonyStructureViewModel.
2. WHEN the Build button is clicked, THE Colony_Daily_Build_Form SHALL initialize the structure's BuildCompletionTime as a new CountDownTime with TimeRemaining set to the calculated build time.
3. WHEN the Build button is clicked, THE Colony_Daily_Build_Form SHALL call PlayerContext.writeContext() to persist the change.
4. WHEN the Build button is clicked, THE Colony_Daily_Build_Form SHALL fire PlayerContext.OnColonyDataChanged for the affected colony.
5. WHEN the Build button is clicked, THE Colony_Daily_Build_Form SHALL remove that colony from the displayed buildable list.

### Requirement 7: Build Completion Processing

**User Story:** As a player, I want structures to be marked as built when their build timer expires, so that they become functional.

#### Acceptance Criteria

1. WHEN BuildCompletionTime.TimeRemaining reaches zero or below for a ColonyStructure, THE Colony.ProcessColony() SHALL set that structure's IsBuilt to true.
2. WHEN a structure is marked as built by ProcessColony(), THE Colony.ProcessColony() SHALL set BuildCompletionTime to null.
3. THE Colony.ProcessColony() SHALL process structure building (BuildCompletionTime expiration) before processing mining, refining, manufacturing, and research, per REQ-ARCH-080.

### Requirement 8: Single Build Constraint

**User Story:** As a player, I want only one structure building at a time per colony, so that the game's single-build constraint is enforced.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL NOT display a Build button for any colony that already has a Building_Structure.
2. WHILE a colony has a Building_Structure, THE Colony_Daily_Build_Form SHALL exclude that colony from the buildable list entirely.

### Requirement 9: Form Layout and Resize

**User Story:** As a player, I want the form to resize properly, so that the content remains usable at different window sizes.

#### Acceptance Criteria

1. THE Colony_Daily_Build_Form SHALL have a left panel with the route selector and filter, and a right scrollable panel with the buildable structures list.
2. THE Colony_Daily_Build_Form SHALL have layout event handlers that resize the panels and content when the form resizes.
3. THE Colony_Daily_Build_Form SHALL reside in its own directory under Forms/ColonyDailyBuild/ with Designer.cs and .resx files.

### Requirement 10: Colony Form — Structure Build Controls

**User Story:** As a player, I want to start building a staged structure directly from the colony form and see the build countdown, so that I can manage individual colony builds without the daily build form.

#### Acceptance Criteria

1. WHEN a ColonyStructure is staged (IsStaged=true, IsBuilt=false) and no other structure on the colony is currently building, the ColonyStructure control SHALL display a "Build" button.
2. WHEN the Build button is clicked on the ColonyStructure control, the structure SHALL transition from staged to building: IsStaged set to false, BuildCompletionTime initialized with the Builder-skill-adjusted build time.
3. WHEN a ColonyStructure is in the building state (BuildCompletionTime is not null and TimeRemaining > 0), the ColonyStructure control SHALL display the build countdown time remaining using the same completion time display pattern as mining/refining/manufacturing.
4. WHEN a ColonyStructure is in the building state, the ColonyStructure control SHALL display a "Done" button that calls Colony.ProcessColony() to complete the build (same pattern as mining/refining Done).
5. WHEN a ColonyStructure is in the building state, the ColonyStructure control SHALL NOT show any process-specific controls (mining survey selection, refining resource selection, manufacturing blueprint selection, etc.).
6. THE Build button SHALL only be visible when the structure is staged AND no other structure on the colony has an active BuildCompletionTime.
7. AFTER a build is started or completed, the ColonyStructure control SHALL fire ColonyStructureDataChanged to trigger recalculation and refresh of all structure controls.
