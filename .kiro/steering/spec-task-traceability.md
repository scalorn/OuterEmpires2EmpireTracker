---
inclusion: always
---
# Spec Task Traceability

## Rule: Every acceptance criterion must have a task

When creating tasks.md for a spec, EVERY acceptance criterion in requirements.md (or bugfix.md) MUST map to at least one task. Tasks must reference the specific requirement and criterion they satisfy.

## Task Format

Tasks MUST reference their requirement:

```markdown
- [ ] 2.1 Implement SQLite storage backend
  - Satisfies: Req 5, Criterion 5 ("SQLite: single database file, no external dependencies")
```

NOT this (too vague, no traceability):
```markdown
- [ ] 2.1 Implement storage backends
```

## Verification Step (MANDATORY)

After writing tasks.md, perform a traceability matrix check BEFORE implementation begins:

1. List every acceptance criterion from requirements.md (every checkbox/SHALL statement)
2. For each criterion, identify which task covers it
3. If ANY criterion has no task → add a task
4. If ANY task doesn't trace to a criterion → question why it exists

## "Done" Means Requirements Met, Not Code Written

A task is NOT complete just because code exists and compiles. It is complete when:
1. The acceptance criterion it references is demonstrably satisfied
2. The behavior described in the criterion is testable and tested
3. The design document's architecture for that feature is followed (not shortcuts)

## Post-Implementation Verification (MANDATORY)

After all tasks are marked complete, walk through EVERY acceptance criterion in requirements.md and verify:
- The code implements it (not a placeholder, not a TODO)
- The architecture matches the design document (not a shortcut)
- Tests cover it (or it's verified via build/audit)

Do NOT declare a spec "complete" until this verification passes.

## Common Failures to Avoid

1. **"Infrastructure exists but logic is placeholder"** — If the spec says "SHALL process colony timers" and you wrote a timer loop that logs "placeholder," that's NOT done.
2. **"Code exists but doesn't use the specified architecture"** — If the design says "Server references Common for shared models" and you used raw JSON instead, that's NOT done.
3. **"Most criteria are met"** — If 18/20 requirements are done but 2 say SHALL and aren't implemented, the spec is NOT done.
4. **"It's a future enhancement"** — If the spec says SHALL (not SHOULD or MAY), it's required, not optional.
