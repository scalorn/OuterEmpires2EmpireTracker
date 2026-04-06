# Colony Import Requirements

## HTML Clipboard Import

**REQ-CI-001** The colony form SHALL provide an "Import Clipboard" button that reads HTML from the system clipboard and parses it into the selected colony.
**REQ-CI-002** The colony form SHALL provide an "Import Colony" button that reads an HTML file from disk and parses it into the selected colony.
**REQ-CI-003** The parser SHALL use SGML reader to convert game HTML into well-formed XML for processing.

## Parsed Data

**REQ-CI-010** The parser SHALL extract PlanetName and SystemName from the colony overview section.
**REQ-CI-011** The parser SHALL extract colony structures from embedded JSON data in the HTML, including:
- Structure UUID and flatpack blueprint UUID
- Game sequence number
- Build state (Built, Staged, Online)
- Worker assignments
- Process timers (mining, refining, manufacturing, research)
**REQ-CI-012** The parser SHALL extract commodity demands (CommodityRequested entries) from the HTML.
**REQ-CI-013** The parser SHALL match flatpack blueprint UUIDs against the known blueprint list to resolve blueprint types.

## Merge Semantics

**REQ-CI-020** When importing into an existing colony, structures SHALL be merged by UUID — existing structures are updated, new structures are added.
**REQ-CI-021** The parser SHALL preserve any locally-set data (nicknames, manual overrides) that is not present in the game HTML.
**REQ-CI-022** SystemName SHALL be extracted from the location bar data when available.
