namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Lightweight DTO carrying item details for adding to a delivery plan stop.
    /// Used by AddDropOffItem and AddPickUpItem service methods.
    /// </summary>
    public class DeliveryItemInfo
    {
        public ItemType.ItemTypeEnum ItemType { get; set; }

        public string BaseItemTypeID { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }

        public string ResourcePurity { get; set; }
    }
}