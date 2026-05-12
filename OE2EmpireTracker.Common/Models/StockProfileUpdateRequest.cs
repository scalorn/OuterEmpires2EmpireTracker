using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing stock profile.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class StockProfileUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyStockProfile Original { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public List<StockProfileEntry> Entries { get; set; }
    }
}
