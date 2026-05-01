# OE2 Empire Tracker — Requirements

This directory contains the formal requirements for the OE2 Empire Tracker application.
Each file covers one domain. Requirements are written as verifiable statements.

## Files

| File | Domain |
|------|--------|
| [Architecture.md](Architecture.md) | Cross-cutting concerns: persistence, MVVM, testing, logging |
| [BackgroundProcessing.md](BackgroundProcessing.md) | Background timer, colony processing cycle, error handling |
| [BlueprintProperties.md](BlueprintProperties.md) | Blueprint property validation rules, commodity industries |
| [Colony.md](Colony.md) | Colony management, structures, items, commodity requests |
| [ColonyActivity.md](ColonyActivity.md) | Colony Activity form: countdown timers, filtering, display |
| [ColonyAdminSummary.md](ColonyAdminSummary.md) | Colony Administration tab: per-colony status report |
| [ColonyDailyBuild.md](ColonyDailyBuild.md) | Colony Daily Build form: eligibility, build initiation |
| [ColonyImport.md](ColonyImport.md) | Colony HTML clipboard import, parsing, merge semantics |
| [DataChangeEvents.md](DataChangeEvents.md) | Data change events, form subscriptions, write-through pattern |
| [DataModel.md](DataModel.md) | Core data classes, serialization, static reference data |
| [Delivery.md](Delivery.md) | Delivery routes, delivery planning, space stations |
| [EvolutionGraph.md](EvolutionGraph.md) | Blueprint evolution chain chart |
| [GameMechanics.md](GameMechanics.md) | Mining, refining, manufacturing, research, build time formulas |
| [MainMenu.md](MainMenu.md) | File menu lifecycle, Manage menu, Help/About |
| [MDIWindowMenu.md](MDIWindowMenu.md) | MDI Window menu, layout commands, window numbering |
| [PlayerProfile.md](PlayerProfile.md) | Player profile management, skills, ranks |
| [PlayerProfileImport.md](PlayerProfileImport.md) | Player profile clipboard import from game HTML |
| [Preferences.md](Preferences.md) | Configurable thresholds, intervals, countdown format |
| [PricingPlan.md](PricingPlan.md) | Pricing plans, resource prices, price calculator |
| [SafeFileWriter.md](SafeFileWriter.md) | Atomic file write strategy (temp-then-replace) |
| [BuildPlanner.md](BuildPlanner.md) | Build plans, build items, resource checks, delivery generation, queue calculator |
| [Contacts.md](Contacts.md) | Factions, external characters |
| [Market.md](Market.md) | Market listings, transactions, profit/loss |
| [Ships.md](Ships.md) | Ship templates, ship instances, cargo, stats computation |
| [Stations.md](Stations.md) | Station model, holds, components, munitions |
| [StockTargets.md](StockTargets.md) | Stock plans, targets, profiles, shortfall checks, replenishment |
| [SupplyChains.md](SupplyChains.md) | Supply chain stages, threshold checks, background processing |
| [Survey.md](Survey.md) | Survey management and blueprint scanning |
| [UIStatePersistence.md](UIStatePersistence.md) | Window state persistence, preferences store |
| [NonFunctional.md](NonFunctional.md) | Performance targets, scale assumptions, crash recovery, concurrency |

## Conventions

- **SHALL** — mandatory requirement
- **SHOULD** — recommended but not mandatory
- **Verifiable** — every requirement has an observable outcome that can be tested
- **ID format** — `REQ-{DOMAIN}-{NNN}` e.g. `REQ-COL-001`
- **UTC storage** — All DateTime values SHALL be stored in UTC. The game operates in UTC timezone. Application-generated timestamps use `DateTime.UtcNow`. Display in the UI converts to the user's local timezone.
