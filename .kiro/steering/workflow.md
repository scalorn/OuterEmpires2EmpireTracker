# Workflow Rules

## After Every Piece of Work
1. **Build and test** before committing — the solution must compile and all tests must pass
2. **Always commit** — never ask whether to commit; just do it after verifying the build/tests pass
3. **Commit** all changes with a detailed commit message describing what was done and why
3. **Update the spec** — if the work relates to a feature or fix with a spec under `.kiro/specs/`:
   - Update **requirements.md**, **design.md**, and **tasks.md** to reflect any new or changed behavior
   - If no spec exists, create one with requirements.md, design.md, and tasks.md (tasks marked done)
   - Spec updates must be included in the same commit as the code changes, not in a separate commit
4. **Update the spec** — if the work relates to a feature or fix with a spec under `spec`:
   - Update any relevant spec files to reflect any new or changed behavior
   - Spec updates must be included in the same commit as the code changes, not in a separate commit
5. Do not leave uncommitted changes at the end of a task

## Commit Messages
- First line: concise summary of the change
- Body: list the specific changes grouped by area (UI, parser, tests, etc.)
- Reference the spec if one exists
- Include a `Prompt:` footer with the user's original request that triggered the change (quote it verbatim, trimmed to one or two sentences if long)

## Testing
- Use `getDiagnostics` to verify code compiles cleanly after changes
- Do NOT use `dotnet test` — it is incompatible with this project (see tech.md)
- Use `vstest.console` against the built test DLL when tests need to run from CLI
- When the running app locks the exe, use `getDiagnostics` instead of building
