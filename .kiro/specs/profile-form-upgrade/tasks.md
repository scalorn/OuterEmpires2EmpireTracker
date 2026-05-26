# Implementation Plan: Profile Form Upgrade

## Overview

This plan implements the profile form upgrade in vertical slices: ViewModel properties first, then the FormatActiveTime helper, then PlayerSkillBlock enhancements (dynamic height, progress bar, remaining time), then FormPlayerProfile identity/time fields and ListView column. Each task is sized to ≤5 files, ≤200 new lines, and ≤3 acceptance criteria.

## Tasks

- [x] 1. Add identity and time properties to PlayerProfileViewModel
  - [x] 1.1 Add CharacterId, FirstName, LastName, ActiveTimeMinutes properties and LoadFrom/Reset logic
    - Add four new properties: CharacterId (int), FirstName (string), LastName (string), ActiveTimeMinutes (int)
    - Update LoadFrom to copy from ReadOnlyPlayerProfile snapshot
    - Update Reset to set CharacterId=0, FirstName="", LastName="", ActiveTimeMinutes=0
    - Update IsDirty to compare new fields against original snapshot
    - _Satisfies: Req 1, Criterion 1 ("PlayerProfileViewModel SHALL expose CharacterId, FirstName, LastName"); Req 2, Criterion 1 ("expose ActiveTimeMinutes"); Req 1, Criterion 5 ("Reset sets to defaults"); Req 2, Criterion 5 ("Reset sets ActiveTimeMinutes to 0")_
    - _Inputs: OE2EmpireTracker.Desktop/ViewModels/PlayerProfileViewModel.cs, OE2EmpireTracker.Common/ViewModels/PlayerProfileViewModel.cs_
    - _Output: Modified PlayerProfileViewModel.cs (whichever is the active one)_
    - _Verification: getDiagnostics clean, existing property tests still pass_

  - [x] 1.2 Write property test: LoadFrom round-trip preserves identity and time fields
    - **Property 1: LoadFrom round-trip preserves identity and time fields**
    - **Validates: Requirements 1.1, 2.1**
    - Extend or add test in PlayerProfileViewModelPropertyTests.cs
    - Generate arbitrary CharacterId (int range), FirstName (0–50 chars), LastName (0–50 chars), ActiveTimeMinutes (int range)
    - Assert ViewModel fields match source after LoadFrom
    - _Inputs: OE2EmpireTracker.Tests/ViewModels/PlayerProfileViewModelPropertyTests.cs_
    - _Output: Modified PlayerProfileViewModelPropertyTests.cs_
    - _Verification: vstest.console passes_


- [x] 2. Implement FormatActiveTime helper method
  - [x] 2.1 Create static FormatActiveTime method on PlayerProfileViewModel
    - Implement conversion: totalMinutes → "{D}d {H}h {M}m" with leading-zero omission rules
    - If totalMinutes <= 0: return "—" (em-dash U+2014)
    - Omit "0d" when D==0; omit "0h" only when both D==0 and H==0
    - When only minutes remain (D=0, H=0): show "{M}m" alone
    - _Satisfies: Req 2, Criterion 2 ("formatted value SHALL convert ActiveTimeMinutes to format"); Req 2, Criterion 3 ("if 0, display em-dash"); Req 2, Criterion 6 ("if negative, treat as 0")_
    - _Inputs: OE2EmpireTracker.Desktop/ViewModels/PlayerProfileViewModel.cs (or Common)_
    - _Output: Modified PlayerProfileViewModel.cs with FormatActiveTime static method_
    - _Verification: getDiagnostics clean_

  - [x] 2.2 Write property test: ActiveTimeMinutes formatting round-trip
    - **Property 4: ActiveTimeMinutes formatting round-trip**
    - **Validates: Requirements 2.2, 2.3, 2.6, 5.2**
    - For any non-negative int totalMinutes, parse the output D/H/M components and verify D×1440 + H×60 + M == totalMinutes
    - For totalMinutes == 0 or negative, verify output is "—"
    - _Inputs: OE2EmpireTracker.Tests/ViewModels/PlayerProfileViewModelPropertyTests.cs_
    - _Output: New test method in PlayerProfileViewModelPropertyTests.cs_
    - _Verification: vstest.console passes_

  - [x] 2.3 Write unit tests for FormatActiveTime edge cases
    - Test specific examples: 0 → "—", 1 → "1m", 59 → "59m", 60 → "1h 0m", 1440 → "1d 0h 0m", -5 → "—"
    - _Satisfies: Req 2, Criteria 2, 3, 6_
    - _Inputs: OE2EmpireTracker.Tests/ViewModels/PlayerProfileViewModelPropertyTests.cs_
    - _Output: New test methods_
    - _Verification: vstest.console passes_


- [x] 3. Implement PlayerSkillBlock dynamic height
  - [x] 3.1 Add height calculation logic to PlayerSkillBlock SkillData setter
    - When any metadata present (EffectDescription non-empty, AmountPerLevel > 0, TrainingPercentageComplete > 0, RemainingMinutes > 0): set height to 42px
    - When no metadata: set height to 24px
    - Update MinimumSize, MaximumSize, and Size properties
    - Position metadata labels: EffectDescription at x=48, y=26; AmountPerLevel at x=200, y=26
    - _Satisfies: Req 3, Criterion 1 ("height to 42 pixels when metadata present"); Req 3, Criterion 2 ("height to 24 pixels when no metadata"); Req 3, Criterion 6 ("collapse from 42 back to 24")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs, PlayerSkillBlock.Designer.cs_
    - _Output: Modified PlayerSkillBlock.cs_
    - _Verification: getDiagnostics clean, build succeeds_

  - [x] 3.2 Write property test: PlayerSkillBlock height calculation
    - **Property 5: PlayerSkillBlock height calculation**
    - **Validates: Requirements 3.1, 3.2, 3.6**
    - For any LocalSkillData, height is 42 when any metadata field is non-zero/non-empty, 24 otherwise
    - _Inputs: OE2EmpireTracker.Tests/Forms/ (new file)_
    - _Output: New test file OE2EmpireTracker.Tests/Forms/PlayerSkillBlockPropertyTests.cs_
    - _Verification: vstest.console passes_

- [x] 4. Checkpoint - Verify ViewModel and SkillBlock height logic
  - Ensure all tests pass, ask the user if questions arise.


- [x] 5. Implement PlayerSkillBlock progress bar
  - [x] 5.1 Add custom-painted progress bar Panel to PlayerSkillBlock
    - Add Panel control (60×12 px) on metadata row at y=26, after AmountPerLevel label
    - Implement Paint handler: fill width = (clamp(TrainingPercentageComplete, 0, 100) / 100.0) × 60
    - Fill color: SystemColors.Highlight; background: SystemColors.ControlLight
    - Draw centered "{N}%" text at 7pt font
    - Set Visible = false when TrainingPercentageComplete == 0
    - _Satisfies: Req 4, Criterion 1 ("display progress bar when > 0"); Req 4, Criterion 2 ("fixed width 60px, height 12px, fill calculation"); Req 4, Criterion 4 ("hidden when 0")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs, PlayerSkillBlock.Designer.cs_
    - _Output: Modified PlayerSkillBlock.cs and Designer.cs_
    - _Verification: getDiagnostics clean, build succeeds_

  - [x] 5.2 Write property test: Progress bar width and visibility
    - **Property 6: Progress bar width and visibility**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**
    - For any int TrainingPercentageComplete: hidden when 0, visible otherwise
    - When visible: fill width = (clamp(value, 0, 100) / 100.0) × 60; text = "{N}%"
    - _Inputs: OE2EmpireTracker.Tests/Forms/PlayerSkillBlockPropertyTests.cs_
    - _Output: New test method in PlayerSkillBlockPropertyTests.cs_
    - _Verification: vstest.console passes_


- [x] 6. Implement PlayerSkillBlock remaining time label
  - [x] 6.1 Add remaining time label to PlayerSkillBlock metadata row
    - Display FormatActiveTime(RemainingMinutes) after progress bar (or after AmountPerLevel if no progress bar)
    - Set Visible = false when RemainingMinutes <= 0
    - Treat negative RemainingMinutes as 0 (hide label)
    - _Satisfies: Req 5, Criterion 1 ("display remaining time when > 0"); Req 5, Criterion 3 ("hidden when 0"); Req 5, Criterion 5 ("negative treated as 0, hide label")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs_
    - _Output: Modified PlayerSkillBlock.cs_
    - _Verification: getDiagnostics clean, build succeeds_

  - [x] 6.2 Write property test: Remaining time label visibility
    - **Property 7: Remaining time label visibility**
    - **Validates: Requirements 5.1, 5.3, 5.5**
    - For any int RemainingMinutes: label visible iff RemainingMinutes > 0; negative treated as 0
    - _Inputs: OE2EmpireTracker.Tests/Forms/PlayerSkillBlockPropertyTests.cs_
    - _Output: New test method in PlayerSkillBlockPropertyTests.cs_
    - _Verification: vstest.console passes_

- [x] 7. Checkpoint - Verify PlayerSkillBlock enhancements
  - Ensure all tests pass, ask the user if questions arise.


- [x] 8. Add identity fields to FormPlayerProfile
  - [x] 8.1 Add CharacterId, FirstName, LastName labels to FormPlayerProfile
    - Add lblCharacterId / lblCharacterIdValue after Player Name row
    - Show CharacterId value or "—" when <= 0
    - Add lblFirstName / lblFirstNameValue — hide row when empty
    - Add lblLastName / lblLastNameValue — hide row when empty
    - Update PopulateForm to set values and visibility
    - _Satisfies: Req 1, Criterion 2 ("display Character ID label, show em-dash when 0 or negative"); Req 1, Criterion 3 ("display First/Last Name, hide when empty"); Req 1, Criterion 4 ("refresh on PlayerProfileDataChanged")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs, FormPlayerProfile.Designer.cs_
    - _Output: Modified FormPlayerProfile.cs and Designer.cs_
    - _Verification: getDiagnostics clean, build succeeds_

  - [x] 8.2 Write property test: CharacterId display formatting
    - **Property 2: CharacterId display formatting**
    - **Validates: Requirements 1.2**
    - For any int CharacterId: display "—" when <= 0, decimal string when > 0
    - _Inputs: OE2EmpireTracker.Tests/Forms/PlayerSkillBlockPropertyTests.cs (or new FormPlayerProfilePropertyTests.cs)_
    - _Output: New test file OE2EmpireTracker.Tests/Forms/FormPlayerProfilePropertyTests.cs_
    - _Verification: vstest.console passes_

  - [x] 8.3 Write property test: FirstName/LastName row visibility
    - **Property 3: FirstName/LastName row visibility**
    - **Validates: Requirements 1.3**
    - For any string value: row visible iff string is non-empty (not null and not "")
    - _Inputs: OE2EmpireTracker.Tests/Forms/FormPlayerProfilePropertyTests.cs_
    - _Output: New test method in FormPlayerProfilePropertyTests.cs_
    - _Verification: vstest.console passes_


- [ ] 9. Add active time display to FormPlayerProfile
  - [x] 9.1 Add Active Time label and formatted value to FormPlayerProfile
    - Add lblActiveTime / lblActiveTimeValue after First/Last Name rows
    - Display FormatActiveTime(ActiveTimeMinutes) from ViewModel
    - Update PopulateForm to set active time label
    - Refresh on PlayerProfileDataChanged event (existing handler)
    - _Satisfies: Req 2, Criterion 2 ("display Active Time label and formatted value"); Req 2, Criterion 4 ("refresh on Profile_Sync via PlayerProfileDataChanged")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs, FormPlayerProfile.Designer.cs_
    - _Output: Modified FormPlayerProfile.cs and Designer.cs_
    - _Verification: getDiagnostics clean, build succeeds_

- [x] 10. Add ListView ID column to FormPlayerProfile
  - [x] 10.1 Add "ID" column to profile ListView and update PopulateListView
    - Add third column "ID" (width 60px) after "Name" and "Faction"
    - Display CharacterId as string, or empty string when CharacterId == 0
    - Add SubItem at index 2 in PopulateListView
    - Preserve selected profile on refresh
    - _Satisfies: Req 6, Criterion 1 ("third column ID after Name and Faction"); Req 6, Criterion 2 ("empty string when 0"); Req 6, Criterion 3 ("width 60px")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs, FormPlayerProfile.Designer.cs_
    - _Output: Modified FormPlayerProfile.cs and Designer.cs_
    - _Verification: getDiagnostics clean, build succeeds_

  - [x] 10.2 Write property test: ListView CharacterId column display
    - **Property 8: ListView CharacterId column display**
    - **Validates: Requirements 6.1, 6.2, 6.5**
    - For any int CharacterId: SubItem at index 2 contains CharacterId as string when > 0, empty string when == 0
    - _Inputs: OE2EmpireTracker.Tests/Forms/FormPlayerProfilePropertyTests.cs_
    - _Output: New test method in FormPlayerProfilePropertyTests.cs_
    - _Verification: vstest.console passes_

- [x] 11. Checkpoint - Verify FormPlayerProfile enhancements
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 12. Wire PopulateListView refresh on PlayerProfilesChanged event
  - [x] 12.1 Ensure ListView refresh on PlayerProfilesChanged preserves selection
    - Verify PopulateListView is called when PlayerProfilesChanged fires
    - Ensure currently selected profile is preserved after refresh
    - Verify CharacterId values update when Profile_Sync completes
    - _Satisfies: Req 6, Criterion 4 ("refresh ListView on PlayerProfilesChanged, preserve selection"); Req 6, Criterion 5 ("PopulateListView includes CharacterId SubItem")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs_
    - _Output: Modified FormPlayerProfile.cs (if wiring not already present)_
    - _Verification: getDiagnostics clean, build succeeds_

- [ ] 13. Verify metadata row layout and FlowLayoutPanel reflow
  - [x] 13.1 Ensure PlayerSkillBlock metadata row positioning and parent reflow
    - Verify EffectDescription label at x=48, y=26
    - Verify AmountPerLevel label at x=200, y=26
    - Verify progress bar color: SystemColors.Highlight fill, SystemColors.ControlLight background
    - Verify progress bar text: "{N}%" centered, 7pt font
    - Ensure FlowLayoutPanel reflows when height changes (no overlapping)
    - _Satisfies: Req 3, Criterion 3 ("expanded height 42px = 24+18"); Req 3, Criterion 4 ("metadata row positions"); Req 3, Criterion 5 ("FlowLayoutPanel reflows"); Req 4, Criterion 3 ("percentage text centered, 7pt"); Req 4, Criterion 5 ("fill color Highlight, background ControlLight")_
    - _Inputs: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs_
    - _Output: Verification pass (may require minor adjustments to PlayerSkillBlock.cs)_
    - _Verification: getDiagnostics clean, build succeeds, visual inspection_

- [-] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements and criteria for traceability
- Checkpoints ensure incremental validation at logical boundaries
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The FormatActiveTime helper is shared between FormPlayerProfile (ActiveTimeMinutes) and PlayerSkillBlock (RemainingMinutes)
- Designer.cs files are modified via the WinForms Designer; hand-edits should follow existing patterns
- All new fields are read-only display values — no write-back to data model

## Traceability Matrix

| Requirement | Criteria | Task(s) |
|-------------|----------|---------|
| Req 1 (Identity Fields) | 1.1 | 1.1 |
| Req 1 (Identity Fields) | 1.2 | 8.1, 8.2 |
| Req 1 (Identity Fields) | 1.3 | 8.1, 8.3 |
| Req 1 (Identity Fields) | 1.4 | 8.1 |
| Req 1 (Identity Fields) | 1.5 | 1.1 |
| Req 2 (Active Time) | 2.1 | 1.1 |
| Req 2 (Active Time) | 2.2 | 2.1, 2.2 |
| Req 2 (Active Time) | 2.3 | 2.1, 2.3 |
| Req 2 (Active Time) | 2.4 | 9.1 |
| Req 2 (Active Time) | 2.5 | 1.1 |
| Req 2 (Active Time) | 2.6 | 2.1, 2.2 |
| Req 3 (SkillBlock Height) | 3.1 | 3.1, 3.2 |
| Req 3 (SkillBlock Height) | 3.2 | 3.1, 3.2 |
| Req 3 (SkillBlock Height) | 3.3 | 13.1 |
| Req 3 (SkillBlock Height) | 3.4 | 13.1 |
| Req 3 (SkillBlock Height) | 3.5 | 13.1 |
| Req 3 (SkillBlock Height) | 3.6 | 3.1, 3.2 |
| Req 4 (Progress Bar) | 4.1 | 5.1, 5.2 |
| Req 4 (Progress Bar) | 4.2 | 5.1, 5.2 |
| Req 4 (Progress Bar) | 4.3 | 13.1 |
| Req 4 (Progress Bar) | 4.4 | 5.1, 5.2 |
| Req 4 (Progress Bar) | 4.5 | 13.1 |
| Req 5 (Remaining Time) | 5.1 | 6.1, 6.2 |
| Req 5 (Remaining Time) | 5.2 | 2.1 (shared formatter) |
| Req 5 (Remaining Time) | 5.3 | 6.1, 6.2 |
| Req 5 (Remaining Time) | 5.4 | 12.1 (via existing event) |
| Req 5 (Remaining Time) | 5.5 | 6.1, 6.2 |
| Req 6 (ListView Column) | 6.1 | 10.1, 10.2 |
| Req 6 (ListView Column) | 6.2 | 10.1, 10.2 |
| Req 6 (ListView Column) | 6.3 | 10.1 |
| Req 6 (ListView Column) | 6.4 | 12.1 |
| Req 6 (ListView Column) | 6.5 | 10.1, 10.2 |


## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["2.2", "2.3", "3.1"] },
    { "id": 3, "tasks": ["3.2", "5.1"] },
    { "id": 4, "tasks": ["5.2", "6.1"] },
    { "id": 5, "tasks": ["6.2", "8.1"] },
    { "id": 6, "tasks": ["8.2", "8.3", "9.1"] },
    { "id": 7, "tasks": ["10.1"] },
    { "id": 8, "tasks": ["10.2", "12.1"] },
    { "id": 9, "tasks": ["13.1"] }
  ]
}
```
