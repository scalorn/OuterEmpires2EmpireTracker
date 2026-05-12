namespace OE2EmpireTracker.Interfaces
{
    /// <summary>
    /// Provides runtime-configured game constants that may vary by server/game version.
    /// Implemented by EmpireContext in the Tool project.
    /// </summary>
    public interface IGameConfig
    {
        int RefiningBaseRate { get; }

        decimal WorkerVolume { get; }

        int CommoditiesPerCycle { get; }

        long CommodityCycleSeconds { get; }

        int StructureCap { get; }
    }
}
