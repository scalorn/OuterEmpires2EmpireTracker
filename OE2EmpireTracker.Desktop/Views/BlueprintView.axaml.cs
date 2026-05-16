using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels;
using ScottPlot.Avalonia;

namespace OE2EmpireTracker.Desktop.Views;

public partial class BlueprintView : UserControl
{
    public BlueprintView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private static string? FindFirstNumericProperty(OE2EmpireTracker.Models.Blueprint bp)
    {
        if (bp.Properties?.Properties is null)
        {
            return null;
        }

        foreach (var key in bp.Properties.Properties.Keys)
        {
            if (key.StartsWith("_"))
            {
                continue;
            }

            bp.Properties.GetString(key, string.Empty, out string val);
            if (double.TryParse(val, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return key;
            }
        }

        return null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is BlueprintViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintViewModel.SelectedBlueprint))
        {
            Dispatcher.UIThread.Post(UpdateEvolutionChart);
        }
    }

    private void UpdateEvolutionChart()
    {
        var plot = this.FindControl<AvaPlot>("EvolutionPlot");
        if (plot is null)
        {
            return;
        }

        plot.Plot.Clear();

        if (DataContext is not BlueprintViewModel vm || vm.SelectedBlueprint is null)
        {
            plot.Refresh();
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            plot.Refresh();
            return;
        }

        // Find all evolutions of this blueprint (same name, different evolution numbers)
        string bpName = vm.SelectedBlueprint.Name;
        string bpType = vm.SelectedBlueprint.BlueprintType;
        int bpClass = vm.SelectedBlueprint.ShipClass;

        var evolutions = dataService.GetCurrentPlayerBlueprints()
            .Where(b => b.Name == bpName && b.BluePrintType == bpType && b.Class == bpClass)
            .OrderBy(b => b.Evolution)
            .ToList();

        if (evolutions.Count < 2)
        {
            plot.Refresh();
            return;
        }

        // Find the first numeric property to plot
        var firstBp = evolutions[0];
        string? plotProperty = FindFirstNumericProperty(firstBp);
        if (plotProperty is null)
        {
            plot.Refresh();
            return;
        }

        // Collect data points: evolution number → property value
        var xValues = new List<double>();
        var yValues = new List<double>();

        foreach (var bp in evolutions)
        {
            bp.Properties.GetString(plotProperty, "0", out string valStr);
            if (double.TryParse(valStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                xValues.Add(bp.Evolution);
                yValues.Add(val);
            }
        }

        if (xValues.Count < 2)
        {
            plot.Refresh();
            return;
        }

        // Plot the data
        var scatter = plot.Plot.Add.Scatter(xValues.ToArray(), yValues.ToArray());
        scatter.LineWidth = 2;
        scatter.MarkerSize = 8;

        plot.Plot.Title($"{bpName} — {plotProperty}");
        plot.Plot.XLabel("Evolution");
        plot.Plot.YLabel(plotProperty);

        plot.Refresh();
    }
}
