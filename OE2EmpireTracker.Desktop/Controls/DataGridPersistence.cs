using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.Controls;

/// <summary>
/// Attached property behavior that auto-saves and restores DataGrid column widths.
/// Usage in AXAML: ctrl:DataGridPersistence.GridId="ColonyStructures"
/// When GridId is set, the behavior:
/// - On Loaded: restores saved column widths from GridStateService
/// - On Unloaded: saves current widths to GridStateService
/// - On ColumnDisplayIndexChanged: saves current widths to GridStateService
/// </summary>
public static class DataGridPersistence
{
    public static readonly AttachedProperty<string> GridIdProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, string>(
            "GridId", typeof(DataGridPersistence), string.Empty);

    static DataGridPersistence()
    {
        GridIdProperty.Changed.AddClassHandler<DataGrid>(OnGridIdChanged);
    }

    public static string GetGridId(DataGrid grid)
    {
        return grid.GetValue(GridIdProperty);
    }

    public static void SetGridId(DataGrid grid, string value)
    {
        grid.SetValue(GridIdProperty, value);
    }

    private static void OnGridIdChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs e)
    {
        string gridId = e.NewValue as string ?? string.Empty;
        if (string.IsNullOrEmpty(gridId))
        {
            return;
        }

        grid.Loaded += (_, _) => RestoreColumnWidths(grid, gridId);
        grid.Unloaded += (_, _) => SaveColumnWidths(grid, gridId);
        grid.ColumnDisplayIndexChanged += (_, _) => SaveColumnWidths(grid, gridId);
    }

    private static void RestoreColumnWidths(DataGrid grid, string gridId)
    {
        var service = App.Services?.GetService<GridStateService>();
        if (service is null)
        {
            return;
        }

        var saved = service.Load(gridId);
        if (saved is null)
        {
            return;
        }

        foreach (var col in grid.Columns)
        {
            string header = col.Header?.ToString() ?? string.Empty;
            if (saved.TryGetValue(header, out double width) && width > 0)
            {
                col.Width = new DataGridLength(width);
            }
        }
    }

    private static void SaveColumnWidths(DataGrid grid, string gridId)
    {
        var service = App.Services?.GetService<GridStateService>();
        if (service is null)
        {
            return;
        }

        var widths = new Dictionary<string, double>();
        foreach (var col in grid.Columns)
        {
            string header = col.Header?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(header))
            {
                widths[header] = col.ActualWidth;
            }
        }

        if (widths.Count > 0)
        {
            service.Save(gridId, widths);
        }
    }
}
