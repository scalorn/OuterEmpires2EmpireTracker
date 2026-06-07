# Workflow Rules

## After Every Piece of Work
1. **Build and test** before committing — the solution must compile with **zero errors AND zero warnings** (including StyleCop SA* warnings) and all tests must pass. StyleCop warnings are not "just style" — they are enforced rules. Fix every SA* warning before committing.
2. **Run audit** — `node .kiro/tools/audit.js` must report **zero findings**. There is no such thing as "pre-existing" or "accepted baseline" — every finding is a real issue that must be fixed before committing. If audit exits with code 1, stop and fix every finding.
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
- Do NOT use `dotnet test` — it is incompatible with this project (see tech.md) — **EXCEPTION: OE2EmpireTracker.Server.Tests is a .NET 8 SDK-style project where `dotnet test` DOES work**
- Use `vstest.console` against the built test DLL when WinForms tests need to run from CLI
- When the running app locks the exe, use `getDiagnostics` instead of building
- **ALL tests must pass before committing. Zero exceptions.** If any test fails, you MUST investigate and fix it before proceeding. You may NOT dismiss failures as `pre-existing`, `environment issue`, or `not caused by my changes`. Tests do not randomly break — if they fail, something changed, and it is your responsibility to find out what. If the failure is genuinely unrelated to your work, fix it anyway or explain to the user exactly what broke and why, and get explicit approval before committing with failures.
- **Full test verification before commit requires ALL THREE suites:**
  1. `dotnet test OE2EmpireTracker.Server.Tests --no-build` (server integration + property tests) — timeout: 180000
  2. `npx vitest run` from OE2EmpireTracker.Web/ (TypeScript unit + property tests) — timeout: 60000
  3. `vstest.console` against OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll (WinForms NUnit tests) — **timeout: 960000 (16 minutes)** — the full suite has slow property tests that use real rate limiters
- **Do NOT rely on postTaskExecution hooks for WinForms test verification.** The hook timeout is too short for the 14-minute full suite. You MUST run vstest.console manually with a 960000ms timeout before committing. The post-task hooks only catch server test and build failures.
- **After running vstest.console, ALWAYS run `node .kiro/tools/trxparse.js`** to check results. Do not read raw console output — it gets truncated on long runs.
- **Frontend build verification: Use `npm run build` from OE2EmpireTracker.Web/, NOT `tsc --noEmit` alone.** The build script runs `generate-types` first (regenerates generated.ts from server schema), then `tsc --noEmit`, then `vite build`. Running `tsc --noEmit` alone skips type generation and may miss type conflicts with the generated file.
- **If a postTaskExecution hook fails (exit code 1), you MUST investigate before proceeding.** Run the command manually to see full output. Never dismiss hook failures.

## Error Recovery and Tooling
- **When you encounter a recurring error or friction**, stop and ask: can I write or improve a tool, script, or steering rule to prevent this from happening again? If yes, do it before retrying.
- **When a subagent is cancelled or fails mid-task**, check what partial work was completed (files written, tests missing, commits not done). Pick up from where it left off rather than re-delegating the entire task. If the service code exists but tests don't, write the tests directly. If code exists but wasn't committed, build/test/commit directly.
- **When delegating to subagents**, keep context files minimal. Large spec files (1000+ lines) should only be passed when the subagent genuinely needs them. Prefer passing only the files the task directly touches, plus the tasks.md. If a subagent keeps getting cancelled, reduce context and/or implement the task directly instead of re-delegating.
- **If the same type of failure happens twice**, create a tool or steering rule to prevent it. Don't just retry the same approach.
