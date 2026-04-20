# Specification & Design Documentation

## This Folder (`spec/`)

Project-level documentation, requirements, and planning.

| File / Directory | Purpose |
|------|---------|
| [BACKLOG.md](BACKLOG.md) | Open features and enhancements to be worked on |
| [Recommendations.md](Recommendations.md) | Completed issues, recommendations, and their resolutions |
| [GOALS.md](GOALS.md) | High-level project goals |
| [Ambiguities.md](Ambiguities.md) | Open questions and ambiguities |
| [COMPLETED.md](COMPLETED.md) | Completed work log |
| [requirements/](requirements/) | Feature requirements documents (29 domains — see [requirements/README.md](requirements/README.md)) |
| [design/](design/) | Empire Systems architecture, data models, services, cascade processing, migration, code standards |
| [flows/](flows/) | User interaction flows and cascade diagrams (17 flows) |
| [mockups/](mockups/) | Form mockups and UI control descriptions |
| [decisions/](decisions/) | Resolved design questions (OQ-30 through OQ-40) |

## Structured Specs (`.kiro/specs/`)

Detailed design specs live under `.kiro/specs/`. Each spec folder contains requirements, design, and implementation tasks for a feature. These are the authoritative design decisions for implemented features. There are currently 40 spec folders — see the directory listing for the full set. The `empire-systems` spec (8 iterations) is complete. `manufacturing-queue` is superseded by the Build Planner in empire-systems.