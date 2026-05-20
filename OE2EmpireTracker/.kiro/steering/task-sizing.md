# Task Sizing Rules

Every task in a spec's tasks.md MUST be sized to complete correctly in a single pass. Large tasks that touch many files or combine multiple concerns are the primary cause of rework, partial implementations, and subtle bugs.

## Hard Limits

| Metric | Maximum | Rationale |
|--------|---------|-----------|
| Files modified | 5 | Keeps blast radius manageable, fits in agent context |
| New lines of code | 200 | Beyond this, quality drops and errors compound |
| Acceptance criteria covered | 3 | One task, one verifiable outcome |
| Dependencies on other tasks | 1 | Reduces ordering complexity |
| Context files needed to understand the task | 4 | What the agent needs to read to do the work |

## Task Structure

Every task MUST follow this pattern:

```
Task: [verb] [specific thing] in [specific file(s)]
  - Satisfies: Req X, Criterion Y
  - Inputs: [what files to read]
  - Output: [what files are created/modified]
  - Verification: [how to confirm it's done — test name, build check, audit check]
```

## Decomposition Rules

1. **Separate logic from wiring from tests.** "Implement service method" and "Wire service into endpoint" and "Write tests for service" are three tasks, not one.

2. **One file pattern, not N files.** If the same change needs to happen in 10 files (e.g. adding a permission check to 10 endpoints), make it 2-3 tasks grouped by related endpoints, not one mega-task.

3. **Each task has a concrete verification.** Not "implement X" but "implement X, verified by [specific test / diagnostic / audit check]." If you can't name the verification, the task is too vague.

4. **Prefer vertical slices over horizontal layers.** "Add GET /api/v1/foo endpoint + test" is better than "Add all 5 endpoints" followed by "Add all 5 tests." Vertical slices are independently shippable.

5. **Never combine model changes with consumer changes.** If you're adding a new field to a model, that's one task. Updating the 3 services that use it is 1-3 more tasks. Combining them risks the model change being correct but the consumer changes being inconsistent.

## Anti-Patterns (tasks that will fail)

- "Implement the permission system" — too broad, no specific output
- "Update all endpoints to check permissions" — too many files, no single verification
- "Add model + service + endpoint + tests for feature X" — four concerns in one task
- "Refactor PlayerContext to support Y" — PlayerContext is 4000+ lines, touching it requires extreme precision
- "Make all entity setters internal" — 30+ files, impossible to verify all consumers in one pass

## When to Split

If a task description contains AND, split it:
- "Add PermissionService AND wire it into endpoints" → 2+ tasks
- "Create the model AND the service AND the tests" → 3 tasks
- "Update Colony AND Blueprint AND Survey forms" → 3 tasks

## Verification Checklist (for task authors)

Before finalizing tasks.md, verify each task against:
- [ ] Can I name the exact files that will be modified? (≤5)
- [ ] Can I estimate the lines of code? (≤200)
- [ ] Can I name the specific test or check that proves it's done?
- [ ] Does the agent need to read more than 4 files to understand the context?
- [ ] Does this task have more than 1 prerequisite task?

If any answer violates the limits, split the task.
