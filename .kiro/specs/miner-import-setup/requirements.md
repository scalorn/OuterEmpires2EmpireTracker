# Requirements Document

## Introduction

When a colony is imported (or reimported) from the game's JSON data, mining rigs that are actively mining a resource are not fully set up in the tracker. The parser extracts `MiningSurveyResource` and `RefiningResourcePurity` from the game data, but it never assigns a `MiningSurvey` (survey UUID) or starts the `ProcessCompletionTime` (mining timer). This means the miner shows up in the tracker without a survey and without an active timer, even though the game says it is mining. The user must manually select a survey and click Start, which defeats the purpose of importing.

This feature automates miner and refinery setup during import so that mining rigs and refineries are fully operational in the tracker immediately after import, matching the game's authoritative state. It also ensures warehouse resource records exist for mined and refined resources so the UI works correctly.

## Glossary

- **Colony_Parser**: The `ColonyParser` class responsible for parsing game JSON and merging structures into existing colony data
- **Colony**: A player colony containing structures, an inventory, and a planet name
- **Colony_Structure**: An individual building within a colony (e.g. mining rig, refinery)
- **Survey**: A planet resource survey containing a list of resources with amounts and purities, identified by a UUID
- **Survey_Resource**: A single resource entry within a survey, containing resource name, purity, and amount per hour
- **Player_Context**: The singleton service that holds all player data including the survey list
- **Mining_Timer**: The `ProcessCompletionTime` countdown timer on a colony structure that drives hourly mining cycles
- **Best_Survey**: For a given miner, the survey for the colony's planet whose Survey_Resource `Amount` for the mined resource most closely matches the Max_Rate reported by the game. Because different scanners produce different survey results, the best survey for resource A may be a different survey than the best survey for resource B on the same planet. Selection is per-miner, not per-colony.
- **Default_Survey**: A synthetic survey auto-created during import when no real survey exists for a colony's planet that contains the mined resource. Identified by SurveyID = "DEFAULT" (NickName is not set) and a deterministic UUID derived from the colony dedup key (OwnerUUID, PlanetName, SystemName) using a dedicated namespace. At most one Default_Survey exists per colony.
- **Max_Rate**: The `maxRate` field from the game JSON on a mining rig building, representing the current mining output rate per hour (e.g. 123.32). A value of 0 means the miner is assigned to a resource but not actively mining (offline or no workers).

## Requirements

### Requirement 1: Assign Best Survey on Import

**User Story:** As a player, I want the tracker to automatically assign the best matching survey to a mining rig during import, so that the miner is fully configured without manual intervention.

#### Acceptance Criteria

1. WHEN the Colony_Parser parses a mining rig with a non-empty `MiningSurveyResource`, THE Colony_Parser SHALL search the Player_Context survey list for real surveys (excluding Default_Surveys) matching the Colony planet name (case-insensitive) that contain the mined resource at the matching purity.
2. WHEN multiple real surveys exist for the same planet, resource, and purity AND the Max_Rate is greater than zero, THE Colony_Parser SHALL select the Best_Survey by choosing the survey whose Survey_Resource `Amount` (parsed as a decimal) most closely matches the Max_Rate from the game JSON (smallest absolute difference). This selection is per-miner — different miners on the same colony may be assigned different surveys.
3. WHEN multiple real surveys exist for the same planet, resource, and purity AND the Max_Rate is zero (miner assigned but not actively mining), THE Colony_Parser SHALL select the real survey with the highest `Amount` for the mined resource.
4. WHEN a matching real survey is found, THE Colony_Parser SHALL set the Colony_Structure `MiningSurvey` property to the UUID of the selected survey.
5. IF no real survey exists for the Colony planet name that contains the mined resource at the matching purity, THEN THE Colony_Parser SHALL create or update a Default_Survey for the colony and assign it to the Colony_Structure (see Requirement 6).
6. WHEN both a Default_Survey and a real survey match the planet, resource, and purity, THE Colony_Parser SHALL always prefer the real survey over the Default_Survey.

### Requirement 2: Start Mining Timer on Import

**User Story:** As a player, I want the tracker to automatically start the mining timer when a miner is actively mining in the game, so that mining output is tracked from the moment of import.

#### Acceptance Criteria

1. WHEN the Colony_Parser has successfully assigned a `MiningSurvey` and `MiningSurveyResource` to a mining rig, THE Colony_Parser SHALL create a Mining_Timer on the Colony_Structure only if the Max_Rate for the building is greater than zero.
2. IF the Max_Rate for the mining rig is zero, THEN THE Colony_Parser SHALL assign the survey but not start a Mining_Timer, because the miner is assigned but not actively mining.
3. THE Mining_Timer SHALL be configured as a repeating timer with an interval equal to `GameConstants.SecondsPerHour`.
4. THE Mining_Timer SHALL align its first interval boundary to the next clock-hour boundary, matching the behavior of the manual Start button (`cmdSubStart_Click`).
5. IF the Colony_Structure already has an active Mining_Timer (non-null `ProcessCompletionTime` with `IsRepeating` true), THEN THE Colony_Parser SHALL preserve the existing timer and not create a new one.

### Requirement 3: Preserve Existing Survey Assignment on Reimport

**User Story:** As a player, I want reimporting a colony to keep my existing survey assignment if it is still valid, so that I do not lose my manual configuration.

#### Acceptance Criteria

1. WHILE a Colony_Structure already has a non-empty `MiningSurvey` that references a valid real survey in the Player_Context, WHEN the Colony_Parser merges a reimported mining rig, THE Colony_Parser SHALL preserve the existing `MiningSurvey` value.
2. WHEN the Colony_Parser merges a reimported mining rig and the existing `MiningSurvey` references a survey that no longer exists in the Player_Context, THE Colony_Parser SHALL assign a new Best_Survey using the same selection rules as Requirement 1.
3. WHEN the Colony_Parser merges a reimported mining rig and the existing `MiningSurvey` references a Default_Survey, THE Colony_Parser SHALL check whether a real survey now exists for the planet, resource, and purity. If a real survey is available, THE Colony_Parser SHALL upgrade the assignment to the real survey.

### Requirement 4: Apply Miner Setup to Both New and Merged Structures

**User Story:** As a player, I want miner setup to work for both first-time imports and reimports, so that miners are always correctly configured regardless of import scenario.

#### Acceptance Criteria

1. WHEN a new Colony_Structure is added to the colony during import (no existing match found), THE Colony_Parser SHALL apply survey assignment and timer setup to the new structure.
2. WHEN an existing Colony_Structure is updated via merge during reimport, THE Colony_Parser SHALL apply survey assignment and timer setup to the merged structure.
3. THE Colony_Parser SHALL apply miner setup after the merge step completes, so that both parsed and existing data are available for decision-making.

### Requirement 5: Log Miner Setup Actions

**User Story:** As a developer, I want the tracker to log miner setup actions during import, so that I can diagnose issues with survey assignment and timer creation.

#### Acceptance Criteria

1. WHEN the Colony_Parser assigns a survey to a mining rig, THE Colony_Parser SHALL log an informational message containing the structure display name, the selected survey UUID, and the resource name.
2. WHEN the Colony_Parser starts a Mining_Timer on a mining rig, THE Colony_Parser SHALL log an informational message containing the structure display name.
3. WHEN the Colony_Parser skips timer creation because Max_Rate is zero, THE Colony_Parser SHALL log an informational message containing the structure display name and the resource name.
4. WHEN the Colony_Parser creates a Default_Survey, THE Colony_Parser SHALL log an informational message containing the colony planet name and the Default_Survey UUID.
5. WHEN the Colony_Parser adds a resource to an existing Default_Survey, THE Colony_Parser SHALL log an informational message containing the resource name and the Default_Survey UUID.

### Requirement 6: Create and Manage Default Survey When No Real Survey Exists

**User Story:** As a player, I want the tracker to automatically create a default survey when I import a colony with active miners but have not yet imported a real survey for that planet, so that miners are fully operational without requiring a separate survey import step.

#### Acceptance Criteria

1. WHEN no real survey matches the colony planet, mined resource, and purity, THE Colony_Parser SHALL create a Default_Survey with a deterministic UUID generated from the colony dedup key (OwnerUUID, PlanetName, SystemName) using a dedicated "DefaultSurvey" namespace in DeterministicUUID.
2. THE Default_Survey SHALL have its SurveyID set to "DEFAULT" and its NickName left unset (null), so that it is identifiable in the UI without appearing to have a user-assigned name.
3. THE Default_Survey SHALL have its PlanetName, SystemName, and OwnerUUID set to match the colony being imported.
4. WHEN a Default_Survey is created for a mining rig, THE Colony_Parser SHALL add a Survey_Resource entry for the mined resource using the resource name from `MiningSurveyResource`, the purity from `RefiningResourcePurity`, and the amount from the Max_Rate value in the game JSON. If Max_Rate is zero, the amount SHALL be stored as "0".
5. WHEN a Default_Survey already exists for the colony (same deterministic UUID found in Player_Context), THE Colony_Parser SHALL update the existing Default_Survey by adding new resource entries or updating existing resource amounts rather than creating a duplicate.
6. THE Colony_Parser SHALL add the Default_Survey to the Player_Context SurveyList so that it is visible in the survey UI and available for survey assignment.
7. THE Colony_Parser SHALL extract the Max_Rate value from the `maxRate` field of the building JSON during structure parsing, so that it is available for Default_Survey resource population.
8. WHEN a real survey is later imported that covers the same planet, resource, and purity, THE Colony_Parser SHALL prefer the real survey over the Default_Survey during subsequent reimports (per Requirement 1, criterion 6).
9. AFTER miner setup is complete for all structures in an import, THE Colony_Parser SHALL remove any resource entries from the Default_Survey that are no longer being mined by any mining rig in the colony. If the Default_Survey has no remaining resources after cleanup, THE Colony_Parser SHALL remove the Default_Survey from the Player_Context SurveyList entirely.

### Requirement 7: Ensure Warehouse Resource Record Exists for Active Miners

**User Story:** As a player, I want the tracker to ensure a warehouse resource record exists for any resource being mined, so that the refinery UI can see it and the mining output has somewhere to go.

#### Acceptance Criteria

1. WHEN a mining rig is set up with a `MiningSurvey` and `MiningSurveyResource` during import, THE Colony_Parser SHALL check whether the colony's warehouse (Colony.Items) contains a resource record matching the mined resource name and purity.
2. IF no matching resource record exists in the warehouse, THE Colony_Parser SHALL create one with a quantity of 0, so that the refinery UI can list it as a refinable resource and mining output has a target inventory slot.
3. THE Colony_Parser SHALL NOT overwrite or modify existing warehouse resource records — only create missing ones.

### Requirement 8: Start Refinery Timer on Import

**User Story:** As a player, I want the tracker to automatically start the refinery timer when a refinery is actively refining in the game, so that refining progress is tracked from the moment of import.

#### Acceptance Criteria

1. WHEN the Colony_Parser parses a refinery structure with a non-empty `RefiningResource` and `RefiningResourcePurity`, THE Colony_Parser SHALL check whether the colony's warehouse contains a resource record matching the refining resource name and purity.
2. IF no matching resource record exists in the warehouse, THE Colony_Parser SHALL create one with a quantity of 0, so that the refinery UI can display the resource in its dropdown.
3. WHEN the refinery has a `RefiningResource` and `RefiningResourcePurity` set AND the structure is built and online, THE Colony_Parser SHALL create a repeating timer with an interval equal to `GameConstants.SecondsPerHour`, aligned to the next clock-hour boundary (matching the manual Start button behavior).
4. IF the Colony_Structure already has an active refinery timer (non-null `ProcessCompletionTime` with `IsRepeating` true), THEN THE Colony_Parser SHALL preserve the existing timer and not create a new one.
5. THE Colony_Parser SHALL apply refinery setup after the merge step completes, consistent with miner setup timing (Requirement 4).
