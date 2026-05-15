using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

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
            Margin = new Avalonia.Thickness(20),
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
}
