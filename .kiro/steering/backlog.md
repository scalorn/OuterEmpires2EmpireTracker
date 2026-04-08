---
inclusion: fileMatch
fileMatchPattern: '**/BACKLOG*'
---

# Backlog Rules

- The authoritative backlog file is `spec/BACKLOG.md`. There is only one backlog file.
- NEVER create a new BACKLOG.md anywhere else in the repository (root, .kiro/, etc.).
- When adding backlog items, append them to `spec/BACKLOG.md` under the appropriate section.
- New items must have a unique ID following the `BL-NNN` pattern, incrementing from the highest existing ID.
- Each item needs a `### BL-NNN: Title` heading, a `**Dependencies:**` line, and a description paragraph.
