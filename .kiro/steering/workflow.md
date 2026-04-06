# Workflow Rules

## After Every Piece of Work
1. **Build and test** before committing — the solution must compile and all tests must pass
2. **Commit** all changes with a detailed commit message describing what was done and why
3. **Update or create a spec** under `.kiro/specs/` documenting the work if it relates to a feature or fix
   - If a spec already exists for the feature, update its tasks.md to reflect completed work
   - If no spec exists, create one with requirements.md, design.md, and tasks.md (tasks marked done)
4. Do not leave uncommitted changes at the end of a task

## Commit Messages
- First line: concise summary of the change
- Body: list the specific changes grouped by area (UI, parser, tests, etc.)
- Reference the spec if one exists

## Testing
- Use `getDiagnostics` to verify code compiles cleanly after changes
- Do NOT use `dotnet test` — it is incompatible with this project (see tech.md)
- Use `vstest.console` against the built test DLL when tests need to run from CLI
- When the running app locks the exe, use `getDiagnostics` instead of building
