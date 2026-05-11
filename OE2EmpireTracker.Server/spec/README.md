# OE2EmpireTracker.Server — Spec

This directory contains the **server-specific** working specification for the Remote Faction Service.

## Relationship to Umbrella Spec

The authoritative source is the umbrella spec at:
- `.kiro/specs/remote-faction-service/requirements.md`
- `.kiro/specs/remote-faction-service/design.md`

This project-level spec extracts and summarizes the server-relevant portions for quick reference during development. When in doubt, the umbrella spec is canonical.

## Contents

| File | Purpose |
|------|---------|
| `requirements.md` | Server-specific requirements (Req 1–10, 12–15, 17–20) |
| `design.md` | Server architecture, storage, API design, auth, WebSocket, background processing |
| `api-reference.md` | Quick-reference endpoint map with methods, paths, auth, and shapes |

## What's NOT Here (Yet)

Client-side requirements (Req 11: Client Connectivity, Req 16: Data Portability) remain in the umbrella spec. They will move to a `OE2EmpireTracker/Client/` spec when that work begins.
