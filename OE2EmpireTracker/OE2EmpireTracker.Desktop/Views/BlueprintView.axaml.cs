using Avalonia.Controls;
using OE2EmpireTracker.Desktop.ViewModels;

namespace OE2EmpireTracker.Desktop.Views;

public partial class BlueprintView : UserControl
{
    public BlueprintView()
    {
        InitializeComponent();
    }

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is BlueprintViewModel vm)
        {
            vm.MarkDirtyCommand.Execute(null);
        }
    }
}
