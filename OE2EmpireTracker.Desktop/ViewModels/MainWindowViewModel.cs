using System.Collections.ObjectModel;
using System.Linq;
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
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel. Manages the Dock layout, menu commands, and document creation.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly DockFactory _factory;

    [ObservableProperty]
    private IRootDock? _layout;

    [ObservableProperty]
    private PlayerProfile? _selectedPlayer;

    private IDocumentDock? _documentDock;

    public MainWindowViewModel()
    {
        _factory = new DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _documentDock = _factory.DocumentDock;
        LoadPlayerProfiles();
    }

    public ObservableCollection<PlayerProfile> PlayerProfiles { get; } = new ObservableCollection<PlayerProfile>();

    /// <summary>
    /// Opens a document tab by type. If a document of that type already exists, focuses it.
    /// </summary>
    public void OpenDocument(string documentType, string title)
    {
        if (_documentDock is null)
        {
            return;
        }

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

        var newDoc = CreateDocument(documentType, title);
        if (newDoc is null)
        {
            return;
        }

        _factory.AddDockable(_documentDock, newDoc);
        _factory.SetActiveDockable(newDoc);
        _factory.SetFocusedDockable(_documentDock, newDoc);
    }

    [RelayCommand]
    private static void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
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
            "BlueprintList" => new BlueprintViewModel(),
            "ColonyList" => new ColonyViewModel(),
            "ColonyActivity" => new ColonyActivityViewModel(),
            "ContactsList" => new ContactsViewModel(),
            "DeliveryExecution" => new DeliveryExecutionViewModel(),
            "DeliveryList" => new DeliveryRouteViewModel(),
            "Help" => new HelpViewModel(),
            "MarketList" => new MarketViewModel(),
            "AsteroidList" => new AsteroidViewModel(),
            "Preferences" => new PreferencesViewModel(),
            "PricingPlanList" => new PricingPlanViewModel(),
            "Profile" => new PlayerProfileViewModel(),
            "ShipList" => new ShipInstanceViewModel(),
            "ShipTemplateList" => new ShipTemplateViewModel(),
            "StationList" => new StationViewModel(),
            "StockTargets" => new StockTargetsViewModel(),
            "SupplyChainList" => new SupplyChainViewModel(),
            "SurveyList" => new SurveyViewModel(),
            "SystemList" => new SystemListViewModel(),
            _ => new PlaceholderViewModel(title),
        };
    }

    partial void OnSelectedPlayerChanged(PlayerProfile? value)
    {
        if (value is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is not null)
        {
            dataService.SetCurrentPlayer(value.UUID);
        }
    }

    private void LoadPlayerProfiles()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        PlayerProfiles.Clear();
        foreach (var profile in dataService.PlayerProfiles)
        {
            PlayerProfiles.Add(profile);
        }

        var current = PlayerProfiles.FirstOrDefault(p => p.UUID == dataService.CurrentPlayerUUID);
        if (current is not null)
        {
            SelectedPlayer = current;
        }
        else if (PlayerProfiles.Count > 0)
        {
            SelectedPlayer = PlayerProfiles[0];
        }
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

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is not null)
        {
            dataService.LoadFromFile(path);
            LoadPlayerProfiles();
        }
    }

    [RelayCommand]
    private void OpenDocumentFromMenu(string? documentType)
    {
        if (string.IsNullOrEmpty(documentType))
        {
            return;
        }

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
}
