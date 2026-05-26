# Implementation Plan: Profile API Enrichment

## Overview

Enrich the local PlayerProfile, PlayerRank, and PlayerSkill data models to align with the Game API's field naming, add previously-unmodeled fields, update merge/sync logic, and update the Player Profile UI. Work is decomposed into model changes, DTO changes, converter, merge logic, wrappers, viewmodels, consumer renames, UI, and tests — each sized to ≤5 files and ≤200 new lines.

## Tasks

- [x] 1. Model layer changes
  - [x] 1.1 Add new properties to PlayerProfile model
    - Add CharacterId (int), FirstName (string), LastName (string), ActiveTimeMinutes (int) to PlayerProfile.cs
    - All default to safe values (0 or empty string)
    - _Requirements: 1.1, 2.1, 3.1_
    - _Files: OE2EmpireTracker.Common/Models/PlayerProfile.cs_

  - [x] 1.2 Rename PlayerRank properties and add RankName
    - Rename Title → RankName, CurrentXP → CurrentXp, NextXP → XpToNextLevel
    - Add [JsonProperty] attributes for new canonical names
    - Add [JsonConverter(typeof(PlayerRankJsonConverter))] attribute
    - _Requirements: 4.1, 5.1, 5.2_
    - _Files: OE2EmpireTracker.Common/Models/PlayerRank.cs_

  - [x] 1.3 Add metadata and training progress properties to PlayerSkill
    - Add SkillId (int), EffectDescription (string), AmountPerLevel (int), SkillGroupName (string), IsUnlocked (bool)
    - Add TargetLevel (int), TrainingPercentageComplete (int), RemainingMinutes (int)
    - All default to safe values
    - _Requirements: 6.1, 7.1_
    - _Files: OE2EmpireTracker.Common/Models/PlayerSkill.cs_

- [x] 2. PlayerRankJsonConverter for backward-compatible deserialization
  - [x] 2.1 Create PlayerRankJsonConverter class
    - New file implementing JsonConverter for PlayerRank
    - Map old names (Title, NextXP, CurrentXP) to new properties on read
    - New names take precedence when both old and new are present
    - Serialize using new canonical names only
    - Handle invalid types gracefully (default to empty/0)
    - _Requirements: 4.2, 4.4, 4.5, 5.3, 5.5, 12.2, 12.3_
    - _Files: OE2EmpireTracker.Common/Models/PlayerRankJsonConverter.cs (new)_

- [x] 3. DTO changes
  - [x] 3.1 Add fields to GameApiProfileResponse and GameApiRankResponse
    - Add CharacterId, FirstName, LastName, ActiveTimeMinutes, SkillInTraining to GameApiProfileResponse
    - Add LevelName, XpToNextLevel, CurrentXp to GameApiRankResponse
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
    - _Files: OE2EmpireTracker.Common/Models/GameApiProfileResponse.cs_

  - [x] 3.2 Add fields to GameApiSkillResponse and create GameApiSkillInTrainingResponse
    - Add SkillId, EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked to GameApiSkillResponse
    - Create new GameApiSkillInTrainingResponse DTO with SkillName, TargetLevel, TrainingPercentageComplete, RemainingMinutes
    - _Requirements: 8.6, 8.7, 8.8_
    - _Files: OE2EmpireTracker.Common/Models/GameApiSkillResponse.cs, OE2EmpireTracker.Common/Models/GameApiSkillInTrainingResponse.cs (new)_

- [x] 4. Merge logic changes in GameApiSyncScheduler
  - [x] 4.1 Extend MergeProfileData for new scalar fields
    - Add merge logic for CharacterId, FirstName, LastName, ActiveTimeMinutes
    - Clamp negative ActiveTimeMinutes to 0
    - Null/absent fields leave local unchanged
    - _Requirements: 1.2, 1.3, 2.2, 2.3, 3.2, 3.3, 3.4, 9.1, 9.2, 9.3_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_

  - [x] 4.2 Update MergeRank for renamed properties and new fields
    - Update MergeRank to write to RankName, XpToNextLevel, CurrentXp
    - Map remote LevelName → local RankName
    - Map remote XpToNextLevel → local XpToNextLevel
    - Map remote CurrentXp → local CurrentXp
    - _Requirements: 4.3, 5.4, 9.4, 9.5_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_

  - [x] 4.3 Extend MergeSkills for metadata fields
    - Merge SkillId, EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked from remote to local
    - Null string fields become empty string
    - _Requirements: 6.2, 6.3, 9.6_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_

  - [x] 4.4 Extend MergeSkills for training progress
    - Match skillInTraining.SkillName to local skill, set TargetLevel/TrainingPercentageComplete/RemainingMinutes
    - Reset training fields to 0 on non-matching skills
    - Reset all training fields when skillInTraining is null
    - Pass remote.SkillInTraining to MergeSkills from MergeProfileData caller
    - _Requirements: 7.2, 7.3, 7.4, 9.7, 9.8, 9.9_
    - _Files: OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs_

- [-] 5. Checkpoint - Verify model and merge logic
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. ReadOnly wrapper updates
  - [x] 6.1 Update ReadOnlyPlayerRank with renamed properties
    - Replace Title → RankName, CurrentXP → CurrentXp, NextXP → XpToNextLevel
    - Update ToString() to return RankName
    - _Requirements: 12.6, 13.5_
    - _Files: OE2EmpireTracker.Common/Models/ReadOnlyPlayerRank.cs_

  - [x] 6.2 Add new properties to ReadOnlyPlayerProfile and ReadOnlyPlayerSkill
    - Add CharacterId, FirstName, LastName, ActiveTimeMinutes to ReadOnlyPlayerProfile
    - Add SkillId, EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked, TargetLevel, TrainingPercentageComplete, RemainingMinutes to ReadOnlyPlayerSkill
    - _Requirements: 12.6_
    - _Files: OE2EmpireTracker.Common/Models/ReadOnlyPlayerProfile.cs, OE2EmpireTracker.Common/Models/ReadOnlyPlayerSkill.cs_

- [x] 7. ViewModel and edit buffer changes
  - [x] 7.1 Rename LocalRankData properties
    - Rename Title → RankName, CurrentXP → CurrentXp, NextXP → XpToNextLevel
    - _Requirements: 13.1, 13.2, 13.3_
    - _Files: OE2EmpireTracker.Common/ViewModels/LocalRankData.cs_

  - [x] 7.2 Add metadata properties to LocalSkillData
    - Add EffectDescription, AmountPerLevel, TrainingPercentageComplete, RemainingMinutes
    - These are read-only from API, not editable in UI
    - _Requirements: 6.1, 7.1_
    - _Files: OE2EmpireTracker.Common/ViewModels/LocalSkillData.cs_

  - [x] 7.3 Update PlayerProfileViewModel for renamed rank properties
    - Update CopyRank to use CurrentXp, XpToNextLevel, RankName
    - Update IsRankDirty to compare new property names
    - Update BuildUpdateRequest / BuildCreateRequest to use new property names
    - _Requirements: 13.1, 13.2, 13.3_
    - _Files: OE2EmpireTracker.Common/ViewModels/PlayerProfileViewModel.cs_

  - [x] 7.4 Update PlayerProfileViewModel to copy skill metadata into LocalSkillData
    - Update LoadFrom to copy EffectDescription, AmountPerLevel, TrainingPercentageComplete, RemainingMinutes from ReadOnlyPlayerSkill into LocalSkillData
    - _Requirements: 6.1, 7.1_
    - _Files: OE2EmpireTracker.Common/ViewModels/PlayerProfileViewModel.cs_

- [x] 8. PlayerProfileService changes
  - [x] 8.1 Rename ApplyRank parameters and update request DTOs
    - Rename ApplyRank parameters to use curXp, xpToNext, rankName
    - Update PlayerProfileUpdateRequest / PlayerProfileCreateRequest field names
    - PublicTitle → PublicRankName, PublicCurrentXP → PublicCurrentXp, PublicNextXP → PublicXpToNextLevel (same for Private, Military)
    - _Requirements: 13.4_
    - _Files: OE2EmpireTracker.Common/Services/PlayerProfileService.cs_

- [x] 9. Consumer renames — non-UI code
  - [x] 9.1 Update PlayerProfileParser for renamed properties
    - Replace all references to Title → RankName, CurrentXP → CurrentXp, NextXP → XpToNextLevel
    - _Requirements: 13.1, 13.2, 13.3_
    - _Files: OE2EmpireTracker.Common/Parsers/PlayerProfileParser.cs_

  - [x] 9.2 Update PlayerProfileEndpoints for renamed properties
    - Replace all references to Title → RankName, CurrentXP → CurrentXp, NextXP → XpToNextLevel
    - Update wire format from title/currentXP/nextXP to rankName/currentXp/xpToNextLevel
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
    - _Files: OE2EmpireTracker.Server/Endpoints/Typed/PlayerProfileEndpoints.cs_

- [~] 10. Checkpoint - Verify all non-UI code compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. UI changes — FormPlayerProfile rank labels
  - [x] 11.1 Add rank name labels to FormPlayerProfile Designer
    - Add lblPublicRankName, lblPrivateRankName, lblMilitaryRankName labels
    - Position each after the corresponding rank level text box
    - _Requirements: 10.1, 10.2_
    - _Files: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.Designer.cs_

  - [x] 11.2 Wire rank name labels in FormPlayerProfile code-behind
    - Add UpdateRankNameLabel helper method
    - Call UpdateRankNameLabel for each rank in PopulateForm()
    - Update consumer references from Title/CurrentXP/NextXP to RankName/CurrentXp/XpToNextLevel
    - _Requirements: 10.1, 10.2, 10.3, 13.1, 13.2, 13.3_
    - _Files: OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs_

- [ ] 12. UI changes — PlayerSkillBlock metadata display
  - [x] 12.1 Add metadata labels to PlayerSkillBlock Designer
    - Add lblEffectDescription, lblAmountPerLevel, lblTrainingProgress, lblRemainingTime labels
    - Position below/beside skill name
    - _Requirements: 11.1, 11.2, 11.3, 11.4_
    - _Files: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.Designer.cs_

  - [x] 12.2 Wire metadata display in PlayerSkillBlock code-behind
    - Add FormatMinutesAsCountdown helper method
    - Populate labels in PopulateForm() with visibility logic
    - Hide labels when no data (empty description, zero amounts)
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_
    - _Files: OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs_

- [~] 13. Checkpoint - Verify UI compiles and renders
  - Ensure all tests pass, ask the user if questions arise.

- [x] 14. Tests — PlayerRankJsonConverter
  - [x] 14.1 Write unit tests for PlayerRankJsonConverter
    - Test old names (Title, NextXP, CurrentXP) map to new properties
    - Test new names take precedence when both old and new present
    - Test invalid types default gracefully (empty string, 0)
    - Test round-trip serialization uses new canonical names
    - _Requirements: 4.2, 4.4, 4.5, 5.3, 5.5, 12.2, 12.3, 12.5_
    - _Files: OE2EmpireTracker.Tests/Models/PlayerRankJsonConverterTests.cs (new)_

- [x] 15. Tests — Merge logic
  - [x] 15.1 Write unit tests for MergeProfileData new fields
    - Test CharacterId, FirstName, LastName, ActiveTimeMinutes merge correctly
    - Test null/absent fields leave local unchanged
    - Test negative ActiveTimeMinutes clamps to 0
    - _Requirements: 1.2, 1.3, 2.2, 2.3, 3.2, 3.3, 3.4, 9.1, 9.2, 9.3_
    - _Files: OE2EmpireTracker.Tests/Services/MergeProfileDataTests.cs (new or extend existing)_

  - [x] 15.2 Write unit tests for MergeRank with new fields
    - Test RankName, XpToNextLevel, CurrentXp merge from API
    - Test null LevelName leaves local RankName unchanged
    - _Requirements: 4.3, 5.4, 9.4, 9.5_
    - _Files: OE2EmpireTracker.Tests/Services/MergeRankTests.cs (new or extend existing)_

  - [x] 15.3 Write unit tests for MergeSkills metadata and training progress
    - Test metadata fields merge correctly
    - Test training progress sets on matching skill
    - Test training fields reset to 0 on non-matching skills
    - Test null skillInTraining resets all training fields
    - _Requirements: 6.2, 6.3, 7.2, 7.3, 7.4, 9.6, 9.7, 9.8_
    - _Files: OE2EmpireTracker.Tests/Services/MergeSkillsTests.cs (new or extend existing)_

- [x] 16. Tests — Backward compatibility and consumer renames
  - [x] 16.1 Write backward compatibility tests for PlayerData.json loading
    - Load a JSON with old field names, verify correct mapping
    - Load a JSON with both old and new names, verify new wins
    - Load a JSON missing new fields, verify defaults
    - _Requirements: 1.4, 2.5, 3.6, 4.2, 5.3, 6.4, 7.5, 12.1, 12.4_
    - _Files: OE2EmpireTracker.Tests/Models/BackwardCompatibilityTests.cs (new or extend existing)_

  - [x] 16.2 Update existing test references from old to new property names
    - Update all test files referencing Title/CurrentXP/NextXP to RankName/CurrentXp/XpToNextLevel
    - Covers PlayerProfileTests, PlayerProfileViewModelPropertyTests, PlayerProfileServicePropertyTests, PlayerProfileParserTests, ReadOnlyPlayerProfileGapFillTests, ReadOnlyWrapperGenerators, JsonDefaultSkipTests
    - _Requirements: 13.6, 13.7_
    - _Files: OE2EmpireTracker.Tests/* (multiple test files, grouped by rename pattern)_

- [~] 17. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Model changes (tasks 1.x) must complete before merge logic (tasks 4.x) and wrappers (tasks 6.x)
- Consumer renames (tasks 9.x) depend on model renames (tasks 1.2) and wrapper updates (tasks 6.1)
- UI tasks (11.x, 12.x) depend on ViewModel changes (7.x)
- Test tasks (14.x–16.x) depend on all implementation being complete
- Task 16.2 (updating existing test references) should be done last as it touches many files

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["2.1", "3.1", "3.2"] },
    { "id": 2, "tasks": ["4.1", "4.2", "4.3", "4.4", "6.1", "6.2"] },
    { "id": 3, "tasks": ["7.1", "7.2", "8.1"] },
    { "id": 4, "tasks": ["7.3", "7.4", "9.1", "9.2"] },
    { "id": 5, "tasks": ["11.1", "12.1"] },
    { "id": 6, "tasks": ["11.2", "12.2"] },
    { "id": 7, "tasks": ["14.1", "15.1", "15.2", "15.3"] },
    { "id": 8, "tasks": ["16.1", "16.2"] }
  ]
}
```
