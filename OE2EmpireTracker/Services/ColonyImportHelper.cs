using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    public static class ColonyImportHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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
        /// Searches colonies for a case-insensitive PlanetName + SystemName match.
        /// Returns the matching colony, or null if none found.
        /// This is the primary dedup key -- one colony per planet per player.
        /// </summary>
        public static Colony FindByPlanet(IEnumerable<Colony> colonies, string planetName, string systemName)
        {
            if (colonies == null || string.IsNullOrEmpty(planetName))
                return null;

            return colonies.FirstOrDefault(c =>
                string.Equals(c.PlanetName, planetName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.SystemName ?? "", systemName ?? "", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Merges identity fields (PlanetName, SystemName) from source into target.
        /// Does NOT merge structures or commodities -- that is handled by ColonyParser.ProcessHtml.
        /// Only sets ColonyName if the target doesn't already have one,
        /// preserving user-corrected names (e.g. fixing game truncation bugs).
        /// </summary>
        public static void MergeIdentity(Colony target, Colony source)
        {
            target.PlanetName = source.PlanetName;
            target.SystemName = source.SystemName;
            // Only set ColonyName if the target doesn't already have one.
            // This preserves user-corrected names (e.g. fixing game truncation bugs).
            if (string.IsNullOrEmpty(target.ColonyName))
            {
                target.ColonyName = source.ColonyName;
            }
            target.LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow);
        }

        /// <summary>
        /// Creates a new Colony from a parsed temporary colony, assigning UUID and OwnerUUID.
        /// Copies all parsed data (ColonyName, PlanetName, SystemName, Structures, Commodities).
        /// </summary>
        public static Colony CreateFromTemp(Colony tempColony, string ownerUUID)
        {
            var colony = new Colony();
            colony.UUID = DeterministicUUID.Generate(ownerUUID, tempColony.PlanetName, tempColony.SystemName);
            colony.OwnerUUID = ownerUUID;
            colony.ColonyName = tempColony.ColonyName;
            colony.PlanetName = tempColony.PlanetName;
            colony.SystemName = tempColony.SystemName;
            colony.Structures = tempColony.Structures;
            colony.Commodities = tempColony.Commodities;
            colony.LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow);
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
