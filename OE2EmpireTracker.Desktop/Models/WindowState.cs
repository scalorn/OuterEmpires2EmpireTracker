namespace OE2EmpireTracker.Desktop.Models;

/// <summary>
/// Persisted window position, size, and maximized state.
/// </summary>
public sealed class SavedWindowState
{
    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; } = 1280;

    public double Height { get; set; } = 800;

    public bool IsMaximized { get; set; }
}
