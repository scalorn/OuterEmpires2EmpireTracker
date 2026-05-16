using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Models;
using OE2EmpireTracker.Desktop.Services;

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

        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 380,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
        };

        panel.Children.Add(new TextBlock
        {
            Text = "You have unsaved changes. Are you sure you want to exit?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
        };

        var yesButton = new Button { Content = "Yes, Exit" };
        var noButton = new Button { Content = "Cancel" };

        yesButton.Click += (_, _) => dialog.Close(true);
        noButton.Click += (_, _) => dialog.Close(false);

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        var result = await dialog.ShowDialog<bool?>(this);
        if (result == true)
        {
            dataService.IsDirty = false;
            Close();
        }
    }

    private void OnWindowPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            _lastNormalPosition = e.Point;
        }
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
