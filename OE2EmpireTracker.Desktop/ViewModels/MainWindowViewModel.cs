using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel. Manages the Dock layout and document creation.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly DockFactory _factory;

    [ObservableProperty]
    private IRootDock? _layout;

    private IDocumentDock? _documentDock;

    public MainWindowViewModel()
    {
        _factory = new DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout);
        _documentDock = _factory.DocumentDock;
    }

    /// <summary>
    /// Opens a document tab by type. If a document of that type already exists, focuses it.
    /// </summary>
    public void OpenDocument(string documentType, string title)
    {
        if (_documentDock is null)
        {
            return;
        }

        // Check if already open
        if (_documentDock.VisibleDockables is not null)
        {
            foreach (var existing in _documentDock.VisibleDockables)
            {
                if (existing is DocumentViewModel doc && doc.Title == title)
                {
                    _factory.SetActiveDockable(doc);
                    return;
                }
            }
        }

        // Create new document
        var newDoc = CreateDocument(documentType, title);
        if (newDoc is null)
        {
            return;
        }

        _factory.AddDockable(_documentDock, newDoc);
        _factory.SetActiveDockable(newDoc);
        _factory.SetFocusedDockable(_documentDock, newDoc);
    }

    private static DocumentViewModel? CreateDocument(string documentType, string title)
    {
        return documentType switch
        {
            "About" => new AboutViewModel(),
            "ColonyList" => new ColonyListViewModel(),
            _ => new PlaceholderViewModel(title),
        };
    }
}
