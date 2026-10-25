# OE2 Empire Tracker V2 — Technical Seed

**Status:** Seed document. Authoritative for stack and architecture decisions. Not authoritative for spec format.
**Audience:** An AI agent working on a Linux host with full access to the V1 repository, its `spec/` tree, and its `.kiro/` steering and tooling.
**Date:** 2026-10-25

---

## 0. How to use this document

This is the starting point for a **complete rewrite** of OE2 Empire Tracker. V1 is not being migrated; it is being used as a specification source.

Read this document before writing any code. It tells you:

- What V1 is, and which parts of it are assets versus dead weight (§1)
- The owner's hard constraints (§2)
- The decided technology stack, which alternatives were rejected and why, and how the choices compare against real applications shipping to the same spread of platforms (§3)
- The target architecture and repository layout (§5, §6)
- Founding rules that must hold from the first commit (§7)
- Toolchain and CI expectations (§8)
- The spec process seam — **do not invent a spec format** (§9)
- The build-out sequence (§10)
- Open questions that need the owner's input before the relevant phase starts (§11)

- The target architecture and repository layout (§4, §5)

If you believe a decision in §3 is wrong, raise it with the owner before acting. Do not silently substitute a different technology. §3.8 records what was already considered and rejected, and §3.9 records how the surviving choices compare against Bitwarden, 1Password, Anki, Logseq, Obsidian, VS Code, and Element X — so that these decisions are not relitigated from scratch.

---

## 1. What V1 is, and what carries forward

### 1.1 Measured inventory

| Project | Stack | Scale | Disposition |
|---|---|---|---|
| `OE2EmpireTracker.Common` | C#, netstandard2.0 — Models, Services, ViewModels, Parsers, Constants, Storage, Client | 364 files, ~4.2 MB | **Specification source.** Port behaviour, not code. |
| `OE2EmpireTracker` | WinForms, .NET Framework 4.8.1 | 107 files, ~2.5 MB | **Delete.** Cannot build on Linux. Visual reference only. |
| `OE2EmpireTracker.Desktop` | Avalonia 12, net8.0, 27 views | 126 files | **Delete.** Layout reference only. |
| `OE2EmpireTracker.Server` | ASP.NET Core net8.0 | 59 files | **Specification source** for API surface. |
| `OE2EmpireTracker.Web` | React 19, Vite, Tailwind 4, TanStack Query, Zustand | 191 files | **Partial carry-forward.** Patterns and component structure are reusable. |
| `spec/` | Requirements, design, flows, mockups, decisions | large | **Primary asset.** Language-agnostic. |
| `docs/` | 30 user-facing help documents | 30 files | **Primary asset.** |
| `.kiro/steering/`, `.kiro/tools/` | Steering rules, 20 audit checks | | **Concepts carry forward**, implementations are rewritten. |

### 1.2 The real assets

In descending order of value:

1. **`spec/`** — requirements with stable `REQ-xxx` IDs, ASCII mockups, Mermaid flow diagrams, resolved design decisions. This is the accumulated understanding of the problem and it is not tied to any language.
2. **`OE2EmpireTracker.Common`** — executable documentation of the game's economic rules. When the spec is silent or ambiguous about a rule, the C# is the tiebreaker. Read it; do not transliterate it.
3. **`docs/`** — user-facing documentation that describes intended behaviour in plain language.
4. **`.kiro/steering/`** — hard-won operational lessons. §7 converts the important ones into V2 founding rules so they do not have to be rediscovered.
5. **`OE2EmpireTracker.Tests` / `.Server.Tests`** — the assertions encode edge cases that the spec does not mention. Mine them for property-test cases.

### 1.3 The dead weight, and why it must go

- **.NET Framework 4.8.1 + WinForms** — Windows-only, requires Visual Studio MSBuild, cannot build on a Linux host. This single dependency is why the V1 toolchain is Windows-bound.
- **`packages.config` + solution-level `packages/`** — obsolete NuGet model, part of the same lock-in.
- **PowerShell scripts** (`oebackup.ps1`, `oepublish.ps1`, `test-discovery.ps1`) and PowerShell-shaped tooling — replaced by a POSIX task runner.
- **Three divergent UI implementations** (WinForms, Avalonia, React) over one domain. V2 has one view layer with three shells.
- **Three storage backends in one library** (`Npgsql` 4.1.13, `AWSSDK.DynamoDBv2`, `Microsoft.Data.Sqlite` all referenced from `Common`). V2 separates client storage from server storage behind ports.
- **JSON file persistence** (`PlayerData.json`, `BaselineData.json`). Does not scale with data growth; retired in favour of SQLite. See §4.3 for the three roles JSON retains.

---

## 2. Hard constraints

These come from the project owner and are not open for negotiation by the implementing agent.

| # | Constraint | Implication |
|---|---|---|
| C1 | **Maturity over novelty.** Prefer boring, actively maintained, widely deployed technology. Do not build on anything whose primary selling point is promise. | Disqualifies the Rust GUI ecosystem, Dioxus/Blitz, Deno Desktop, Electrobun, and any pre-1.0 framework in a load-bearing position. |
| C2 | **Linux is the development host.** No build step may require Windows or macOS except platform-native packaging and signing. | All day-to-day work — build, test, lint, run, debug — must work on Linux. |
| C3 | **Targets:** Windows, macOS, Linux desktop; browser; Android; iOS. | Six deployment targets, three shells. |
| C4 | **MDI on desktop.** Multiple overlapping, movable, resizable child windows inside one parent frame. This is a deliberate choice, not an oversight. | The desktop shell owns a real window manager. See §4.4. |
| C5 | **Adaptive shells are acceptable.** Mobile and web may be single-view-at-a-time. | No requirement for one UI framework across all targets. |
| C6 | **Visual consistency is desirable, not required.** | Shared design tokens are sufficient; a shared renderer is not needed. |
| C7 | **Prefer non-garbage-collected native code for the engine.** | Domain logic, rules, calculation, and optimisation are compiled native code. The view layer is exempt. |
| C8 | **Open source, GitHub, asynchronous multi-developer collaboration.** | Module boundaries must allow independent work without collision. Cross-language contracts must be generated, not hand-maintained. |
| C9 | **AI-native development process.** | The toolchain must make agent mistakes loud and early. Compile-time enforcement is preferred over review discipline. |

### 2.1 Reading C7 correctly

The performance argument for native code is weaker than it looks for this application. V1's responsiveness problems came from `DataGridView` and synchronous work on the UI thread, not from garbage collection. The places where native code genuinely pays are the build-order optimiser, supply-chain solving, timer simulation across thousands of structures, and parsing — all of which are **engine**, not view.

So C7 is satisfied by making the engine native and leaving the view layer in a managed language. That is the correct allocation: non-GC where throughput and determinism matter, managed where developer velocity and ecosystem matter.

The stronger argument for a strict native language here is **C9, not performance**. See §3.2.

### 2.2 What being the sole user permits

The owner is currently the only active user of V1. A handful of people trialled it; none are known to be using it; no substantive feedback has ever been received. V1 will stay maintained until V2 can take over.

This removes an entire class of constraint, and the agent should take full advantage of it:

- **No backwards compatibility burden.** V2 defines its own schema freely. No data-format versioning, no dual-run support, no migration tooling beyond the one-way V1 import in §4.3 that the owner runs once at switchover.
- **No runtime interoperability with V1.** The two never need to talk to each other or share a store.
- **No user base to regress.** Breaking changes during Phases 0–6 cost nothing. Prefer getting the design right over preserving an interim decision.
- **No deprecation cycles.** Delete and replace rather than layering compatibility shims.

It also creates one obligation:

- **The owner is the only domain expert and the only user.** Do not infer intent from “what users would expect” or invent requirements to fill gaps. The authorities are `legacy/spec/`, `legacy/csharp/`, and the owner. When those three are silent or contradictory, ask — and log it in `spec/INBOX.md` (§9.2).

Because V1 continues to evolve, `legacy/` needs a defined refresh procedure rather than a one-time copy:

- `just refresh-legacy` re-copies `spec/`, `docs/`, and the C# sources from the V1 working tree
- it records the source V1 commit SHA in `legacy/PROVENANCE.md`
- the resulting diff is reviewed, because a change in V1 may represent a rule the V2 port has not yet accounted for

### 2.3 Why C2 is a hard constraint and not a preference

An agent reading this later may wonder whether “Linux is the development host” is dogma. It is not. It is a conclusion reached from sustained evidence, and the session that produced this document is a fair sample.

While writing one Markdown file and repairing one four-line shell script, the following were all hit. **None relates to the problem being solved:**

- `git` was not on `PATH` at all, so every git invocation needed an absolute path to the executable.
- PowerShell does not treat `\` as an escape character. This is what had silently broken `oebackup.ps1`'s ssh identity path, for an unknown length of time.
- Windows PowerShell 5.1 does not support the `?.` null-conditional operator, so the first script revision was a syntax error.
- `$ErrorActionPreference = 'Stop'` converts a native command's routine stderr output into a thrown exception, so ordinary git warnings aborted the script.
- `Test-NetConnection` progress rendering swamped stdout and made the result unreadable, twice.
- Multi-line commands were silently swallowed by the shell wrapper, returning nothing at all, twice.
- Reliable file writing required standing up a dedicated MCP server, because the built-in write tooling fails silently on larger content.

Seven distinct failures, none of them domain problems. Individually each is trivial. Collectively they tax every operation, and under an agent-driven workflow (C9) that tax is paid on every tool call rather than once per developer. The cost is not any single failure; it is that they never stop.

A `just` recipe on Linux produces none of them: one shell with predictable quoting, tools on `PATH`, exit codes that mean what they say, and no escaping translation layer between the agent and the command.

**The corollary matters more than the inconvenience.** V1's `oebackup.ps1` printed `Backup complete.` unconditionally while all three of its pushes failed, and had done so since the script was written. Windows-shell friction does not merely slow work down — it hides failures behind false success. Founding rule 10 exists for this reason, and so does the insistence throughout §6 that enforcement be mechanical rather than a matter of discipline.

---

## 3. The decided stack

### 3.1 Summary

| Layer | Technology | Why |
|---|---|---|
| Domain engine | **Rust** (2024 edition, stable toolchain) | §3.2 |
| Server | **Rust + axum + sqlx + PostgreSQL** | §3.3 |
| View layer | **TypeScript + React + Vite + Tailwind** | §3.4 |
| Desktop shell | **Electron + electron-builder** | §3.5 |
| Web shell | **Vite SPA**, engine as WebAssembly | §3.6 |
| Mobile shell | **Capacitor** (Android, iOS), engine as WebAssembly | §3.7 |
| Client storage | **SQLite** (desktop/mobile), **OPFS/IndexedDB** (browser) | §4.3 |
| Task runner | **`just`** | §8 |

Two languages total: Rust and TypeScript. Both are mature, both have deep AI training corpora, both are first-class on Linux.

### 3.2 Rust for the engine

**Maturity (C1):** Stable since 2015, on a six-week release train, used in the Linux kernel, Windows, Android, and across AWS infrastructure. The specific crates in the plan are all long-established: `serde`, `thiserror`, `tokio`, `axum`, `sqlx`, `rusqlite`, `proptest`, `rust_decimal`, `wasm-bindgen`.

**Targeting (C3):** Rust is the only non-GC language whose toolchain treats WebAssembly as a first-class target rather than an afterthought, while also covering Windows, macOS, Linux, Android (NDK), and iOS. `cargo` plus `cross` handles the matrix with materially less friction than CMake plus a C++ package manager.

**AI-native (C9) — the decisive reason.** For an agent-driven rewrite of a rules-heavy domain, the compiler is the most valuable reviewer available. Exhaustive `match`, no null, ownership rules, and newtype-enforced invariants mean agent-generated code either compiles or fails loudly. In C# or TypeScript, an agent produces plausible code that compiles and is quietly wrong — which is the exact failure mode documented repeatedly in V1's `.kiro/steering/common-mistakes.md`. Shifting those errors from runtime to compile time is worth more to this project than the performance gain.

### 3.3 Rust + axum for the server

Same engine crate, no reimplementation of rules on the server. `axum` is maintained under the `tokio` organisation and is the mainstream choice; `sqlx` gives compile-time-checked SQL, which extends the C9 benefit into the data layer.

Client storage is **SQLite**. Server storage is **PostgreSQL** by default, with **DynamoDB retained as a supported option** for the faction server's durable storage — the owner has confirmed this must remain available. V1 referenced the DynamoDB SDK but never fully implemented or tested it, so treat it as a greenfield adapter, not a port.

This is the concrete reason server storage sits behind the `Storage` port (§4.3) rather than being assumed to be Postgres: `oe2-storage-postgres` and `oe2-storage-dynamodb` are peer adapters, selected by configuration. Neither may leak its types into `oe2-core` or `oe2-server`'s handlers. Build Postgres first; DynamoDB becomes the proof that the port abstraction actually holds.

**Security note:** the server is a network-exposed service. Authentication and authorisation must be designed before the first endpoint ships, not added later. V1 has `CredentialStore`, faction push, and sharing features, so there is an existing model to draw on. Do not stand up an unauthenticated API “temporarily”.

### 3.4 TypeScript + React for the view layer

This application is grids, forms, tables, and charts. HTML and CSS have no rival for that category, and React's ecosystem for it is the most mature available in any language.

- **Data grid:** TanStack Table + TanStack Virtual (headless, already in use in V1's web project). AG Grid Community is the fallback if built-in grouping and pivoting turn out to be needed.
- **Server state:** TanStack Query (carried from V1).
- **Client state:** Zustand (carried from V1).
- **Styling:** Tailwind, driven by a generated design-token file.
- **Charts:** Apache ECharts — handles large series well and is the replacement for V1's ScottPlot usage.

The view layer also gives real HTML accessibility, which no canvas-rendering toolkit matches.

### 3.5 Electron for the desktop shell

**This pick is driven by C1 and reverses an earlier lean toward Tauri.** Electron has been in production since 2013 and ships VS Code, Slack, Discord, and Obsidian. It bundles its own Chromium, so rendering is identical across Windows, macOS, and Linux rather than varying with the host webview. As of 2026 it remains [the default for desktop apps built on a web stack](https://www.forasoft.com/blog/article/electron-desktop-app-development-guide-for-business), with alternatives winning on footprint and losing on ecosystem depth and runtime consistency.

The costs are a roughly 150 MB install and higher memory use. For a desktop planning tool this is an acceptable trade, and it buys both maturity and consistency.

The engine is loaded into Electron's main process as a **native Node addon via `napi-rs`**, which is mature (it powers Rspack among others) and distributes prebuilt binaries per OS, CPU, and libc so contributors do not need a Rust toolchain to run the app. The WebAssembly build remains available as a fallback path.

**Design the shell boundary so Electron is swappable.** If its footprint later becomes unacceptable, Tauri should be a shell replacement, not a rewrite. Keep all Electron-specific code inside `shells/desktop`.

### 3.6 Vite SPA for the web shell

The engine compiles to `wasm32-unknown-unknown` via `wasm-bindgen` and runs in the browser, so planning calculations execute client-side with **bit-identical results to the desktop app**. This is the consistency guarantee that actually matters for a planning tool, and no API-based split can provide it.

Persistence goes through the storage port: OPFS where available, IndexedDB otherwise, with the server as the source of truth.

### 3.7 Capacitor for the mobile shells

Capacitor is maintained by the Ionic team, is at version 8.x, and is widely deployed in production. It [generates standalone `android/` and `ios/` projects that are committed to source control](https://ionicframework.com/docs/v8/react/your-first-app/deploying-mobile) and edited with native tooling when needed, which keeps the native layer inspectable rather than hidden behind a black box.

The same React components run in the mobile webview with a stack-navigation shell instead of the MDI shell. The engine runs as the same WebAssembly build used by the web shell, which keeps the mobile build matrix small.

Android development works entirely on Linux. **iOS build and signing require macOS** — this is true of every stack, including Flutter and Qt, and is handled by a `macos-latest` CI runner.

### 3.8 Rejected alternatives

Do not relitigate these without owner input.

| Option | Why rejected |
|---|---|
| **Rust GUI** (egui, Slint, Iced, Dioxus native) | Fails C1. The ecosystem is [still widely assessed as immature and fragmented in 2026](https://daily.dev/posts/a-2026-survey-of-rust-gui-libraries-e93cfc0b7). Dockable windows, 10k-row virtualised grids, charts, and accessibility are exactly where it is weakest. |
| **C++ / Qt 6** | Strongest runner-up. Real `QMdiArea` MDI, excellent model/view for large datasets, all six targets, Linux-native tooling. Rejected on three counts: [Qt for WebAssembly is GPLv3-or-commercial for open source, not LGPLv3](https://wiki.qt.io/Qt_for_WebAssembly), which would force GPLv3 on the whole project; Qt WASM supports a reduced module set and has had persistent mobile input problems; and C++ with AI agents converts agent errors into undefined behaviour instead of compile errors, forfeiting the main benefit sought under C9. |
| **C# / Avalonia** | Technically viable and would preserve V1's domain code, but the owner has chosen to rewrite, and it fails C7. Avalonia also has no MDI primitive. |
| **Flutter (+ Rust core)** | Genuinely good, and the best option if pixel-identical rendering were a hard requirement. Rejected because C6 downgrades that to a nice-to-have, while Flutter costs a third language, a weaker data-grid and accessibility story, and a heavier web target. |
| **.NET MAUI** | No Linux desktop support, poor tooling on a Linux host, and it deliberately uses per-platform native controls. Fails C2 and C3. |
| **Tauri 2 for desktop** | Preferred on footprint and Rust integration, but newer than Electron and its mobile support is the newest part of it. Fails C1 *relative to Electron*. Revisit later as a shell swap. |
| **Deno Desktop, Electrobun** | Too new for a load-bearing position. Fails C1. |
| ~~**DynamoDB**~~ | **Not rejected.** Initially proposed for removal; the owner has confirmed it must remain an option for the faction server's durable storage. See §3.3. |

### 3.9 Grounding: what comparable applications chose

The stack in §3.1 was sanity-checked against real applications that ship to a similar spread of platforms. The short version: **the Rust-core-plus-thin-clients pattern is the dominant architecture among applications that span this many targets, and it was arrived at independently by several teams in different domains.** This plan is not a novel bet.

| Application | Shared core | Desktop | Web | Mobile |
|---|---|---|---|---|
| **Bitwarden** | Rust SDK with generated Kotlin, Swift, and TypeScript bindings | Electron + a Rust native module | Web vault, shared codebase | Native Kotlin and Swift over the same SDK |
| **1Password 8** | Rust core | Electron | core compiled to WASM | thin clients over the Rust core |
| **Anki** | Rust (`rslib`) | Qt + TypeScript/HTML hybrid | AnkiWeb | AnkiDroid via JNI; iOS via C FFI |
| **Logseq** | ClojureScript | Electron | web app | Capacitor, with `android/` and `ios/` committed |
| **Obsidian** | TypeScript | Electron | none official | Capacitor |
| **VS Code** | TypeScript, layered by target runtime | Electron | vscode.dev, github.dev, code-server | none |
| **Element X** | `matrix-rust-sdk` | Element Web in Electron | Element Web | native SwiftUI and Jetpack Compose |

#### What this confirms

**The shared native core is the consensus answer.** Bitwarden, 1Password, Anki, and Element all converged on a Rust core with thin per-platform clients, in four different problem domains, without copying each other. [1Password's stated motivation](https://www.1password.community/1password-at-home-31/electron-version-question-17423) is directly relevant: before the Rust core they had separate development teams maintaining a separate app per platform. That is precisely the trap V1 fell into with three divergent UIs over one domain.

**Electron is what serious cross-platform desktop apps actually ship.** Bitwarden, 1Password, Obsidian, Logseq, and VS Code. Bitwarden's desktop client additionally [relies on a Rust native module compiled into the Electron build](https://contributing.bitwarden.com/getting-started/clients/desktop/), which is exactly the `napi-rs` arrangement in §3.5.

**The shells pattern is validated at the largest scale available.** VS Code organises code [by target runtime environment inside each layer, so only runtime-appropriate APIs are used](https://github.com/microsoft/vscode/wiki/source-code-organization), and ships that one codebase as a desktop app, vscode.dev, github.dev, and code-server. This is the same discipline as §5's `shells/` boundary and founding rule 7.

**Nobody shares the UI across desktop and mobile. Everyone shares the core.** Logseq's desktop uses a single main view while [its mobile app maintains independent navigation stacks per tab](https://deepwiki.com/logseq/logseq/8.1-electron-integration). Flutter's own guidance says [it is not always ideal for a multiplatform app to offer identical functionality everywhere](https://docs.flutter.dev/ui/adaptive-responsive/best-practices). Your MDI-on-desktop, single-view-on-mobile instinct is the normal pattern, not an eccentricity — C5 is how these applications are actually built.

#### Where comparable apps chose differently, and why

**Element X chose native mobile UI (SwiftUI, Jetpack Compose) over a webview.** Different constraint, not a better answer: Element is a real-time messenger where mobile *is* the product, so native gesture handling, notifications, and battery behaviour justify two extra UI codebases. Obsidian and Logseq, where mobile is a companion to a desktop-first tool, both chose Capacitor. **OE2 is desktop-first with mobile as a companion view, which puts it in the Obsidian/Logseq category.** This is the discriminator, and it supports §3.7.

**Anki's desktop UI is Qt, which is the stack §3.8 rejected.** Worth looking at closely, because Anki is the closest domain analogue — a data-dense study tool with a Rust core. The instructive detail is that Anki's GUI is [a mix of Qt and TypeScript/HTML/CSS, with the majority of new UI code going into the TypeScript tree](https://github.com/ankitects/anki/blob/main/docs/architecture.md). Even the Qt-based application reaches for HTML for its dense content, and is migrating in that direction. That is evidence against going full-Qt, from the project best positioned to have made it work.

**Bitwarden, Anki, and Element use FFI bindings to reach mobile; this plan uses WASM in a webview.** Theirs is the higher-performance path. Ours is simpler and keeps the build matrix small, which is the right default for a companion view. See §3.9.1 for the escape hatch.

**Flutter's self-rendered UI is a double-edged trade.** It is a strength for product consistency but [a weakness when users expect platform-specific behaviour](https://attractgroup.com/blog/flutter-desktop-app-development/). Since C6 makes visual consistency a nice-to-have, this cuts against Flutter rather than for it, and confirms the §3.8 rejection.

**No application in this set does MDI.** That is not evidence against it — it reflects that MDI fell out of fashion, which you already know. It does mean there is no reference implementation to borrow, and `@oe2/mdi` is genuinely owned code. Web-based docking and tiling is well-trodden ground (VS Code's own editor groups, `dockview`, `golden-layout`), so the primitives exist even though the MDI policy does not.

#### 3.9.1 The one refinement this review produces

The only element of §3.1 that is *less* common than the alternatives is running the engine as **WASM inside a webview on mobile**. 1Password proves the WASM path works — [they ported their Rust core to WASM for the web client](https://syntax.fm/show/776/how-1password-uses-wasm-and-rust-for-local-first-dev-with-andrew-burkhart/transcript) — but for *mobile* the comparable apps all use FFI instead: Anki via JNI and C FFI, Bitwarden and Element via generated Kotlin and Swift bindings.

Keep WASM-in-webview as the Phase 6 default, because it keeps the build matrix small and mobile is a companion view. But record the escape hatch explicitly:

- If mobile performance or memory becomes a problem, add **`uniffi`** (Mozilla's binding generator, used by both Bitwarden and Element) to generate Kotlin and Swift bindings over `oe2-core`, and call it through a Capacitor native plugin.
- Because the engine is already a standalone Rust crate with no UI coupling, this is an additive change, not a rewrite.

**This is the strategic payoff of the Rust core that is worth stating plainly:** it keeps the native-mobile door open. If OE2's mobile app ever becomes a primary experience rather than a companion, you can add SwiftUI and Jetpack Compose shells over the same engine — exactly the Element X architecture — without touching a line of domain logic. No other choice in §3.8 preserves that option.

#### 3.9.2 One warning from the evidence

1Password's move to Electron [drew sustained criticism](https://www.macworld.com/article/677513/new-version-of-1password-for-mac-is-flooded-with-criticism.html), and the substance of the complaint is worth internalising: not that Electron is slow, but that web-first designers produce desktop UIs that behave like web pages. Users notice missing keyboard conventions, wrong focus behaviour, and absent native affordances.

This is a UX discipline problem rather than a framework problem, and it is the recurring complaint about every application in the §3.9 table that chose Electron. It is not self-correcting. **§4.5 makes it a contract with named requirements and two mechanical checks**, because “be tasteful about desktop conventions” is not an instruction an agent can act on.

---

## 4. Architecture

### 4.1 Layers

```
            ┌─────────────────┐  ┌───────────────┐  ┌───────────────┐
  SHELLS   │ desktop (Electron)│  │ web (Vite SPA)│  │ mobile (Cap.) │
            │   MDI window mgr │  │  routed pages │  │  stack nav    │
            └────────┬────────┘  └───────┬───────┘  └───────┬───────┘
                     └──────────────────┼────────────────┘
            ┌─────────────────────┴────────────────────┐
  VIEW      │ @oe2/ui   presentational components     │
            │ @oe2/hooks  headless state + behaviour  │
            │ @oe2/tokens  design tokens (generated)  │
            │ @oe2/contract  API + engine types (gen) │
            └─────────────────────┬────────────────────┘
            ┌─────────────────────┴────────────────────┐
  ENGINE    │ oe2-core    pure domain, sans-IO        │
  (Rust)    │ oe2-storage-*   storage adapters        │
            │ oe2-wasm / oe2-node   bindings          │
            └─────────────────────┬────────────────────┘
            ┌─────────────────────┴────────────────────┐
  SERVER    │ oe2-server  axum + sqlx + PostgreSQL    │
            └────────────────────────────────────────┘
```

### 4.2 The engine is sans-IO

`oe2-core` performs **no I/O and has no ambient dependencies**. It is a pure state machine over domain data: given current state and a command, it produces new state plus a list of effects. Everything environmental is injected as a trait:

- `Clock` — current time. **Never call `SystemTime::now()` or `Instant::now()` in core.**
- `IdGenerator` — UUID creation.
- `Storage` — load and persist aggregates.
- `Random` — any stochastic behaviour.

This is not architectural purity for its own sake. Three concrete reasons:

1. **`wasm32-unknown-unknown` forces it.** There is no filesystem, no sockets, no threads by default, and `SystemTime` misbehaves. A core that assumes ambient I/O will not compile to the web and mobile targets.
2. **Determinism.** The same engine must produce identical results in the browser, on the desktop, and on the server. Injected time and IDs are the only way to guarantee that.
3. **Testability.** A pure core is exhaustively property-testable with no mocking. V1's `.kiro/steering` records repeated pain from non-deterministic time in tests — V1 eventually introduced a `SystemClock` abstraction and had to retrofit it everywhere. V2 starts with it.

I/O lives in the storage adapter crates and in the shells.

### 4.3 Storage ports

| Crate / module | Backend | Used by |
|---|---|---|
| `oe2-storage-sqlite` | SQLite via `rusqlite` | desktop (native addon) |
| `oe2-storage-web` | OPFS, IndexedDB fallback | web, mobile (through the shell) |
| `oe2-storage-postgres` | PostgreSQL via `sqlx` | server (default) |
| `oe2-storage-dynamodb` | DynamoDB via `aws-sdk-dynamodb` | server (faction durable storage option) |

All implement the same `Storage` trait defined in `oe2-core`. No crate depends on more than one backend. This is the fix for V1's situation where `Common` referenced Postgres, DynamoDB, and SQLite simultaneously — which would have shipped a Postgres driver inside an iOS app.

#### JSON file persistence is retired

V1 persists to `PlayerData.json` and `BaselineData.json`. **This is dropped as the persistence format.** The owner's assessment is that it does not scale as data grows, and the mechanics confirm it: whole-file read and write, no indexes, no partial updates, no transactions, and a rewrite cost proportional to total data size regardless of how small the change was.

JSON survives in three narrower roles, and only these:

1. **One-way import** of V1 data, run once by the owner at switchover.
2. **Test fixtures** — V1's real `PlayerData.json` files are valuable as Phase 2 cross-check inputs.
3. **User-facing export and share** — a serialisation format, never the store.

#### Local-only operation is a supported mode, not a degraded fallback

The desktop application must be fully functional with SQLite alone and no server reachable, ever. The server is opt-in, for faction sharing and multi-device sync. Treat “no account, no network, no server” as a first-class configuration that CI exercises — not an offline mode that merely degrades gracefully.

#### Storage backends and the test strategy

The storage choices double as the test strategy, which is a deliberate benefit:

- **SQLite in-memory** — fast adapter and integration tests, no fixtures on disk, no cleanup. This is the default for the bulk of storage tests.
- **DynamoDB Local** — run as a container for `oe2-storage-dynamodb` integration tests. The repository already contains a `dynamodb-local-metadata.json`, so this path has been explored in V1.
- **PostgreSQL** — a container in CI for `oe2-storage-postgres`.

All three run on Linux and all three run in the Linux CI job, which keeps C2 intact. Because `oe2-core` is sans-IO (§4.2), the overwhelming majority of domain tests need none of them.

### 4.4 The MDI shell

MDI is a deliberate requirement (C4) and the owner's primary interaction model. One fact shapes the implementation: **MDI is not a native pattern on any platform except Win32.** macOS has never had it, Linux window managers do not provide it, and Qt's `QMdiArea` is itself a reimplementation inside a widget. So MDI is code you own regardless of stack.

Build it as an owned package, `@oe2/mdi`, rendered inside Electron's single window:

- Absolutely positioned child windows with managed z-order
- Drag, resize, minimise, maximise, restore, close
- Cascade, tile horizontally, tile vertically
- A window menu listing open children
- Per-window state in a store, with layout persisted and restored across sessions
- Keyboard navigation between children, and focus trapping within the active child

Use `react-rnd` for the drag and resize primitive and own the window chrome, lifecycle, and layout algorithms yourself. Do not take a dependency on a whole-MDI library for something this central — the primitive is small and stable, the policy is yours.

Because the chrome is CSS, matching V1's WinForms appearance is a styling exercise. Capture it in `@oe2/tokens` and reference V1's screenshots and `spec/mockups/` as the visual target.

**Alternative considered:** Electron can open real OS child windows parented to the main window, which on Windows looks very close to native MDI. Rejected as the default because behaviour diverges substantially on macOS and across Linux window managers. In-renderer MDI is consistent and fully controllable. Revisit only if the owner asks.

The web and mobile shells do not use `@oe2/mdi`. They compose the same views from `@oe2/ui` into routed pages (web) and a navigation stack (mobile), per C5.

### 4.5 The desktop input and density contract

The most common failure mode of web-stack desktop applications is that they feel like a phone app stretched to fill a monitor. The owner's analogy is exact: **a console game poorly ported to desktop.** You can always tell. The port carries over the console's input assumptions — a dozen buttons and a thumbstick — onto a machine with 102 keys, a scroll wheel, three mouse buttons, and a 27-inch screen. Nothing is technically broken. It just feels wrong, and it is slower to use.

This is the single most likely way V2's desktop shell disappoints. MDI alone will not prevent it. What follows is a contract, not an aspiration.

#### Input: the keyboard is a primary input device, not an accessibility afterthought

- Every action reachable by mouse is reachable by keyboard. No mouse-only interactions.
- A real menu bar with mnemonics (`Alt+F`) and the accelerator displayed on every item.
- Function keys carry conventional meanings: `F1` help, `F2` rename or edit, `F3` find next, `F5` refresh.
- `Esc` cancels and `Enter` commits, in every dialog and every inline editor.
- Explicit, sensible tab order. Focus is always visible.
- Grids support type-ahead, `Home`/`End`/`PgUp`/`PgDn`, `Ctrl+A`, shift-click for ranges, ctrl-click for disjoint selection, and in-place edit on `Enter` or `F2`.
- MDI children cycle with `Ctrl+Tab` and `Ctrl+Shift+Tab`; `Ctrl+W` closes the active child.
- Right-click context menus wherever the action set is non-trivial.
- One discoverable shortcut map. A command palette is a welcome addition and never a substitute for menus.

#### Density: the target is a mouse pointer, not a fingertip

- Hit targets sized for a pointer. Touch-sized controls on desktop are the single clearest tell of a phone port.
- Grid rows at desktop density. A large monitor should show hundreds of rows, not twenty.
- No scroll-jacking. No infinite scroll where a scrollbar or paging is correct. No hover-only affordances that hide information from keyboard users.
- Multi-monitor aware: MDI layout persists across sessions, and genuine OS windows are available for the few views that need to escape the MDI frame.
- Mobile and desktop share hooks, never component density. `@oe2/ui` components take a density prop; the shell sets it.

#### Enforcement

Most of this is reviewable rather than compilable, but two checks are mechanical and must exist by the end of Phase 3:

1. Every command in the command registry has a keyboard binding, or an explicit opt-out carrying a reason.
2. No click handler on a non-focusable element in `packages/ui` (lint rule — it also catches real accessibility bugs).

---

## 5. Repository layout

A single repository, a Cargo workspace plus a pnpm workspace. Boundaries are chosen so that work can be sharded across developers with minimal collision (C8).

```
oe2/
  Cargo.toml                  # Rust workspace root
  pnpm-workspace.yaml         # JS workspace root
  justfile                    # all tasks; no PowerShell anywhere
  rust-toolchain.toml         # pinned toolchain
  .github/workflows/          # CI, see §8

  crates/
    oe2-core/                 # pure domain. no I/O. the crown jewel.
      src/
        model/                # entities, value objects, newtypes
        rules/                # game rules, one module per domain
        calc/                 # calculators (build time, status, yields)
        planning/             # optimiser, supply chain solving
        parse/                # HTML and import parsers
        ports/                # Clock, IdGenerator, Storage, Random
        command/              # commands + effects (the only mutation path)
      tests/                  # property tests via proptest
    oe2-storage-sqlite/
    oe2-storage-postgres/
    oe2-storage-dynamodb/
    oe2-storage-web/
    oe2-sync/                 # offline queue, reconciliation
    oe2-node/                 # napi-rs bindings  -> desktop
    oe2-wasm/                 # wasm-bindgen bindings -> web, mobile
    oe2-server/               # axum service
    oe2-contract/             # the single source of truth for shared types
    xtask/                    # codegen, schema export, audit drivers

  packages/
    tokens/                   # design tokens -> CSS vars + tailwind preset
    contract/                 # GENERATED TypeScript from oe2-contract
    ui/                       # presentational components
    hooks/                    # headless state and behaviour
    mdi/                      # the MDI window manager

  shells/
    desktop/                  # Electron main + preload + renderer entry
    web/                      # Vite SPA entry
    mobile/                   # Capacitor entry; android/ and ios/ committed

  legacy/                     # V1, read-only reference
    spec/                     # copied from V1 spec/
    docs/                     # copied from V1 docs/
    csharp/                   # Common + Server, for rule archaeology
    screenshots/

  spec/                       # V2 spec. OWNED BY THE INCOMING PROCESS. See §9.
  tools/                      # audit and validation tooling
```

### 5.1 Notes on the layout

- **`oe2-contract` is the only cross-language boundary.** It defines the API DTOs and the engine's public command and query types. `packages/contract` is generated from it by `xtask` and is never hand-edited. This lets a frontend developer and a backend developer work in parallel against a shared, machine-checked contract — one of the specific gaps the owner identified in V1 (C8).
- **`legacy/` is read-only.** Never modify it. It exists so an agent can answer “what did V1 actually do here?” without guessing. Delete it only when the owner says V2 has reached parity.
- **`packages/ui` is presentational only.** No data fetching, no business rules. `packages/hooks` holds behaviour so the three shells compose the same logic with different layouts.
- **`xtask`** replaces ad-hoc scripts. Codegen, schema export, and audit entry points are Rust, cross-platform, and testable.

---

## 6. Founding rules

The table below carries forward V1's hard-won lessons; §6.1 to §6.3 add rules that
are specific to V2. **Every rule here must be enforced mechanically, not by
discipline.** V1 demonstrated repeatedly that a rule depending on someone
remembering it is a rule that is already broken.

V1's `.kiro/steering/` is a record of problems discovered the hard way. These are the ones worth carrying, restated for V2.

| V1 lesson | V2 rule | Enforcement |
|---|---|---|
| Non-deterministic time broke tests repeatedly; `SystemClock` was retrofitted | Time is injected via the `Clock` port. No direct clock access in `oe2-core`. | clippy lint denying `SystemTime::now` / `Instant::now` outside the adapter |
| Entity mutation leaked out of the service layer into forms and view models | Commands are the only mutation path. Core types expose no public mutable state. | Rust privacy; `#[non_exhaustive]`; no `pub` fields on aggregates |
| Collections were treated as ordered when they were unordered bags | Collections are unordered. Sorting happens at the point of consumption, via one shared helper. | audit check, carried from V1's `inline-sort-check` |
| Magic strings diverged from constants | Domain vocabulary is enums and newtypes, not strings. | the type system |
| StyleCop warnings were dismissed as cosmetic and accumulated | Zero warnings. `#![deny(warnings)]`, `clippy::pedantic` as the baseline, `cargo fmt --check` in CI. | CI gate |
| Audit findings were rationalised as “pre-existing baseline” | Audit exits non-zero or the build fails. No baseline file for new checks. | CI gate, not an advisory hook |
| Partial test runs hid real failures | One command runs every suite. CI runs it on every push. | `just test` + CI |
| Spec drift from code was invisible | Traceability is validated by tooling. See §9. | incoming spec process |

### 6.1 Two new rules specific to V2

**No floating point in domain values.** V1's economy maths used `double` in places. In V2 the same engine runs as WASM in a browser and as native code on a server, and float behaviour plus accumulated rounding will produce divergent results between them, which destroys the determinism argument in §4.2. Therefore:

- Quantities, counts, and capacities: integers (`u64` / `i64`) in base units
- Prices, rates, multipliers, and purities: `rust_decimal::Decimal`
- `f64` is permitted only in chart rendering and never in a persisted, compared, or hashed value
- Add a clippy lint and an audit check for `f32` / `f64` appearing in `oe2-core::model`

**No panics in the engine.** `oe2-core` returns `Result` and never panics. `unwrap`, `expect`, indexing that can panic, and arithmetic that can overflow are denied outside `#[cfg(test)]`. A panic inside WASM aborts the whole module, and a panic on the server takes down a request handler; neither is an acceptable way to report a domain error. Use `thiserror` for typed domain errors and `anyhow` only at shell boundaries.

### 6.2 Traceability with provable regenerability

**The rule.** Every requirement traces forward to the code implementing it and the
tests verifying it, and every code unit traces back to a requirement — both
directions mechanically checked. The standard is stronger than documentation:
**it must be possible to delete any implementation module and reconstruct
equivalent behaviour from its requirement references and its tests.**

#### What the regeneration inputs actually are

Be precise here, because the achievable version differs from the obvious reading.
The inputs are:

1. the requirement text in `spec/`, reached via the deleted module's `Satisfies:` tags
2. the module's public API documentation
3. **the module's test suite**

Spec prose alone is not sufficient, and no realistic specification ever will be. No
requirements document encodes every numeric tolerance, error variant, sort order and
boundary condition. The tests carry those, which is the right place for them,
because tests are executable. So the standard is: **specification and tests are
jointly sufficient to regenerate the implementation.**

#### The consequences that make this work

If tests are a regeneration input, three things follow, and they are the real value
of this rule:

- **Tests are never regenerated in the same exercise.** That would be circular.
  Tests are authored from the spec and serve as the oracle; the implementation is the
  regenerable artifact.
- **Tests must not couple to implementation detail.** A test asserting on internal
  structure will fail against a correct regeneration. This rule therefore forbids
  implementation-coupled tests *mechanically*, which is worth having on its own.
- **Uncovered code is unregenerable code.** If the oracle never executes a path, it
  does not constrain that path, so nothing pins its behaviour. This is why §6.3 is
  not a separate rule — it is the measurement that makes this one provable.

#### Annotation mechanism

Structured doc-comment tags, parsed by `xtask`. **Not** a proc macro: a macro
validating against an external spec file is fragile, slows every build, and buys
nothing a lint cannot.

```rust
/// Computes the ideal structure count for a colony.
///
/// Satisfies: REQ-COL-014, REQ-COL-015
pub fn ideal_structure_count(/* ... */) { }
```

```rust
/// Verifies: REQ-COL-014
#[test]
fn ideal_count_scales_with_population() { }
```

TypeScript uses the same tags in JSDoc, checked by the same `xtask`.

**Granularity: tag modules, public API items, and every test.** Do not tag private
functions — it is noise and it rots. The regeneration unit is the module, so the
module is the annotation unit.

#### `just trace` fails on

- a `Satisfies:` or `Verifies:` tag naming a requirement ID absent from `spec/`
- a requirement with no implementation
- a requirement with no verifying test
- a public item in `oe2-core` with neither a tag nor a listed exemption

**Exemptions must be individually reasoned.** Not everything implements a
requirement: `serde` impls, `Display`, error conversions, port trait definitions.
These go in `spec/trace-exemptions.toml` with a one-line justification each, and the
file is reviewed. A blanket skip pattern would hollow this rule out within a month.

#### The regeneration drill

This is what converts the aspiration into evidence. A **scheduled** CI job, not a
per-PR gate:

1. select a module from `oe2-core`
2. delete its implementation, keeping its tests and public signatures
3. regenerate from the requirement text plus the tests
4. **pass condition:** the full suite passes and the coverage gate still holds

**A failed drill is a specification defect, not a code defect.** The output is a gap
in `spec/`, filed and fixed. This is the only mechanism in the plan that tests the
specification itself rather than the code, which is what justifies the CI time.

Scope it to `oe2-core` modules. The shells are not regenerable in this sense and are
out of scope.

### 6.3 Coverage, and what to do when 100% is not reachable

**The rule.** 100% coverage of `oe2-core`, `oe2-sync` and the storage adapters, with
every exclusion named and justified individually.

#### Why 100% rather than a percentage target

A threshold of 90% tells you nothing about *which* 10% is unverified, and it
ratchets downward under delivery pressure because every uncovered line is
pre-approved. “100% minus an explicit exclusion list” is enforceable, the exclusions
are visible, and each one is arguable on review. Keep the bar at 100 and argue about
the list instead.

**The owner's diagnosis is the operative principle and should be quoted in review:**
if a branch cannot be covered, the usual cause is that the failure cannot be
injected, which means there is no seam, which is a design defect. The correct
response is to fix the design, not to lower the target. In a sans-IO core (§4.2)
where time, IDs, randomness and storage all arrive through injected ports, there is
very little excuse available.

#### A measurement problem that changes the rule's shape

Rust branch coverage is **not reliably measurable on stable**. `cargo-llvm-cov`
reports line, region and branch coverage, but
[branch coverage is optional and requires nightly](https://github.com/taiki-e/cargo-llvm-cov/blob/main/README.md),
and it has known inconsistencies on generic functions. A hard 100% gate on an
unreliable measurement is worse than no gate, because it teaches people to disbelieve
the gate.

So the Rust gate is:

| Measure | Toolchain | Gate |
|---|---|---|
| **Region coverage** | `cargo-llvm-cov`, stable | **100%**, exclusions listed |
| Line coverage | `cargo-llvm-cov`, stable | 100%, implied by region |
| Branch coverage | nightly | reported, informational, **not gated** |

**Region coverage is the correct stable proxy and is stricter than line coverage.**
Regions are sub-expression granular, so both arms of a `match` and both sides of a
short-circuit `&&` are distinct regions. It delivers most of what branch coverage
would, on a stable toolchain, which matters under C1.

TypeScript has no such problem: `vitest` reports statement, branch, function and
line coverage, and 100% thresholds are gated per package.

#### Coverage is necessary and not sufficient

Coverage proves the tests *execute* the code. It does not prove they *constrain* it.
100% coverage with weak assertions is worthless, and under §6.2 it would also mean
the regeneration oracle has no teeth — a module could be “fully covered” and still
not be regenerable. Mutation testing closes that gap by checking whether a change to
the code is actually detected.

`cargo-mutants` for Rust, with one caveat recorded against C1: by its author's own
description it is a
[semi-actively-maintained spare-time project](https://github.com/sourcefrog/cargo-mutants)
with a single maintainer. That is accepted here **only** because it is a measurement
tool — if it stalls, nothing ships broken and the coverage and property-test gates
are unaffected. **Nothing in the build or release path may depend on it.**

Mutation testing is therefore scheduled, not per-PR:

- weekly CI run over `oe2-core`
- surviving mutants tracked in `spec/mutation-survivors.toml` with a reason each
- target for `oe2-core` is **zero unexplained survivors**
- a surviving mutant is a missing assertion, which is also a regenerability gap

`stryker-mutator` is the TypeScript counterpart, scoped to `@oe2/hooks`.

#### Where 100% is deliberately not the target

The shells. Covering Electron window-management glue, Capacitor bridges and
`napi-rs` boundary code to 100% means mocking entire platform APIs for very little
return.

This is legitimate **only** because of founding rule 7: the shells contain no
business logic. All logic lives in `oe2-core` and `@oe2/hooks`, both of which are
held at 100%. The two rules are load-bearing for each other — **if a shell ever
needs heavy testing, that is evidence it is doing something that belongs in the
core**, and the fix is to move the logic, not to raise the shell's coverage target.

An audit check for business logic appearing under `shells/` is what keeps this
honest. Without it, the shell exemption becomes a place for logic to hide from the
coverage gate.

#### 6.3.1 How §6.2 and §6.3 fit together

These arrived as two separate owner requirements but they are one mechanism, and an
agent should treat them that way:

| Question | Answered by |
|---|---|
| What should this code do? | the requirement, reached via `Satisfies:` tags |
| Does the test suite reach every path? | region coverage, 100% |
| Does the test suite *constrain* every path? | mutation testing, zero survivors |
| Can the implementation be reconstructed? | the regeneration drill |

Read in that order: traceability supplies the intent, coverage proves the oracle is
complete, mutation testing proves the oracle has teeth, and the drill proves the
whole chain actually works. Remove any one and “provable regenerability” degrades to
a claim rather than a measurement.

The owner's framing is the right one to hold onto: **if these rules cannot be kept
in a greenfield rewrite, that is evidence the design is wrong, not that the rules
are unreasonable.** V1 could not have met them — a 4000-line `PlayerContext`
singleton and 8800-line storage class are not testable to 100% and not regenerable
in units. A sans-IO core with injected ports is both, which is a large part of why
§4.2 is written the way it is.

---

## 7. Founding rules checklist

These must be true from the first commit, not added in a later cleanup phase.

1. `oe2-core` has no I/O and no dependency on `std::time`, filesystem, or network.
2. All environmental dependencies are injected through ports.
3. Commands are the only way state changes.
4. No `f32` / `f64` in domain values; no panics in the engine.
5. `packages/contract` is generated, never hand-edited; CI fails if it is stale.
6. `packages/ui` contains no business logic and no data fetching.
7. Shell-specific code stays inside `shells/`. The engine and view layer know nothing about Electron, Capacitor, or the browser.
8. Every game rule has at least one property test.
9. Zero warnings. `cargo clippy -- -D warnings`, `cargo fmt --check`, `tsc --noEmit`, `eslint` all clean.
10. One command builds everything; one command tests everything; both work on Linux.
11. No PowerShell. No Windows-only tooling in any build path.
12. The server has authentication from its first endpoint.
13. `LICENSE` (MIT) exists at the repository root, and `cargo-deny` rejects copyleft dependencies.
14. The desktop shell satisfies the input and density contract in §4.5. Keyboard parity with the mouse is a requirement, not a stretch goal.
15. No JSON file persistence. SQLite is the local store; JSON is import, export, and fixtures only (§4.3).
16. The desktop application is fully functional with no server and no network, and CI proves it.
17. Every requirement in `spec/` traces to an implementation and to a verifying test. `just trace` enforces both directions and fails the build (§6.2).
18. Every public item in `oe2-core` carries a `Satisfies:` tag or a reasoned entry in `spec/trace-exemptions.toml`. No blanket skip patterns.
19. 100% region coverage on `oe2-core`, `oe2-sync` and the storage adapters, with every exclusion individually justified (§6.3).
20. **No test asserts on implementation detail.** Tests are the regeneration oracle and must be behavioural (§6.2).
21. Zero unexplained surviving mutants in `oe2-core`. Nothing in the build or release path depends on `cargo-mutants`.
22. Shells contain no business logic. This is what makes their reduced coverage target legitimate, and it is audit-enforced (§6.3).

---

## 8. Toolchain and CI

### 8.1 Local toolchain (Linux)

- `rustup` with the toolchain pinned in `rust-toolchain.toml`; targets `wasm32-unknown-unknown` and the Android NDK triples
- Node LTS with `pnpm`
- `just` as the task runner — this is the direct replacement for V1's PowerShell scripts
- `cargo-nextest` for test running, `cargo-deny` for licence and advisory auditing, `wasm-bindgen-cli`, `napi-rs` CLI
- `cargo-llvm-cov` for region and line coverage (§6.3), `cargo-mutants` for the scheduled mutation run
- Android SDK and NDK for mobile builds

Minimum task surface in the `justfile`:

```
just setup            # install toolchains, verify environment
just build            # engine + contract codegen + all packages
just test             # every suite: cargo nextest, vitest, wasm tests
just lint             # clippy, fmt, eslint, tsc
just audit            # spec and code audit checks
just trace            # bidirectional requirement traceability (§6.2)
just coverage         # cargo-llvm-cov + vitest coverage, enforces 100% (§6.3)
just check            # lint + build + test + trace + coverage + audit
                      #   (the pre-commit gate)
just mutants          # mutation run; slow, normally left to scheduled CI
just regen-drill      # regeneration drill for one module (§6.2); scheduled CI
just dev-desktop      # Electron with hot reload
just dev-web          # Vite dev server
just dev-mobile       # Capacitor live reload on device
just package-linux    # distributable artifacts
just refresh-legacy   # re-sync legacy/ from the V1 tree, record SHA (§2.2)
```

`just check` is the single gate. If it passes, the change is committable. There is no second manual checklist — V1's experience was that multi-step manual verification gets skipped under pressure.

### 8.2 CI matrix

| Runner | Responsibility |
|---|---|
| `ubuntu-latest` | Everything: engine, server, WASM, all packages, lint, full test suite, traceability, coverage, audit, Linux and Android packaging. **This is the gate for every PR.** |
| `ubuntu-latest` (scheduled, weekly) | `just mutants` over `oe2-core` and `just regen-drill` on a rotating module. Not a PR gate — both are slow, and a regen-drill failure is a specification defect to triage rather than a change to block (§6.2). |
| `windows-latest` | Windows Electron build and signing. Release only. |
| `macos-latest` | macOS Electron build and signing; iOS build and signing. Release only. |

PRs must pass the Linux job. The platform jobs run on release tags, which keeps PR feedback fast and keeps Windows and macOS out of the daily loop (C2).

---

## 9. The spec process — deferred, with a defined seam

**Do not design a spec format. Do not create files under `spec/` beyond the placeholder described below.**

The owner has a brownfield spec process under development in another project, and V2 will be reshaped into it. That process imposes hard format requirements, which V1's spec deliberately lacked. The owner's assessment of V1's spec, which this document accepts:

- Nothing validates that a spec document is well formed
- Nothing validates that cross-references between requirements, design, flows, and mockups actually resolve
- The structure does not support asynchronous multi-developer work — there is no clean way to shard ownership or detect conflicting edits

### 9.1 What the seed assumes about the incoming process

The architecture in this document assumes the spec process will provide:

1. **Stable, unique requirement identifiers** that code and tests can reference
2. **Machine-validated structure** — a schema or linter that fails CI on a malformed document
3. **Validated interlinking** — every reference between requirement, design, flow, mockup, test, and help document resolves, in both directions
4. **Shardable ownership** — documents partitioned so two developers can work concurrently without conflict
5. **Granularity fine enough to annotate code with** — §6.2 requires `Satisfies:`
   tags naming individual requirement IDs on modules and public API items. A
   requirement the size of a chapter cannot be referenced usefully; the ID must
   correspond to roughly one verifiable behaviour.
6. **Completeness sufficient to drive regeneration** — the regeneration drill
   (§6.2) consumes requirement text plus tests. Requirements written as summaries
   rather than as specifications of behaviour will cause drills to fail, and those
   failures are reported as specification defects.

If any of those assumptions is wrong, flag it to the owner rather than working around it.

**Note on 5 and 6:** these two are new as of the §6.2 traceability rule and are the
most demanding things this document asks of the spec process. They are worth
confirming before the process is finalised, because retrofitting requirement
granularity after the fact means renumbering every ID that code already references.

### 9.2 Interim arrangement

Until the process arrives:

- `legacy/spec/` is the **read-only requirements source**. When you need to know what the application should do, read it.
- `spec/` contains only a `README.md` stating that the directory is reserved and naming this document as the reason.
- Record every requirement gap, ambiguity, or contradiction you encounter in `spec/INBOX.md` as a flat append-only list with a reference to the `legacy/spec/` location. Do not attempt to resolve them by inventing requirements. This file becomes the migration input when the real process lands.
- Phase 0 and Phase 1 (§10) can proceed without the spec process, because they are infrastructure. **Phase 2 onwards should not start until the spec process is in place**, because that is where domain requirements begin driving code.

---

## 10. Build-out sequence

Each phase ends with `just check` passing and a working artifact. No phase is “done” because code exists — it is done when the verification named in the phase passes.

**Phase 0 — Skeleton.** Repository layout per §5, Cargo and pnpm workspaces, pinned toolchain, `justfile`, Linux CI running lint plus a trivial test, `legacy/` populated, `spec/README.md` placeholder. **The coverage and traceability gates must be wired up and enforcing in Phase 0, against a trivial amount of code** — a 100% gate introduced once there are 40,000 lines is a gate that gets switched off. *Verification:* `just check` passes on a clean clone on Linux, CI is green, and `just coverage` fails if a line is deliberately left untested.

**Phase 1 — Walking skeleton end to end.** One trivial aggregate carried all the way through: `oe2-core` command, `oe2-contract` type, generated TS contract, `oe2-storage-sqlite`, `oe2-node` addon, Electron shell rendering one MDI child window, Vite shell rendering one page, Capacitor Android build. *Verification:* the same command produces the same result in all three shells, proven by a test, and the contract staleness check fails when `oe2-contract` changes without regeneration.

**Phase 2 — Domain core.** Port the game rules from `legacy/csharp/` and `legacy/spec/` into `oe2-core`: resources, purities, refining, colonies, structures, timers, blueprints. Property tests for every rule. No UI work. *Verification:* property tests cover every rule; 100% region coverage holds; `just trace` reports no untraced requirement and no untagged public item; results cross-checked against V1 behaviour on real `PlayerData.json` fixtures. **Run the first regeneration drill on one small module in this phase** and judge the value before extending it.

**Phase 3 — Desktop MDI shell.** `@oe2/mdi` complete per §4.4, `@oe2/tokens` matching the V1 appearance, the grid and form primitives, and the highest-value views first — colony, blueprint, survey. *Verification:* layout persists across restart; a 10k-row grid scrolls smoothly; keyboard navigation works.

**Phase 4 — Server and sync.** `oe2-server` with authentication designed first, PostgreSQL schema and migrations, `oe2-sync` with the offline queue and reconciliation. Draw on V1's `SyncManager`, `OfflineQueue`, and `QueuedChange` for the model. *Verification:* offline edits reconcile correctly; conflict cases are covered by tests; no endpoint is reachable unauthenticated.

**Phase 5 — Web shell.** Routed-page composition of the Phase 3 views, engine as WASM, OPFS persistence. *Verification:* a planning calculation produces results identical to the desktop shell, proven by a shared test vector.

**Phase 6 — Mobile shells.** Capacitor Android and iOS, stack navigation, mobile-specific layouts over the shared hooks. *Verification:* installed and exercised on a real device for each platform.

**Phase 7 — Parity and retirement.** Walk `legacy/spec/` requirement by requirement, confirm coverage, port `docs/`, then delete `legacy/` on the owner's sign-off.

### 10.1 A note on Phase 3 ordering

Resist building all views before any are good. Take colony management — the densest, most-used area — all the way to production quality first. It will surface every weakness in `@oe2/mdi`, the grid primitive, and the token system while those are still cheap to change.

---

## 11. Open questions for the owner

Do not guess at these. Ask before the relevant phase begins.

1. ~~**DynamoDB.**~~ **ANSWERED.** Must remain an option for the faction server's durable storage. Never fully implemented or tested in V1, so it is a greenfield adapter. Reflected in §3.3, §4.3, and §5. *Open sub-question for Phase 4:* is DynamoDB intended as an alternative to PostgreSQL, or specifically for the faction/shared data while player data stays relational?
2. **Hosting and deployment target** for `oe2-server`. Container on a VPS, a managed platform, or self-hosted? Affects the PostgreSQL and auth choices. *(Phase 4.)*
3. **Authentication model.** **Partially answered:** the owner confirms auth was never fully implemented or tested in V1, so it is greenfield work and must be designed, not ported. V1's `CredentialStore` is a sketch, not a specification. *Still open:* local accounts, an OAuth provider, or something else? Multi-player faction sharing implies real identity and authorisation, so this needs a design document before any endpoint ships. **Treat as the highest-risk item in Phase 4.** *(Phase 4, blocking.)*
4. **Game API integration.** V1 has `GameApiRequestQueue`, a token-bucket rate governor, and `docs/game-api-swagger.json`. Is live API integration in scope for V2, and from which phase? *(Affects engine port design.)*
5. ~~**Licence.**~~ **ANSWERED.** A permissive licence — **MIT or BSD** (GPLv2 also acceptable). The owner's reasoning: the risk of commercial appropriation is negligible for a tool serving at most a few thousand players of a niche game. *Action for Phase 0:* pick MIT (the simpler and more widely understood of the two), add `LICENSE` at the repository root, and configure `cargo-deny` to allow MIT, BSD-2/3-Clause, Apache-2.0, ISC, Zlib, and Unicode-3.0 while denying copyleft dependencies. Note this retroactively confirms the §3.8 rejection of Qt: GPLv3 for the WebAssembly target is incompatible with a permissive project licence.
6. **iOS distribution.** Is there an Apple Developer account and a macOS machine or CI budget? If not, iOS slips to a later phase and should be removed from Phase 6. *(Phase 6.)*
7. **Spec process timing.** Phase 2 onwards depends on it. Should Phase 0 and Phase 1 proceed in parallel with its development, or wait? *(Blocks Phase 2.)*
8. ~~**V1 maintenance during the rewrite.**~~ **ANSWERED.** V1 stays maintained until the owner can switch to V2. The owner is the sole active user. Consequences are written up in §2.2, including the `just refresh-legacy` procedure that `legacy/` now requires.

---

## 12. Known risks

| Risk | Assessment | Mitigation |
|---|---|---|
| MDI window manager is net-new work the project owns | Moderate, but unavoidable in any stack — see §4.4 | Scope it explicitly in Phase 3; it becomes a reusable asset |
| Rust slows early feature velocity while ownership patterns in the domain model settle | Real, and expect it for the first few weeks of Phase 2 | Accept it; it is repaid during refactors and is the price of the C9 benefit |
| Three shells drift into three forks | The main long-term architectural risk | `@oe2/hooks` holds all behaviour; shells own layout only; audit check on business logic appearing under `shells/` |
| Electron footprint becomes unacceptable | Low probability, known mitigation | Shell boundary is designed for a Tauri swap (§3.5) |
| WASM payload size on the web shell | Moderate | Keep `oe2-core` dependency-light; measure from Phase 1; `wasm-opt` in the release build |
| Capacitor mobile UX feels non-native | Likely to some degree | Accepted under C5 and C6; mobile is a companion view. Obsidian and Logseq made the same trade for the same reason (§3.9) |
| WASM-in-webview underperforms on mobile where comparable apps use FFI | The least-proven element of the plan (§3.9.1) | Measure in Phase 6; `uniffi` bindings are an additive escape hatch because the engine is UI-independent |
| Desktop shell drifts into web-page idioms, as happened to 1Password | **High.** The most common criticism of every Electron app in §3.9, and the owner's stated pet hate — the console-game-ported-to-desktop smell | The §4.5 contract with named requirements, founding rule 14, and two mechanical checks by end of Phase 3 |
| Porting game rules introduces behavioural regressions against V1 | High, and the most likely source of real bugs | Phase 2 cross-checks against V1 using real `PlayerData.json` fixtures; mine V1's tests for edge cases |
| The 100% coverage gate slows early feature work | Certain, and partly the point | Accepted. The cost lands in Phase 2 where the core is pure and coverage is cheapest; it is far more expensive to retrofit |
| The coverage exclusion list erodes into a dumping ground | **The main long-term threat to §6.3** | Every exclusion needs an individual written reason; review the list as a whole each phase, not entry by entry |
| The regeneration drill has no precedent and may prove expensive or inconclusive | Real — this is the most novel process idea in the plan | Scheduled, never a PR gate. Start with one small `oe2-core` module in Phase 2 and judge the value before extending it |
| `cargo-mutants` has a single maintainer with limited time | Accepted under C1 | Measurement only; nothing in the build or release path may depend on it (§6.3) |

---

## 13. Document provenance

Technology assessments in §3 were made against sources current as of 2026-10-25, covering Electron and Tauri maturity, Capacitor release status, `napi-rs` production use, Qt for WebAssembly licensing, Rust GUI ecosystem maturity, and Avalonia platform support.

§3.9 is a deliberate sanity check against shipping applications rather than vendor documentation, covering the published architectures of Bitwarden, 1Password 8, Anki, Logseq, Obsidian, VS Code, and Element X. It was added at the owner's request on the principle that technology choices are expensive to change later and should be grounded in what comparable projects actually did.

Linked source content was rephrased for compliance with licensing restrictions.

This document is authoritative for stack and architecture. It is **not** authoritative for spec format (§9) or for the questions in §11. When the brownfield spec process lands, the content here should be migrated into it and this file retired.
