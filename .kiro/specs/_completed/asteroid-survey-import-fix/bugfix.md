# Bugfix Requirements Document

## Introduction

When importing an asteroid survey via FormSurvey, the system fails to fully create the associated Asteroid entity. Specifically:

1. **Missing max reserve data on Asteroid**: The `SurveyParser.ProcessHtml` method detects `ScanDetailOutputMaxReserve` HTML nodes and uses their presence to set `SurveyType = Asteroid`, but never extracts the actual max reserve values. Consequently, `SurveyImportHelper.LinkOrCreateAsteroid` creates an Asteroid entity with an empty `Reserves` list — the max reserve data from the survey HTML is lost.

2. **Missing max reserve display on survey form**: When viewing an asteroid survey in FormSurvey, the `PopulateFormFromViewModel` method does not look up the linked Asteroid entity (via `survey.AsteroidUUID`) to surface its max reserve data. The SurveyViewModel also has no asteroid-related properties. The user sees no max reserve information on the survey form.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN an asteroid survey is imported from clipboard HTML containing `ScanDetailOutputMaxReserve` nodes THEN the system detects the asteroid survey type but discards the max reserve values — the created Asteroid entity has an empty `Reserves` list

1.2 WHEN an asteroid survey is re-imported (dedup merge) from clipboard HTML containing updated max reserve values THEN the system merges survey fields but does not update the linked Asteroid entity's `Reserves` with the new max reserve data

1.3 WHEN a user selects an asteroid survey in FormSurvey that has a linked Asteroid entity with populated `Reserves` THEN the system does not display the max reserve data anywhere on the survey form

### Expected Behavior (Correct)

2.1 WHEN an asteroid survey is imported from clipboard HTML containing `ScanDetailOutputMaxReserve` nodes THEN the system SHALL extract each resource's max reserve value from the HTML, create the Asteroid entity, and populate its `Reserves` list with one `AsteroidReserve` per resource (matching resource name and purity from the survey, with `MaxReserve` set to the parsed value)

2.2 WHEN an asteroid survey is re-imported (dedup merge) from clipboard HTML containing updated max reserve values THEN the system SHALL update the linked Asteroid entity's `Reserves` to reflect the latest max reserve data from the HTML

2.3 WHEN a user selects an asteroid survey in FormSurvey that has a linked Asteroid entity with populated `Reserves` THEN the system SHALL display the max reserve values on the survey form, associated with each corresponding resource row

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a planet survey (non-asteroid) is imported from clipboard HTML THEN the system SHALL CONTINUE TO create the survey without creating an Asteroid entity and without attempting to extract max reserve data

3.2 WHEN an asteroid survey is imported and the parser detects `/cycle` rate units or `ScanDetailOutputMaxReserve` nodes THEN the system SHALL CONTINUE TO set `SurveyType = Asteroid` on the survey

3.3 WHEN a survey (planet or asteroid) is imported THEN the system SHALL CONTINUE TO correctly parse and store resource names, purities, and amounts in the survey's `Resources` dictionary

3.4 WHEN a user selects a planet survey in FormSurvey THEN the system SHALL CONTINUE TO display the survey form without any asteroid-specific max reserve information

3.5 WHEN an existing asteroid survey is re-imported (dedup merge) THEN the system SHALL CONTINUE TO preserve the survey's UUID, OwnerUUID, and NickName
