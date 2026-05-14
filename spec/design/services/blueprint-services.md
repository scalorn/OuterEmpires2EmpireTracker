<!-- Extracted from spec/design/services.md — Blueprint domain -->
# Services — Blueprints

## BlueprintImportHandler

Static service in Services/BlueprintImportHandler.cs.

```csharp
public static class BlueprintImportHandler
{
    public static ImportType ClassifyImport(Blueprint tempBP);
    public static FindTargetResult FindTarget(Blueprint tempBP, Blueprint selected, PlayerContext pc, EmpireContext ec);
    public static Blueprint MergeAndPersist(FindTargetResult findResult, Blueprint tempBP, PlayerContext pc, EmpireContext ec);
}
```

Logic:
- ClassifyImport: ResourcesOnly (has resources but no properties/type), Full (has name), NoName (fallback).
- FindTarget: checks selected blueprint match first (Name+Evolution+Type), then dedup via MarketBlueprintImporter.FindByDedupKey. Routes Evo0→global, others→player.
- MergeAndPersist: updates existing or creates new with deterministic UUID (global) or random UUID (player), persists, fires event.

## CrateImporter

Static service in Services/CrateImporter.cs.

```csharp
public static class CrateImporter
{
    public static CrateImportResult ImportFromFile(string filePath, PlayerContext pc, EmpireContext ec);
    public static CrateImportResult ImportFromJson(string json, PlayerContext pc, EmpireContext ec);
}
```

Logic:
- Imports blueprints from a JSON file produced by the OE2 Blueprint Scraper browser console script.
- Each JSON entry contains name, evolution, techLevel, description, iconClass, properties, and resources scraped from the game UI.
- Parses each entry into a temporary Blueprint, applies property key remapping (same as BlueprintScanner), normalizes values.
- Routes via MarketBlueprintImporter.IsGlobalRoute (Evo0→global, others→player).
- Dedup via MarketBlueprintImporter.FindByDedupKey; updates existing or creates new.
- Persists once at the end (batch save), fires BlueprintDataChanged event.
- Blueprint type resolved via name-based classification (ReclassifyByName). Icon CSS class stored as `_IconClass` property for future mapping.


## Class Diagram

```mermaid
classDiagram
    class BlueprintImportHandler {
        <<static>>
        -Logger Log
        +ClassifyImport(tempBP) ImportType
        +LogParsedBlueprint(tempBP) void
        +FindTarget(tempBP, selected, pc, ec) FindTargetResult
        +MergeAndPersist(findResult, tempBP, pc, ec) Blueprint
    }

    class ImportType {
        <<enum>>
        ResourcesOnly
        Full
        NoName
    }

    class FindTargetResult {
        +Blueprint Target
        +bool IsNew
        +bool IsGlobal
        +bool IsSelectedMatch
    }

    class CrateImporter {
        <<static>>
        -Logger Log
        +ImportFromFile(filePath, pc, ec) CrateImportResult
        +ImportFromJson(json, pc, ec) CrateImportResult
        -ImportSingleEntry(entry, pc, ec) CrateImportEntry
        -FindTypeByName(blueprintName, ec) string
        -FindTypeByDescription(description, ec) string
        -NormalizePropertyValue(key, value) string
        -ReclassifyByName(resolvedType, blueprintName) string
    }

    class CrateImportResult {
        +int TotalInFile
        +int Created
        +int Updated
        +int Skipped
        +int Failed
        +List~string~ Errors
        +List~CrateImportEntry~ Entries
    }

    class CrateImportEntry {
        +string Name
        +int Evolution
        +string TechLevel
        +ImportAction Action
        +string Storage
        +string SkipReason
    }

    BlueprintImportHandler --> ImportType
    BlueprintImportHandler --> FindTargetResult
    BlueprintImportHandler --> PlayerContext
    BlueprintImportHandler --> EmpireContext
    BlueprintImportHandler --> MarketBlueprintImporter
    CrateImporter --> CrateImportResult
    CrateImportResult --o CrateImportEntry
    CrateImporter --> PlayerContext
    CrateImporter --> EmpireContext
    CrateImporter --> MarketBlueprintImporter
```
