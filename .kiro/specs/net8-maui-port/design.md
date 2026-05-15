# .NET 8 + Avalonia Port — Design

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              Avalonia Window (MainWindow)                │
│  ┌─────────────┐  ┌──────────────────────────────────┐  │
│  │  Navigation │  │         Tab Container            │  │
│  │   Sidebar   │  │  ┌────┐ ┌────┐ ┌────┐ ┌────┐   │  │
│  │             │  │  │Tab1│ │Tab2│ │Tab3│ │... │   │  │
│  │  • Colony   │  │  └────┘ └────┘ └────┘ └────┘   │  │
│  │  • Blueprint│  │  ┌──────────────────────────┐   │  │
│  │  • Survey   │  │  │     Content View          │   │  │
│  │  • Ships    │  │  │   (AXAML UserControl)     │   │  │
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

## Layer Mapping (WinForms → Avalonia)

| WinForms Layer | Avalonia Layer | Notes |
|---------------|-----------|-------|
| Forms/*.Designer.cs | Views/*.axaml | AXAML replaces Designer-generated code |
| Forms/*.cs (code-behind) | Views/*.axaml.cs (minimal) + ViewModels/ | Logic moves to ViewModel |
| Controls/ | Controls/ (Avalonia custom controls) | Rewritten for Avalonia |
| ViewModels/ | ViewModels/ (enhanced) | Add INotifyPropertyChanged, commands |
| Services/ | Services/ (via DI) | Singleton → DI-registered singleton |
| Models/ | OE2EmpireTracker.Common | Already cross-platform |
| Persistence/ | Services/Platform/ | Platform-abstracted |

## Project Structure

```
OE2EmpireTracker.Desktop/
├── App.axaml / App.axaml.cs        # Application entry, theme, DI setup
├── Program.cs                       # Entry point (BuildAvaloniaApp)
├── MainWindow.axaml / .axaml.cs    # Main window with sidebar + tab host
│
├── Views/                           # AXAML UserControls (one per "form")
│   ├── ColonyView.axaml
│   ├── BlueprintView.axaml
│   ├── SurveyView.axaml
│   ├── ShipTemplateView.axaml
│   ├── DeliveryRouteView.axaml
│   ├── PlayerProfileView.axaml
│   ├── MarketView.axaml
│   ├── PreferencesView.axaml
│   ├── AboutView.axaml
│   └── ...
│
├── ViewModels/                      # MVVM ViewModels
│   ├── Base/
│   │   └── ViewModelBase.cs         # ObservableObject base, IsBusy, etc.
│   ├── MainWindowViewModel.cs       # Tab management, navigation
│   ├── ColonyViewModel.cs
│   ├── BlueprintViewModel.cs
│   ├── SurveyViewModel.cs
│   └── ...
│
├── Controls/                        # Custom Avalonia controls
│   ├── FilteredComboBox.axaml       # Replacement for FilteredTextComboSet
│   ├── ValidatedTextBox.cs          # TextBox with validation behavior
│   └── DataGridExtensions/          # Custom DataGrid cell templates
│
├── Dock/                            # Dock library integration
│   ├── DockFactory.cs               # Creates dock layout, document/tool instances
│   ├── DocumentTemplates.axaml      # DataTemplates mapping ViewModels → Views in dock
│   └── LayoutSerializer.cs          # Save/restore dock layout to JSON
│
├── Services/                        # Application services + platform abstractions
│   ├── IClipboardService.cs
│   ├── IFileSystemService.cs
│   ├── IWindowStateService.cs
│   ├── NavigationService.cs         # Tab/view navigation management
│   ├── TimerService.cs              # Background processing timer
│   └── Platform/                    # Platform-specific implementations (if needed)
│
├── Converters/                      # AXAML value converters
│   ├── BoolToVisibilityConverter.cs
│   ├── StatusToColorConverter.cs
│   └── ...
│
├── Assets/                          # Images, icons, fonts
│
├── Styles/                          # Shared styles and themes
│   └── AppStyles.axaml
│
└── OE2EmpireTracker.Desktop.csproj
```

## Key Design Decisions

### 1. Navigation: Dock Library (wieslawsoltes/Dock)

The main window uses the Dock library to provide a VS Code / Visual Studio-style layout:
- **Left sidebar**: Dockable tool panel with navigation tree (all feature areas)
- **Center**: DocumentDock area where each opened feature is a tabbed document
- **Floating**: Any document tab can be detached into its own floating window
- **Splitting**: Document area can be split horizontally/vertically for side-by-side views
- **Layout persistence**: Dock's built-in serialization saves/restores the full layout (JSON format via Dock.Serializer.Newtonsoft)

Key Dock concepts:
- `RootDock` — top-level container
- `DocumentDock` — hosts document tabs (our feature views)
- `ToolDock` — hosts dockable tool panels (sidebar, activity log, etc.)
- `DockableControl` — individual dockable item (wraps our UserControls)
- `IFactory` — creates and manages dock layout programmatically

```xml
<!-- MainWindow.axaml (simplified) -->
<DockControl Layout="{Binding Layout}">
    <!-- Dock handles all tab/panel rendering -->
</DockControl>
```

```csharp
// MainWindowViewModel manages the Dock layout
public class MainWindowViewModel : ViewModelBase
{
    public IRootDock Layout { get; set; }
    
    [RelayCommand]
    private void OpenDocument(string documentType)
    {
        // Create new document ViewModel, add to DocumentDock
        var factory = _dockFactory;
        var document = factory.CreateDocument(documentType);
        _documentDock.VisibleDockables?.Add(document);
        factory.SetActiveDockable(document);
    }
}
```

### 2. MVVM with CommunityToolkit.Mvvm

Use `CommunityToolkit.Mvvm` for:
- `[ObservableProperty]` source generator (eliminates INotifyPropertyChanged boilerplate)
- `[RelayCommand]` for command binding
- `ObservableObject` base class
- Messaging (`WeakReferenceMessenger`) for cross-ViewModel communication

Rationale: CommunityToolkit.Mvvm is simpler than ReactiveUI, uses familiar patterns (closer to existing WinForms code), and has excellent source generator support. ReactiveUI is more powerful but adds complexity that isn't needed here.

### 3. Dependency Injection Strategy

```csharp
// Program.cs / App.axaml.cs
public static void ConfigureServices(IServiceCollection services)
{
    // Existing services (from Common) — registered as singletons
    services.AddSingleton<IEmpireContext, EmpireContext>();
    services.AddSingleton<IPlayerContext, PlayerContext>();

    // Platform services
    services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
    services.AddSingleton<IFileSystemService, FileSystemService>();
    services.AddSingleton<IWindowStateService, WindowStateService>();

    // ViewModels — transient (one per tab instance)
    services.AddTransient<ColonyViewModel>();
    services.AddTransient<BlueprintViewModel>();

    // Navigation
    services.AddSingleton<NavigationService>();
}
```

Avalonia doesn't have built-in DI like MAUI, so we wire it up manually in `App.axaml.cs` using `Microsoft.Extensions.DependencyInjection`. ViewModels are resolved from the container when tabs are opened.


### 4. ViewModel Adaptation Strategy

The existing ViewModels (e.g., `ColonyViewModel`, `BlueprintViewModel`) wrap domain models and expose typed operations. For Avalonia:

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

// After (Avalonia)
public partial class ColonyViewModel : ViewModelBase
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
    string GetDataDirectory();      // %APPDATA% on Windows, ~/.local/share/ on Linux
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

For Avalonia, most of these can use Avalonia's built-in APIs directly:
- Clipboard: `TopLevel.GetTopLevel(control)?.Clipboard`
- File dialogs: `TopLevel.StorageProvider`
- Window state: `Window.Position`, `Window.Width/Height`, `Window.WindowState`

### 6. DataGrid Approach

Avalonia has a **built-in DataGrid** (`Avalonia.Controls.DataGrid`) that supports:
- Sortable columns (click header)
- Column resizing
- Row selection (single and multi)
- Cell editing (text, checkbox, combo box via `DataGridTemplateColumn`)
- Virtualization (handles large datasets)

This is a significant advantage over MAUI. The built-in DataGrid covers most WinForms DataGridView use cases. Custom cell templates handle the rest:

```xml
<!-- AXAML: Custom combo box column with filtering -->
<DataGridTemplateColumn Header="Type">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding TypeName}" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
    <DataGridTemplateColumn.CellEditingTemplate>
        <DataTemplate>
            <AutoCompleteBox Items="{Binding $parent[DataGrid].DataContext.AvailableTypes}"
                            Text="{Binding TypeName}" />
        </DataTemplate>
    </DataGridTemplateColumn.CellEditingTemplate>
</DataGridTemplateColumn>
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

Configure Serilog as the provider behind `ILogger<T>` for file output. Serilog has excellent cross-platform file sink support.

---

## Migration Mapping: Forms → Views

| WinForms Form | Avalonia View | Complexity | Phase |
|--------------|-----------|-----------|-------|
| FormAbout | AboutView | Low | 3 |
| FormHelp | HelpView | Low | 3 |
| FormPreferences | PreferencesView | Low | 3 |
| FormPlayerProfile | PlayerProfileView | Medium | 3 |
| FormSurvey | SurveyView | Medium | 3 |
| FormContacts | ContactsView | Low | 3 |
| FormColonyV2 | ColonyView | High | 4 |
| FormBlueprintV2 | BlueprintView | High | 4 |
| FormDeliveryRoute | DeliveryRouteView | High | 4 |
| FormDeliveryExecution | DeliveryExecutionView | High | 4 |
| FormShipTemplate | ShipTemplateView | Medium | 4 |
| FormShipInstance | ShipInstanceView | Medium | 4 |
| FormMarket | MarketView | Medium | 4 |
| FormStation | StationView | Medium | 4 |
| FormBuildPlanner | BuildPlannerView | High | 4 |
| FormColonyDailyBuild | ColonyDailyBuildView | Medium | 4 |
| FormColonyActivity | ColonyActivityView | Medium | 4 |
| FormSupplyChain | SupplyChainView | Medium | 4 |
| FormStockTargets | StockTargetsView | Medium | 4 |
| FormAsteroid | AsteroidView | Medium | 4 |
| FormSystem | SystemView | Medium | 4 |
| FormPricingPlan | PricingPlanView | Medium | 4 |
| MainWindow | MainWindow (shell) | High | 2 |


## Package Dependencies (Avalonia Project)

| Package | Purpose | Replaces |
|---------|---------|----------|
| Avalonia | UI framework | System.Windows.Forms |
| Avalonia.Desktop | Desktop platform support | — |
| Avalonia.Themes.Fluent | Modern theme (light + dark) | System theme |
| Avalonia.Controls.DataGrid | Built-in DataGrid | DataGridView |
| Dock.Avalonia | Docking layout system | MDI (MdiClient) |
| Dock.Model.Mvvm | Dock MVVM integration | — |
| Dock.Serializer.Newtonsoft | Layout persistence (JSON) | — |
| Dock.Avalonia.Themes.Fluent | Dock fluent theme | — |
| CommunityToolkit.Mvvm | MVVM source generators | Manual INotifyPropertyChanged |
| Microsoft.Extensions.DependencyInjection | DI container | Singleton pattern |
| Microsoft.Extensions.Logging | Logging abstraction | NLog |
| Serilog.Extensions.Logging | File logging provider | NLog file target |
| Serilog.Sinks.File | Log file output | NLog file target |
| Newtonsoft.Json | JSON serialization | Same (from Common) |
| Polly | Resilience (from Common) | Same |

## Event System Migration

Current WinForms uses `PlayerContext.*Changed` events with direct form subscriptions. Avalonia equivalent:

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
    // Refresh data on UI thread
    Dispatcher.UIThread.Post(() => RefreshColony(m.ColonyId));
});
```

## Avalonia-Specific Patterns

### View Locator
Avalonia uses a ViewLocator to automatically resolve Views for ViewModels:
```csharp
public class ViewLocator : IDataTemplate
{
    public Control Build(object data)
    {
        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);
        return type != null ? (Control)Activator.CreateInstance(type)! : new TextBlock { Text = name };
    }

    public bool Match(object data) => data is ViewModelBase;
}
```

### Reactive Extensions (optional)
Avalonia has deep ReactiveUI integration, but we'll use CommunityToolkit.Mvvm for simplicity. If reactive streams are needed later (e.g., debounced search), we can add `System.Reactive` without adopting full ReactiveUI.

## Testing Strategy

- Unit tests for ViewModels (mock services via DI) — standard NUnit, no UI dependency
- Avalonia headless testing (`Avalonia.Headless`) for control behavior tests
- Integration tests for platform services
- Existing Common tests remain unchanged
- UI automation tests (Avalonia has headless test support) — Phase 5
