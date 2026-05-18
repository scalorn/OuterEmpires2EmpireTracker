using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Models;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels;
using OE2EmpireTracker.Desktop.ViewModels.Messages;

namespace OE2EmpireTracker.Desktop.Views;

public partial class MainWindow : Window
{
    private const double MinWindowWidth = 320;
    private const double MinWindowHeight = 200;

    /// <summary>
    /// Tracks the last known normal-state position and size so we can persist
    /// them even when the window is currently maximized.
    /// </summary>
    private PixelPoint _lastNormalPosition;
    private double _lastNormalWidth;
    private double _lastNormalHeight;

    public MainWindow()
    {
        InitializeComponent();
        PositionChanged += OnWindowPositionChanged;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RestoreWindowState();

        // Capture initial normal bounds
        _lastNormalPosition = Position;
        _lastNormalWidth = Bounds.Width;
        _lastNormalHeight = Bounds.Height;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage());
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            OpenContextSensitiveHelp();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Track normal-state bounds whenever the window is not maximized
        if (change.Property == WindowStateProperty)
        {
            if (WindowState == Avalonia.Controls.WindowState.Normal)
            {
                _lastNormalPosition = Position;
                _lastNormalWidth = Bounds.Width;
                _lastNormalHeight = Bounds.Height;
            }
        }
        else if (change.Property == BoundsProperty
            && WindowState == Avalonia.Controls.WindowState.Normal)
        {
            _lastNormalWidth = Bounds.Width;
            _lastNormalHeight = Bounds.Height;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        SaveWindowState();

        var dataService = App.Services?.GetService<DataService>();
        if (dataService is null || !dataService.IsDirty)
        {
            return;
        }

        e.Cancel = true;

        var result = await ShowUnsavedChangesDialog();

        if (result == "Save")
        {
            dataService.WriteContext();
            Close();
        }
        else if (result == "Discard")
        {
            dataService.IsDirty = false;
            Close();
        }

        // "Cancel" — do nothing, stay open
    }

    private async System.Threading.Tasks.Task<string> ShowUnsavedChangesDialog()
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 400,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        string dialogResult = "Cancel";

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
        };

        panel.Children.Add(new TextBlock
        {
            Text = "You have unsaved changes. What would you like to do?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
        };

        var saveButton = new Button { Content = "Save" };
        var discardButton = new Button { Content = "Discard" };
        var cancelButton = new Button { Content = "Cancel" };

        saveButton.Click += (_, _) =>
        {
            dialogResult = "Save";
            dialog.Close();
        };

        discardButton.Click += (_, _) =>
        {
            dialogResult = "Discard";
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            dialogResult = "Cancel";
            dialog.Close();
        };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(discardButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return dialogResult;
    }

    private void OnWindowPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            _lastNormalPosition = e.Point;
        }
    }

    private void OpenContextSensitiveHelp()
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm is null)
        {
            return;
        }

        // Determine the active document type from the current tab
        string? documentType = GetActiveDocumentType(vm);

        // Look up the help topic
        var registry = App.Services?.GetService<HelpTopicRegistry>();
        string? topicFileName = null;
        if (registry is not null && documentType is not null)
        {
            topicFileName = registry.GetTopicForDocumentType(documentType);
        }

        // Open the Help view and navigate to the topic
        vm.OpenDocument("Help", "Help");

        if (topicFileName is not null)
        {
            // Find the Help document and navigate to the topic
            NavigateHelpToTopic(vm, topicFileName);
        }
    }

    private string? GetActiveDocumentType(MainWindowViewModel vm)
    {
        // Map ViewModel types to document type strings
        var layout = vm.Layout;
        if (layout?.ActiveDockable is DocumentViewModel activeDoc)
        {
            return activeDoc switch
            {
                ColonyViewModel => "ColonyList",
                ColonyListViewModel => "ColonyList",
                BlueprintViewModel => "BlueprintList",
                SurveyViewModel => "SurveyList",
                ShipInstanceViewModel => "ShipList",
                ShipTemplateViewModel => "ShipTemplateList",
                DeliveryRouteViewModel => "DeliveryList",
                DeliveryExecutionViewModel => "DeliveryExecution",
                MarketViewModel => "MarketList",
                PlayerProfileViewModel => "Profile",
                SystemListViewModel => "SystemList",
                StationViewModel => "StationList",
                BuildPlannerViewModel => "BuildPlanList",
                StockTargetsViewModel => "StockTargets",
                SupplyChainViewModel => "SupplyChainList",
                ContactsViewModel => "ContactsList",
                PricingPlanViewModel => "PricingPlanList",
                PreferencesViewModel => "Preferences",
                ColonyActivityViewModel => "ColonyActivity",
                AsteroidViewModel => "AsteroidList",
                _ => null,
            };
        }

        return null;
    }

    private void NavigateHelpToTopic(MainWindowViewModel vm, string topicFileName)
    {
        // Find the Help ViewModel in the dock and navigate to the topic
        var layout = vm.Layout;
        if (layout is null)
        {
            return;
        }

        // Search through all dockables for the HelpViewModel
        var helpVm = FindHelpViewModel(layout);
        helpVm?.NavigateToTopic(topicFileName);
    }

    private HelpViewModel? FindHelpViewModel(IDockable dockable)
    {
        if (dockable is HelpViewModel help)
        {
            return help;
        }

        if (dockable is IDocumentDock docDock && docDock.VisibleDockables is not null)
        {
            foreach (var child in docDock.VisibleDockables)
            {
                var found = FindHelpViewModel(child);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        if (dockable is IRootDock rootDock && rootDock.VisibleDockables is not null)
        {
            foreach (var child in rootDock.VisibleDockables)
            {
                var found = FindHelpViewModel(child);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        if (dockable is IProportionalDock propDock && propDock.VisibleDockables is not null)
        {
            foreach (var child in propDock.VisibleDockables)
            {
                var found = FindHelpViewModel(child);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private void RestoreWindowState()
    {
        var prefs = App.Services?.GetService<PreferencesStore>();
        if (prefs is null)
        {
            return;
        }

        var state = prefs.WindowState;
        var logger = App.Services?.GetService<ILogger<MainWindow>>();

        // Validate bounds
        if (state.Width < MinWindowWidth || state.Height < MinWindowHeight)
        {
            logger?.LogDebug("Saved window size too small, using defaults");
            return;
        }

        // Check if the saved position is on-screen
        var screens = Screens;
        if (screens is null || screens.All.Count == 0)
        {
            return;
        }

        bool isOnScreen = false;
        var savedRect = new PixelRect(
            (int)state.Left,
            (int)state.Top,
            (int)state.Width,
            (int)state.Height);

        foreach (var screen in screens.All)
        {
            if (screen.WorkingArea.Intersects(savedRect))
            {
                isOnScreen = true;
                break;
            }
        }

        if (!isOnScreen)
        {
            logger?.LogDebug("Saved window position is off-screen, using defaults");
            return;
        }

        // Apply saved state
        Position = new PixelPoint((int)state.Left, (int)state.Top);
        Width = state.Width;
        Height = state.Height;

        if (state.IsMaximized)
        {
            WindowState = Avalonia.Controls.WindowState.Maximized;
        }

        logger?.LogDebug(
            "Restored window state: {Left},{Top} {Width}x{Height} Maximized={Max}",
            state.Left,
            state.Top,
            state.Width,
            state.Height,
            state.IsMaximized);
    }

    private void SaveWindowState()
    {
        var prefs = App.Services?.GetService<PreferencesStore>();
        if (prefs is null)
        {
            return;
        }

        var state = new SavedWindowState();

        if (WindowState == Avalonia.Controls.WindowState.Maximized)
        {
            state.IsMaximized = true;
            state.Left = _lastNormalPosition.X;
            state.Top = _lastNormalPosition.Y;
            state.Width = _lastNormalWidth;
            state.Height = _lastNormalHeight;
        }
        else if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            state.IsMaximized = false;
            state.Left = Position.X;
            state.Top = Position.Y;
            state.Width = Bounds.Width;
            state.Height = Bounds.Height;
        }
        else
        {
            // Minimized - don't save
            return;
        }

        prefs.SaveWindowState(state);
    }
}
