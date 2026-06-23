# Requirements Document

## Introduction

Player Profile Import enables players to import their OE2 character profile data into the Empire Tracker by copying the game's profile panel HTML from the browser clipboard. The parser extracts character identity, faction, credits, rank progression (public, private, military), available skill points, skill group unlock states, and individual skill levels and training status. The imported data creates or updates a PlayerProfile in the tracker, following the same clipboard-based import pattern used by ColonyParser and SurveyParser.

## Glossary

- **Profile_Parser**: The component that parses clipboard HTML from the game's profile panel into a PlayerProfile model
- **Player_Profile_Form**: The WinForms form that displays and manages player profile data
- **Profile_Panel**: The game's left slideout panel containing character information, ranks, skills, and skill groups
- **Top_Bar**: The game's top-left UI bar containing character name, faction tag, and credit display
- **Rank_Track**: One of three career progression tracks (Public, Private, Military), each with a level and XP values
- **Skill_Group**: A named group of related skills that can be locked (requiring SP to unlock) or unlocked
- **Skill**: An individual trainable ability within a skill group, with a level and optional training-in-progress state
- **Clipboard_HTML**: The HTML fragment extracted from the system clipboard after a user copies the profile panel from the game's browser UI
- **PlayerContext**: The singleton service that manages player data persistence including player profiles

## Requirements

### Requirement 1: Parse Character Identity from Clipboard HTML

**User Story:** As a player, I want to import my character name and faction from the game's profile panel, so that the tracker can identify my character.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing a Top_Bar with `ui_character_detail` is provided, THE Profile_Parser SHALL extract the character first name and last name from the `ui_text_white` div and combine them into a single full name
2. WHEN Clipboard_HTML containing a Top_Bar with `ui_character_detail` is provided, THE Profile_Parser SHALL extract the faction abbreviation from the `ui_text_purple` div, stripping the surrounding brackets
3. IF the `ui_character_detail` element is missing from the Clipboard_HTML, THEN THE Profile_Parser SHALL log an error and leave the name and faction fields unchanged

### Requirement 2: Parse Credits from Clipboard HTML

**User Story:** As a player, I want to import my total credits from the game's profile panel, so that the tracker reflects my current financial state.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing a `ui_credit_detail` element is provided, THE Profile_Parser SHALL extract the exact credit value from the `data-ui-tooltip` attribute (formatted as `#N,NNN,NNN.NN`) and parse it into a decimal TotalCredits value
2. IF the `ui_credit_detail` element is missing or the tooltip value cannot be parsed, THEN THE Profile_Parser SHALL log a warning and leave TotalCredits unchanged

### Requirement 3: Parse Rank Tracks from Profile Panel

**User Story:** As a player, I want to import my public, private, and military rank levels and XP, so that the tracker shows my career progression.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing Profile_Panel rank sections is provided, THE Profile_Parser SHALL extract the rank level for each of the three Rank_Tracks (Public, Private, Military)
2. WHEN Clipboard_HTML containing Profile_Panel rank sections with XP data is provided, THE Profile_Parser SHALL extract the current XP and next-level XP threshold for each Rank_Track
3. THE Profile_Parser SHALL populate the corresponding PlayerRank (Rank, CurrentXP, NextXP) for each Rank_Track on the PlayerProfile
4. IF a Rank_Track section is missing from the Clipboard_HTML, THEN THE Profile_Parser SHALL log a warning and leave that rank unchanged

### Requirement 4: Parse Skill Points

**User Story:** As a player, I want to import my available skill points, so that the tracker knows how many SP I have to allocate.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing the Profile_Panel skill points display is provided, THE Profile_Parser SHALL extract the available skill points as an integer and set SkillPoints on the PlayerProfile
2. IF the skill points element is missing or the value cannot be parsed, THEN THE Profile_Parser SHALL log a warning and leave SkillPoints unchanged

### Requirement 5: Parse Skill Group States

**User Story:** As a player, I want to import which skill groups are locked or unlocked, so that the tracker reflects my current skill tree state.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing Skill_Group sections is provided, THE Profile_Parser SHALL identify each Skill_Group by its display name
2. WHEN a Skill_Group section indicates the group is unlocked, THE Profile_Parser SHALL set the corresponding SkillGroup to true on the PlayerProfile
3. WHEN a Skill_Group section indicates the group is locked (requiring SP to unlock), THE Profile_Parser SHALL set the corresponding SkillGroup to false on the PlayerProfile
4. THE Profile_Parser SHALL match Skill_Group display names to SkillGroupName enum values using the Description attribute

### Requirement 6: Parse Individual Skills

**User Story:** As a player, I want to import my individual skill levels and training status, so that the tracker shows my current skill progression.

#### Acceptance Criteria

1. WHEN Clipboard_HTML containing skill entries within Skill_Group sections is provided, THE Profile_Parser SHALL extract each skill's display name and current level
2. WHEN a skill entry indicates training is in progress, THE Profile_Parser SHALL set TrainingStarted to true on the corresponding PlayerSkill
3. WHEN a skill entry includes a training time remaining value, THE Profile_Parser SHALL parse the time remaining and set the CompletionTime on the corresponding PlayerSkill
4. THE Profile_Parser SHALL match skill display names to SkillName enum values using the Description attribute
5. IF a skill display name does not match any known SkillName enum value, THEN THE Profile_Parser SHALL log a warning and skip that skill

### Requirement 7: Create or Update PlayerProfile on Import

**User Story:** As a player, I want the import to create a new profile if one does not exist for my character, or update the existing one, so that I do not lose previously tracked data.

#### Acceptance Criteria

1. WHEN the Profile_Parser produces a parsed profile and a PlayerProfile with the same name already exists in PlayerContext, THE Player_Profile_Form SHALL update the existing profile with the parsed data
2. WHEN the Profile_Parser produces a parsed profile and no PlayerProfile with the same name exists in PlayerContext, THE Player_Profile_Form SHALL create a new PlayerProfile with a generated UUID and add it to PlayerContext
3. THE Player_Profile_Form SHALL match profiles by character name (case-insensitive comparison)

### Requirement 8: Import Button on Player Profile Form

**User Story:** As a player, I want an Import button on the Player Profile form, so that I can trigger the clipboard import with a single click.

#### Acceptance Criteria

1. THE Player_Profile_Form SHALL display an Import button accessible to the user
2. WHEN the user clicks the Import button, THE Player_Profile_Form SHALL read Clipboard_HTML from the system clipboard and invoke the Profile_Parser
3. WHEN the import completes successfully, THE Player_Profile_Form SHALL refresh the displayed profile data to reflect the imported values
4. IF the system clipboard does not contain HTML data, THEN THE Player_Profile_Form SHALL display an informational message indicating no profile data was found on the clipboard

### Requirement 9: Extend PlayerProfile and PlayerRank Models

**User Story:** As a player, I want the tracker to store all the profile data available in the game, so that nothing is lost during import.

#### Acceptance Criteria

1. THE PlayerProfile model SHALL include a `CitizenId` string property to store the character's citizen ID (e.g. "43 - 4944 - 3a32 - 3339")
2. THE PlayerProfile model SHALL include a `RegistrationDate` string property to store the character's registration date (e.g. "2223-01-06-16:20")
3. THE PlayerProfile model SHALL include an `ActiveTime` string property to store the character's active play time (e.g. "1Mn 2W 2D 6H 47m")
4. THE PlayerRank model SHALL include a `Title` string property to store the rank title (e.g. "Under Secretary (Grade 3)")
5. WHEN Clipboard_HTML containing the Profile_Panel headline section is provided, THE Profile_Parser SHALL extract CitizenId, RegistrationDate, and ActiveTime and set them on the PlayerProfile
6. WHEN Clipboard_HTML containing rank sections is provided, THE Profile_Parser SHALL extract the rank title text and set it on the corresponding PlayerRank
7. All new model properties SHALL default to `string.Empty` so existing serialized data loads without errors

### Requirement 10: HTML Parsing Infrastructure

**User Story:** As a developer, I want the profile parser to follow the same HTML parsing pattern as ColonyParser and SurveyParser, so that the codebase remains consistent.

#### Acceptance Criteria

1. THE Profile_Parser SHALL use SgmlReader to parse Clipboard_HTML into an XmlDocument, following the same pattern as ColonyParser.ParseHtmlToXml
2. THE Profile_Parser SHALL use XPath queries to locate elements by CSS class names within the parsed XmlDocument
3. THE Profile_Parser SHALL use ClipboardHelper.ExtractHtmlFragment to extract the HTML fragment from raw clipboard data
4. THE Profile_Parser SHALL provide a ProcessHtml method that accepts an HTML string and a PlayerProfile, and a ProcessClipboard method that reads from the system clipboard
