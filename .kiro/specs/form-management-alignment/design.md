# Design: Align Form Management Pattern

## Overview
Apply the Delivery Routes form management pattern (New/Save/Delete, no Cancel, Save preserves selection) to Colony, Blueprint, Survey, and PlayerProfile forms.

## Reference Implementation
`FormDeliveryRoute.cs` serves as the reference pattern:
- `cmdNew_Click`: Clears form, deselects list
- `cmdSave_Click`: Persists data, refreshes list, keeps form populated
- `cmdDelete_Click`: Confirms via MessageBox, removes record, clears form

## Changes Per Form

### Colony Form (`FormColony.cs` / `FormColony.Designer.cs`)
- Add `cmdNew` button and `cmdNew_Click` handler calling `ClearForm()` + deselect
- Add `cmdDelete` button and `cmdDelete_Click` handler with confirmation dialog
- Remove Cancel button from Designer
- `cmdSave_Click` already preserves form — add list refresh via `PopulateListView`

### Blueprint Form (`FormBlueprint.cs` / `FormBlueprint.Designer.cs`)
- Add `cmdNew` button and `cmdNew_Click` handler calling `ClearForm()`
- Add `cmdDelete` button and `cmdDelete_Click` handler with confirmation dialog
- Remove Cancel button from Designer
- Modify `btnSave_Click` to not call `ClearForm()` after save

### Survey Form (`FormSurvey.cs` / `FormSurvey.Designer.cs`)
- Add `cmdNew` button and `cmdNew_Click` handler
- Add `cmdDelete` button and `cmdDelete_Click` handler with confirmation dialog
- Remove Cancel button from Designer
- Modify save to not clear form

### PlayerProfile Form (`FormPlayerProfile.cs` / `FormPlayerProfile.Designer.cs`)
- Add `cmdNew` button and `cmdNew_Click` handler
- Modify `cmdDelete_Click` to add confirmation dialog
- Remove Cancel button from Designer
- Save already preserves form state
