# Delivery Execution

## Phase 6: Delivery Execution — Form

**REQ-DEL-050** A separate Delivery Execution form SHALL display a delivery plan in stop-by-stop sequence.
**REQ-DEL-050a** The form SHALL be launchable from the Route Builder (with route+plan pre-selected) or from the Edit menu (user selects route then plan).
**REQ-DEL-050b** The form left panel SHALL have route selector and plan selector dropdowns.
**REQ-DEL-051** The form SHALL show a consolidated "load list" at the top — items that need to be loaded before departure. An item needs pre-loading if it is dropped off at a stop but not picked up at any earlier stop in sufficient quantity.
**REQ-DEL-052** Below the load list, all stops SHALL be visible at once in a scrollable layout, each showing drop-off and pick-up items.
**REQ-DEL-052a** Stops that have no drop-off items, no pick-up items, and are not refuel stops SHALL be hidden from the execution view. Only stops with actionable content are displayed.
**REQ-DEL-053** Each item SHALL have a checkbox to mark it as delivered/picked up. Checking SHALL auto-save immediately.
**REQ-DEL-054** When a commodity is marked as delivered, the corresponding CommodityRequested on the colony SHALL be updated (Delivered count incremented, Fulfilled set if complete).
**REQ-DEL-055** When a flatpack is marked as delivered, the corresponding planned structure on the colony SHALL be marked as Staged.
**REQ-DEL-056** When all items on all stops are marked delivered/picked up, the plan SHALL be marked as Completed.
**REQ-DEL-057** The execution form SHALL be accessible from the Route Builder via an "Execute" button, or from the Edit menu as "Delivery Execution".

## Delivery Fulfillment

**REQ-DEL-090** DeliveryFulfillment.FulfillCommodity SHALL decrement the delivery item quantity and add the commodity to the destination colony's warehouse.  
**REQ-DEL-091** DeliveryFulfillment.StageFlatpack SHALL mark the colony structure as staged and set its build sequence.  
**REQ-DEL-092** DeliveryFulfillment.DeliverWorkers SHALL add worker details to the destination colony's assigned workers.  
**REQ-DEL-093** Fulfillment operations SHALL be idempotent — fulfilling an already-fulfilled item SHALL have no effect.  
**REQ-DEL-094** Station stop fulfillment SHALL update the station's hold inventory for the current player.  
## Cargo Volume

**REQ-DEL-100** CargoVolumeService.ComputeLoadVolume SHALL compute total volume and mass for a delivery load list by summing per-item volume × quantity.  
**REQ-DEL-101** Per-item volume SHALL be: Resource=1, Commodity=10, WorkDetail=50, Blueprint/Survey=0, manufactured items=CargoVolumeSize blueprint property.  
**REQ-DEL-102** CargoVolumeService.SplitIntoTrips SHALL distribute items across trips within a cargo capacity limit, assigning items in order with oversized items getting their own trip.  