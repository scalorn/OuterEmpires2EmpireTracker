# Colony Requirements

## User Goal

The user wants to model their in-game colonies — tracking structures, resources, workers, and production — so they can plan builds, monitor status, and optimize colony layouts without constantly switching to the game.

## Out of Scope

- Real-time synchronization with the game (all data is manually imported or entered)
- Colony combat or defense mechanics
- Automated colony management or AI-driven build decisions
- Inter-player colony trading or sharing

## Files

| File | Description | Requirements |
|------|-------------|--------------|
| [data.md](data.md) | Colony data model and serialization | REQ-COL-001 to REQ-COL-003 |
| [status-calculation.md](status-calculation.md) | Resource status calculation across structures | REQ-COL-010 to REQ-COL-020, REQ-COL-110 to REQ-COL-114 |
| [structure-management.md](structure-management.md) | Structure UI, state transitions, and lifecycle | REQ-COL-030 to REQ-COL-038a, REQ-COL-040 to REQ-COL-045 |
| [mining.md](mining.md) | Mining rig structures and processing | REQ-COL-050 to REQ-COL-056b |
| [warehouse.md](warehouse.md) | Colony warehouse items and overflow rules | REQ-COL-060 to REQ-COL-067, REQ-COL-085 to REQ-COL-089 |
| [commodities.md](commodities.md) | Commodity request management | REQ-COL-070 to REQ-COL-075 |
| [persistence.md](persistence.md) | Colony persistence and MVVM pattern | REQ-COL-080 to REQ-COL-082, REQ-COL-090 to REQ-COL-093 |
| [optimization.md](optimization.md) | Build order optimization, bootstrap, and processing order | REQ-COL-095 to REQ-COL-095g, REQ-COL-096 to REQ-COL-096g, REQ-COL-100 |
| [flows.md](flows.md) | User interaction flows and data flow diagrams | — |
| [mockups.md](mockups.md) | Form mockups and ASCII wireframes | — |
