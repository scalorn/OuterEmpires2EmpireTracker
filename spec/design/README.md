<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Empire Systems — Design Specification

## Directory Structure

| Directory | Contents |
|---|---|
| [design/](design/) | Architecture, data models, services, cascade processing, migration, code standards, remediation, correctness properties, indexing, PlayerContext changes |
| [flows/](flows/) | User interaction flows and cascade diagrams (17 flows) |
| [mockups/](mockups/) | Form mockups and UI control descriptions |
| [decisions/](decisions/) | Resolved design questions (OQ-30 through OQ-40) |

## Source

All documents in this directory were extracted from the monolithic `.kiro/specs/empire-systems/design.md` (~3900 lines) into focused, AI-friendly files (<500 lines each). The original design.md remains the source of truth; these files are organized views of the same content.

## Design Files

| File | Contents |
|---|---|
| [architecture.md](architecture.md) | System architecture and component overview |
| [cascade-processing.md](cascade-processing.md) | Background cascade processing design |
| [code-standards.md](code-standards.md) | Code standards, error handling, and testing strategy |
| [correctness-properties.md](correctness-properties.md) | P1-P12 property-based test specifications |
| [data-models.md](data-models.md) | Data model definitions |
| [indexing.md](indexing.md) | Data flow analysis, in-memory indexing, and index lifecycle |
| [migration.md](migration.md) | Data migration strategy |
| [playercontext-changes.md](playercontext-changes.md) | PlayerContext thread safety, new fields, init methods, events |
| [reference-counting.md](reference-counting.md) | Reference counting design |
| [remediation.md](remediation.md) | R1-R7 pre-iteration remediation items and sequencing |
| [services.md](services.md) | Service layer design |

## Related

- [requirements/](../requirements/) — Feature requirements
- [.kiro/specs/empire-systems/](../.kiro/specs/empire-systems/) — Original spec (design.md, requirements.md, tasks.md)