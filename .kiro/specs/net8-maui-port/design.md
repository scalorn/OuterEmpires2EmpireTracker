# .NET 8 + MAUI Port — Design

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    MAUI Shell (AppShell)                 │
│  ┌─────────────┐  ┌──────────────────────────────────┐  │
│  │  Navigation │  │         Tab Container            │  │
│  │   Sidebar   │  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐   │  │
│  │             │  │  │Tab1│ │Tab2│ │Tab3│ │... │   │  │
│  │  • Colony   │  │  └────┘ └────┘ └────┘ └────┘   │  │
│  │  • Blueprint│  │  ┌──────────────────────────┐   │  │
│  │  • Survey   │  │  │     Content Page          │   │  │
│  │  • Ships    │  │  │   (XAML ContentPage)      │   │  │
│  │  • Delivery │  │  │                           │   │  │
│  │  • Market   │  │  │   ViewModel (MVVM)        │   │  │
│  │  • Profile  │  │  │         ↓                 │   │  │
│  │  • System   │  │  │   Services (DI)           │   │  │
│  │  • Settings │  │  │         ↓                 │   │  │
│  │             │  │  │   Common (Models)         │   │  │
│  └─────────────┘  │  └──────────────────────────┘   │  │
│                    └──────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Layer Mapping (WinForms → MAUI)

| WinForms Layer | MAUI Layer | Notes |
|---------------|-----------|-------|
| Forms/*.Designer.cs | Pages/*.xaml | XAML replaces Designer-generated code |
| Forms/*.cs (code-behind) | Pages/*.xaml.cs (minimal) + ViewModels/ | Logic moves to ViewModel |
| Controls/ | Controls/ (MAUI custom controls) | Rewritten for MAUI |
| ViewModels/ | ViewModels/ (enhanced) | Add INotifyPropertyChanged, commands |
| Services/ | Services/ (via DI) | Singleton → DI-registered singleton |
| Models/ | OE2EmpireTracker.Common | Already cross-platform |
| Persistence/ | Services/Platform/ | Platform-abstracted |

## Project Structure

```
OE2EmpireTracker.Maui/
├── App.xaml / App.xaml.cs          # Application entry, DI setup
├── AppShell.xaml / AppShell.xaml.cs # Navigation shell (sidebar + tabs)
├── MauiProgram.cs                  # Host builder, service registration
│
├── Pages/                          # XAML ContentPages (one per "form")
│   ├── ColonyPage.xaml
│   ├── BlueprintPage.xaml
│   ├── SurveyPage.xaml
│   ├── ShipTemplatePage.xaml
│   ├── DeliveryRoutePage.xaml
│   ├── PlayerProfilePage.xaml
│   ├── MarketPage.xaml
│   ├── PreferencesPage.xaml
│   ├── AboutPage.xaml
│   └── ...
│
├── ViewModels/                     # MVVM ViewModels
│   ├── Base/
│   │   └── BaseViewModel.cs        # INotifyPropertyChanged, IsBusy, etc.
│   ├── ColonyViewModel.cs
│   ├── BlueprintViewModel.cs
│   ├── SurveyViewModel.cs
│   └── ...
│
├── Controls/                       # Custom MAUI controls
│   ├── DataGrid/                   # DataGrid abstraction (wraps third-party or custom)
│   ├── FilteredPicker.cs           # Replacement for FilteredTextComboSet
│   ├── ValidatedEntry.cs           # Replacement for ValidatedTextBox
│   └── TabDocumentContainer.cs     # MDI replacement — tabbed document host
│
├── Services/                       # Platform services
│   ├── Platform/
│   │   ├── IClipboardService.cs
│   │   ├── IFileSystemService.cs
│   │   ├── IWindowStateService.cs
│   │   └── Implementations/        # Platform-specific implementations
│   ├── NavigationService.cs        # Tab/page navigation management
│   └── TimerService.cs             # Background processing timer
│
├── Converters/                     # XAML value converters
│   ├── BoolToVisibilityConverter.cs
│   ├── StatusToColorConverter.cs
│   └── ...
│
├── Resources/                      # MAUI resources
│   ├── Styles/
│   ├── Fonts/
│   └── Images/
│
├── Platforms/                      # Platform-specific code
│   ├── Windows/
│   ├── MacCatalyst/
│   ├── iOS/ (future)
│   └── Android/ (future)
│
└── OE2EmpireTracker.Maui.csproj
```

## Key Design Decisions

### 1. Navigation: Sidebar + Tabbed Documents

The main window uses a split layout:
- **Left sidebar**: Fixed navigation menu listing all feature areas (Colony, Blueprint, Survey, etc.)
- **Main area**: Tabbed document container where each opened feature is a tab
- Clicking a sidebar item opens a new tab (or focuses existing tab for that feature)
- Tabs can be closed, reordered, and duplicated
- This mirrors VS Code / browser UX which is familiar to users

### 2. MVVM with CommunityToolkit.Mvvm

Use `CommunityToolkit.Mvvm` for:
- `[ObservableProperty]` source generator (eliminates INotifyPropertyChanged boilerplate)
- `[RelayCommand]` for command binding
- `ObservableObject` base class
- Messaging (`WeakReferenceMessenger`) for cross-ViewModel communication

### 3. Dependency Injection Strategy

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>();

    // Existing services (from Common) — registered as singletons
    builder.Services.AddSingleton<IEmpireContext, EmpireContext>();
    builder.Services.AddSingleton<IPlayerContext, PlayerContext>();

    // Platform services
    builder.Services.AddSingleton<IClipboardService, ClipboardService>();
    builder.Services.AddSingleton<IFileSystemService, FileSystemService>();

    // ViewModels — transient (one per page instance)
    builder.Services.AddTransient<ColonyViewModel>();
    builder.Services.AddTransient<BlueprintViewModel>();

    // Pages
    builder.Services.AddTransient<ColonyPage>();
    builder.Services.AddTransient<BlueprintPage>();
}
```

### 4. ViewModel Adaptation Strategy

The existing ViewModels (e.g., `ColonyViewModel`, `BlueprintViewModel`) wrap domain models and expose typed operations. For MAUI:

**Keep**: Business logic, property calculations, validation rules
**Add**: `INotifyPropertyChanged` (via `ObservableObject`), `ICommand` properties, `ObservableCollection<T>`
**Remove**: WinForms-specific patterns (BindingList, direct control references)

```csharp
// Before (WinForms)
public class ColonyViewModel
{
    private readonly Colony _colony;
    public BindingList<ColonyStructureViewModel> Structures { get; }
    public void RefreshStructures() { /* rebuilds BindingList */ }
}

// After (MAUI)
public partial class ColonyViewModel : ObservableObject
{
    private readonly Colony _colony;

    [ObservableProperty]
    private ObservableCollection<ColonyStructureViewModel> _structures;

    [RelayCommand]
    private void RefreshStructures() { /* rebuilds ObservableCollection */ }
}
```

### 5. Platform Abstraction

```csharp
public interface IFileSystemService
{
    string GetDataDirectory();      // %APPDATA% on Windows, ~/Library/... on macOS
    string GetLogDirectory();
    Task<string> ReadFileAsync(string relativePath);
    Task WriteFileAsync(string relativePath, string content);
}

public interface IClipboardService
{
    Task<string> GetHtmlAsync();    // HTML clipboard content for paste import
    Task<string> GetTextAsync();
    Task SetTextAsync(string text);
}

public interface IWindowStateService
{
    Task SaveStateAsync(string windowId, WindowState state);
    Task<WindowState> LoadStateAsync(string windowId);
}
```

### 6. DataGrid Approach

Recommended evaluation order:
1. **Syncfusion MAUI DataGrid** — free community license for <$1M revenue, full-featured
2. **DevExpress MAUI DataGrid** — commercial, very mature
3. **Custom CollectionView** — fallback if licensing is a concern

The DataGrid wrapper SHALL be abstracted so the underlying implementation can be swapped:
```csharp
// Abstract interface — pages bind to this
public interface IDataGridSource<T>
{
    ObservableCollection<T> Items { get; }
    T SelectedItem { get; set; }
    ICommand SortCommand { get; }
    ICommand FilterCommand { get; }
}
```

### 7. Logging Migration

```csharp
// Before (NLog)
private static readonly Logger Log = LogManager.GetCurrentClassLogger();
Log.Info("Colony loaded: {0}", colony.Name);

// After (Microsoft.Extensions.Logging)
private readonly ILogger<ColonyViewModel> _logger;
_logger.LogInformation("Colony loaded: {Name}", colony.Name);
```

Configure Serilog or NLog as the provider behind `ILogger<T>` for file output.

---

## Migration Mapping: Forms → Pages

| WinForms Form | MAUI Page | Complexity | Phase |
|--------------|-----------|-----------|-------|
| FormAbout | AboutPage | Low | 3 |
| FormHelp | HelpPage | Low | 3 |
| FormPreferences | PreferencesPage | Low | 3 |
| FormPlayerProfile | PlayerProfilePage | Medium | 3 |
| FormSurvey | SurveyPage | Medium | 3 |
| FormColonyV2 | ColonyPage | High | 4 |
| FormBlueprintV2 | BlueprintPage | High | 4 |
| FormDeliveryRoute | DeliveryRoutePage | High | 4 |
| FormDeliveryExecution | DeliveryExecutionPage | High | 4 |
| FormShipTemplate | ShipTemplatePage | Medium | 4 |
| FormShipInstance | ShipInstancePage | Medium | 4 |
| FormMarket | MarketPage | Medium | 4 |
| FormStation | StationPage | Medium | 4 |
| FormBuildPlanner | BuildPlannerPage | High | 4 |
| FormColonyDailyBuild | ColonyDailyBuildPage | Medium | 4 |
| FormColonyActivity | ColonyActivityPage | Medium | 4 |
| FormSupplyChain | SupplyChainPage | Medium | 4 |
| FormStockTargets | StockTargetsPage | Medium | 4 |
| FormContacts | ContactsPage | Low | 3 |
| FormAsteroid | AsteroidPage | Medium | 4 |
| FormSystem | SystemPage | Medium | 4 |
| MainWindow | AppShell + TabContainer | High | 2 |


## Package Dependencies (MAUI Project)

| Package | Purpose | Replaces |
|---------|---------|----------|
| Microsoft.Maui.Controls | MAUI framework | System.Windows.Forms |
| CommunityToolkit.Mvvm | MVVM source generators | Manual INotifyPropertyChanged |
| CommunityToolkit.Maui | MAUI helpers, converters | — |
| Newtonsoft.Json | JSON serialization | Same (from Common) |
| Microsoft.Extensions.Logging | Logging abstraction | NLog |
| Serilog.Extensions.Logging | File logging provider | NLog file target |
| Serilog.Sinks.File | Log file output | NLog file target |
| Polly | Resilience (from Common) | Same |
| (DataGrid TBD) | Tabular data display | DataGridView |

## Event System Migration

Current WinForms uses `PlayerContext.*Changed` events with direct form subscriptions. MAUI equivalent:

1. **WeakReferenceMessenger** (CommunityToolkit.Mvvm) for cross-ViewModel notifications
2. ViewModels subscribe to messages instead of forms subscribing to context events
3. Messages are strongly typed (e.g., `ColonyChangedMessage`, `PlayerSwitchedMessage`)

```csharp
// Message definition
public record ColonyChangedMessage(string ColonyId);

// Publishing (in service/context)
WeakReferenceMessenger.Default.Send(new ColonyChangedMessage(colonyId));

// Subscribing (in ViewModel)
WeakReferenceMessenger.Default.Register<ColonyChangedMessage>(this, (r, m) =>
{
    // Refresh data
});
```

## Testing Strategy

- Unit tests for ViewModels (mock services via DI)
- Integration tests for platform services
- UI tests via MAUI testing framework (Appium or similar) — Phase 5
- Existing Common tests remain unchanged
