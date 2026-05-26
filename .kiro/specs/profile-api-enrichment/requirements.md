# Requirements Document

## Introduction

The OE2 Empire Tracker's local data model diverges from the game API's naming and structure. This feature aligns the local PlayerProfile, PlayerRank, and PlayerSkill models with the game API's field names and adds previously-unmodeled fields (character ID, name parts, active time, skill metadata, training progress). The merge/sync logic is updated to populate all fields, and the Player Profile UI is updated to display the new information.

## Glossary

- **Profile_Sync**: The periodic process that fetches player data from the Game API and merges it into the local PlayerProfile.
- **Merge_Logic**: The code path (MergeProfileData, MergeRank, MergeSkills) in GameApiSyncScheduler that applies API response data to local model objects using "API wins" strategy.
- **PlayerProfile**: The local data model representing a player's profile, persisted in PlayerData.json.
- **PlayerRank**: A sub-model of PlayerProfile representing rank data for Public, Private, or Military tracks.
- **PlayerSkill**: A sub-model of PlayerProfile representing a single skill's level and training state.
- **Game_API**: The Outer Empires 2 public REST API at oe2-pub-api-dev.azure-api.net.
- **DTO**: Data Transfer Object — a class that maps directly to the JSON structure returned by the Game API.
- **Backward_Compatibility**: The ability to load existing PlayerData.json files that lack newly-added or renamed fields without error.
- **Player_Profile_UI**: The FormPlayerProfile form and its child controls (PlayerSkillBlock) that display player data.

## Requirements

### Requirement 1: Add CharacterId to PlayerProfile

**User Story:** As a player, I want my game character's numeric ID stored locally, so that future features (colony/asset API calls) can reference it.

#### Acceptance Criteria

1. THE PlayerProfile SHALL include a CharacterId property of type int with a default value of 0.
2. WHEN the Game_API returns a characterId field in the character response, THE Merge_Logic SHALL set PlayerProfile.CharacterId to the API value.
3. IF the Game_API response does not include a characterId field or the field is null, THEN THE Merge_Logic SHALL leave PlayerProfile.CharacterId unchanged.
4. WHEN an existing PlayerData.json file lacks the CharacterId field, THE deserialization SHALL produce a PlayerProfile with CharacterId equal to 0 without error.


### Requirement 2: Add FirstName and LastName to PlayerProfile

**User Story:** As a player, I want my character's first and last name stored separately, so that the tool can display or format names with finer granularity.

#### Acceptance Criteria

1. THE PlayerProfile SHALL include FirstName and LastName properties of type string, each defaulting to empty string.
2. WHEN the Game_API returns firstName and lastName fields in the character response, THE Merge_Logic SHALL set PlayerProfile.FirstName and PlayerProfile.LastName to the API values.
3. IF the Game_API returns null or omits the firstName or lastName field, THEN THE Merge_Logic SHALL retain the existing PlayerProfile.FirstName or PlayerProfile.LastName value unchanged.
4. THE Merge_Logic SHALL continue to set PlayerProfile.Name from the API response (preserving existing behavior).
5. WHEN an existing PlayerData.json file lacks FirstName or LastName fields, THE deserialization SHALL produce a PlayerProfile with those fields as empty string without error.

### Requirement 3: Add ActiveTimeMinutes to PlayerProfile

**User Story:** As a player, I want my precise active time stored as an integer (minutes), so that the tool can perform calculations and display formatted active time.

#### Acceptance Criteria

1. THE PlayerProfile SHALL include an ActiveTimeMinutes property of type int with a default value of 0.
2. WHEN the Game_API returns an activeTimeMinutes field in the character response with a non-negative integer value, THE Merge_Logic SHALL set PlayerProfile.ActiveTimeMinutes to the API value.
3. IF the Game_API returns an activeTimeMinutes value that is negative, THEN THE Merge_Logic SHALL set PlayerProfile.ActiveTimeMinutes to 0.
4. IF the Game_API response does not include the activeTimeMinutes field or the field is null, THEN THE Merge_Logic SHALL leave PlayerProfile.ActiveTimeMinutes unchanged from its current value.
5. THE Merge_Logic SHALL continue to set PlayerProfile.ActiveTime (string) from the API response if provided (preserving existing behavior).
6. WHEN an existing PlayerData.json file lacks the ActiveTimeMinutes field, THE deserialization SHALL produce a PlayerProfile with ActiveTimeMinutes equal to 0 without error.


### Requirement 4: Rename PlayerRank.Title to RankName

**User Story:** As a developer, I want the local rank title field named consistently with the game API (RankName), so that the data model is self-documenting and aligned with the API contract.

#### Acceptance Criteria

1. THE PlayerRank SHALL rename the existing Title property to RankName (type string, default empty string).
2. WHEN an existing PlayerData.json file contains a "Title" field on a PlayerRank, THE deserialization SHALL map the "Title" value to the RankName property without data loss. IF the PlayerData.json file contains both a "Title" and a "RankName" field on the same PlayerRank entry, THE deserialization SHALL use the "RankName" value and ignore the "Title" value.
3. WHEN the Game_API returns a levelName field in a levels array entry, THE Merge_Logic SHALL set the corresponding PlayerRank.RankName to the API value.
4. THE serialization SHALL write the field as "RankName" in PlayerData.json (new canonical name).
5. IF deserialization encounters a PlayerRank entry where the "Title" field value is not a valid string (e.g. unexpected JSON type), THE deserialization SHALL default RankName to empty string and continue loading the file without skipping the rank entry.

### Requirement 5: Rename PlayerRank.NextXP to XpToNextLevel and Add CurrentXp

**User Story:** As a developer, I want rank XP fields named consistently with the game API (XpToNextLevel, CurrentXp), so that the data model matches the API contract.

#### Acceptance Criteria

1. THE PlayerRank SHALL rename the existing NextXP property to XpToNextLevel (type long, default 0).
2. THE PlayerRank SHALL rename the existing CurrentXP property to CurrentXp (type long, default 0).
3. WHEN an existing PlayerData.json file contains "NextXP" or "CurrentXP" fields on a PlayerRank, THE deserialization SHALL map them to XpToNextLevel and CurrentXp respectively, preserving the exact numeric values. IF the mapping fails due to invalid data, THE deserialization SHALL continue loading the file and default the affected property to 0.
4. WHEN the Game_API returns xpToNextLevel and currentXp fields in a rank entry, THE Merge_Logic SHALL set the corresponding PlayerRank properties to the API values using "API wins" strategy.
5. THE serialization SHALL write the fields as "XpToNextLevel" and "CurrentXp" in PlayerData.json (new canonical names).


### Requirement 6: Add Skill Metadata to PlayerSkill

**User Story:** As a player, I want to see skill details (ID, effect description, bonus per level, group name, unlock status), so that I can understand what each skill does and plan my training.

#### Acceptance Criteria

1. THE PlayerSkill SHALL include the following properties with specified defaults:
   - SkillId (int, default 0)
   - EffectDescription (string, default empty string)
   - AmountPerLevel (int, default 0)
   - SkillGroupName (string, default empty string)
   - IsUnlocked (bool, default false)
2. WHEN the Game_API returns skill metadata fields (skillId, effectDescription, amountPerLevel, skillGroupName, isUnlocked) in the skills response, THE Merge_Logic SHALL set the corresponding PlayerSkill properties to the API values.
3. IF the Game_API returns null for a string metadata field (effectDescription or skillGroupName), THEN THE Merge_Logic SHALL set the corresponding PlayerSkill property to empty string rather than null.
4. WHEN an existing PlayerData.json file lacks any of these metadata fields on a PlayerSkill, THE deserialization SHALL produce a PlayerSkill with those fields at their default values without error.

### Requirement 7: Add Training Progress to PlayerSkill

**User Story:** As a player, I want to see my current skill training progress (target level, percentage complete, time remaining), so that I can monitor training without opening the game.

#### Acceptance Criteria

1. THE PlayerSkill SHALL include the following properties with specified defaults:
   - TargetLevel (int, default 0, valid range 0 to 2,147,483,647)
   - TrainingPercentageComplete (int, default 0, valid range 0 to 100)
   - RemainingMinutes (int, default 0, valid range 0 to 2,147,483,647)
2. WHEN the Game_API returns skillInTraining data containing a SkillName that matches an existing PlayerSkill, THE Merge_Logic SHALL set that PlayerSkill's TargetLevel, TrainingPercentageComplete, and RemainingMinutes to the corresponding API values.
3. WHEN the Game_API returns skillInTraining data, THE Merge_Logic SHALL set TargetLevel, TrainingPercentageComplete, and RemainingMinutes to 0 on every PlayerSkill whose name does not match the skillInTraining SkillName.
4. WHEN the Game_API returns no skillInTraining data (field is null or absent), THE Merge_Logic SHALL set TargetLevel, TrainingPercentageComplete, and RemainingMinutes to 0 on all PlayerSkill objects.
5. WHEN an existing PlayerData.json file lacks any of these training progress fields on a PlayerSkill, THE deserialization SHALL produce a PlayerSkill with those fields at their default values without error.


### Requirement 8: Update Game API Response DTOs

**User Story:** As a developer, I want the API response DTOs to map all enriched fields from the Game API, so that the merge logic can access them.

#### Acceptance Criteria

1. THE GameApiProfileResponse DTO SHALL include a CharacterId property of type int mapped to the JSON field "characterId", defaulting to 0 when absent.
2. THE GameApiProfileResponse DTO SHALL include FirstName and LastName properties of type string mapped to "firstName" and "lastName", each defaulting to null when absent.
3. THE GameApiProfileResponse DTO SHALL include an ActiveTimeMinutes property of type int mapped to "activeTimeMinutes", defaulting to 0 when absent.
4. THE GameApiRankResponse DTO SHALL include a LevelName property of type string mapped to "levelName" and an XpToNextLevel property of type long mapped to "xpToNextLevel", each defaulting to null and 0 respectively when absent.
5. THE GameApiRankResponse DTO SHALL include a CurrentXp property of type long mapped to "currentXp", defaulting to 0 when absent.
6. THE GameApiSkillResponse DTO SHALL include the following properties mapped to their specified JSON fields: SkillId (int, "skillId"), EffectDescription (string, "effectDescription"), AmountPerLevel (int, "amountPerLevel"), SkillGroupName (string, "skillGroupName"), and IsUnlocked (bool, "isUnlocked").
7. THE Game_API response model SHALL include a GameApiSkillInTrainingResponse DTO with the following properties: SkillName (string, "skillName"), TargetLevel (int, "targetLevel"), TrainingPercentageComplete (int, "trainingPercentageComplete"), and RemainingMinutes (int, "remainingMinutes").
8. THE GameApiProfileResponse DTO SHALL include a SkillInTraining property of type GameApiSkillInTrainingResponse mapped to "skillInTraining", defaulting to null when no skill is in training.

### Requirement 9: Update Merge Logic for Enriched Fields

**User Story:** As a developer, I want the merge logic to populate all new and renamed fields from the API response, so that local data stays current with the game.

#### Acceptance Criteria

1. WHEN the Game_API returns a non-null characterId, THE MergeProfileData method SHALL set PlayerProfile.CharacterId to the API value using "API wins" strategy.
2. WHEN the Game_API returns non-null firstName and lastName, THE MergeProfileData method SHALL set PlayerProfile.FirstName and PlayerProfile.LastName to the API values using "API wins" strategy.
3. WHEN the Game_API returns a non-negative activeTimeMinutes, THE MergeProfileData method SHALL set PlayerProfile.ActiveTimeMinutes to the API value using "API wins" strategy. IF the API value is negative, THEN THE MergeProfileData method SHALL set PlayerProfile.ActiveTimeMinutes to 0.
4. WHEN the Game_API returns a non-null levelName for a rank, THE MergeRank method SHALL set PlayerRank.RankName to the API value using "API wins" strategy.
5. WHEN the Game_API returns xpToNextLevel and currentXp for a rank, THE MergeRank method SHALL set PlayerRank.XpToNextLevel and PlayerRank.CurrentXp to the API values using "API wins" strategy.
6. WHEN the Game_API returns skill metadata fields in the skills dictionary, THE MergeSkills method SHALL match each remote skill to the local PlayerSkill by skill name (dictionary key) and set SkillId, EffectDescription, AmountPerLevel, SkillGroupName, and IsUnlocked on the matched PlayerSkill using "API wins" strategy.
7. WHEN the Game_API returns a non-null skillInTraining object, THE MergeSkills method SHALL match the training skill by SkillName and set TargetLevel, TrainingPercentageComplete, and RemainingMinutes on the matched PlayerSkill using "API wins" strategy.
8. WHEN a skill is not identified as in-training by the API (skillInTraining is null or its SkillName does not match the skill), THE MergeSkills method SHALL reset TargetLevel, TrainingPercentageComplete, and RemainingMinutes to 0 on that skill.
9. IF the Game_API response omits a field (null value in the DTO), THEN THE Merge_Logic SHALL leave the corresponding local property unchanged for that field.


### Requirement 10: Update Player Profile UI for Rank Names

**User Story:** As a player, I want to see my rank title displayed in the Player Profile form, so that I know my rank name without looking it up.

#### Acceptance Criteria

1. THE Player_Profile_UI SHALL display the RankName as a read-only label positioned immediately after the rank level text box for each rank track (Public, Private, Military), formatted as the RankName string with no additional decoration.
2. IF RankName is empty string for a given rank track, THEN THE Player_Profile_UI SHALL hide the rank name label for that track, displaying only the numeric rank level text box.
3. WHILE the Player_Profile_UI is open, WHEN a Profile_Sync completes for the displayed profile, THE Player_Profile_UI SHALL refresh the rank name labels within the same UI update cycle triggered by the PlayerProfileDataChanged event.

### Requirement 11: Update Player Profile UI for Skill Metadata

**User Story:** As a player, I want to see skill effect descriptions and training progress in the Player Profile form, so that I can understand my skills and monitor training.

#### Acceptance Criteria

1. IF a skill has a non-empty EffectDescription, THEN THE PlayerSkillBlock control SHALL display the EffectDescription text below or beside the skill name.
2. IF a skill has an AmountPerLevel value greater than 0, THEN THE PlayerSkillBlock control SHALL display the value formatted as "+{AmountPerLevel}% per level".
3. IF a skill has TrainingPercentageComplete greater than 0, THEN THE PlayerSkillBlock control SHALL display the training progress as "{TrainingPercentageComplete}%" regardless of whether the value exceeds 100.
4. IF a skill has RemainingMinutes greater than 0, THEN THE PlayerSkillBlock control SHALL display the remaining training time formatted as the standard countdown format (e.g. "2d 5h 30m 10s"), converting RemainingMinutes to the appropriate day/hour/minute/second components.
5. WHEN a Profile_Sync completes and the form is open, THE Player_Profile_UI SHALL refresh to show updated skill metadata and training progress.
6. IF a skill has an empty EffectDescription and AmountPerLevel equal to 0, THEN THE PlayerSkillBlock control SHALL display only the skill name and level without metadata indicators.


### Requirement 12: Backward Compatibility and Field Migration

**User Story:** As a player with existing save data, I want my PlayerData.json to load without error after the update, so that I do not lose any data.

#### Acceptance Criteria

1. WHEN a PlayerData.json file created before this feature is loaded, THE deserialization SHALL succeed without throwing exceptions.
2. WHEN a PlayerData.json file contains the old field names ("Title", "NextXP", "CurrentXP") on PlayerRank objects, THE deserialization SHALL map them to the new property names (RankName, XpToNextLevel, CurrentXp) preserving the original values exactly (no truncation, rounding, or type coercion).
3. IF a PlayerData.json file contains both an old field name and its corresponding new field name on the same object (e.g. both "Title" and "RankName"), THEN THE deserialization SHALL use the new field name's value as the authoritative value.
4. WHEN a PlayerData.json file lacks any of the new fields (CharacterId, FirstName, LastName, ActiveTimeMinutes, SkillId, EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked, TargetLevel, TrainingPercentageComplete, RemainingMinutes), THE deserialization SHALL use the defined default values for those fields.
5. WHEN a PlayerData.json file is saved after loading, THE serialization SHALL write all fields using the new canonical names (RankName, XpToNextLevel, CurrentXp) in PascalCase consistent with the existing file format.
6. THE ReadOnlyPlayerProfile, ReadOnlyPlayerRank, and ReadOnlyPlayerSkill wrappers SHALL expose the renamed and new properties matching the updated model.

### Requirement 13: Update Existing Consumers of Renamed Fields

**User Story:** As a developer, I want all code that references the old field names (Title, NextXP, CurrentXP) to be updated to use the new names, so that the codebase compiles and behaves correctly.

#### Acceptance Criteria

1. THE FormPlayerProfile, PlayerProfileViewModel, LocalRankData, PlayerProfileService, PlayerProfileEndpoints, PlayerProfileParser, and all other code referencing PlayerRank.Title SHALL be updated to use PlayerRank.RankName.
2. THE FormPlayerProfile, PlayerProfileViewModel, LocalRankData, PlayerProfileService, PlayerProfileEndpoints, and all other code referencing PlayerRank.CurrentXP SHALL be updated to use PlayerRank.CurrentXp.
3. THE FormPlayerProfile, PlayerProfileViewModel, LocalRankData, PlayerProfileService, PlayerProfileEndpoints, and all other code referencing PlayerRank.NextXP SHALL be updated to use PlayerRank.XpToNextLevel.
4. THE MergeRank method, PlayerProfileService, and PlayerProfileEndpoints SHALL be updated to write to RankName, CurrentXp, and XpToNextLevel instead of Title, CurrentXP, and NextXP.
5. THE ReadOnlyPlayerRank wrapper SHALL expose RankName, CurrentXp, and XpToNextLevel (replacing Title, CurrentXP, NextXP), and its ToString override SHALL return RankName.
6. THE test projects (PlayerProfileTests, PlayerProfileViewModelPropertyTests, PlayerProfileServicePropertyTests, PlayerProfileParserTests, SerializationBackwardCompatibilityTests, ReadOnlyPlayerProfileGapFillTests, ReadOnlyWrapperGenerators, JsonDefaultSkipTests) SHALL be updated to reference the new property names.
7. THE solution SHALL compile with zero errors and zero warnings after all renames are applied.
