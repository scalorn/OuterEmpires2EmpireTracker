# Discussion: BaselineData.json — Long-Term Maintainability, Upgradability, and Stability

## Problem Statement

BaselineData.json is currently a mutable file that ships with the app, gets modified by user imports, and has no versioning or identity scheme that survives across installations. With 100+ users, the developer needs it to be all three of: upgradable by the developer, preserving of user customizations, and consistent across different machines.

## Sub-Problems

### 1. Global Blueprint Identity

Global blueprints currently get random UUIDs assigned on import. Each user's machine has different UUIDs for the same blueprint. If the developer ships a new BaselineData.json, all UUID references (colony structures pointing to FlatpackBlueprintUUID, evolution chains via BaseBlueprintUUID, surveys via ScannerBlueprintUUID) break.

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

The developer assigns UUIDs to global blueprints and those UUIDs are the "official" canonical ones. The app ships with a canonical baseline. On first run or upgrade, the app merges the canonical data into the user's file, matching by dedup key. If a user imported the same blueprint with a different UUID, the migration remaps all references (colony structures, evolution chains, surveys) from the user's UUID to the canonical one.

**Pros:**
- UUIDs stay as UUIDs — no format changes, no compound key threading
- Clean separation: the developer controls the canonical identity
- One-time migration per version bump

**Cons:**
- Migration code that walks all references (colony structures, evolution chains, surveys) to remap UUIDs
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

## Code-Embedded Data

Anything that might change should be data-driven, not code-embedded. Candidates:

- `RefiningRecipes.cs` — synthetic refining recipes and rates
- `Commodity.ConstructionResources` — resources needed to construct each commodity
- `GameConstants.cs` — some values here are game-derived and could change (RefiningBaseRate, CommoditiesPerCycle, etc.)

These should move into BaselineData.json (or a separate data file) and participate in the same versioning scheme.

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

## Recommendation

A combination approach:

1. **Deterministic UUIDs (Option 4)** for global blueprint identity — simplest, no schema change, no canonical file to maintain
2. **Split file** for upgrade safety — BaselineData.json is read-only app data, UserBaseline.json holds user overrides
3. **Version number on BaselineData.json** (file-level, not per-record) — triggers migration on upgrade
4. **Declarative migration framework** — rename tables, UUID remapping, applied sequentially by version
5. **Move code-embedded data to BaselineData.json** — commodity recipes, refining recipes, game constants

This gives you: stable identity across machines, safe upgrades that don't destroy user data, a path for the developer to push changes, and data-driven configuration that users can fix without code changes.

---

## Open Questions

1. Should the split be BaselineData.json + UserBaseline.json, or should global blueprints move entirely to a separate file (e.g. GlobalBlueprints.json)?
2. For the deterministic UUID scheme, what namespace UUID should be used for the v5 generation?
3. How should the migration framework handle the first migration for existing users who already have random UUIDs?
4. Should BlueprintTypes also get deterministic IDs, or are their string IDs (e.g. "Flatpacks/MiningRig") already stable enough?
5. What's the priority ordering — should identity stabilization happen before or after the file split?
