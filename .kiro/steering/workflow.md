# Workflow Rules

## After Every Piece of Work
1. **Build and test** before committing — the solution must compile with **zero warnings** and all tests must pass
2. **Run audit** — `node .kiro/tools/audit.js` must report no new findings (PERF timing on small combo methods is an accepted baseline)
3. **Always commit** — never ask whether to commit; just do it after verifying the build/tests pass
3. **Commit** all changes with a detailed commit message describing what was done and why
3. **Update the spec** — if the work relates to a feature or fix with a spec under `.kiro/specs/`:
   - Update **requirements.md**, **design.md**, and **tasks.md** to reflect any new or changed behavior
   - If no spec exists, create one with requirements.md, design.md, and tasks.md (tasks marked done)
   - Spec updates must be included in the same commit as the code changes, not in a separate commit
4. **Update the spec** — if the work relates to a feature or fix with a spec under `spec`:
   - Update any relevant spec files to reflect any new or changed behavior
   - Spec updates must be included in the same commit as the code changes, not in a separate commit
5. **Update mockups** — if the work changes any form's layout, controls, or behavior:
   - Update the corresponding mockup in `spec/mockups/` in the same commit as the code changes
   - If a control is added, removed, renamed, or repositioned, the mockup ASCII wireframe and control list must reflect it
   - If no mockup exists for the form, create one
6. **Update user flows** — if the work changes form behavior or event subscriptions:
   - Verify the user flow diagrams (Mermaid sequence diagrams) in the relevant spec/requirements/ file still match
   - If a new interaction path is added, add it to the flow diagram
   - If the event subscription matrix in DataChangeEvents.md is affected, update it (event-matrix.js will catch mismatches)
6. Do not leave uncommitted changes at the end of a task
7. **Run backup script** after every commit: `D:\projects\OuterEmpires2\OE2EmpireTracker\oebackup.ps1`

## Commit Messages
- First line: concise summary of the change
- Body: list the specific changes grouped by area (UI, parser, tests, etc.)
- Reference the spec if one exists
- Include a `Prompt:` footer with ALL user prompts since the last commit, not just the most recent one. Quote each prompt verbatim (trimmed to one or two sentences if long), one per line. This provides full traceability of the conversation that led to the change.

## Testing
- Use `getDiagnostics` to verify code compiles cleanly after changes
- Do NOT use `dotnet test` — it is incompatible with this project (see tech.md)
- Use `vstest.console` against the built test DLL when tests need to run from CLI
- When the running app locks the exe, use `getDiagnostics` instead of building

## Error Recovery and Tooling
- **When you encounter a recurring error or friction**, stop and ask: can I write or improve a tool, script, or steering rule to prevent this from happening again? If yes, do it before retrying.
- **When a subagent is cancelled or fails mid-task**, check what partial work was completed (files written, tests missing, commits not done). Pick up from where it left off rather than re-delegating the entire task. If the service code exists but tests don't, write the tests directly. If code exists but wasn't committed, build/test/commit directly.
- **When delegating to subagents**, keep context files minimal. Large spec files (1000+ lines) should only be passed when the subagent genuinely needs them. Prefer passing only the files the task directly touches, plus the tasks.md. If a subagent keeps getting cancelled, reduce context and/or implement the task directly instead of re-delegating.
- **If the same type of failure happens twice**, create a tool or steering rule to prevent it. Don't just retry the same approach.
