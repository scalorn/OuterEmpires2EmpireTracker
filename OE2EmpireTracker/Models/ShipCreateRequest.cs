// -----------------------------------------------------------------------
// <copyright file="ShipCreateRequest.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new ship.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class ShipCreateRequest
    {
        /// <summary>Gets or sets the ship name.</summary>
        public string Name { get; set; }
    }
}
