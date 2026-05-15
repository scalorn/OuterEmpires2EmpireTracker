using Avalonia.Controls;
using OE2EmpireTracker.Desktop.ViewModels;

namespace OE2EmpireTracker.Desktop.Views;

public partial class SurveyView : UserControl
{
    public SurveyView()
    {
        InitializeComponent();
    }

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is SurveyViewModel vm)
        {
            vm.MarkDirtyCommand.Execute(null);
        }
    }
}
