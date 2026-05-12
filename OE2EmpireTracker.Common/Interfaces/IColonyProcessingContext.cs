using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Interfaces
{
    /// <summary>
    /// Provides data lookups needed by Colony.ProcessColony().
    /// Implemented by PlayerContext/EmpireContext in the Tool project.
    /// </summary>
    public interface IColonyProcessingContext
    {
        /// <summary>
        /// Finds a blueprint by UUID.
        /// </summary>
        ReadOnlyBlueprint FindBlueprint(string uuid);

        /// <summary>
        /// Finds a survey by UUID.
        /// </summary>
        Survey FindSurvey(string uuid);

        /// <summary>
        /// Finds a player profile by UUID.
        /// </summary>
        PlayerProfile FindPlayerProfile(string ownerUUID);

        /// <summary>
        /// Finds a blueprint type by type name.
        /// </summary>
        BlueprintType FindBlueprintType(string typeName);

        /// <summary>
        /// Adds a newly created blueprint (e.g. from research evolution).
        /// </summary>
        void AddBlueprint(Blueprint blueprint);
    }
}
