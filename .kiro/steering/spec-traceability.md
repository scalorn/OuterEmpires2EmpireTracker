---
inclusion: always
---
# Spec Traceability Rules

Every piece of work must be traceable through the full documentation chain. Nothing exists in code that isn't documented. Nothing exists in requirements that can't be traced forward to design and code.

## Traceability Chain

`
Requirements (spec/requirements/) 
    → Design (spec/design/)
    → User Flows (spec/flows/)
    → Mockups (spec/mockups/)
    → Code (OE2EmpireTracker/)
    → Tests (OE2EmpireTracker.Tests/)
    → Help Docs (docs/)
`

## Rules

### Forward Traceability (Requirements → Code)
- Every requirement (REQ-xxx) MUST trace forward to at least one design element, code file, or test
- Every design doc MUST reference which requirements it satisfies
- Every mockup MUST reference which requirements it implements
- Every user flow MUST reference which requirements it demonstrates

### Backward Traceability (Code → Requirements)
- Every service class MUST have corresponding requirements in spec/requirements/
- Every form MUST have a mockup in spec/mockups/ and requirements covering its behavior
- Every data model MUST have requirements in spec/requirements/DataModel.md or a domain-specific file
- Every user-facing feature MUST have a help doc in docs/

### When Making Changes
1. Before writing code: verify requirements exist in spec/requirements/. If not, add them first.
2. Before creating a form: verify a mockup exists in spec/mockups/. If not, create it first.
3. After completing work: update spec/requirements/, spec/design/, spec/flows/, spec/mockups/, and docs/ as needed.
4. The spec update MUST be in the same commit as the code change.

### Cross-References
- Requirements use IDs: REQ-{DOMAIN}-{NNN} (e.g. REQ-COL-001)
- Design docs reference requirements they satisfy: "Satisfies: REQ-COL-001, REQ-COL-002"
- Mockups reference requirements they implement: "Implements: REQ-BPL-080"
- Flows reference requirements they demonstrate: "Demonstrates: REQ-MKT-020"

### What Goes Where
| Artifact | Location | Content |
|----------|----------|---------|
| Requirements | spec/requirements/{Domain}.md | SHALL statements, verifiable behaviors |
| Architecture | spec/design/architecture.md | System-level diagrams, layer descriptions |
| Data Models | spec/design/data-models.md | Entity definitions, field specs, relationships |
| Services | spec/design/services.md | Service APIs, logic descriptions |
| User Flows | spec/flows/user-flows.md | Mermaid sequence diagrams for user interactions |
| Mockups | spec/mockups/{feature}.md | ASCII wireframes, control descriptions |
| Design Decisions | spec/decisions/ | Resolved questions, rationale |
| Help Docs | docs/{topic}.md | User-facing documentation (embedded in app) |

### Gap Detection
- If you write code for a feature and can't find its requirements → add requirements first
- If you find a requirement with no design doc → add the design element
- If you find a mockup with no requirements → add the requirements
- If you find code with no help doc → add the help doc