# Spec-Kit vs OE2EmpireTracker: Comparison of Spec Development Approaches

## Executive Summary

GitHub's spec-kit and our project take fundamentally different approaches to specification-driven development. Spec-kit is a **greenfield-oriented, AI-agent-driven workflow toolkit** designed for new projects where specs generate code. Our system is a **brownfield, traceability-focused audit framework** designed for an existing codebase where specs document and constrain ongoing development. Both are valid but serve different needs.

---

## 1. Philosophy

### Spec-Kit
- **Specs generate code.** The specification is the primary artifact; code is its expression.
- Designed for starting new features/projects from scratch with AI agents.
- Emphasizes a linear pipeline: Specify -> Clarify -> Plan -> Tasks -> Implement.
- The "constitution" defines immutable project principles that constrain all generated code.
- Treats specs as executable -- precise enough that AI can produce working implementations.

### Our Approach
- **Specs document and constrain code.** Code and specs co-evolve; neither is purely generative.
- Designed for an existing, growing codebase with continuous feature additions.
- Emphasizes bidirectional traceability: every requirement traces to code, every piece of code traces to requirements.
- Steering rules and audit tools enforce consistency after the fact.
- Specs are living documentation that must stay synchronized with implementation.

---

## 2. Artifact Structure Comparison

| Aspect | Spec-Kit | Our Project |
|--------|----------|-------------|
| Spec location | specs/NNN-feature-name/spec.md | .kiro/specs/feature-name/ + spec/requirements/Domain.md |
| Plan document | specs/NNN/plan.md (tech stack, architecture) | spec/design/ (architecture.md, services.md, data-models.md) |
| Task breakdown | specs/NNN/tasks.md (phased, parallelizable) | .kiro/specs/feature/tasks.md (numbered, hierarchical) |
| Data models | specs/NNN/data-model.md (per-feature) | spec/design/data-models.md (centralized) |
| API contracts | specs/NNN/contracts/ | N/A (WinForms app, no API) |
| Research notes | specs/NNN/research.md | spec/decisions/ |
| User flows | Embedded in spec user stories | spec/flows/ (domain-specific Mermaid diagrams) |
| Mockups | No built-in support | spec/mockups/feature.md (ASCII wireframes) |
| Constitution | memory/constitution.md | .kiro/steering/ (multiple rule files) |
| Backlog/tracking | Uses git branches per feature | spec/BACKLOG.md, spec/COMPLETED.md |
| Help docs | N/A | docs/ (user-facing, embedded in app) |
| Ambiguity tracking | [NEEDS CLARIFICATION] markers in spec | spec/Ambiguities.md (centralized) |

---

## 3. Tooling Comparison

### Spec-Kit Tools
- **specify CLI** (Python, requires Python >= 3.11): The core tool. Bootstraps projects, installs integrations, manages extensions.
- **Slash commands** (AI agent prompts): /speckit.specify, /speckit.clarify, /speckit.plan, /speckit.tasks, /speckit.implement, /speckit.analyze, /speckit.checklist, /speckit.constitution
- **Shell scripts** (bash + PowerShell): check-prerequisites, create-new-feature, setup-plan, common utilities
- **Extension system**: Plugin architecture for adding capabilities (community catalog of 70+ extensions)
- **Workflow engine**: YAML-defined multi-step workflows with gates (approve/reject between phases)
- **Git integration**: Auto-creates feature branches, numbers specs sequentially

### Our Tools
- **audit.js** (Node.js): Runs 11 automated checks (magic strings, spec coverage, PERF timing, reference counters, control wiring, mockup controls, dead code, duplicate code, UTF-8 encoding, logger check, unused classes)
- **fwrite.js**: Reliable file writing that bypasses tool size limits
- **commit.js**: Git commits with proper multi-line messages
- **trxparse.js**: Test result parsing from TRX files
- **Steering rules** (.kiro/steering/*.md): Always-on or conditional guidance for the AI agent
- **Hooks** (.kiro/hooks/): Event-driven automation (post-task audit, file-change triggers)
- **MSBuild + vstest.console**: Build and test verification

---

## 4. Can We Run Spec-Kit's Tools on This Machine?

### Python Requirement
- **Spec-kit requires Python >= 3.11**
- **This machine has Python 3.10.11** (via `py -3`)
- **Verdict: NO.** The specify CLI will not install or run. You would need to upgrade Python to 3.11+.

### Other Dependencies
- **uv / uvx**: Not installed. Required for the recommended installation method.
- **pipx**: Not installed. Required for the alternative installation method.
- **pip**: Available (via `py -3 -m pip`), but version is old (23.0.1) and Python version is too low.
- **Git**: Available and working (required by spec-kit for branch management).
- **Node.js**: Available (we use it for our tools already).

### What Would Be Needed to Run Spec-Kit
1. Upgrade Python to 3.11+ (or install 3.11+ alongside 3.10)
2. Install uv: `py -3 -m pip install uv` (after Python upgrade)
3. Install specify CLI: `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`
4. Run: `specify init . --integration kiro-cli` to bootstrap

### Practical Concern
Even if installed, spec-kit assumes you are starting fresh features on new git branches. Our project already has an established spec structure. The `specify init` would create a `.specify/` directory and install slash-command prompts, but our existing `spec/` and `.kiro/specs/` directories would be ignored by spec-kit entirely.

---

## 5. Integration Problems

### Fundamental Mismatches

1. **Branch-per-feature assumption**: Spec-kit creates a new git branch and a numbered directory (`specs/003-feature-name/`) for every feature. We work on a single branch with specs organized by domain, not by sequence number.

2. **Single-spec-file vs distributed specs**: Spec-kit puts everything for a feature in one `spec.md` file (user stories, requirements, success criteria). We split requirements across domain files (`spec/requirements/Colony.md`, `spec/requirements/Delivery.md`, etc.) because features often touch multiple domains.

3. **No mockup support**: Spec-kit has no concept of ASCII wireframes or UI mockups. Our `spec/mockups/` directory and the `mockup-controls.js` audit tool have no equivalent. For a WinForms app, this is a significant gap.

4. **No backward traceability tooling**: Spec-kit's `/speckit.analyze` command checks forward traceability (spec -> plan -> tasks) but has no equivalent to our audit tools that check code-to-spec alignment (spec-coverage.js, control-wiring.js, mockup-controls.js).

5. **No build/test integration**: Spec-kit's `/speckit.implement` command tells the AI to write code, but has no built-in build verification, test running, or compilation checking. Our workflow mandates build + test + audit after every change.

6. **Constitution vs Steering**: Spec-kit's constitution is a single document with immutable principles. Our steering system is multiple files with conditional inclusion (always-on vs file-match vs manual). Our approach is more granular and context-sensitive.

7. **No reference counting or indexing concepts**: Our `spec/design/reference-counting.md` and `spec/design/indexing.md` have no parallel in spec-kit. These are domain-specific design documents that spec-kit's generic templates don't accommodate.

### Workflow Conflicts

1. **Their flow**: specify -> clarify -> plan -> tasks -> implement (linear, one-shot)
2. **Our flow**: requirements -> design -> tasks -> implement -> audit -> commit (iterative, continuous)
3. Spec-kit expects the AI to generate the entire implementation from tasks in one pass. We work incrementally with checkpoints, property tests, and manual verification.
4. Spec-kit's task format uses `[P]` markers for parallelism and user-story grouping. Our tasks use numbered hierarchies with requirement cross-references.

### What Would Need to Change to Adopt Spec-Kit

- Abandon our `spec/` directory structure entirely, or maintain two parallel spec systems
- Rewrite all requirements into spec-kit's user-story format with acceptance scenarios
- Lose our mockup-to-code verification pipeline
- Lose our 11 automated audit checks (or rewrite them to work with spec-kit's structure)
- Accept that spec-kit has no concept of our domain-specific design documents
- Install Python 3.11+ on this machine

---

## 6. Things Spec-Kit Does That We Don't

### Valuable Ideas We Could Adopt

1. **Structured clarification process** (`/speckit.clarify`): A formal ambiguity-detection taxonomy with categories (Functional Scope, Domain/Data Model, Interaction/UX, Non-Functional, Integration, Edge Cases, Constraints, Terminology). We track ambiguities in `spec/Ambiguities.md` but don't have a systematic detection process.

2. **Constitution/principles document**: A single place declaring immutable project principles. Our steering rules are scattered across multiple files. A unified "project constitution" could be valuable.

3. **Spec quality checklist with self-validation**: Spec-kit generates a checklist after writing a spec and validates against it. We don't have automated spec-quality checks (our audits check code, not spec quality).

4. **Cross-artifact consistency analysis** (`/speckit.analyze`): Detects terminology drift, coverage gaps, and contradictions across spec/plan/tasks. We don't have an equivalent -- our audit tools check code-to-spec, not spec-to-spec.

5. **Extension/plugin system**: Spec-kit has a rich extension ecosystem (70+ community extensions) for adding capabilities. Our tools are bespoke Node.js scripts. An extension system could make our audit tools more modular and shareable.

6. **Workflow engine with gates**: YAML-defined workflows with human approval gates between phases. We rely on steering rules and hooks, which are less structured.

7. **Feature numbering and branch management**: Automatic sequential numbering of features with branch creation. We don't have systematic feature numbering.

8. **Research document**: A dedicated place for technical research and library comparisons before planning. We make decisions but don't always document the research that led to them (spec/decisions/ is closest).

### Ideas That Don't Fit Our Needs

1. **Tech-stack-agnostic spec writing**: Spec-kit insists specs contain no implementation details. For a mature WinForms app, our specs necessarily reference WinForms concepts (DataGridView, BindingSource, etc.) because the tech stack is fixed.

2. **Test-first from spec**: Spec-kit's constitution mandates TDD with tests written before implementation. We do write tests, but our property-test approach is driven by design documents, not by spec acceptance criteria.

3. **Code generation from spec**: The entire premise of "specs generate code" doesn't apply to a brownfield project with 50+ existing forms and services.

---

## 7. Things We Do That Spec-Kit Doesn't

1. **Automated code auditing** (11 checks): Magic strings, dead code, duplicate code, unused classes, PERF timing, control wiring, mockup-to-code sync, reference counter completeness, logger presence, UTF-8 encoding, spec coverage. Spec-kit has nothing comparable.

2. **ASCII mockups with code verification**: Our mockup-controls.js cross-references mockup control names against Designer.cs files. This is unique to our WinForms workflow.

3. **Bidirectional traceability enforcement**: Every class must appear in a spec file (spec-coverage.js). Every form must have a mockup. Every mockup control must exist in code. Spec-kit only checks forward (spec -> tasks).

4. **Domain-specific design documents**: Reference counting, indexing strategies, cascade processing, correctness properties. These are architectural concerns that spec-kit's generic templates don't address.

5. **Centralized ambiguity tracking**: `spec/Ambiguities.md` with AMB-xxx IDs that persist across sessions. Spec-kit's [NEEDS CLARIFICATION] markers are resolved inline and disappear.

6. **Build/test/audit pipeline**: Mandatory compilation with zero warnings, test execution, and audit pass before every commit. Spec-kit has no build integration.

7. **User-facing help documentation**: Our `docs/` directory with topic-based help files embedded in the app. Spec-kit doesn't address end-user documentation.

8. **Backlog and completion tracking**: `spec/BACKLOG.md` and `spec/COMPLETED.md` provide project-level work tracking. Spec-kit relies on git branches (one branch = one feature, merge = done).

9. **Conditional steering**: Our steering files can be always-on, triggered by file patterns, or manually included. Spec-kit's constitution is always-on with no conditional logic.

10. **Post-commit automation**: Backup scripts, audit hooks that run after every task. Spec-kit has extension hooks but they're focused on the specify/plan/implement lifecycle, not on post-commit workflows.

---

## 8. Recommendations

### Don't Adopt Spec-Kit Wholesale
The tool is designed for a fundamentally different workflow (greenfield, code-generation-from-spec). Adopting it would mean abandoning our mature audit infrastructure and traceability system for something that doesn't support brownfield development.

### Ideas Worth Stealing

1. **Formalize a project constitution**: Consolidate our steering rules into a single `spec/constitution.md` that declares immutable principles (write-through pattern, PERF timing, reference counting, etc.). Keep steering files for conditional/contextual guidance.

2. **Add a spec-quality audit check**: Write a new audit.js check that validates our requirements files have testable statements, no vague adjectives without metrics, and proper REQ-xxx IDs on every requirement.

3. **Add a cross-spec consistency check**: Detect terminology drift across our spec files (same concept named differently in different documents).

4. **Structured clarification taxonomy**: When starting a new feature, use spec-kit's ambiguity categories as a checklist: Functional Scope, Data Model, UX Flow, Non-Functional, Integration, Edge Cases, Constraints, Terminology.

5. **Research documents**: For complex features, create a `spec/decisions/NNN-research.md` documenting alternatives considered and why we chose what we chose.

### Don't Bother With

1. The specify CLI itself (Python dependency, wrong workflow model)
2. The extension ecosystem (designed for their tool, not ours)
3. Branch-per-feature numbering (we already have .kiro/specs/ per feature)
4. Their task format (our hierarchical numbered format with requirement refs is better for our needs)

---

## 9. Summary Table

| Capability | Spec-Kit | Us | Winner |
|-----------|----------|-----|--------|
| Greenfield feature creation | Excellent | Good | Spec-Kit |
| Brownfield/existing code | Poor | Excellent | Us |
| Code-to-spec traceability | None | 11 automated checks | Us |
| Spec-to-code traceability | Good (analyze command) | Good (requirement refs) | Tie |
| UI mockup verification | None | Automated | Us |
| Build/test integration | None | Mandatory pipeline | Us |
| Spec quality validation | Good (checklist + analyze) | None | Spec-Kit |
| Ambiguity detection | Excellent (structured taxonomy) | Manual (Ambiguities.md) | Spec-Kit |
| Extension ecosystem | 70+ community plugins | Bespoke scripts | Spec-Kit |
| Cross-artifact consistency | Good (analyze) | Partial (spec-coverage) | Spec-Kit |
| Domain-specific design docs | Generic templates only | Rich (indexing, ref-counting, etc.) | Us |
| User documentation | None | docs/ with help system | Us |
| Tool prerequisites | Python 3.11+, uv, git | Node.js, git (already have) | Us |
