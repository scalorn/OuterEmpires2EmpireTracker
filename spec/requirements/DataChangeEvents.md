# Data Change Events Requirements

## Event Types

**REQ-DCE-001** PlayerContext SHALL expose the following events:
- `CurrentPlayerChanged` — fired when the selected player changes
- `ColonyDataChanged(ColonyUUID)` — fired when colony data is modified
- `BlueprintDataChanged(BlueprintUUID)` — fired when blueprint data is modified
- `SurveyDataChanged(SurveyUUID)` — fired when survey data is modified
- `DeliveryDataChanged` — fired when delivery route or plan data is modified
- `PlayerProfileDataChanged(PlayerUUID)` — fired when player profile data is modified
- `PlayerProfilesChanged` — fired when the profile list changes (add/remove)
- `PricingDataChanged` — fired when pricing plan data is modified (resource prices, plan settings)

## Event Args

**REQ-DCE-010** ColonyDataChanged SHALL use `ColonyDataChangedEventArgs` containing the colony UUID.
**REQ-DCE-011** BlueprintDataChanged SHALL use `BlueprintDataChangedEventArgs` containing the blueprint UUID.
**REQ-DCE-012** SurveyDataChanged SHALL use `SurveyDataChangedEventArgs` containing the survey UUID.
**REQ-DCE-013** PlayerProfileDataChanged SHALL use `PlayerProfileDataChangedEventArgs` containing the player UUID.
**REQ-DCE-014** DeliveryDataChanged and CurrentPlayerChanged SHALL use plain `EventArgs`.

## Form Subscriptions

**REQ-DCE-020** All MDI child forms SHALL subscribe to relevant data change events in their constructor or load handler.
**REQ-DCE-021** All MDI child forms SHALL unsubscribe from events in OnFormClosed.
**REQ-DCE-022** Event handlers SHALL check `IsDisposed` before accessing form controls to prevent ObjectDisposedException.
**REQ-DCE-023** When a data change event fires, the form SHALL refresh its display if the changed entity is currently selected or visible.

## Write-Through Pattern

**REQ-DCE-030** All editable form controls SHALL write to the data model immediately on change (TextChanged, SelectedIndexChanged, CheckedChanged).
**REQ-DCE-031** Save buttons SHALL only call `writeContext()` — they SHALL NOT re-read form fields into the model.
