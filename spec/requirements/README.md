# OE2 Empire Tracker — Requirements

This directory contains the formal requirements for the OE2 Empire Tracker application.
Each file covers one domain. Requirements are written as verifiable statements.

## Files

| File | Domain |
|------|--------|
| [DataModel.md](DataModel.md) | Core data classes, serialization, static reference data |
| [Colony.md](Colony.md) | Colony management, structures, items, commodity requests |
| [PlayerProfile.md](PlayerProfile.md) | Player profile management, skills, ranks |
| [Survey.md](Survey.md) | Survey management and blueprint scanning |
| [Architecture.md](Architecture.md) | Cross-cutting concerns: persistence, MVVM, testing, logging |

## Conventions

- **SHALL** — mandatory requirement
- **SHOULD** — recommended but not mandatory
- **Verifiable** — every requirement has an observable outcome that can be tested
- **ID format** — `REQ-{DOMAIN}-{NNN}` e.g. `REQ-COL-001`
