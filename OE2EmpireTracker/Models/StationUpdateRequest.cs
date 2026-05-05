// -----------------------------------------------------------------------
// <copyright file="StationUpdateRequest.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing station.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class StationUpdateRequest
    {
        /// <summary>
        /// Gets or sets the original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyStation Original { get; set; }

        /// <summary>Gets or sets the station name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the station type.</summary>
        public StationType StationType { get; set; }

        /// <summary>Gets or sets the ownership type.</summary>
        public StationOwnership Ownership { get; set; }

        /// <summary>Gets or sets the station blueprint UUID.</summary>
        public string StationBlueprintUUID { get; set; }

        /// <summary>Gets or sets the hull current HP.</summary>
        public int HullCurrentHP { get; set; }

        /// <summary>Gets or sets the hull max HP.</summary>
        public int HullMaxHP { get; set; }

        /// <summary>Gets or sets the hull max repair percent.</summary>
        public decimal HullMaxRepairPercent { get; set; }

        /// <summary>Gets or sets the component slots.</summary>
        public List<ShipComponentSlot> Components { get; set; }

        /// <summary>Gets or sets the hold items.</summary>
        public ItemBag Hold { get; set; }

        /// <summary>Gets or sets the munitions hold items.</summary>
        public ItemBag MunitionsHold { get; set; }
    }
}
