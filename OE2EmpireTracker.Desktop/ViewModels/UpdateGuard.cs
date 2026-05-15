using System;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Equivalent of the WinForms ProgrammaticUpdateGuard for ViewModels.
/// Suppresses property change notifications during bulk updates.
/// Usage: using var guard = new UpdateGuard(viewModel);
/// </summary>
public readonly struct UpdateGuard : IDisposable
{
    private readonly IUpdatable _target;

    public UpdateGuard(IUpdatable target)
    {
        _target = target;
        _target.BeginUpdate();
    }

    public void Dispose()
    {
        _target.EndUpdate();
    }
}

/// <summary>
/// Interface for ViewModels that support programmatic update suppression.
/// </summary>
public interface IUpdatable
{
    /// <summary>Gets whether a programmatic update is in progress.</summary>
    bool IsUpdating { get; }

    /// <summary>Begins a programmatic update (increments counter).</summary>
    void BeginUpdate();

    /// <summary>Ends a programmatic update (decrements counter).</summary>
    void EndUpdate();
}
