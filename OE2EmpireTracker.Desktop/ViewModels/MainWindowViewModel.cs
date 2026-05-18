using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel. Manages the Dock layout, menu commands, and document creation.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly DockFactory _factory;
    private readonly DispatcherTimer _statusTimer;

    [ObservableProperty]
    private IRootDock? _layout;

    [ObservableProperty]
    private PlayerProfile? _selectedPlayer;

    [ObservableProperty]
    private string _windowTitle = "OE2 Empire Tracker \u2014 New";

    [ObservableProperty]
    private string _nextProcessCountdown = "Next: --s";

    [ObservableProperty]
    private string _memoryUsage = "Memory: 0 MB";

    [ObservableProperty]
    private bool _lastCycleHadError;

    [ObservableProperty]
    private IBrush _statusForeground = Brushes.Gray;

    private IDocumentDock? _documentDock;

    public MainWindowViewModel()
    {
        _factory = new DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _documentDock = _factory.DocumentDock;
        LoadPlayerProfiles();
        UpdateWindowTitle();

        // 1-second UI timer for status bar updates
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _statusTimer.Tick += OnStatusTimerTick;
        _statusTimer.Start();
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

    /// <summary>
    /// Updates the window title based on the current file path.
    /// </summary>
    public void UpdateWindowTitle()
    {
        var dataService = GetService<DataService>();
        if (dataService?.CurrentFilePath is not null)
        {
            var fileName = Path.GetFileName(dataService.CurrentFilePath);
            WindowTitle = $"OE2 Empire Tracker \u2014 {fileName}";
        }
        else
        {
            WindowTitle = "OE2 Empire Tracker \u2014 New";
        }
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

    private static T? GetService<T>()
        where T : class
    {
        return App.Services?.GetService<T>();
    }

    private static DocumentViewModel? CreateDocument(string documentType, string title)
    {
        return documentType switch
        {
            "About" => new AboutViewModel(),
            "AsteroidList" => new AsteroidViewModel(),
            "BlueprintList" => new BlueprintViewModel(),
            "BuildPlanList" => new BuildPlannerViewModel(),
            "ColonyList" => new ColonyViewModel(),
            "ColonyActivity" => new ColonyActivityViewModel(),
            "DailyBuild" => new ColonyDailyBuildViewModel(),
            "ContactsList" => new ContactsViewModel(),
            "DeliveryExecution" => new DeliveryExecutionViewModel(),
            "DeliveryList" => new DeliveryRouteViewModel(),
            "Help" => new HelpViewModel(),
            "MarketList" => new MarketViewModel(),
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

    /// <summary>Closes all open document tabs (B3: Window menu).</summary>
    [RelayCommand]
    private void CloseAllTabs()
    {
        if (_documentDock?.VisibleDockables is null)
        {
            return;
        }

        var dockables = _documentDock.VisibleDockables.ToList();
        foreach (var dockable in dockables)
        {
            _factory.RemoveDockable(dockable, collapse: false);
        }
    }

    [RelayCommand]
    private void NewFile()
    {
        var dataService = GetService<DataService>();
        if (dataService is null)
        {
            return;
        }

        dataService.NewContext();

        // Clear document tabs
        if (_documentDock?.VisibleDockables is not null)
        {
            var dockables = _documentDock.VisibleDockables.ToList();
            foreach (var dockable in dockables)
            {
                _factory.RemoveDockable(dockable, collapse: false);
            }
        }

        // Clear player profiles
        PlayerProfiles.Clear();
        SelectedPlayer = null;

        // Update config
        var configService = GetService<AppConfigService>();
        if (configService is not null)
        {
            configService.LastOpenedPath = null;
        }

        UpdateWindowTitle();
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        var dataService = GetService<DataService>();
        if (dataService is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(dataService.CurrentFilePath))
        {
            dataService.WriteContext();
        }
        else
        {
            await SaveFileAsAsync();
        }
    }

    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        var window = GetMainWindow();
        if (window is null)
        {
            return;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Player Data",
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
            },
        });

        if (file is null)
        {
            return;
        }

        var path = file.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        var dataService = GetService<DataService>();
        if (dataService is null)
        {
            return;
        }

        dataService.CurrentFilePath = path;
        dataService.WriteContext();

        var configService = GetService<AppConfigService>();
        if (configService is not null)
        {
            configService.LastOpenedPath = path;
        }

        UpdateWindowTitle();
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

        var dataService = GetService<DataService>();
        if (dataService is not null)
        {
            dataService.LoadFromFile(path);
            LoadPlayerProfiles();

            var configService = GetService<AppConfigService>();
            if (configService is not null)
            {
                configService.LastOpenedPath = path;
            }

            UpdateWindowTitle();
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
            "AsteroidList" => "Asteroids",
            "BlueprintList" => "Blueprints",
            "BuildPlanList" => "Build Plans",
            "ColonyList" => "Colonies",
            "ColonyActivity" => "Colony Activity",
            "DailyBuild" => "Daily Build",
            "ContactsList" => "Contacts",
            "DeliveryList" => "Delivery Routes",
            "MarketList" => "Market",
            "Preferences" => "Preferences",
            "PricingPlanList" => "Pricing Plans",
            "Profile" => "Profile",
            "ShipList" => "Ships",
            "StationList" => "Stations",
            "StockTargets" => "Stock Targets",
            "SupplyChainList" => "Supply Chains",
            "SurveyList" => "Surveys",
            "SystemList" => "Systems",
            "Help" => "Help",
            "About" => "About",
            _ => documentType,
        };

        OpenDocument(documentType, title);
    }

    partial void OnSelectedPlayerChanged(PlayerProfile? value)
    {
        if (value is null)
        {
            return;
        }

        var dataService = GetService<DataService>();
        if (dataService is not null)
        {
            dataService.SetCurrentPlayer(value.UUID);
        }
    }

    private void LoadPlayerProfiles()
    {
        var dataService = GetService<DataService>();
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

    private void OnStatusTimerTick(object? sender, EventArgs e)
    {
        var processor = GetService<BackgroundProcessor>();
        if (processor is not null && processor.IsRunning)
        {
            var remaining = processor.NextProcessTime - OE2EmpireTracker.Services.SystemClock.UtcNow;
            int seconds = Math.Max(0, (int)remaining.TotalSeconds);
            NextProcessCountdown = $"Next: {seconds}s";
            LastCycleHadError = processor.LastCycleHadError;
            StatusForeground = processor.LastCycleHadError ? Brushes.Red : Brushes.Gray;
        }
        else
        {
            NextProcessCountdown = "Next: --s";
            StatusForeground = Brushes.Gray;
        }

        long memoryMb = GC.GetTotalMemory(false) / 1024 / 1024;
        MemoryUsage = $"Memory: {memoryMb} MB";
    }
}
