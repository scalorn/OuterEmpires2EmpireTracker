using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel. Manages the Dock layout, menu commands, and document creation.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly DockFactory _factory;

    [ObservableProperty]
    private IRootDock? _layout;

    private IDocumentDock? _documentDock;

    public MainWindowViewModel()
    {
        _factory = new DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _documentDock = _factory.DocumentDock;
    }

    /// <summary>
    /// Opens a document tab by type. If a document of that type already exists, focuses it.
    /// </summary>
    public void OpenDocument(string documentType, string title)
    {
        if (_documentDock is null)
        {
            return;
        }

        // Check if already open
        if (_documentDock.VisibleDockables is not null)
        {
            foreach (var existing in _documentDock.VisibleDockables)
            {
                if (existing is DocumentViewModel doc && doc.Title == title)
                {
                    _factory.SetActiveDockable(doc);
                    return;
                }
            }
        }

        // Create new document
        var newDoc = CreateDocument(documentType, title);
        if (newDoc is null)
        {
            return;
        }

        _factory.AddDockable(_documentDock, newDoc);
        _factory.SetActiveDockable(newDoc);
        _factory.SetFocusedDockable(_documentDock, newDoc);
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private static DocumentViewModel? CreateDocument(string documentType, string title)
    {
        return documentType switch
        {
            "About" => new AboutViewModel(),
            "ColonyList" => new ColonyViewModel(),
            "Help" => new HelpViewModel(),
            "Preferences" => new PreferencesViewModel(),
            "SystemList" => new SystemListViewModel(),
            "ColonyActivity" => new ColonyActivityViewModel(),
            "MarketList" => new MarketViewModel(),
            "AsteroidList" => new AsteroidViewModel(),
            "PricingPlanList" => new PricingPlanViewModel(),
            "SupplyChainList" => new SupplyChainViewModel(),
            "ContactsList" => new ContactsViewModel(),
            "ShipTemplateList" => new ShipTemplateViewModel(),
            "ShipList" => new ShipInstanceViewModel(),
            "StationList" => new StationViewModel(),
            "Profile" => new PlayerProfileViewModel(),
            "SurveyList" => new SurveyViewModel(),
            "DeliveryExecution" => new DeliveryExecutionViewModel(),
            "DeliveryList" => new DeliveryRouteViewModel(),
            "StockTargets" => new StockTargetsViewModel(),
            "BlueprintList" => new BlueprintViewModel(),
            _ => new PlaceholderViewModel(title),
        };
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var window = GetMainWindow();
        if (window is null)
        {
            return;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Player Data",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
            },
        });

        if (files.Count == 0)
        {
            return;
        }

        var file = files[0];
        var path = file.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        // Reload data from the selected file
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is not null)
        {
            dataService.LoadFromFile(path);
        }
    }

    [RelayCommand]
    private void OpenDocumentFromMenu(string? documentType)
    {
        if (string.IsNullOrEmpty(documentType))
        {
            return;
        }

        // Use the document type as both type and title for menu-opened docs
        var title = documentType switch
        {
            "ColonyList" => "Colonies",
            "BlueprintList" => "Blueprints",
            "SurveyList" => "Surveys",
            "ShipList" => "Ships",
            "DeliveryList" => "Delivery Routes",
            "MarketList" => "Market",
            "Profile" => "Profile",
            "SystemList" => "Systems",
            "Help" => "Help",
            "About" => "About",
            _ => documentType,
        };

        OpenDocument(documentType, title);
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
