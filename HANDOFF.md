# Session Handoff — 2026-10-25

**Purpose:** Preserve the full context of the 2026-10-25 session across conversation
compaction or a new session. Written because build verification is **parked** until
the owner finishes using the V1 app.

**Delete this file** once §2 (parked build verification) is complete and committed.
Everything in §3 lives permanently in `V2-TECHNICAL-SEED.md` and does not need to
be carried forward from here.

**Read `V2-TECHNICAL-SEED.md` first** if the topic is V2 architecture. This file is
operational state, not design.

---

## 1. Repository state

| | |
|---|---|
| Branch | `mainline` |
| Remotes | `origin` (GitHub), `backup` (owner's AWS EC2, 54.242.161.0), `niabus` (192.168.4.36 — **down**, being rebuilt as the Linux dev host) |
| Pushed through | `1845f14f` — origin and backup are current |
| Working tree | 6 modified files, intentionally uncommitted. See §2. |

### Commits made this session (all pushed)

```
1845f14f  Ignore server runtime secrets; untrack 8.9 MB of build and diagnostic detritus
3bd456dd  Add seed section 2.3: evidence for the Linux dev host constraint
7ff9da71  Fix oebackup.ps1: broken ssh path, false success reporting, unreachable-remote handling
f00b4c84  Add V2 technical seed: Rust core, React view layer, Electron/Capacitor shells
```

The session began as a design discussion about V2's technology stack. It expanded
into repairing the backup script (which turned out to be silently broken) and then
into repo hygiene. The design output is `V2-TECHNICAL-SEED.md`.

---

## 2. PARKED: build verification

### Why it is parked

`OE2EmpireTracker.exe` (PID 30068 at the time) and Visual Studio (PID 22520) hold
the output DLLs and PDBs. A full solution build fails with `MSB3021` / `MSB3026` /
`MSB3027` file-lock errors. The owner is actively using the app and expects to be a
few hours. **Do not attempt a full solution build until the app and Visual Studio
are closed.**

### What IS verified

`node .kiro/tools/server-build-check.js` exits **0** with no warnings. That builds
`OE2EmpireTracker.Server` and, transitively, `OE2EmpireTracker.Common`. So both C#
changes below **compile cleanly and produce zero warnings**.

### What is NOT verified

- Full solution build (WinForms project, `OE2EmpireTracker.Tests`, `Server.Tests`, `Desktop`)
- All three test suites. **No tests have been run this session.**
- Runtime behaviour of the two C# fixes, one of which changes failure-path behaviour.

### The 6 held files

```
 M .editorconfig                                     +21
 M .kiro/tools/audit.js                              +49 -16
 M OE2EmpireTracker.Common/Services/SafeFileWriter.cs    +38
 M OE2EmpireTracker.Common/Storage/SqliteBackend.cs      +26
 M OE2EmpireTracker.Web/src/api/types/generated.ts    +1 -1
 M OE2EmpireTracker/OE2EmpireTracker.csproj           +4 -1
```

**1. `.editorconfig`** — two new scoped suppression blocks.

- `[OE2EmpireTracker.Server/**/*.cs]` disables `SA1201`, `SA1202`, `SA1204`.
  This is the **fix for the `AD0001` analyzer crash**. StyleCop.Analyzers 1.1.118
  (pinned in `Directory.Build.props`) predates C# 9 and throws
  `KeyNotFoundException: The given key 'RecordDeclaration' was not present in the
  dictionary` on the Server project's `record` types. Verified: the 4 `AD0001`
  warnings are gone.
- `[OE2EmpireTracker.Common/Storage/SqliteBackend.cs]` disables `SA1204`.
  That 8868-line class groups its private static SQL logging helpers in a labelled
  region at the end. `SA1204` would require hoisting them above ~8600 lines of
  instance members. Consistent with the 20+ documented suppressions already present.

**2. `.kiro/tools/audit.js`** — exit-code propagation fix.

The aggregate printed `Total: 0 findings` and exited 0 while `server-build-check.js`
exited 1 with 4 real warnings. Cause: the `catch` block read only `err.stdout`, and
`server-build-check` reports entirely on **stderr**, so `output` was empty, `count`
became 0, and `totalFindings` never increased. Changes:

- merge `err.stderr` into the captured output
- **a non-zero exit always counts as at least 1 finding** (the actual bug)
- new `ERROR` status for a check that was killed or timed out
- per-tool timeout; `Server Build` raised to 300s because it runs a real build
- failure details always printed, no longer only under `--fix`

**3. `Common/Services/SafeFileWriter.cs`** — behaviour fix. **This is the change
most in need of a test run.**

The in-flight version was:

```csharp
try { File.Replace(tempPath, fullPath, backupPath); }
catch { File.WriteAllText(fullPath, content); }
```

Four problems: bare `catch` swallows everything silently; `File.WriteAllText`
truncates before writing, reintroducing the exact corruption window this class
exists to prevent; the temp file leaks on the fallback path; nothing is logged.

Replaced with a `ReplaceByMove` helper that catches only `IOException` and
`UnauthorizedAccessException`, logs a warning with the cause, deletes any stale
backup, then `File.Move(fullPath, backupPath)` followed by
`File.Move(tempPath, fullPath)`. Two moves preserve atomicity — the target is never
left truncated and a recoverable copy exists at every instant. Also added
`using System;` (needed for `Exception` / `UnauthorizedAccessException`).

The original intent was legitimate: `File.Replace` genuinely fails on cloud-synced
folders, when antivirus holds a handle, and on some network volumes. Only the
fallback mechanism was wrong. **If the owner knows which specific failure prompted
this, confirm the new fallback actually covers it.**

**4. `Common/Storage/SqliteBackend.cs`** — latent crash fix.

The pre-existing code, in **three identical copies** (`ExecuteNonQueryLogged`,
`ExecuteReaderLogged`, `ExecuteScalarLogged`):

```csharp
cmd.CommandText.Replace("\n", " ").Replace("\r", string.Empty)
   .Substring(0, Math.Min(cmd.CommandText.Length, 200))
```

Stripping `\r` shortens the string, but the `Substring` bound comes from the
**unflattened** `cmd.CommandText.Length`. Any statement containing `\r` and shorter
than 200 characters throws `ArgumentOutOfRangeException`. Example: `"SELECT 1\r\n"`
is length 10; flattened it is length 9; `Substring(0, 10)` throws.

The owner's in-flight edit worked around this by assigning the flattened text back
to `cmd.CommandText` before logging, which does fix the length mismatch but mutates
the command object as a side effect of logging — a latent bug if the command is
reused, and done *after* execution so it serves no purpose.

Replaced all three with a single `FormatSqlForLog(string commandText)` helper that
bounds truncation by the flattened length and does not touch `cmd`. Also removed a
double blank line at the `SQL Logging Helpers` banner (`SA1507`).

**5. `Web/src/api/types/generated.ts`** — owner's change, regenerated codegen
timestamp only. No semantic content.

**6. `OE2EmpireTracker/OE2EmpireTracker.csproj`** — owner's change, adds
`<SubType>UserControl</SubType>` to `Controls\FilteredTextComboSet.cs`. Legitimate
designer metadata.

### OPEN DECISION: 50 × SA1210 in `Server.Tests`

Fixing the `AD0001` crash unmasked **54 previously invisible warnings**. Two were in
`SqliteBackend.cs` and are fixed. The remaining **50 are `SA1210` (using directives
should be ordered alphabetically) in `OE2EmpireTracker.Server.Tests`** and are
**not fixed**. The full solution build will not be warning-free until they are.

Two options:

- **Fix them.** Reordering using directives has no semantic effect in C#, so this is
  zero-risk, just tedious. Recommended, given the warning count was never honest.
- **Suppress `SA1210` for `Server.Tests`.** Matches the existing precedent where
  `SA1201/1202/1204` are relaxed for `OE2EmpireTracker.Tests`.

### Verification procedure (run once the app and VS are closed)

```powershell
$env:PATH = "D:\Program Files\Git\cmd;$env:PATH"   # git is not on PATH by default

# 1. Full solution. Must be zero errors AND zero warnings.
& "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    OE2EmpireTracker.sln /p:Configuration=Debug /v:minimal

# 2. Server tests (this is the one project where dotnet test works)
dotnet test OE2EmpireTracker.Server.Tests --no-build      # timeout 180000

# 3. TypeScript tests
cd OE2EmpireTracker.Web; npx vitest run; cd ..             # timeout 60000

# 4. WinForms tests — needs a 10 minute timeout
& "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
    OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll /Logger:trx   # timeout 600000
node .kiro/tools/trxparse.js      # always parse the TRX, do not read console output

# 5. Audit gate
node .kiro/tools/audit.js

# 6. Commit — ALWAYS pass --files, see hazards below
node .kiro/tools/commit.js "summary" "body" --files "a,b,c"
.\oebackup.ps1
```

Suggested commit split for the held files: the two C# bug fixes together (they are
both latent-defect repairs in `Common`), and `.editorconfig` + `audit.js` together
(both are gate-correctness fixes). The `generated.ts` and `.csproj` changes are the
owner's and can ride along with either.

---

## 3. The design discussion — V2

**Full output: `V2-TECHNICAL-SEED.md`** (committed, 662 lines, 13 sections). It is
written as a handoff to an agent working on a Linux host and is authoritative for
stack and architecture. Do not re-litigate §3.8 (rejected alternatives) or §3.9
(grounding against real applications) without the owner.

### Decided stack, in one line each

| Layer | Choice |
|---|---|
| Engine | Rust, sans-IO pure domain core, compiles to native + WASM |
| Server | Rust + axum + sqlx |
| View | TypeScript + React + Vite + Tailwind |
| Desktop | Electron, MDI shell, engine via `napi-rs` native addon |
| Web | Vite SPA, engine as WASM |
| Mobile | Capacitor (Android, iOS), engine as WASM |

Two languages total: Rust and TypeScript.

### The reasoning that is easy to lose

These are the non-obvious conclusions. If the conversation compacts, these are the
parts worth keeping:

1. **Rust was chosen for AI-native development, not performance.** The compiler is
   the strictest automated reviewer of agent-generated code. This app's bottleneck
   was never GC — it was `DataGridView` and synchronous UI-thread work.
2. **Electron over Tauri**, reversing an earlier lean, purely on the owner's
   maturity-over-novelty requirement. The shell boundary is kept swappable.
3. **MDI is not native anywhere except Win32.** macOS never had it, Linux WMs do not
   provide it, Qt's `QMdiArea` is itself a reimplementation. So MDI is owned code in
   every possible stack, which removes “has native MDI” as a selection criterion and
   makes HTML/CSS a perfectly good substrate.
4. **Qt was the strongest runner-up and failed on licensing**, not capability: Qt for
   WebAssembly is GPLv3-or-commercial for open source, incompatible with the
   permissive licence the owner wants. C++ with agents was the secondary objection.
5. **The sans-IO core is mandatory, not stylistic.** `wasm32-unknown-unknown` has no
   filesystem, no sockets, and a `SystemTime` that misbehaves, so a core assuming
   ambient I/O cannot compile to the web and mobile targets.
6. **No floats in domain values.** The same engine runs as WASM in a browser and
   native on a server; float drift between them would destroy the determinism that
   makes “identical results everywhere” true.
7. **WASM-in-webview on mobile is the least-proven element.** Comparable apps (Anki,
   Bitwarden, Element) all use FFI there. `uniffi` is the recorded escape hatch, and
   it is additive because the engine has no UI coupling.
8. **Traceability and coverage are one mechanism, not two rules.** Regeneration
   consumes spec text *plus tests*, because no realistic spec encodes every
   tolerance and error variant. That makes the tests the oracle, which means
   uncovered code is literally unregenerable code — so 100% coverage is what makes
   regenerability provable rather than aspirational. It also means tests may never
   assert on implementation detail, or a correct regeneration would fail them.
9. **Rust branch coverage is not reliably measurable on stable** — `cargo-llvm-cov`
   needs nightly for it and is inconsistent on generics. The gate is therefore
   **100% region coverage** (sub-expression granular, stricter than line coverage)
   plus scheduled mutation testing, which together prove more than a branch
   percentage would. Do not “restore” a branch-coverage gate without rechecking
   whether the tooling has improved.

### Owner decisions captured this session (do not re-ask)

- **Licence:** permissive. MIT recommended, BSD or GPLv2 also acceptable. Rationale:
  negligible commercial-appropriation risk for a niche game tool.
- **DynamoDB:** must remain an option for the faction server's durable storage.
  Never fully implemented or tested in V1, so treat as a greenfield adapter.
- **JSON file persistence:** retired. Does not scale. SQLite is the local store;
  JSON survives only as V1 import, test fixtures, and user-facing export.
- **SQLite local-only** is a first-class supported mode, not a degraded fallback.
- **Test storage strategy:** SQLite in-memory plus DynamoDB Local.
- **Auth:** never implemented or tested in V1. Greenfield, highest-risk Phase 4 item.
- **V1 lifecycle:** stays maintained until switchover. Owner is the **sole active
  user**; a few people trialled V1, none active, no substantive feedback ever
  received. Therefore **no backwards-compatibility burden at all**.
- **MDI on desktop** is deliberate and heavily used. Keep it.
- **Visual consistency** is a nice-to-have, not a requirement.
- **Maturity over novelty** is a hard requirement.
- **Complete traceability with provable regenerability** (seed §6.2). Every
  requirement traces both ways; it must be possible to delete a module and
  reconstruct it from its REQ references and its tests.
- **100% coverage** of the core, server logic and storage adapters (seed §6.3).
  The owner's stated reasoning, which should be quoted in review: an uncoverable
  branch usually means the failure cannot be injected, which means there is no
  seam, which is a design defect. Fix the design, not the target.

### Still open (from `V2-TECHNICAL-SEED.md` §11)

1. **Spec process timing — this is the real blocker for V2 work.** The owner has a
   brownfield spec process under development in another project, with hard format
   requirements, schema validation, validated interlinking, and shardable ownership.
   V2 will be reshaped into it. Seed §9 deliberately designs no spec format and
   defines the seam. **Phases 0 and 1 are infrastructure and can start now; Phase 2
   onwards should wait for the spec process**, because that is where requirements
   begin driving code.
2. Hosting and deployment target for `oe2-server`.
3. Auth model specifics — local accounts, OAuth provider, or something else.
4. Game API integration scope and which phase it lands in.
5. iOS distribution — is there an Apple Developer account and a Mac or CI budget?
   If not, iOS slips out of Phase 6.
6. DynamoDB sub-question: alternative to PostgreSQL, or specifically for
   faction/shared data while player data stays relational?

---

## 4. Machine environment set up this session

**None of this is in source control.** It was all necessary because this is a fresh
desktop. Recorded here so it is not rediscovered.

- `D:\Program Files\Git\cmd` added to the **user** `PATH`. Git was not on `PATH` at
  all. Note that agent-spawned shells may still not pick it up — prepend it.
- `C:\Users\jasegler\.ssh` created (did not exist).
- `~/.ssh/config` written with `Host` entries for `github.com`, `54.242.161.0` and
  `192.168.4.36`, each with `IdentityFile ~/.ssh/id_scalorn` and
  `IdentitiesOnly yes`. **This is why no script needs `GIT_SSH_COMMAND` any more.**
- `~/.ssh/known_hosts` populated with:
  - GitHub's ed25519, ecdsa and rsa host keys, taken from GitHub's published
    fingerprint documentation and **verified** — all three SHA256 values matched.
  - The EC2 host's ed25519 and ecdsa keys, added after the owner confirmed
    `54.242.161.0` is their own personal AWS EC2 instance under sole control
    (it previously hosted early versions of the faction server).
    `SHA256:Xe+Ti+VzBPCCXSM1ykuFjgqutmv0Uqk3XrDENQSGVQI` (ED25519)
    `SHA256:rpPWIIlVulOQrD1OSaeVIl56KUXUP3H78RVh1Tvwr2w` (ECDSA)
- `~/.ssh/id_scalorn` was **restored by the owner** from the previous machine.
  ed25519, comment `scalorn@gmail.com`,
  `SHA256:VKwdvqRylROcHX5Sipng3zpE9YomJa75o2rBMQtDLLA`. Its owner is
  `BUILTIN\Administrators` from the cross-drive copy; Git's bundled MSYS ssh
  tolerates this, so **no ACL change is needed**. Do not “fix” it.
- The old desktop's drive is mounted at `E:`. `E:\Users\jasegler\.ssh` requires
  administrator rights to read — confirmed `UnauthorizedAccessException`. Do not
  waste a cycle searching it.

`.\oebackup.ps1` now works: pushes `origin` and `backup`, skips `niabus` after a
5s TCP probe, exits 0.

---

## 5. The recurring theme: three false-success gates

This session found three independent quality gates that reported success while
failing. It is worth stating together because it is the evidence base for
`V2-TECHNICAL-SEED.md` §2.3 and for §6's insistence that enforcement be mechanical
rather than a matter of discipline.

1. **`oebackup.ps1`** printed `Backup complete.` unconditionally while all three of
   its pushes failed — and had done so since it was written. Its
   `GIT_SSH_COMMAND` contained `C:\\\\users\\\\jasegler\\...`, over-escaped for a
   shell where `\` is not an escape character at all. Fixed in `7ff9da71`.
2. **`audit.js`** reported `Total: 0 findings` and exited 0 while a sub-check exited
   1 with real warnings. Fix held in §2.
3. **StyleCop `AD0001`** — a crashed analyzer silently stopped producing diagnostics,
   hiding **54 warnings** behind a “zero warnings” build. Partially fixed; 50
   `SA1210` remain, see §2.

The pattern: none of these failed loudly. All three required someone to go looking.

---

## 6. Hazards and gotchas for the next session

**Repo**

- **`commit.js` defaults to `git add -A`.** Always pass `--files`. Before
  `1845f14f`, `OE2EmpireTracker.Server/data/tokens.json` (auth tokens) was
  untracked-but-not-ignored, so a bare `commit.js` call would have published it.
  Now ignored, but the `git add -A` default remains a hazard for anything else.
- `oe2-galaxy-systems.json` (7 MB) is **real game data**, referenced by
  `SystemImporter.cs`, `SystemListViewModel.cs`, `DistanceCalculatorTests.cs` and
  `SystemGridIndexTests.cs`. Do not “clean it up”.
- Untracking in `1845f14f` did not rewrite history. `Snapshot` (5.5 MB) and
  `diag.txt` are still in the pack; recover with `git show 6ef2afbb:Snapshot`.

**Shell — Windows PowerShell 5.1, not 7**

- No `?.` null-conditional operator. It is a syntax error.
- `\` is **not** an escape character. Backtick is.
- `$ErrorActionPreference = 'Stop'` turns a native command's routine stderr into a
  thrown `NativeCommandError`. Git writes warnings to stderr constantly. Use
  `'Continue'` and read `$LASTEXITCODE`.
- Native stderr arrives wrapped in `ErrorRecord`; unwrap via `.Exception.Message`
  or it renders as `System.Management.Automation.RemoteException`.
- **Multi-line command blocks are silently swallowed** by the agent's shell wrapper
  — it echoes the first line and returns nothing. Happened twice. Use single-line
  commands joined with `;`.
- `Test-NetConnection` progress rendering swamps stdout into unreadable whitespace.
  Use `System.Net.Sockets.TcpClient` instead.
- `git` is not on `PATH` in fresh agent shells. Prepend
  `$env:PATH = "D:\Program Files\Git\cmd;$env:PATH"`.

**Tooling**

- Use the `fwrite` MCP tools for all file writes. The built-in write tools fail
  silently on larger content. Keep each call under ~2000 characters and chunk with
  `append_file`.
- `fwrite append_file` takes **`content`**, not `text`. `replace_in_file` requires
  **`path`** — easy to omit and it fails validation.
- `read_file` `offset` is **0-indexed**, so `offset: 1` skips line 1. This hid a
  `using System.IO;` directive and cost a cycle.
- The WinForms test suite needs a **600000 ms** timeout. Always parse results with
  `node .kiro/tools/trxparse.js` rather than reading console output.
- Frontend verification must use `npm run build`, not `tsc --noEmit` alone — the
  build regenerates `generated.ts` first.

---

## 7. Suggested next actions, in order

1. **When the app and VS are closed:** run §2's verification procedure. Decide the
   `SA1210` question. Commit the held files. Run `.\oebackup.ps1`.
2. **Delete this file** once step 1 is committed.
3. **Answer the spec process timing question** (§3, open item 1). It gates Phase 2
   of V2 and nothing else can be sequenced without it.
4. **Optionally start V2 Phase 0** (repository skeleton, Cargo + pnpm workspaces,
   pinned toolchain, `justfile`, Linux CI, `legacy/` populated). Phase 0 and Phase 1
   are pure infrastructure and do not depend on the spec process. See
   `V2-TECHNICAL-SEED.md` §10.
5. Consider whether `niabus` (192.168.4.36), currently being rebuilt, is intended to
   become the Linux dev host from `V2-TECHNICAL-SEED.md` C2. If so, V2 Phase 0 may
   be better started there than on this Windows desktop.

---

## 8. One thing deliberately not done

The seed document recommends retiring the WinForms project and the
.NET Framework 4.8.1 target, because they are the reason the V1 toolchain is
Windows-bound. **Nothing was retired this session.** V1 remains fully intact and
maintained, per the owner's decision in §3. The seed's §1.1 disposition table
(“Delete”, “Specification source”) describes the **V2 plan**, not actions taken.
