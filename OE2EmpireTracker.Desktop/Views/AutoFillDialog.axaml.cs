using System.ComponentModel;
using Avalonia.Controls;
using OE2EmpireTracker.Desktop.ViewModels;

namespace OE2EmpireTracker.Desktop.Views;

/// <summary>
/// Dialog window for auto-filling delivery plan items from build plan shortfalls.
/// </summary>
public partial class AutoFillDialog : Window
{
    public AutoFillDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AutoFillDialogViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AutoFillDialogViewModel.Confirmed))
        {
            Close();
        }
    }
}
