using Avalonia.Controls;
using OE2EmpireTracker.Desktop.ViewModels;

namespace OE2EmpireTracker.Desktop.Views;

public partial class PlayerProfileView : UserControl
{
    public PlayerProfileView()
    {
        InitializeComponent();
    }

    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is PlayerProfileViewModel vm)
        {
            vm.MarkSkillsDirtyCommand.Execute(null);
        }
    }
}
