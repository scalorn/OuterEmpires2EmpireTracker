# Discussion: BaselineData.json — Long-Term Maintainability, Upgradability, and Stability

## Problem Statement

BaselineData.json is currently a mutable file that ships with the app, gets modified by user imports, and has no versioning or identity scheme that survives across installations. With 100+ users, the developer needs it to be all three of: upgradable by the developer, preserving of user customizations, and consistent across different machines.

## Sub-Problems

### 1. Global Blueprint Identity

Global blueprints currently get random UUIDs assigned on import. Each user's machine has different UUIDs for the same blueprint. If the developer ships a new BaselineData.json, all UUID references (colony structures pointing to FlatpackBlueprintUUID, evolution chains via BaseBlueprintUUID) break.

Note: Only the Blueprint array in BaselineData.json uses UUIDs. The other data types are already stable:
- **ShipClass** — integer Id (1-8)
- **BlueprintType** — human-readable string Id (e.g. "Flatpacks/MiningRig", "Hull")
- **TechLevel** — Name only, no Id

So the UUID stability problem is scoped entirely to global blueprints. There are no global surveys.

### 2. Blueprint Renames

The developer has renamed blueprints to fix naming inconsistencies. Users who imported before the rename have the old name. A new BaselineData.json would create duplicates or orphan references.

### 3. User Customizations vs Developer Updates

Users may fix data in BaselineData.json that the game changed (e.g. correcting a property value). The developer shouldn't overwrite those fixes on upgrade. But the developer does need the ability to push new official data when upgrading to a new version.

### 4. Code-Embedded Data

Some data that might change is buried in code (e.g. commodity construction resources in RefiningRecipes.cs, Commodity.ConstructionResources). Users can't fix this without being developers. This data should be data-driven and participate in the same upgrade scheme.

---

## Options for Global Blueprint Identity

### Option 1: Compound Key Field (User's Idea)

Add a second field to Blueprint that stores a deterministic compound key based on the dedup fields (Name + Evolution + BluePrintType + Class + TechLevel). All lookups that currently use UUID would also support lookup by compound key.

**Pros:**
- Deterministic — same blueprint always gets the same key
- Leverages existing dedup logic

**Cons:**
- Big change to thread through everywhere (colony structures, evolution chains, surveys all reference by UUID)
- Separator concerns — if the game uses the separator character in a blueprint name, the key breaks
- Two identity fields on every blueprint is confusing

**Separator mitigation:** Use a hash (e.g. SHA256 of `Name|Evolution|BluePrintType|Class|TechLevel`) instead of a concatenated string. Deterministic, collision-resistant, no separator issues. The "key" is the hash; the dedup fields are still stored separately.

### Option 2: G: Prefix on UUID (User's Idea)

Replace the UUID for global blueprints with a leading identifier like `G:<compound key>`. Internally hide the fact that globals aren't real UUIDs.

**Pros:**
- Single field, no schema change
- Deterministic for globals

**Cons:**
- Leaky abstraction — every piece of code that touches UUIDs needs to know about the special format
- Fragile — string parsing of UUIDs throughout the codebase
- Same compound key / separator concerns as Option 1

### Option 3: Canonical UUIDs Shipped with the App

The developer assigns UUIDs to global blueprints and those UUIDs are the "official" canonical ones. The app ships with a canonical baseline. On first run or upgrade, the app merges the canonical data into the user's file, matching by dedup key. If a user imported the same blueprint with a different UUID, the migration remaps all references (colony structures, evolution chains) from the user's UUID to the canonical one.

**Pros:**
- UUIDs stay as UUIDs — no format changes, no compound key threading
- Clean separation: the developer controls the canonical identity
- One-time migration per version bump

**Cons:**
- Migration code that walks all references (colony structures, evolution chains) to remap UUIDs
- The developer must maintain a stable set of canonical UUIDs (never reuse or reassign)
- First migration for existing users is the biggest — remapping all their imported UUIDs to canonical ones

### Option 4: Deterministic UUID Generation

Generate UUIDs deterministically from the dedup key using a UUID v5 (name-based, SHA-1) or similar scheme. `UUID = UUIDv5(namespace, Name + "|" + Evolution + "|" + BluePrintType + "|" + Class + "|" + TechLevel)`. Every machine that imports the same blueprint gets the same UUID.

**Pros:**
- No schema change — UUID field stays as-is
- Deterministic — same blueprint always gets the same UUID on every machine
- No migration needed for new users (they get deterministic UUIDs from the start)
- No canonical file to maintain — the identity is derived from the data itself

**Cons:**
- Existing users need a one-time migration to remap random UUIDs to deterministic ones
- If the dedup key fields change (e.g. a rename), the UUID changes — need migration code for that
- UUID v5 uses SHA-1 which is technically deprecated for security, but fine for identity generation

---

## Blueprint Renames

Migration code is the only real option. A rename table (`old name → new name`) applied during version migration. The key is making the migration framework robust enough that rename entries can be added declaratively without writing new code each time.

Example structure:
```csharp
static readonly Dictionary<int, List<(string OldName, string NewName)>> Renames = new()
{
    [2] = new() { ("Mining Rig", "Mining Rig Flatpack"), ("Habitat", "Habitation Block Flatpack") },
    [3] = new() { ("Entertainment Centre", "Entertainment Centre Flatpack") },
};
```

Applied sequentially from the user's current version to the latest.

---

## User Customizations vs Developer Updates — Versioning

Each record (or at least each BlueprintType and global Blueprint) gets a version number. On upgrade:

- If the user's version matches the previous release version → overwrite with new data (user hasn't touched it)
- If the user's version is higher or different → preserve user's data (they customized it)
- New records not in the user's file → add them
- Records removed in the new version → optionally flag for user review

This is essentially a three-way merge: previous release baseline, current release baseline, user's current data.

---

## Code-Embedded Data — Full Audit

All data that is currently hardcoded in C# and could change if the game updates. Each item needs to move to BaselineData.json (or a versioned data section) so users can fix it without code changes and the developer can push updates via the migration framework.

### Already in BaselineData.json (stable)
- **ShipClass** — integer Id, Name. Stable.
- **BlueprintType** — string Id (e.g. "Flatpacks/MiningRig"), Name, Properties array, IconPosition, OutputItemType. Stable human-readable Ids.
- **TechLevel** — Name only. Stable.
- **Blueprint** — UUID (to become deterministic), all blueprint data. Migration target.

### Hardcoded in Constants/ — Candidates for Data-Driven

| File | Data | Risk of Change | Migration Concern |
|---|---|---|---|
| `RefiningRecipes.cs` | 6 synthetic refining recipes (S1/S2 tiers) with input/output resources, consume/produce rates | Medium — game could add S3 tier or change rates | Resource names are strings; if game renames a resource, recipe breaks |
| `ResearchTimeLookup.cs` | Research time by evolution level (15 entries, 2-30 days) | Medium — game could rebalance | Simple key-value, easy to move to JSON |
| `GameConstants.cs` | RefiningBaseRate (25), CommoditiesPerCycle (10), CommodityCycleSeconds (600), StructureCap (65), WorkerVolume (50) | Low-Medium — game could rebalance any of these | Simple scalars |
| `BlueprintPropertyValidation.cs` | ~130 property names with types (Integer/Decimal/Boolean/Time/ComboBox) and validation patterns | Medium — game adds new properties regularly | Append-only in practice, but type changes would need migration |
| `BlueprintTypes.cs` | 6 type constants + prefix strings + extension methods (IsFlatpack, IsCommodityFactory) | Low — we control these names | Extension methods would stay in code; constants could reference JSON data |

### Hardcoded in Models/ — Candidates for Data-Driven

| File | Data | Risk of Change | Migration Concern |
|---|---|---|---|
| `Commodity.cs` | ~80 commodities with Name, CommodityGroup, CommodityIndustry, and ConstructionResources (resource→quantity dictionaries) | High — game adds commodities, could change recipes | Largest hardcoded dataset. Resource names are strings. |
| `CommodityGroup.cs` | ~10 commodity groups (enum + Name) | Low — groups are broad categories | Simple list |
| `CommodityIndustry.cs` | ~15 commodity industries (enum + Name) | Low-Medium — game could add industries | Simple list |
| `Resource.cs` | ~30 resources with enum, Name, ResourceClass, ResourceGroup | Low — resource list is fairly stable | Enum-based, would need careful migration if moved to JSON |
| `ResourcePurity.cs` | 5 purity levels (Low/Medium/High/Refined + enum) | Very Low | Unlikely to change |
| `ResourceClass.cs` / `ResourceGroup.cs` | Classification enums | Very Low | Unlikely to change |

### BlueprintType String Ids — Rename Concerns

BlueprintType Ids are developer-controlled strings, not game-derived. The game doesn't name its types "Flatpacks/MiningRig" — we chose those. Renames should be rare and intentional.

If a BlueprintType Id is renamed (e.g. "Flatpacks/MiningRig" → "Flatpacks/MiningRigV2"), the migration needs to:
1. Update the BlueprintType record's Id in BaselineData.json
2. Update every Blueprint's `BluePrintType` field that references the old Id
3. Update the `BlueprintTypes` constants class
4. Recalculate deterministic UUIDs for affected blueprints (BluePrintType is part of the hash input) and remap references

This is handled by the same idempotent rename table — scan blueprints, find old type, update, recalculate hashes. The blast radius is larger than a blueprint name rename but the mechanism is identical.

**Decision: Keep BlueprintType string Ids as-is.** They're human-readable, stable, and developer-controlled. No need for deterministic hashes on types.

### Priority for Moving to Data-Driven

1. **Commodity.cs** (highest priority) — 80 commodities with construction recipes. Largest dataset, most likely to change, users can't fix without code changes.
2. **RefiningRecipes.cs** — 6 recipes, game could add tiers or change rates.
3. **ResearchTimeLookup.cs** — 15 entries, game could rebalance.
4. **GameConstants.cs** — simple scalars, easy to move.
5. **BlueprintPropertyValidation.cs** — large but append-only in practice. Lower priority.
6. **Resource.cs / CommodityGroup.cs / CommodityIndustry.cs** — stable, low priority. Enum-based design makes migration harder.

---

## Alternative Architecture: Split File Approach

Instead of versioning individual records in a single file, split into two files:

- **BaselineData.json** — read-only, ships with the app, never modified by the user. Contains BlueprintTypes, ShipClasses, TechLevels, Resources, commodity recipes, and canonical global blueprints. Replaced wholesale on upgrade.
- **UserBaseline.json** — user's overrides and additions. Created on first run as empty. User imports and manual edits go here. On load, the app merges both files with UserBaseline taking precedence for matching records.

**Pros:**
- Eliminates the merge problem for most cases — developer freely updates BaselineData.json
- User customizations live in a separate file that's never touched by upgrades
- Simple mental model: "app data" vs "my data"
- No per-record versioning needed

**Cons:**
- Merge logic on load (which file wins for a given record?)
- User might not understand why their change in BaselineData.json disappeared after upgrade
- Still need migration code for breaking changes (renames, removed types, UUID remapping)
- BL-029 in the backlog already explored this and noted concerns about merge complexity

**With the dedup key infrastructure now built, the merge complexity concern from BL-029 is largely addressed.** The app already knows how to match blueprints by dedup key. The merge on load would use the same logic.

---

## Versioned Migration Framework

Adding a `DataVersion` integer to PlayerData.json (and BaselineData.json) fundamentally changes the cost analysis for all options above. Migrations become a one-time cost per file, not a recurring cost.

### How It Works

1. App starts, loads PlayerData.json, reads `"DataVersion": 5`
2. App knows the current version is 8
3. Runs migrations 6, 7, 8 in sequence — each does its remap/rename work
4. Sets `"DataVersion": 8`, saves
5. Next launch: version matches, no migrations run

Each migration runs exactly once per file. A user upgrading from v3 to v8 runs migrations 4→5→6→7→8 on first launch. A user already on v7 only runs 8. A user on v8 skips everything.

### Impact on the Hash vs Canonical UUID Decision

With versioned migrations, the "rename is expensive with hashes" argument is neutralized. A rename migration (compute old hash → compute new hash → remap all references) runs once and is done. The code to walk references and remap UUIDs is the same generic utility regardless of whether you're remapping hash→hash (rename) or random-UUID→hash (initial migration).

This tips the balance back toward **deterministic hashes (Option 4)** because hashes have a key advantage that canonical UUIDs don't: **when a user imports a blueprint the developer hasn't catalogued yet, it automatically gets a stable identity.** Every user who imports that same blueprint gets the same hash. No need to wait for the developer to assign a canonical UUID in the next release.

With canonical UUIDs, any blueprint the developer hasn't explicitly assigned an ID to gets a random UUID — and it's back to the original cross-machine inconsistency problem until the next release includes it.

### Generic UUID Remap Utility

A single reusable method handles all migration scenarios:

```csharp
static void RemapUUID(PlayerContext pc, EmpireContext ec, string oldUUID, string newUUID)
{
    // Global blueprints
    var bp = ec.globalBlueprintList.FirstOrDefault(b => b.UUID == oldUUID);
    if (bp != null) bp.UUID = newUUID;

    // Player blueprints
    var pbp = pc.blueprintList.FirstOrDefault(b => b.UUID == oldUUID);
    if (pbp != null) pbp.UUID = newUUID;

    // Evolution chains
    foreach (var b in ec.globalBlueprintList.Concat(pc.blueprintList))
        if (b.BaseBlueprintUUID == oldUUID) b.BaseBlueprintUUID = newUUID;

    // Colony structure references
    foreach (var colony in pc.colonyList)
        foreach (var s in colony.Structures)
        {
            if (s.FlatpackBlueprintUUID == oldUUID) s.FlatpackBlueprintUUID = newUUID;
            if (s.ResearchingBlueprintUUID == oldUUID) s.ResearchingBlueprintUUID = newUUID;
            if (s.ManufacturingBlueprintUUID == oldUUID) s.ManufacturingBlueprintUUID = newUUID;
        }
}
```

### Renames Are Idempotent — No Version Gating Needed

Blueprint renames don't need to be separated into per-version migration steps. The rename table is a flat list that grows over time and always runs safely on every load:

```csharp
static readonly List<(string OldName, string NewName, int Evolution, string BluePrintType, int Class, string TechLevel)> Renames = new()
{
    ("Mining Rig", "Mining Rig Flatpack", 0, "Flatpacks/MiningRig", 0, null),
    ("Habitat", "Habitation Block Flatpack", 0, "Flatpacks/HabitationBlock", 0, null),
    ("Entertainment Centre", "Entertainment Centre Flatpack", 0, "Flatpacks/EntertainmentCentreFlatpack", 0, null),
};
```

For each entry: compute old hash from old name + other fields, compute new hash from new name + other fields, call `RemapUUID(oldHash, newHash)`. If the old hash isn't found (already migrated, or user never had that blueprint), it's a no-op. No harm done.

This means:
- Renames run on every load, outside the version-gated migration framework
- The rename table is append-only — new renames are added, old ones are never removed
- No need to track which renames have been applied — the hash lookup handles it
- A user upgrading from any version to any version gets all renames applied correctly

The `DataVersion` field is still needed for non-idempotent migrations (adding new fields with defaults, restructuring data, initial random-UUID→hash migration). But renames are the most common maintenance task and they're free of version complexity.

### Revised Migration Architecture

Two separate mechanisms:

1. **Rename table** (idempotent, runs every load) — flat list of old name → new name entries. Computes old/new hashes, remaps if found. Append-only, no version tracking.

2. **Versioned migrations** (non-idempotent, runs once per version bump) — sequential steps gated by `DataVersion`. Used for:
   - Initial migration: random UUIDs → deterministic hashes (v0 → v1)
   - Schema changes: adding new fields, restructuring data
   - Data changes that aren't renames (e.g. correcting a property value that was wrong)

```csharp
// On load:
ApplyRenames(pc, ec);           // Always runs, idempotent
ApplyVersionedMigrations(pc);   // Runs pending migrations based on DataVersion
```

### Revised Recommendation

With versioned migrations:

1. **Deterministic UUIDs (Option 4)** — best fit. Self-describing identity, no canonical file to maintain, new blueprints automatically stable across machines
2. **DataVersion on both PlayerData.json and BaselineData.json** — triggers sequential migrations on load
3. **Generic RemapUUID utility** — one implementation, reused by all migration steps
4. **Idempotent rename table** — flat append-only list, runs every load, no version gating
5. **Versioned migration registry** — for non-idempotent changes (initial UUID→hash, schema changes)
6. **Single BaselineData.json** — no file split. The split-file approach (BL-029) adds merge-on-load complexity, "which file wins" ambiguity, and user confusion about where data lives. With deterministic UUIDs and versioned migrations, a single file handles upgrades cleanly.
7. **Move code-embedded data to BaselineData.json** — commodity recipes, refining recipes, game constants

---

## Open Questions

1. Should the split be BaselineData.json + UserBaseline.json, or should global blueprints move entirely to a separate file (e.g. GlobalBlueprints.json)? **RESOLVED — single file, no split.**
2. For the deterministic UUID scheme, what namespace UUID should be used for the v5 generation?
3. How should the migration framework handle the first migration for existing users who already have random UUIDs? **RESOLVED — see below.**
4. Should BlueprintTypes also get deterministic IDs, or are their string IDs (e.g. "Flatpacks/MiningRig") already stable enough? **RESOLVED — keep string Ids. They're human-readable, developer-controlled, and stable. Renames handled by the same idempotent rename table.**
5. What's the priority ordering — should identity stabilization happen before or after the file split? **RESOLVED — no file split, identity stabilization is the main work.**

### Resolved: Initial Migration for Existing Random UUIDs

The first migration is straightforward:

1. Scan all existing global blueprints
2. For each: compute the deterministic UUID from its dedup fields (Name, Evolution, BluePrintType, Class, TechLevel)
3. If the current UUID doesn't match the deterministic UUID: call `RemapUUID(oldUUID, newUUID)` across all references
4. Store the old UUID in a new `LegacyUUID` field on the Blueprint before overwriting

The `LegacyUUID` field is write-once during migration and read-only after. It serves as a safety net for:
- Users with multiple PlayerData files that reference the old UUIDs
- External tools or exports that used the old UUIDs
- Debugging migration issues ("what was this blueprint's old identity?")

This migration is gated by `DataVersion` (runs once, v0 → v1). After migration, all global blueprints have deterministic UUIDs and all references are updated. The `LegacyUUID` field persists in the JSON but is never used for lookups — it's purely historical.
