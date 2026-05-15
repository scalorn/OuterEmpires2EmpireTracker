# .NET 8 + MAUI Port — Requirements

## Goal

Port the OE2EmpireTracker WinForms desktop client to .NET 8 + MAUI to enable cross-platform support (Windows, macOS, and potentially iOS/Android in the future). The primary driver is macOS support.

## Scope

### In Scope
- The `OE2EmpireTracker` client project (WinForms UI, ViewModels, Controls, Persistence)
- Migration from .NET Framework 4.8.1 to .NET 8
- Migration from WinForms to MAUI XAML UI
- Migration from `packages.config` to PackageReference
- Custom control equivalents in MAUI
- Platform-adaptive persistence (file paths, window state)
- Navigation pattern to replace MDI

### Out of Scope (already cross-platform)
- `OE2EmpireTracker.Common` — .NET Standard 2.0, shared models/services/constants
- `OE2EmpireTracker.Server` — already .NET 8
- `OE2EmpireTracker.Tests` — will need a separate migration pass but not part of this spec
- Game logic, parsers, networking (Client/) — no UI dependency

---

## REQ-MAUI-001: Platform Targets

The MAUI application SHALL target:
1. Windows 10+ (x64) — primary platform, feature parity with current WinForms app
2. macOS 12+ (Apple Silicon + Intel) — primary driver for this port
3. iOS and Android — MAY be supported in a future phase (not required for initial port)

## REQ-MAUI-002: Feature Parity

The MAUI port SHALL provide functional parity with the existing WinForms client for all features:
- Colony management (structures, mining, refining, research, manufacturing, commodity production)
- Blueprint management (scanning, evolution, manufacturing, pricing)
- Survey management (HTML paste import, resource display)
- Player profile management (skills, ranks, professions)
- Delivery routes and plans (logistics across colonies)
- Ship templates and instances (hull selection, slot configuration, pricing)
- Market pricing plans
- Station management
- Build planner and daily build
- Colony activity tracking
- Supply chain visualization
- Stock targets
- Contacts management
- Star system / asteroid management
- Preferences
- Help system
- Timed processing (mining cycles, refining, research, manufacturing countdowns)

## REQ-MAUI-003: Project Structure

The ported solution SHALL use the following project structure:
1. `OE2EmpireTracker.Maui` — new MAUI client project (.NET 8, SDK-style csproj)
2. `OE2EmpireTracker.Common` — unchanged, referenced by MAUI project
3. `OE2EmpireTracker.Server` — unchanged
4. The original `OE2EmpireTracker` WinForms project SHALL remain in the solution during the transition period for reference

## REQ-MAUI-004: Package Management

The MAUI project SHALL use:
1. PackageReference format (not packages.config)
2. Central Package Management (Directory.Packages.props) for version consistency
3. NuGet packages compatible with .NET 8

## REQ-MAUI-005: Navigation Pattern

The current WinForms app uses MDI (Multiple Document Interface) — child forms open within a parent window. MAUI does not support MDI. The MAUI port SHALL use:
1. A tabbed document interface — each "form" becomes a tab within the main window
2. Tabs SHALL be closeable and reorderable
3. Multiple instances of the same form type SHALL be supported (e.g., two colony tabs for different colonies)
4. A navigation sidebar or menu SHALL provide access to all feature areas
5. The app SHALL remember which tabs were open on last close and restore them on next launch

## REQ-MAUI-006: Data Binding

The MAUI port SHALL use:
1. MAUI's native data binding (XAML `{Binding}` markup) instead of WinForms BindingSource
2. ViewModels SHALL implement `INotifyPropertyChanged` (or use CommunityToolkit.Mvvm source generators)
3. Collections SHALL use `ObservableCollection<T>` instead of `BindingList<T>`
4. The existing ViewModel layer SHALL be adapted (not rewritten from scratch) where possible

## REQ-MAUI-007: Custom Control Equivalents

The following WinForms custom controls SHALL have MAUI equivalents:

| WinForms Control | MAUI Equivalent Strategy |
|-----------------|-------------------------|
| DataEntryGridView | CollectionView with DataTemplate + inline editing |
| FilteredTextComboSet | SearchBar + Picker combination or custom handler |
| ValidatedTextBox | Entry with Behaviors for validation |
| DataGridViewFilteredComboBoxColumn | CollectionView column with filtered Picker |
| DataGridViewValidatedTextBoxColumn | CollectionView column with validated Entry |
| RtfBuilder | HTML-based formatted text (WebView or Label with FormattedString) |
| ProgrammaticUpdateGuard | Equivalent pattern using a flag property on ViewModels |
| ListViewItemComparer | CollectionView sorting via IComparer or LINQ |

## REQ-MAUI-008: DataGrid Strategy

The WinForms app heavily uses DataGridView for tabular data with inline editing. MAUI's built-in CollectionView lacks full DataGrid capabilities. The port SHALL:
1. Evaluate third-party MAUI DataGrid controls (Syncfusion, DevExpress, Telerik) for feature parity
2. If a third-party control is selected, it SHALL support: sortable columns, inline cell editing, combo box columns, row selection, and column resizing
3. If no suitable third-party control exists, a custom CollectionView-based solution SHALL be implemented with the above capabilities
4. The choice SHALL be documented in a design decision record

## REQ-MAUI-009: Persistence Adaptation

The MAUI port SHALL adapt persistence for cross-platform:
1. JSON data files (PlayerData.json, BaselineData.json) SHALL be stored in platform-appropriate locations:
   - Windows: `%APPDATA%/OE2EmpireTracker/`
   - macOS: `~/Library/Application Support/OE2EmpireTracker/`
2. SafeFileWriter SHALL work on both Windows and macOS (atomic write via temp + rename)
3. Window state persistence SHALL adapt to each platform's windowing model
4. File paths SHALL use `Path.Combine` and platform-agnostic separators throughout

## REQ-MAUI-010: Logging

The MAUI port SHALL:
1. Replace NLog with `Microsoft.Extensions.Logging` (MAUI's native logging abstraction)
2. Configure file logging to platform-appropriate log directories
3. Maintain the same log levels and categories as the current NLog configuration
4. Support debug output during development

## REQ-MAUI-011: Clipboard and HTML Import

The current app imports data by pasting HTML from the game's web interface. The MAUI port SHALL:
1. Support clipboard paste of HTML content on Windows and macOS
2. The existing HTML parsers (ColonyParser, SurveyParser) SHALL work unchanged
3. Platform-specific clipboard access SHALL be abstracted behind an interface

## REQ-MAUI-012: Timer and Background Processing

The current app uses `System.Windows.Forms.Timer` for periodic processing. The MAUI port SHALL:
1. Use `Microsoft.Maui.Dispatching.IDispatcherTimer` for UI-thread timers
2. Use `System.Threading.Timer` or `PeriodicTimer` for background processing
3. Maintain the same processing intervals and behavior as the current implementation

## REQ-MAUI-013: Dependency Injection

The MAUI port SHALL use .NET's built-in dependency injection:
1. Register services in `MauiProgram.cs` using `builder.Services`
2. Replace singleton pattern (`getInstance()`) with DI-registered singletons
3. ViewModels SHALL be registered and injected (not manually constructed)
4. Platform services (clipboard, file system, preferences) SHALL be injected via interfaces

## REQ-MAUI-014: Migration Strategy

The port SHALL follow an incremental migration strategy:
1. Phase 1: Project setup — create MAUI project, configure dependencies, establish patterns
2. Phase 2: Core infrastructure — navigation shell, DI registration, persistence, logging
3. Phase 3: Feature migration — port forms one at a time, starting with simplest (About, Help, Preferences)
4. Phase 4: Complex features — colony, blueprint, delivery (DataGrid-heavy forms)
5. Phase 5: Polish — platform-specific refinements, performance, testing
6. Each phase SHALL be independently buildable and testable

## REQ-MAUI-015: Shared Code Maximization

The port SHALL maximize code sharing between platforms:
1. All business logic remains in `OE2EmpireTracker.Common` (already cross-platform)
2. ViewModels SHALL be platform-agnostic (no MAUI-specific types in ViewModel layer)
3. Platform-specific code SHALL be isolated behind interfaces with platform implementations
4. At least 90% of non-UI code SHALL be shared across Windows and macOS

## REQ-MAUI-016: Build and CI

The MAUI project SHALL:
1. Build with `dotnet build` (standard .NET 8 SDK tooling)
2. Support building on both Windows and macOS development machines
3. Produce platform-specific packages:
   - Windows: MSIX installer
   - macOS: .app bundle (signed for distribution)
4. The build SHALL produce zero warnings (matching current zero-warnings policy)

---

## Risk Register

| ID | Risk | Impact | Mitigation |
|----|------|--------|------------|
| R1 | MAUI DataGrid maturity — no built-in DataGrid, third-party options vary in quality | High — DataGridView is used in 15+ forms | Evaluate Syncfusion/DevExpress early in Phase 1; have fallback plan using CollectionView |
| R2 | MDI replacement — tabbed interface may not feel as flexible as MDI | Medium — user workflow change | Prototype tab navigation in Phase 2; get user feedback before committing |
| R3 | RichTextBox equivalent — MAUI has no RTF control | Medium — used for formatted output display | Use HTML rendering via WebView or Label.FormattedString |
| R4 | Clipboard HTML access — platform differences in clipboard format handling | Medium — core import workflow | Abstract behind interface; test on both platforms early |
| R5 | MAUI macOS maturity — MAUI on macOS is less mature than Windows | High — primary driver is macOS | Test on macOS continuously from Phase 2; report bugs upstream |
| R6 | Performance — MAUI rendering may be slower than native WinForms for large grids | Medium — colony/blueprint grids can have 100+ rows | Profile early; use virtualization; consider platform-specific optimizations |
| R7 | Package ecosystem — some NuGet packages may not support .NET 8 or macOS | Low — most modern packages target .NET Standard 2.0+ | Audit all dependencies in Phase 1 |

---

## Open Questions

1. **DataGrid choice** — Which third-party DataGrid control (if any) should be used? Needs evaluation in Phase 1.
2. **Tab vs. sidebar navigation** — Should the main navigation be a left sidebar (like VS Code) or a top tab bar (like a browser)? Needs UX prototyping.
3. **Theming** — Should the MAUI app support dark mode? The current WinForms app uses system theme.
4. **Mobile future** — How much should the UI be designed with future mobile support in mind? (Responsive layouts vs. desktop-optimized fixed layouts)
5. **Offline-first** — With the server component existing, should the MAUI app lean more into online sync or maintain the current offline-first model?
