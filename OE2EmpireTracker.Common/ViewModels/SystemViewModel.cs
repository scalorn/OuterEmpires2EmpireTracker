// -----------------------------------------------------------------------
// <copyright file="SystemViewModel.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Read-only display wrapper for a <see cref="ReadOnlyStarSystem"/>.
    /// Exposes all system properties for DataGridView binding plus computed display properties.
    /// </summary>
    public class SystemViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ReadOnlyStarSystem _system;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemViewModel"/> class.
        /// </summary>
        /// <param name="system">The read-only star system to wrap.</param>
        public SystemViewModel(ReadOnlyStarSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        // -----------------------------------------------------------------------
        // Passthrough properties from ReadOnlyStarSystem
        // -----------------------------------------------------------------------

        /// <summary>Gets the game database identifier.</summary>
        public int Id => _system.Id;

        /// <summary>Gets the system display name.</summary>
        public string Name => _system.Name;

        /// <summary>Gets the normalized X coordinate.</summary>
        public decimal X => _system.X;

        /// <summary>Gets the normalized Y coordinate.</summary>
        public decimal Y => _system.Y;

        /// <summary>Gets the quadrant (1-4).</summary>
        public int Quadrant => _system.Quadrant;

        /// <summary>Gets the sector (1-4).</summary>
        public int Sector => _system.Sector;

        /// <summary>Gets the region (1-4).</summary>
        public int Region => _system.Region;

        /// <summary>Gets the locality (1-4).</summary>
        public int Locality => _system.Locality;

        /// <summary>Gets the spectral class (M, K, G, F, W, X).</summary>
        public string SpectralClass => _system.SpectralClass;

        /// <summary>Gets the faction identifier (0 = unclaimed).</summary>
        public int FactionId => _system.FactionId;

        /// <summary>Gets the faction name (empty when unclaimed).</summary>
        public string FactionName => _system.FactionName;

        /// <summary>Gets the faction hex color code (empty when unclaimed).</summary>
        public string FactionColor => _system.FactionColor;

        /// <summary>Gets a value indicating whether the system has an orbital.</summary>
        public bool HasOrbital => _system.HasOrbital;

        /// <summary>Gets a value indicating whether the system has a spaceport.</summary>
        public bool HasSpaceport => _system.HasSpaceport;

        /// <summary>Gets a value indicating whether the system has a starbase.</summary>
        public bool HasStarbase => _system.HasStarbase;

        // -----------------------------------------------------------------------
        // Computed display properties
        // -----------------------------------------------------------------------

        /// <summary>Gets the formatted grid location string (e.g. "Q1-S2-R3-L4").</summary>
        public string GridLocation => $"Q{_system.Quadrant}-S{_system.Sector}-R{_system.Region}-L{_system.Locality}";

        /// <summary>Gets the display name for DataGridView binding.</summary>
        public string DisplayName => _system.Name;

        /// <summary>Gets the underlying read-only star system.</summary>
        public ReadOnlyStarSystem System => _system;
    }
}
