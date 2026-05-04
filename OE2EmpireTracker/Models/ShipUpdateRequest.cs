// -----------------------------------------------------------------------
// <copyright file="ShipUpdateRequest.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing ship.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class ShipUpdateRequest
    {
        /// <summary>
        /// Gets or sets the original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyShip Original { get; set; }

        /// <summary>Gets or sets the ship name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the template UUID.</summary>
        public string TemplateUUID { get; set; }

        /// <summary>Gets or sets the hull blueprint UUID.</summary>
        public string HullBlueprintUUID { get; set; }

        /// <summary>Gets or sets the location type.</summary>
        public DestinationType LocationType { get; set; }

        /// <summary>Gets or sets the location UUID.</summary>
        public string LocationUUID { get; set; }

        /// <summary>Gets or sets the hull current HP.</summary>
        public int HullCurrentHP { get; set; }

        /// <summary>Gets or sets the hull max HP.</summary>
        public int HullMaxHP { get; set; }

        /// <summary>Gets or sets the hull max repair percent.</summary>
        public decimal HullMaxRepairPercent { get; set; }

        /// <summary>Gets or sets the component slots.</summary>
        public List<ShipComponentSlot> Components { get; set; }

        /// <summary>Gets or sets the cargo items.</summary>
        public ItemBag Cargo { get; set; }

        /// <summary>Gets or sets the hopper items.</summary>
        public ItemBag Hopper { get; set; }
    }
}
