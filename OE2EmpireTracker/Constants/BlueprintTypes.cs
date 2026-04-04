using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Constants for blueprint type string prefixes to avoid magic strings.
    /// </summary>
    public static class BlueprintTypePrefixes
    {
        /// <summary>Flatpacks prefix for all flatpack blueprints</summary>
        public const string Flatpacks = "Flatpacks/";
    }

    /// <summary>
    /// Constants for specific blueprint type names to avoid magic strings.
    /// </summary>
    /// <remarks>
    /// Use these constants instead of hardcoding string literals like "Flatpacks/MiningRig"
    /// throughout the codebase. This improves maintainability and prevents typos.
    /// </remarks>
    public static class BlueprintTypes
    {
        /// <summary>Mining rig structure type</summary>
        public const string MiningRig = "Flatpacks/MiningRig";

        /// <summary>Refinery structure type</summary>
        public const string Refinery = "Flatpacks/Refinery";

        /// <summary>Research laboratory structure type</summary>
        public const string ResearchLaboratory = "Flatpacks/ResearchLaboratory";

        // Add more blueprint types as discovered in the codebase
        // Example:
        // public const string Agridome = "Flatpacks/Agridome";
        // public const string CommandCenter = "Flatpacks/CommandCenter";
    }

    /// <summary>
    /// Extension methods for working with blueprint types.
    /// </summary>
    public static class BlueprintTypeExtensions
    {
        /// <summary>
        /// Determines if a blueprint type is a flatpack based on its prefix.
        /// </summary>
        /// <param name="blueprintType">The blueprint type string to check.</param>
        /// <returns>True if the type starts with "Flatpacks/", otherwise false.</returns>
        public static bool IsFlatpack(this string blueprintType)
        {
            return !string.IsNullOrEmpty(blueprintType) && 
                   blueprintType.StartsWith(BlueprintTypePrefixes.Flatpacks, StringComparison.OrdinalIgnoreCase);
        }
    }
}