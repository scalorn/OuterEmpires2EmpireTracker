# Spec vs Code Sync Review — parser-idempotency-tests

## Status: All synced

All four discrepancies from the initial review have been resolved:

1. Survey sweep test split into `AllSurveyFiles_ParseTwice_ResourceCountUnchanged` (Property 1) and `AllSurveyFiles_ParseTwice_ResourceValuesPreserved` (Property 2)
2. Colony sweep tests split: structure sweep into count (Property 3) and values (Property 4); commodity sweep into count (Property 5) and values (Property 6)
3. Property comments now use full format matching the design spec
4. Unused `ParseSurveyFromFile` and `ParseColonyFromFile` helpers removed

## Production Code vs Spec

The design.md Data Models section describes parser merge logic:
- `SurveyParser.ParseResource` uses `survey.Resources[resourceName] = resource` — dictionary keying, confirmed in source
- `ColonyParser.ParseColonyBuildingsFromJson` merges by `gameSequence` — confirmed in source
- `ColonyParser.ParseColonyBuildingsFromWorkers` merges by `FlatpackBlueprintUUID` count — confirmed in source
- `ColonyParser.ParseCommodityDemands` merges by commodity `Name` — confirmed in source

All claims in the spec accurately describe the production code behavior.
