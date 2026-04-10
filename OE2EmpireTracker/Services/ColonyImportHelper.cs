using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public static class ColonyImportHelper
    {
        /// <summary>
        /// Searches colonies for a case-insensitive ColonyName match.
        /// Returns the matching colony, or null if none found.
        /// </summary>
        public static Colony FindByName(IEnumerable<Colony> colonies, string colonyName)
        {
            if (colonies == null || string.IsNullOrEmpty(colonyName))
                return null;

            return colonies.FirstOrDefault(c =>
                string.Equals(c.ColonyName, colonyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Merges identity fields (PlanetName, SystemName) from source into target.
        /// Does NOT merge structures or commodities — that is handled by ColonyParser.ProcessHtml.
        /// </summary>
        public static void MergeIdentity(Colony target, Colony source)
        {
            target.PlanetName = source.PlanetName;
            target.SystemName = source.SystemName;
        }

        /// <summary>
        /// Creates a new Colony from a parsed temporary colony, assigning UUID and OwnerUUID.
        /// Copies all parsed data (ColonyName, PlanetName, SystemName, Structures, Commodities).
        /// </summary>
        public static Colony CreateFromTemp(Colony tempColony, string ownerUUID)
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = ownerUUID;
            colony.ColonyName = tempColony.ColonyName;
            colony.PlanetName = tempColony.PlanetName;
            colony.SystemName = tempColony.SystemName;
            colony.Structures = tempColony.Structures;
            colony.Commodities = tempColony.Commodities;
            return colony;
        }

        /// <summary>
        /// Checks whether a colony name is a duplicate among the given colonies,
        /// excluding the colony with the specified UUID.
        /// Returns true if a duplicate exists.
        /// </summary>
        public static bool IsDuplicateName(IEnumerable<Colony> colonies, string name, string excludeUUID)
        {
            if (colonies == null || string.IsNullOrEmpty(name))
                return false;

            return colonies.Any(c =>
                !string.Equals(c.UUID, excludeUUID, StringComparison.Ordinal) &&
                string.Equals(c.ColonyName, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
