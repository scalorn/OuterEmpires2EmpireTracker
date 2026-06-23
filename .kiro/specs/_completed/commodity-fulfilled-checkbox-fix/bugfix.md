# Bugfix Requirements Document

## Introduction

When a user clicks the "Fulfilled" checkbox on a commodity request in the Colony form (FormColonyV2), the checkbox does not stay checked. It reverts to unchecked immediately after clicking. This prevents users from manually marking commodity requests as fulfilled.

The root cause is that `ColonyService.UpdateCommodityRequest` updates the `Delivered`, `Requested`, and `NeedBy` fields but does not update the `Fulfilled` property on the `CommodityRequested` model. After the service call, `OnColonyDataChanged` fires, which repopulates the grid from the model — reading `request.Fulfilled` (still `false`) and overwriting the checkbox value.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the user clicks the Fulfilled checkbox on a commodity request in the Colony form THEN the system reverts the checkbox to unchecked because `ColonyService.UpdateCommodityRequest` does not set `CommodityRequested.Fulfilled`

1.2 WHEN the user clicks the Fulfilled checkbox THEN the system fires `OnColonyDataChanged` which triggers `PopulateCommodityRequestGrid()` which reads the stale `Fulfilled = false` value from the model and overwrites the cell

### Expected Behavior (Correct)

2.1 WHEN the user clicks the Fulfilled checkbox on a commodity request THEN the system SHALL persist `Fulfilled = true` on the `CommodityRequested` model via `ColonyService.UpdateCommodityRequest` and the checkbox SHALL remain checked after the grid repopulates

2.2 WHEN the user unchecks the Fulfilled checkbox on a commodity request THEN the system SHALL persist `Fulfilled = false` on the `CommodityRequested` model and the checkbox SHALL remain unchecked after the grid repopulates

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the user edits the Amount (Requested) column on a commodity request THEN the system SHALL CONTINUE TO update the `Requested` field via `ColonyService.UpdateCommodityRequest` without affecting the `Fulfilled` state

3.2 WHEN the user edits the NeedBy column on a commodity request THEN the system SHALL CONTINUE TO update the `NeedBy` field via `ColonyService.UpdateCommodityRequest` without affecting the `Fulfilled` state

3.3 WHEN `DeliveryFulfillment.FulfillCommodity` marks a commodity as delivered THEN the system SHALL CONTINUE TO set both `Delivered = Requested` and `Fulfilled = true` as it does today

3.4 WHEN the colony data is repopulated after any change THEN the system SHALL CONTINUE TO apply strikethrough styling to fulfilled requests and normal styling to unfulfilled requests
