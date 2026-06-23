# Requirements Document

## Introduction

The profile-api-enrichment spec added enriched fields to the PlayerProfile, PlayerRank, and PlayerSkill models and wired basic display labels for rank names and skill metadata. This feature upgrades the FormPlayerProfile UI to comprehensively display all enriched data: character identity (CharacterId, FirstName, LastName), formatted active time from ActiveTimeMinutes, and improved skill training progress visualization with an expanded PlayerSkillBlock that shows metadata on a second row when present.

## Glossary

- **Player_Profile_UI**: The FormPlayerProfile form and its child controls (PlayerSkillBlock) that display player data.
- **PlayerProfileViewModel**: The edit buffer ViewModel that holds local field copies for the form to read/write.
- **PlayerSkillBlock**: A reusable UserControl that displays a single skill's level, training countdown, and metadata.
- **ActiveTimeMinutes**: An integer field on PlayerProfile representing total active play time in minutes, synced from the Game API.
- **Profile_Sync**: The periodic process that fetches player data from the Game API and merges it into the local PlayerProfile.

## Requirements

### Requirement 1: Display Character Identity Fields

**User Story:** As a player, I want to see my character ID, first name, and last name displayed in the profile form, so that I can verify my character identity at a glance.

#### Acceptance Criteria

1. THE PlayerProfileViewModel SHALL expose CharacterId (int), FirstName (string, max 50 characters), and LastName (string, max 50 characters) properties. WHEN LoadFrom is called with a ReadOnlyPlayerProfile, THE PlayerProfileViewModel SHALL copy CharacterId, FirstName, and LastName from the snapshot.
2. THE Player_Profile_UI SHALL display a read-only "Character ID" label and value positioned after the Player Name row, showing the CharacterId value. IF CharacterId is 0 or negative, THEN THE Player_Profile_UI SHALL display the text "—" (em-dash) in place of the numeric value.
3. THE Player_Profile_UI SHALL display a read-only "First Name" label and value, and a read-only "Last Name" label and value, positioned after the Character ID row. IF FirstName or LastName is empty string, THEN THE Player_Profile_UI SHALL hide the corresponding row entirely.
4. WHEN a PlayerProfileDataChanged event fires for the displayed profile, THE Player_Profile_UI SHALL refresh the CharacterId, FirstName, and LastName display values as part of the existing PopulateForm call within the event handler.
5. WHEN the ViewModel is reset (new profile), THE PlayerProfileViewModel SHALL set CharacterId to 0, FirstName to empty string, and LastName to empty string.

### Requirement 2: Display Formatted Active Time

**User Story:** As a player, I want to see my active play time displayed in a human-readable format (days and hours), so that I can understand my total time investment without mental arithmetic.

#### Acceptance Criteria

1. THE PlayerProfileViewModel SHALL expose an ActiveTimeMinutes (int) property loaded from the ReadOnlyPlayerProfile.ActiveTimeMinutes field, representing total accumulated play time in whole minutes (valid range: 0 to 2,147,483,647).
2. THE Player_Profile_UI SHALL display a read-only "Active Time" label and formatted value positioned after the First/Last Name rows. THE formatted value SHALL convert ActiveTimeMinutes to the format "{D}d {H}h {M}m" where D is days (ActiveTimeMinutes / 1440), H is remaining hours (0-23), and M is remaining minutes (0-59), omitting zero-valued leading components: "0d" is always omitted when D is 0; "0h" is omitted only when both D and H are 0. When only minutes remain (D=0, H=0), the display SHALL show "{M}m" alone.
3. IF ActiveTimeMinutes is 0, THEN THE Player_Profile_UI SHALL display the text "—" (em-dash, U+2014) instead of "0m".
4. WHEN a Profile_Sync completes for the displayed profile, THE Player_Profile_UI SHALL refresh the active time display within the same UI update cycle triggered by the PlayerProfileDataChanged event, with no additional user interaction required.
5. WHEN the ViewModel is reset (new profile), THE PlayerProfileViewModel SHALL set ActiveTimeMinutes to 0.
6. IF ActiveTimeMinutes is negative, THEN THE PlayerProfileViewModel SHALL treat the value as 0 for display formatting purposes.

### Requirement 3: Expand PlayerSkillBlock for Metadata Visibility

**User Story:** As a player, I want to see skill effect descriptions and per-level bonuses without them being clipped, so that I can understand what each skill does.

#### Acceptance Criteria

1. WHEN a skill has a non-empty EffectDescription, or AmountPerLevel greater than 0, or TrainingPercentageComplete greater than 0, or RemainingMinutes greater than 0, THE PlayerSkillBlock control SHALL set its height to 42 pixels and update its MinimumSize and MaximumSize height constraints to 42 pixels, displaying the metadata labels on a second row below the main skill row.
2. IF a skill has an empty EffectDescription and AmountPerLevel equal to 0 and TrainingPercentageComplete equal to 0 and RemainingMinutes equal to 0, THEN THE PlayerSkillBlock control SHALL set its height to 24 pixels and update its MinimumSize and MaximumSize height constraints to 24 pixels (compact single-row layout).
3. THE PlayerSkillBlock expanded height SHALL be 42 pixels (24 pixels for the main row plus 18 pixels for the metadata row).
4. THE metadata row SHALL display the EffectDescription label at horizontal offset 48 pixels (matching the skill name), followed by the AmountPerLevel label at horizontal offset 200 pixels, with both labels positioned at vertical offset 26 pixels from the top of the control.
5. WHEN the PlayerSkillBlock height changes due to metadata presence or absence, THE parent FlowLayoutPanel SHALL reflow to accommodate the new size without overlapping adjacent skill blocks.
6. WHEN SkillData is reassigned and the new data has no metadata (empty EffectDescription, AmountPerLevel equal to 0, TrainingPercentageComplete equal to 0, and RemainingMinutes equal to 0), THE PlayerSkillBlock control SHALL collapse from 42 pixels back to 24 pixels.

### Requirement 4: Skill Training Progress Bar Visualization

**User Story:** As a player, I want to see a visual progress bar for skill training percentage, so that I can quickly gauge training completion at a glance rather than reading a number.

#### Acceptance Criteria

1. IF a skill has TrainingPercentageComplete greater than 0, THEN THE PlayerSkillBlock control SHALL display a horizontal progress bar on the metadata row (the second row at y=26), positioned immediately after the lblAmountPerLevel label, replacing the existing lblTrainingProgress text label.
2. THE progress bar SHALL have a fixed width of 60 pixels and height of 12 pixels, with the filled portion width calculated as (TrainingPercentageComplete / 100.0) * 60 pixels, where TrainingPercentageComplete is clamped to the range 0–100 before calculation.
3. THE progress bar SHALL display the integer percentage text centered horizontally and vertically within the bar bounds, in the format "{N}%" where N is the TrainingPercentageComplete value (integer, no decimal places), using a font size no larger than 7 points to fit within the 12-pixel bar height.
4. IF TrainingPercentageComplete is 0, THEN THE progress bar SHALL be hidden (Visible = false).
5. THE progress bar fill color SHALL be System.Drawing.SystemColors.Highlight and the background SHALL be System.Drawing.SystemColors.ControlLight.

### Requirement 5: Skill Remaining Time on Metadata Row

**User Story:** As a player, I want to see the API-reported remaining training time displayed alongside the progress bar, so that I have both percentage and time-to-completion visible together.

#### Acceptance Criteria

1. IF a skill has RemainingMinutes greater than 0, THEN THE PlayerSkillBlock control SHALL display the remaining time label on the metadata row, positioned after the progress bar (or after AmountPerLevel if no progress bar is shown).
2. THE remaining time SHALL be formatted as "{D}d {H}h {M}m" using the same formatting rules as Requirement 2 (omitting leading zero components), converting RemainingMinutes to day/hour/minute components.
3. IF RemainingMinutes is 0, THEN THE remaining time label SHALL be hidden.
4. WHEN a Profile_Sync completes and the form is open, THE Player_Profile_UI SHALL refresh the remaining time display with updated values.
5. IF RemainingMinutes is negative, THEN THE PlayerSkillBlock SHALL treat the value as 0 and hide the remaining time label.

### Requirement 6: ListView Column Enhancement

**User Story:** As a player, I want the profile list to show additional context (character ID) so that I can distinguish between profiles more easily.

#### Acceptance Criteria

1. THE Player_Profile_UI profile ListView SHALL include a third column with header text "ID", positioned after the existing "Name" and "Faction" columns, displaying the CharacterId integer value as its text for each profile row.
2. IF CharacterId is 0 for a profile, THEN THE ListView SHALL display an empty string in the ID column for that row.
3. THE ID column SHALL have a default width of 60 pixels.
4. WHEN a Profile_Sync completes and the PlayerProfilesChanged event fires, THE Player_Profile_UI SHALL refresh the ListView to reflect updated CharacterId values while preserving the currently selected profile.
5. WHEN the Player_Profile_UI PopulateListView method executes, THE system SHALL include the CharacterId value (or empty string per criterion 2) as a SubItem in each ListViewItem at column index 2.
