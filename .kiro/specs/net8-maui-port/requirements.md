# .NET 8 + Avalonia Port — Requirements

## Goal

Port the OE2EmpireTracker WinForms desktop client to .NET 8 + Avalonia UI to enable cross-platform support (Windows, Linux, macOS). The primary driver is Linux support — the developer is considering moving their primary development environment to Linux.

## Scope

### In Scope
- The `OE2EmpireTracker` client project (WinForms UI, ViewModels, Controls, Persistence)
- Migration from .NET Framework 4.8.1 to .NET 8
- Migration from WinForms to Avalonia XAML UI
- Migration from `packages.config` to PackageReference
- Custom control equivalents in Avalonia
- Platform-adaptive persistence (file paths, window state)
- Navigation pattern to replace MDI

### Out of Scope (already cross-platform)
- `OE2EmpireTracker.Common` — .NET Standard 2.0, shared models/services/constants
- `OE2EmpireTracker.Server` — already .NET 8
- `OE2EmpireTracker.Tests` — will need a separate migration pass but not part of this spec
- Game logic, parsers, networking (Client/) — no UI dependency

---

## REQ-AVA-001: Platform Targets

The Avalonia application SHALL target:
1. Windows 10+ (x64) — current primary platform, feature parity with existing WinForms app
2. Linux (x64, ARM64) — primary driver for this port; Ubuntu 22.04+, Fedora 38+, Arch
3. macOS 12+ (Apple Silicon + Intel) — nice-to-have, not primary driver

## REQ-AVA-002: Feature Parity

The Avalonia port SHALL provide functional parity with the existing WinForms client for all features:
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

## REQ-AVA-003: Project Structure

The ported solution SHALL use the following project structure:
1. `OE2EmpireTracker.Desktop` — new Avalonia client project (.NET 8, SDK-style csproj)
2. `OE2EmpireTracker.Common` — unchanged, referenced by Desktop project
3. `OE2EmpireTracker.Server` — unchanged
4. The original `OE2EmpireTracker` WinForms project SHALL remain in the solution during the transition period for reference

## REQ-AVA-004: Package Management

The Avalonia project SHALL use:
1. PackageReference format (not packages.config)
2. Central Package Management (Directory.Packages.props) for version consistency
3. NuGet packages compatible with .NET 8

## REQ-AVA-005: Navigation and Docking

The current WinForms app uses MDI (Multiple Document Interface) — child forms open within a parent window. The Avalonia port SHALL use the **Dock** library (wieslawsoltes/Dock, MIT license) to provide a VS Code / Visual Studio-style docking layout:
1. A tabbed document area — each "form" becomes a document tab
2. Document tabs SHALL be closeable, reorderable, and splittable (side-by-side)
3. Document tabs SHALL be floatable — users can detach a tab into its own window
4. Multiple instances of the same form type SHALL be supported (e.g., two colony tabs for different colonies)
5. Dockable tool panels SHALL be supported for auxiliary views (e.g., activity log, notifications)
6. A navigation sidebar or menu SHALL provide access to all feature areas
7. The app SHALL persist and restore the full docking layout (tab positions, splits, panel states) across sessions using Dock's built-in layout serialization

## REQ-AVA-006: Data Binding

The Avalonia port SHALL use:
1. Avalonia's native data binding (AXAML `{Binding}` markup) instead of WinForms BindingSource
2. ViewModels SHALL implement `INotifyPropertyChanged` (or use CommunityToolkit.Mvvm source generators or ReactiveUI)
3. Collections SHALL use `ObservableCollection<T>` instead of `BindingList<T>`
4. The existing ViewModel layer SHALL be adapted (not rewritten from scratch) where possible

## REQ-AVA-007: Custom Control Equivalents

The following WinForms custom controls SHALL have Avalonia equivalents:

| WinForms Control | Avalonia Equivalent Strategy |
|-----------------|-------------------------|
| DataEntryGridView | Avalonia DataGrid with custom cell templates |
| FilteredTextComboSet | AutoCompleteBox or custom ComboBox with filter |
| ValidatedTextBox | TextBox with DataValidationErrors + INotifyDataErrorInfo |
| DataGridViewFilteredComboBoxColumn | DataGrid column with ComboBox cell + filter |
| DataGridViewValidatedTextBoxColumn | DataGrid column with validated TextBox cell |
| RtfBuilder | Avalonia.HtmlRenderer or custom formatted TextBlock |
| ProgrammaticUpdateGuard | Equivalent pattern using a flag property on ViewModels |
| ListViewItemComparer | DataGrid sorting via DataGridColumn.Sort or CollectionView |

## REQ-AVA-008: DataGrid Strategy

The WinForms app heavily uses DataGridView for tabular data with inline editing. Avalonia has a built-in DataGrid control. The port SHALL:
1. Use Avalonia's built-in `DataGrid` control as the primary tabular data component
2. The DataGrid SHALL support: sortable columns, inline cell editing, combo box columns, row selection, and column resizing
3. Custom cell templates SHALL be used for specialized columns (filtered combo, validated text)
4. If the built-in DataGrid lacks required features, evaluate `Avalonia.Controls.DataGrid` community extensions or implement custom cell editing templates

## REQ-AVA-009: Persistence Adaptation

The Avalonia port SHALL adapt persistence for cross-platform:
1. JSON data files (PlayerData.json, BaselineData.json) SHALL be stored in platform-appropriate locations:
   - Windows: `%APPDATA%/OE2EmpireTracker/`
   - Linux: `~/.local/share/OE2EmpireTracker/` (XDG_DATA_HOME)
   - macOS: `~/Library/Application Support/OE2EmpireTracker/`
2. SafeFileWriter SHALL work on all platforms (atomic write via temp + rename)
3. Window state persistence SHALL adapt to each platform's windowing model
4. File paths SHALL use `Path.Combine` and platform-agnostic separators throughout

## REQ-AVA-010: Logging

The Avalonia port SHALL:
1. Replace NLog with `Microsoft.Extensions.Logging` (standard .NET logging abstraction)
2. Configure file logging to platform-appropriate log directories:
   - Windows: `%APPDATA%/OE2EmpireTracker/logs/`
   - Linux: `~/.local/share/OE2EmpireTracker/logs/` or `~/.cache/OE2EmpireTracker/logs/`
   - macOS: `~/Library/Logs/OE2EmpireTracker/`
3. Maintain the same log levels and categories as the current NLog configuration
4. Support debug output during development

## REQ-AVA-011: Clipboard and HTML Import

The current app imports data by pasting HTML from the game's web interface. The Avalonia port SHALL:
1. Support clipboard paste of HTML content on Windows and Linux (and macOS)
2. The existing HTML parsers (ColonyParser, SurveyParser) SHALL work unchanged
3. Avalonia's built-in clipboard API (`TopLevel.Clipboard`) SHALL be used with platform-appropriate format handling
4. On Linux, both X11 and Wayland clipboard access SHALL be supported (Avalonia handles this natively)

## REQ-AVA-012: Timer and Background Processing

The current app uses `System.Windows.Forms.Timer` for periodic processing. The Avalonia port SHALL:
1. Use `Avalonia.Threading.DispatcherTimer` for UI-thread timers
2. Use `System.Threading.Timer` or `PeriodicTimer` for background processing
3. Maintain the same processing intervals and behavior as the current implementation

## REQ-AVA-013: Dependency Injection

The Avalonia port SHALL use .NET's built-in dependency injection:
1. Register services using `Microsoft.Extensions.DependencyInjection` in application startup
2. Replace singleton pattern (`getInstance()`) with DI-registered singletons
3. ViewModels SHALL be registered and injected (not manually constructed)
4. Platform services (clipboard, file system, preferences) SHALL be injected via interfaces

## REQ-AVA-014: Migration Strategy

The port SHALL follow an incremental migration strategy:
1. Phase 1: Project setup — create Avalonia project, configure dependencies, establish patterns
2. Phase 2: Core infrastructure — navigation shell, DI registration, persistence, logging
3. Phase 3: Feature migration — port forms one at a time, starting with simplest (About, Help, Preferences)
4. Phase 4: Complex features — colony, blueprint, delivery (DataGrid-heavy forms)
5. Phase 5: Polish — platform-specific refinements, performance, testing
6. Each phase SHALL be independently buildable and testable

## REQ-AVA-015: Shared Code Maximization

The port SHALL maximize code sharing between platforms:
1. All business logic remains in `OE2EmpireTracker.Common` (already cross-platform)
2. ViewModels SHALL be platform-agnostic (no Avalonia-specific types in ViewModel layer)
3. Platform-specific code SHALL be isolated behind interfaces with platform implementations
4. At least 90% of non-UI code SHALL be shared across all platforms

## REQ-AVA-016: Build and CI

The Avalonia project SHALL:
1. Build with `dotnet build` (standard .NET 8 SDK tooling)
2. Support building on Windows, Linux, and macOS development machines
3. Produce platform-specific packages:
   - Windows: self-contained exe or MSIX installer
   - Linux: AppImage, .deb, or self-contained publish
   - macOS: .app bundle
4. The build SHALL produce zero warnings (matching current zero-warnings policy)

## REQ-AVA-017: Theming and Appearance

The Avalonia port SHALL:
1. Support both light and dark themes (Avalonia has built-in theme support)
2. Follow system theme preference by default
3. Allow user override in preferences (force light, force dark, or follow system)
4. Use Avalonia's FluentTheme for a modern appearance consistent across platforms

---

## Risk Register

| ID | Risk | Impact | Mitigation |
|----|------|--------|------------|
| R1 | Avalonia DataGrid maturity — built-in DataGrid exists but may lack some WinForms DataGridView features (e.g., complex cell editing) | Medium — DataGridView is used in 15+ forms | Prototype complex editing scenarios early in Phase 1; custom cell templates as fallback |
| R2 | Dock library learning curve — Dock has its own layout model and MVVM patterns | Low — well-documented, MIT licensed, active community | Follow Dock samples (DockMvvmSample, VisualStudioDemo); prototype in Phase 1 |
| R3 | RichTextBox equivalent — Avalonia has no built-in RTF control | Low — used for formatted output display | Use Avalonia.HtmlRenderer or custom TextBlock with Inlines |
| R4 | Clipboard HTML on Linux — X11/Wayland clipboard format differences | Medium — core import workflow | Test on both X11 and Wayland early; Avalonia abstracts most differences |
| R5 | Linux desktop integration — notifications, system tray, file dialogs | Low — not heavily used | Avalonia has platform abstractions; test on target distros |
| R6 | Performance — Avalonia uses Skia rendering; may differ from native WinForms for large grids | Low — Avalonia is generally performant for desktop | Profile early; use virtualization for large collections |
| R7 | Package ecosystem — some NuGet packages may have Windows-only dependencies | Low — most modern packages target .NET Standard 2.0+ | Audit all dependencies in Phase 1 |

---

## Open Questions

1. **MVVM framework** — CommunityToolkit.Mvvm (simpler, source generators) vs ReactiveUI (more powerful, Avalonia's traditional choice)? Recommend CommunityToolkit.Mvvm for consistency with existing patterns.
2. **Tab vs. sidebar navigation** — Should the main navigation be a left sidebar (like VS Code) or a top tab bar (like a browser)? Needs UX prototyping.
3. **Linux packaging** — AppImage (universal), .deb (Debian/Ubuntu), Flatpak, or self-contained publish? May support multiple.
4. **Offline-first** — With the server component existing, should the Avalonia app lean more into online sync or maintain the current offline-first model?
