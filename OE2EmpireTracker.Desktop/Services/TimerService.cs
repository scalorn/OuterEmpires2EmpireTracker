using System;
using Avalonia.Threading;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Timer service for background processing (mining cycles, refining, research, etc.).
/// Uses Avalonia's DispatcherTimer for UI-thread callbacks.
/// </summary>
public interface ITimerService
{
    /// <summary>Gets whether the timer is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Starts the background processing timer.</summary>
    void Start(TimeSpan interval, Action callback);

    /// <summary>Stops the background processing timer.</summary>
    void Stop();
}

/// <summary>
/// Avalonia DispatcherTimer-based timer service.
/// Callbacks execute on the UI thread.
/// </summary>
public sealed class TimerService : ITimerService
{
    private DispatcherTimer? _timer;
    private Action? _callback;

    public bool IsRunning => _timer?.IsEnabled ?? false;

    public void Start(TimeSpan interval, Action callback)
    {
        Stop();
        _callback = callback;
        _timer = new DispatcherTimer
        {
            Interval = interval,
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }

        _callback = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _callback?.Invoke();
    }
}
