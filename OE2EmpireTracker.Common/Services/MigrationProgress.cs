// <copyright file="MigrationProgress.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Progress data reported during migration.
    /// </summary>
    public class MigrationProgress
    {
        /// <summary>
        /// Gets or sets the name of the entity type currently being migrated.
        /// </summary>
        public string CurrentEntityType { get; set; }

        /// <summary>
        /// Gets or sets the cumulative count of entities processed so far.
        /// </summary>
        public int EntitiesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the current phase description (e.g. "Global", "Character {uuid}", "Baseline").
        /// </summary>
        public string Phase { get; set; }
    }
}
