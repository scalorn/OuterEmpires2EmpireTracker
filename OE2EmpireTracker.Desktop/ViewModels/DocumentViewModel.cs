using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Mvvm.Controls;
using OE2EmpireTracker.Desktop.ViewModels.Messages;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Base class for document ViewModels that live in the Dock document area.
/// Inherits from Dock's Document class which provides docking behavior.
/// Provides message subscription infrastructure so derived classes can
/// refresh when data changes.
/// </summary>
public abstract class DocumentViewModel : Document
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentViewModel"/> class.
    /// Registers for <see cref="PlayerChangedMessage"/> by default so all
    /// document tabs refresh when the active player switches.
    /// </summary>
    protected DocumentViewModel()
    {
        WeakReferenceMessenger.Default.Register<PlayerChangedMessage>(this, (r, m) =>
        {
            ((DocumentViewModel)r).OnPlayerChanged(m.playerUuid);
        });
    }

    /// <summary>
    /// Called when the current player changes. Default implementation calls
    /// <see cref="RefreshData"/>. Override to customize behavior.
    /// </summary>
    /// <param name="playerUuid">The new current player UUID.</param>
    protected virtual void OnPlayerChanged(string playerUuid)
    {
        RefreshData();
    }

    /// <summary>
    /// Reloads data from the service layer. Override in derived classes to
    /// clear and repopulate collections, reset selection, etc.
    /// </summary>
    protected virtual void RefreshData()
    {
    }
}
