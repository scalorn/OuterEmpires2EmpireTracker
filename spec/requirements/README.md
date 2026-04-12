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
| [ColonyDailyBuild.md](ColonyDailyBuild.md) | Colony Daily Build form: eligibility, build initiation |
| [ColonyImport.md](ColonyImport.md) | Colony HTML clipboard import, parsing, merge semantics |
| [DataChangeEvents.md](DataChangeEvents.md) | Data change events, form subscriptions, write-through pattern |
| [DataModel.md](DataModel.md) | Core data classes, serialization, static reference data |
| [Delivery.md](Delivery.md) | Delivery routes, delivery planning, space stations |
| [GameMechanics.md](GameMechanics.md) | Mining, refining, manufacturing, research, build time formulas |
| [PlayerProfile.md](PlayerProfile.md) | Player profile management, skills, ranks |
| [SafeFileWriter.md](SafeFileWriter.md) | Atomic file write strategy (temp-then-replace) |
| [Survey.md](Survey.md) | Survey management and blueprint scanning |
| [UIStatePersistence.md](UIStatePersistence.md) | Window state persistence, preferences store |

## Conventions

- **SHALL** — mandatory requirement
- **SHOULD** — recommended but not mandatory
- **Verifiable** — every requirement has an observable outcome that can be tested
- **ID format** — `REQ-{DOMAIN}-{NNN}` e.g. `REQ-COL-001`
- **UTC storage** — All DateTime values SHALL be stored in UTC. The game operates in UTC timezone. Application-generated timestamps use `DateTime.UtcNow`. Display in the UI converts to the user's local timezone. See BL-068 for the CountDownTime migration to complete this convention across the full codebase.
