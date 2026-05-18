using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OE2EmpireTracker.Desktop.Views;

public partial class ColonyDailyBuildView : UserControl
{
    public ColonyDailyBuildView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
