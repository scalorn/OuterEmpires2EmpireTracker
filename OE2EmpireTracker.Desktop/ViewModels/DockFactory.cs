using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Factory that creates the initial Dock layout with a navigation sidebar and document area.
/// </summary>
public sealed class DockFactory : Factory
{
    private readonly MainWindowViewModel _main;

    public DockFactory(MainWindowViewModel main)
    {
        _main = main;
    }

    /// <summary>
    /// Gets the document dock for adding new documents.
    /// </summary>
    public IDocumentDock? DocumentDock { get; private set; }

    public override IRootDock CreateLayout()
    {
        // Navigation sidebar (tool panel)
        var navigation = new NavigationViewModel(_main);

        // Tool dock on the left
        var toolDock = new ToolDock
        {
            ActiveDockable = navigation,
            VisibleDockables = CreateList<IDockable>(navigation),
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible,
        };

        // Document dock in the center (starts with Colony list open)
        var colonyList = new ColonyListViewModel();
        var documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = colonyList,
            VisibleDockables = CreateList<IDockable>(colonyList),
            CanCreateDocument = false,
        };
        DocumentDock = documentDock;

        // Proportional layout: sidebar (250px) | documents (fill)
        var layout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                toolDock,
                new ProportionalDockSplitter(),
                documentDock),
        };

        // Root dock
        var rootDock = new RootDock
        {
            ActiveDockable = layout,
            DefaultDockable = layout,
            VisibleDockables = CreateList<IDockable>(layout),
        };

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["ColonyList"] = () => layout,
            ["About"] = () => layout,
        };

        DockableLocator = new Dictionary<string, Func<IDockable?>>
        {
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };

        base.InitLayout(layout);
    }
}
