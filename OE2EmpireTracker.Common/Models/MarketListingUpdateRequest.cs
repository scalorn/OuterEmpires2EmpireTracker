namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying field values for updating an existing market listing.
    /// </summary>
    public class MarketListingUpdateRequest
    {
        public string ItemName { get; set; } = string.Empty;

        public ItemType.ItemTypeEnum ItemType { get; set; } = Models.ItemType.ItemTypeEnum.None;

        public string ItemReferenceID { get; set; } = string.Empty;

        public string StationUUID { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal PricePerUnit { get; set; }

        public int CurrentHP { get; set; }

        public int MaxHP { get; set; }

        public decimal MaxRepairPercent { get; set; }
    }
}
