# Colony Items (Warehouse)

**REQ-COL-060** The user SHALL be able to add items of type Resource, Commodity, WorkDetail, Survey, and Blueprint to the colony warehouse.  
**REQ-COL-061** Resource items SHALL require purity selection; the default purity SHALL be Refined.  
**REQ-COL-062** Commodity items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the commodity Name.  
**REQ-COL-063** WorkDetail items SHALL be selected from BlueCollarDetail, WhiteCollarDetail, SpecialistDetail; BaseItemTypeID SHALL be set to the WorkerDetail ID.  
**REQ-COL-064** Survey items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the Survey UUID.  
**REQ-COL-065** Blueprint items SHALL be selected from a filtered combo showing ExtendedName; BaseItemTypeID SHALL be set to the Blueprint UUID.  
**REQ-COL-066** The items grid SHALL display ItemType, ExtendedName, locked amount, and quantity for each item.  
**REQ-COL-067** Pressing the Delete key when an item row is selected SHALL remove that item from the colony and refresh the grid.

## Warehouse Overflow

**REQ-COL-085** The Colony form SHALL have a Warehouse Overflow tab displaying overflow rules for the selected colony.  
**REQ-COL-086** A WarehouseOverflowRule SHALL have UUID, OwnerUUID, IsActive flag, ColonyUUID, ResourceName, ResourcePurity, TriggerThreshold, DestinationType, DestinationUUID, and DeliveryRouteUUID.  
**REQ-COL-087** WarehouseOverflowRule.IsActive SHALL default to true. Inactive rules SHALL be skipped during overflow threshold checks.  
**REQ-COL-088** When warehouse quantity for a resource exceeds TriggerThreshold, the excess (current - threshold) SHALL be moved via a generated delivery plan on the designated route.  
**REQ-COL-089** The Overflow tab SHALL display current quantity vs threshold with color coding (green = below threshold, yellow = approaching, red = exceeded).
