# Design Document: Baseline Data Stability

## Overview

This feature stabilizes BaselineData.json for multi-user distribution by introducing deterministic UUIDs for global blueprints, a versioned migration framework, and externalizing code-embedded game data into JSON. The changes are scoped to:

1. **Identity** — UUID v5 deterministic generation for global blueprints
2. **Migration** — DataVersion field + sequential migration runner + idempotent rename table
3. **Data externalization** — GameConstants, Commodities, RefiningRecipes, ResearchTimeLookup move from code to BaselineData.json
4. **Type safety** — double→decimal for all game data numeric types

## Architecture

### Load Sequence

```
App Start
  → Load BaselineData.json → deserialize BaselineRoot
  → Load PlayerData.json → deserialize PlayerRoot
  → ApplyRenames(ec, pc)              // idempotent, before migrations
  → ApplyVersionedMigrations(ec, pc)  // gated by DataVersion
  → ApplyRenames(ec, pc)              // idempotent, after migrations
  → Persist if any changes made
  → Normal app operation
```

### New Services

```
Services/
  Migration/
    DeterministicUUID.cs        — UUID v5 generation
    RemapUUID.cs                — generic reference walker
    RenameTable.cs              — idempotent rename entries
    MigrationRunner.cs          — sequential migration executor
    Migrations/
      Migration001_DeterministicUUIDs.cs
```

## Components and Interfaces

### DeterministicUUID — UUID v5 Generator

```csharp
namespace OE2EmpireTracker.Services.Migration
{
    public static class DeterministicUUID
    {
        private static readonly Guid Namespace =
            new Guid("e0058083-0f64-b398-ed53-762f7d8b8eb2");

        /// <summary>
        /// Generates a deterministic UUID v5 from the blueprint's dedup key fields.
        /// Input string format: "Name|Evolution|BluePrintType|Class|TechLevel"
        /// </summary>
        public static string Generate(string name, int evolution,
            string blueprintType, int cls, string techLevel)
        {
            string input = $"{name}|{evolution}|{blueprintType}|{cls}|{techLevel}";
            return GenerateV5(Namespace, input).ToString();
        }

        /// <summary>
        /// Generates a deterministic UUID v5 from a Blueprint's dedup key.
        /// </summary>
        public static string Generate(Blueprint bp)
        {
            return Generate(bp.Name, bp.Evolution, bp.BluePrintType,
                bp.Class, bp.TechLevel);
        }

        /// <summary>
        /// UUID v5 implementation: SHA-1 hash of namespace + name,
        /// formatted as a version-5 UUID.
        /// </summary>
        private static Guid GenerateV5(Guid namespaceId, string name)
        {
            // Convert namespace to big-endian bytes (RFC 4122)
            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] hash;

            using (var sha1 = SHA1.Create())
            {
                sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length,
                    null, 0);
                sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                hash = sha1.Hash;
            }

            // Set version 5 and variant bits
            hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122

            // Convert back from big-endian
            SwapByteOrder(hash);
            byte[] result = new byte[16];
            Array.Copy(hash, 0, result, 0, 16);
            return new Guid(result);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            // Swap bytes for little-endian .NET Guid layout
            (guid[0], guid[3]) = (guid[3], guid[0]);
            (guid[1], guid[2]) = (guid[2], guid[1]);
            (guid[4], guid[5]) = (guid[5], guid[4]);
            (guid[6], guid[7]) = (guid[7], guid[6]);
        }
    }
}
```


### RemapUUID — Generic Reference Walker

```csharp
namespace OE2EmpireTracker.Services.Migration
{
    public static class RemapUUID
    {
        /// <summary>
        /// Walks all UUID references across the data model and replaces
        /// oldUUID with newUUID. No-op if oldUUID is not found anywhere.
        /// </summary>
        public static void Remap(EmpireContext ec, PlayerContext pc,
            string oldUUID, string newUUID)
        {
            // Blueprint.UUID (global + player)
            foreach (var bp in ec.globalBlueprintList)
                if (bp.UUID == oldUUID) bp.UUID = newUUID;
            foreach (var bp in pc.blueprintList)
                if (bp.UUID == oldUUID) bp.UUID = newUUID;

            // Blueprint.BaseBlueprintUUID (evolution chains)
            foreach (var bp in ec.globalBlueprintList.Concat(pc.blueprintList))
                if (bp.BaseBlueprintUUID == oldUUID)
                    bp.BaseBlueprintUUID = newUUID;

            // ColonyStructure references (3 fields)
            foreach (var colony in pc.colonyList)
                foreach (var s in colony.Structures)
                {
                    if (s.FlatpackBlueprintUUID == oldUUID)
                        s.FlatpackBlueprintUUID = newUUID;
                    if (s.ResearchingBlueprintUUID == oldUUID)
                        s.ResearchingBlueprintUUID = newUUID;
                    if (s.ManufacturingBlueprintUUID == oldUUID)
                        s.ManufacturingBlueprintUUID = newUUID;
                }
        }
    }
}
```

### RenameTable — Idempotent Blueprint Renames

```csharp
namespace OE2EmpireTracker.Services.Migration
{
    public static class RenameTable
    {
        private static readonly List<RenameEntry> _entries = new List<RenameEntry>
        {
            // Append new renames here. Never remove old entries.
            // new RenameEntry("OldName", "NewName", 0, "Type", 0, null),
        };

        public static void Apply(EmpireContext ec, PlayerContext pc)
        {
            foreach (var entry in _entries)
            {
                string oldUUID = DeterministicUUID.Generate(
                    entry.OldName, entry.Evolution,
                    entry.BluePrintType, entry.Class, entry.TechLevel);
                string newUUID = DeterministicUUID.Generate(
                    entry.NewName, entry.Evolution,
                    entry.BluePrintType, entry.Class, entry.TechLevel);

                // Find blueprint with old UUID
                var bp = ec.globalBlueprintList
                    .FirstOrDefault(b => b.UUID == oldUUID);
                if (bp == null) continue; // no-op

                // Update name and UUID
                bp.Name = entry.NewName;
                bp.UUID = newUUID;
                RemapUUID.Remap(ec, pc, oldUUID, newUUID);
            }
        }
    }

    public class RenameEntry
    {
        public string OldName { get; }
        public string NewName { get; }
        public int Evolution { get; }
        public string BluePrintType { get; }
        public int Class { get; }
        public string TechLevel { get; }

        public RenameEntry(string oldName, string newName,
            int evolution, string blueprintType, int cls, string techLevel)
        {
            OldName = oldName; NewName = newName;
            Evolution = evolution; BluePrintType = blueprintType;
            Class = cls; TechLevel = techLevel;
        }
    }
}
```

### MigrationRunner — Sequential Migration Executor

```csharp
namespace OE2EmpireTracker.Services.Migration
{
    public static class MigrationRunner
    {
        public const int CurrentVersion = 1;

        private static readonly Dictionary<int, Action<EmpireContext, PlayerContext>>
            Migrations = new Dictionary<int, Action<EmpireContext, PlayerContext>>
        {
            { 1, Migration001_DeterministicUUIDs.Run },
        };

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            // Renames before migrations
            RenameTable.Apply(ec, pc);

            // Versioned migrations
            int baselineVersion = ec.DataVersion;
            int playerVersion = pc.DataVersion;

            for (int v = Math.Min(baselineVersion, playerVersion) + 1;
                 v <= CurrentVersion; v++)
            {
                if (Migrations.TryGetValue(v, out var migration))
                {
                    migration(ec, pc);
                }
            }

            ec.DataVersion = CurrentVersion;
            pc.DataVersion = CurrentVersion;

            // Renames after migrations
            RenameTable.Apply(ec, pc);
        }
    }
}
```

### Migration001 — Initial UUID Migration

```csharp
namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration001_DeterministicUUIDs
    {
        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            foreach (var bp in ec.globalBlueprintList.ToList())
            {
                string deterministicUUID = DeterministicUUID.Generate(bp);
                if (bp.UUID == deterministicUUID) continue;

                // Store legacy UUID before overwriting
                if (string.IsNullOrEmpty(bp.LegacyUUID))
                    bp.LegacyUUID = bp.UUID;

                RemapUUID.Remap(ec, pc, bp.UUID, deterministicUUID);
            }
        }
    }
}
```


## Data Models

### Changes to Existing Models

**Blueprint.cs** — add LegacyUUID field:
```csharp
[DefaultValue(null)]
public string LegacyUUID { get; set; }
```

**BaselineRoot.cs** — add DataVersion and new data sections:
```csharp
public int DataVersion { get; set; } = 0;
public BaselineGameConstants GameConstants { get; set; }
public Commodity[] Commodity { get; set; }
public RefiningRecipe[] RefiningRecipe { get; set; }
public ResearchTimeEntry[] ResearchTime { get; set; }
```

**PlayerRoot.cs** — add DataVersion:
```csharp
public int DataVersion { get; set; } = 0;
```

### New Data Models

**BaselineGameConstants.cs**:
```csharp
public class BaselineGameConstants
{
    public int RefiningBaseRate { get; set; } = 25;
    public int CommoditiesPerCycle { get; set; } = 10;
    public long CommodityCycleSeconds { get; set; } = 600;
    public int StructureCap { get; set; } = 65;
    public decimal WorkerVolume { get; set; } = 50m;
}
```

**ResearchTimeEntry.cs**:
```csharp
public class ResearchTimeEntry
{
    public int Evolution { get; set; }
    public long ResearchTimeSeconds { get; set; }
}
```

**Commodity.cs** — the existing model already has the right fields (Name, CommodityGroup, CommodityIndustry, ConstructionResources). The change is moving the static data from the hardcoded `getCommodities()` method into BaselineData.json. The model class stays; the static initializer becomes a fallback.

**RefiningRecipe.cs** — the existing model already has the right fields (InputResource, InputPurity, OutputResource, ConsumeRate, ProduceRate, Tier). Same approach: data moves to JSON, static list becomes fallback.

### EmpireContext Changes

```csharp
// New fields
public int DataVersion { get; set; } = 0;
public BaselineGameConstants GameConstants { get; set; }

// In loadContext():
// 1. Deserialize BaselineRoot (existing)
// 2. Read DataVersion from BaselineRoot
// 3. Load GameConstants (with fallback to defaults if missing)
// 4. Load Commodity array (with fallback to hardcoded list)
// 5. Load RefiningRecipe array (with fallback to hardcoded list)
// 6. Load ResearchTime array (with fallback to hardcoded lookup)
```

### PlayerContext Changes

```csharp
// New field
public int DataVersion { get; set; } = 0;

// In loadContext():
// Read DataVersion from PlayerRoot
```

### GameConstants.cs Changes

The 5 game-derived constants become properties that read from `EmpireContext.getInstance().GameConstants`:

```csharp
// Before:
public const int RefiningBaseRate = 25;

// After:
public static int RefiningBaseRate =>
    EmpireContext.getInstance()?.GameConstants?.RefiningBaseRate ?? 25;
```

Internal constants (SecondsPerHour, PropBuilt, etc.) stay as `const`.

## double → decimal Conversion

### Scope

| Area | Fields/Variables |
|---|---|
| ColonyStructureStatus | All 10 numeric fields (PowerProvided/Required, Habitation, Food, Entertainment, Warehouse × provided/required) |
| Item | Volume |
| ItemProperty | BaseValue, AdjustedValue |
| PropertyBag | getDouble→getDecimal, setProperty(double)→setProperty(decimal) |
| ColonyStatusCalculator | All accumulator variables, GetBlueprintDouble→GetBlueprintDecimal, AppendStatus parameters |
| Colony.ProcessColony | extractionMultiplier, refiningMultiplier, quantity calculations |
| ColonyBootstrap | miningRate, adjustedRate, refinedOutput, BestResourceEntry fields |
| ColonyInactivityCollector | GetMiningOutputRate return type, totalMiningOutput, totalConsumption, supply, available |
| EvolutionChainService | All percentage calculations, SegmentInfo, EvolutionGraphData series |
| BaselineGameConstants | WorkerVolume |

### Excluded (stays double)

| Area | Reason |
|---|---|
| MainWindow performance metrics | System measurements (memory MB, CPU %), not game data |

### JSON Compatibility

Newtonsoft.Json deserializes numeric JSON values into `decimal` fields correctly. Existing JSON files with values like `50.0` will deserialize into `decimal` without issues. No data file changes needed for this conversion.

## Correctness Properties

### Property 1: UUID v5 determinism
For any Dedup_Key, `DeterministicUUID.Generate` shall produce the same UUID on every call. Two different Dedup_Keys shall produce different UUIDs.

### Property 2: RemapUUID completeness
For any data model state and any (oldUUID, newUUID) pair, after `RemapUUID.Remap`, no reference to oldUUID shall remain in any Blueprint.UUID, Blueprint.BaseBlueprintUUID, or ColonyStructure UUID fields.

### Property 3: Rename idempotency
For any rename table and any data model state, applying the rename table twice shall produce the same result as applying it once.

### Property 4: Migration version gating
For any DataVersion V and target version T, migrations V+1 through T shall each run exactly once. If V == T, no migrations run.

### Property 5: LegacyUUID preservation
For any Blueprint that undergoes UUID migration, LegacyUUID shall equal the original UUID and shall not change on subsequent saves/loads.

### Property 6: decimal round-trip
For any decimal value serialized to JSON and deserialized back, the value shall be exactly equal (no floating-point drift).

## Error Handling

| Scenario | Handling |
|---|---|
| BaselineData.json missing DataVersion | Treat as 0, run all migrations |
| PlayerData.json missing DataVersion | Treat as 0, run all migrations |
| GameConstants section missing | Use hardcoded defaults |
| Commodity array missing | Use hardcoded fallback list |
| RefiningRecipe array missing | Use hardcoded fallback list |
| ResearchTime array missing | Use hardcoded fallback lookup |
| RemapUUID called with non-existent UUID | No-op |
| Rename entry targets non-existent blueprint | Skip silently |
| UUID v5 generation with null/empty fields | Use empty string for null fields in the input |
| Migration fails mid-execution | Log error, do not increment DataVersion (migration will retry on next load) |
