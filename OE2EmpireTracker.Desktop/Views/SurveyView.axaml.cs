using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels;
using ScottPlot.Avalonia;

namespace OE2EmpireTracker.Desktop.Views;

public partial class SurveyView : UserControl
{
    public SurveyView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SurveyViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SurveyViewModel.SelectedCombo))
        {
            Dispatcher.UIThread.Post(UpdateDistributionChart);
        }
    }

    private void UpdateDistributionChart()
    {
        var plot = this.FindControl<AvaPlot>("DistributionPlot");
        if (plot is null)
        {
            return;
        }

        plot.Plot.Clear();

        if (DataContext is not SurveyViewModel vm || vm.SelectedCombo is null)
        {
            plot.Refresh();
            return;
        }

        var points = vm.DistributionPoints;
        if (points.Count == 0)
        {
            plot.Plot.Title("Insufficient data (need 2+ yields)");
            plot.Refresh();
            return;
        }

        // Build bar chart data
        double[] positions = points.Select(p => p.BinMidpoint).ToArray();
        double[] values = points.Select(p => p.Percentage).ToArray();

        var bars = plot.Plot.Add.Bars(positions, values);
        bars.LegendText = $"{vm.SelectedCombo.ResourceName} ({vm.SelectedCombo.Purity})";

        plot.Plot.Title($"Yield Distribution — {vm.SelectedCombo.ResourceName} ({vm.SelectedCombo.Purity})");
        plot.Plot.XLabel("Yield");
        plot.Plot.YLabel("Percentage (%)");

        plot.Refresh();
    }
}
