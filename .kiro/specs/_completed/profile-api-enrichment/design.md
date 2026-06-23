# Design Document

## Overview

This feature enriches the local PlayerProfile, PlayerRank, and PlayerSkill data models to align with the Game API's field naming and adds previously-unmodeled fields. The merge/sync logic in GameApiSyncScheduler is extended to populate all new fields, backward-compatible deserialization handles renamed fields, and the Player Profile UI displays rank names and skill metadata.

Satisfies: Requirements 1–13.

## Architecture

### Affected Layers

```
Game API (external)
    ↓ JSON response
DTOs (GameApiProfileResponse, GameApiRankResponse, GameApiSkillResponse, GameApiSkillInTrainingResponse)
    ↓ deserialized
Merge Logic (GameApiSyncScheduler.MergeProfileData / MergeRank / MergeSkills)
    ↓ mutates
Models (PlayerProfile, PlayerRank, PlayerSkill)
    ↓ wrapped by
ReadOnly Wrappers (ReadOnlyPlayerProfile, ReadOnlyPlayerRank, ReadOnlyPlayerSkill)
    ↓ consumed by
ViewModels (PlayerProfileViewModel, LocalRankData, LocalSkillData)
    ↓ displayed by
Forms (FormPlayerProfile, PlayerSkillBlock)
```

### Data Flow: Profile Sync

```
1. GameApiSyncScheduler.SyncCharacterAsync(playerUUID)
2. → client.GetCharacterAsync() → JSON
3. → Deserialize to GameApiProfileResponse (with new fields)
4. → MergeProfileData(local, remote)
   4a. Merge scalar fields (CharacterId, FirstName, LastName, ActiveTimeMinutes)
   4b. MergeRank × 3 (now includes RankName, XpToNextLevel, CurrentXp)
   4c. MergeSkills (now includes metadata + training progress)
5. → PlayerContext.WriteContext() → PlayerData.json
6. → PlayerContext.OnPlayerProfileDataChanged(uuid)
7. → FormPlayerProfile.OnPlayerProfileDataChanged → PopulateForm()
```

## Detailed Design

### 1. Model Changes

#### PlayerProfile (OE2EmpireTracker.Common/Models/PlayerProfile.cs)

Add new properties after existing `ActiveTime`:

```csharp
public int CharacterId { get; set; } = 0;
public string FirstName { get; set; } = string.Empty;
public string LastName { get; set; } = string.Empty;
public int ActiveTimeMinutes { get; set; } = 0;
```

No changes to existing properties. New fields default to safe values for backward compatibility.

#### PlayerRank (OE2EmpireTracker.Common/Models/PlayerRank.cs)

Rename properties and add backward-compatible deserialization:

```csharp
public class PlayerRank
{
    public int Rank { get; set; } = 0;

    [JsonProperty("CurrentXp")]
    public long CurrentXp { get; set; } = 0;

    [JsonProperty("XpToNextLevel")]
    public long XpToNextLevel { get; set; } = 0;

    [JsonProperty("RankName")]
    public string RankName { get; set; } = string.Empty;
}
```

Backward compatibility via a custom JsonConverter or `[JsonExtensionData]` approach — see Section 3.

#### PlayerSkill (OE2EmpireTracker.Common/Models/PlayerSkill.cs)

Add metadata and training progress properties:

```csharp
public class PlayerSkill
{
    // Existing
    public int Level { get; set; } = 0;
    public bool TrainingStarted { get; set; } = false;
    public CountDownTime CompletionTime { get; set; } = new CountDownTime();

    // New: Skill metadata (Req 6)
    public int SkillId { get; set; } = 0;
    public string EffectDescription { get; set; } = string.Empty;
    public int AmountPerLevel { get; set; } = 0;
    public string SkillGroupName { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; } = false;

    // New: Training progress from API (Req 7)
    public int TargetLevel { get; set; } = 0;
    public int TrainingPercentageComplete { get; set; } = 0;
    public int RemainingMinutes { get; set; } = 0;
}
```


### 2. DTO Changes (OE2EmpireTracker.Common/Models/GameApiProfileResponse.cs)

#### GameApiProfileResponse — add fields:

```csharp
[JsonProperty("characterId")]
public int CharacterId { get; set; }

[JsonProperty("firstName")]
public string FirstName { get; set; }

[JsonProperty("lastName")]
public string LastName { get; set; }

[JsonProperty("activeTimeMinutes")]
public int ActiveTimeMinutes { get; set; }

[JsonProperty("skillInTraining")]
public GameApiSkillInTrainingResponse SkillInTraining { get; set; }
```

#### GameApiRankResponse — add fields:

```csharp
[JsonProperty("levelName")]
public string LevelName { get; set; }

[JsonProperty("xpToNextLevel")]
public long XpToNextLevel { get; set; }

[JsonProperty("currentXp")]
public long CurrentXp { get; set; }
```

#### GameApiSkillResponse — add fields:

```csharp
[JsonProperty("skillId")]
public int SkillId { get; set; }

[JsonProperty("effectDescription")]
public string EffectDescription { get; set; }

[JsonProperty("amountPerLevel")]
public int AmountPerLevel { get; set; }

[JsonProperty("skillGroupName")]
public string SkillGroupName { get; set; }

[JsonProperty("isUnlocked")]
public bool IsUnlocked { get; set; }
```

#### New DTO: GameApiSkillInTrainingResponse

```csharp
public class GameApiSkillInTrainingResponse
{
    [JsonProperty("skillName")]
    public string SkillName { get; set; }

    [JsonProperty("targetLevel")]
    public int TargetLevel { get; set; }

    [JsonProperty("trainingPercentageComplete")]
    public int TrainingPercentageComplete { get; set; }

    [JsonProperty("remainingMinutes")]
    public int RemainingMinutes { get; set; }
}
```

### 3. Backward-Compatible Deserialization (PlayerRank Renames)

Strategy: Use `[JsonProperty]` for the new canonical name and add a private setter method via `[JsonExtensionData]` or a custom `JsonConverter` to handle old field names.

Recommended approach — **PlayerRankJsonConverter**:

```csharp
[JsonConverter(typeof(PlayerRankJsonConverter))]
public class PlayerRank { ... }
```

The converter:
1. Reads the JSON object into a JObject.
2. Maps `"Title"` → `RankName` (only if `"RankName"` is absent).
3. Maps `"NextXP"` → `XpToNextLevel` (only if `"XpToNextLevel"` is absent).
4. Maps `"CurrentXP"` → `CurrentXp` (only if `"CurrentXp"` is absent).
5. Writes using new canonical names only.

This satisfies Req 12 criteria 2–5: old names are read, new names win on conflict, serialization always uses new names.

### 4. Merge Logic Changes (GameApiSyncScheduler)

#### MergeProfileData — add after existing merges:

```csharp
// Merge CharacterId (Req 1, 9.1)
if (remote.CharacterId != 0 && remote.CharacterId != local.CharacterId)
{
    local.CharacterId = remote.CharacterId;
    changed = true;
}

// Merge FirstName (Req 2, 9.2)
if (remote.FirstName != null && remote.FirstName != local.FirstName)
{
    local.FirstName = remote.FirstName;
    changed = true;
}

// Merge LastName (Req 2, 9.2)
if (remote.LastName != null && remote.LastName != local.LastName)
{
    local.LastName = remote.LastName;
    changed = true;
}

// Merge ActiveTimeMinutes (Req 3, 9.3)
if (remote.ActiveTimeMinutes != 0 || local.ActiveTimeMinutes != 0)
{
    int value = remote.ActiveTimeMinutes < 0 ? 0 : remote.ActiveTimeMinutes;
    if (value != local.ActiveTimeMinutes)
    {
        local.ActiveTimeMinutes = value;
        changed = true;
    }
}
```

#### MergeRank — update to use new property names:

```csharp
private static bool MergeRank(PlayerRank localRank, GameApiRankResponse remoteRank, string rankName)
{
    if (localRank == null || remoteRank == null) return false;
    bool changed = false;

    if (remoteRank.Level != localRank.Rank)
    {
        localRank.Rank = remoteRank.Level;
        changed = true;
    }

    // RankName from LevelName (Req 4, 9.4)
    if (remoteRank.LevelName != null && remoteRank.LevelName != localRank.RankName)
    {
        localRank.RankName = remoteRank.LevelName;
        changed = true;
    }

    // XpToNextLevel (Req 5, 9.5)
    if (remoteRank.XpToNextLevel != localRank.XpToNextLevel)
    {
        localRank.XpToNextLevel = remoteRank.XpToNextLevel;
        changed = true;
    }

    // CurrentXp (Req 5, 9.5)
    if (remoteRank.CurrentXp != localRank.CurrentXp)
    {
        localRank.CurrentXp = remoteRank.CurrentXp;
        changed = true;
    }

    return changed;
}
```


#### MergeSkills — extend to merge metadata and training progress:

```csharp
private static bool MergeSkills(
    PlayerProfile local,
    Dictionary<string, GameApiSkillResponse> remoteSkills,
    GameApiSkillInTrainingResponse skillInTraining)
{
    bool changed = false;

    foreach (var kvp in remoteSkills)
    {
        string skillName = kvp.Key;
        GameApiSkillResponse remoteSkill = kvp.Value;
        PlayerSkill localSkill = local.GetSkill(skillName);

        // Existing: merge Level
        if (remoteSkill.Level != localSkill.Level)
        {
            localSkill.Level = remoteSkill.Level;
            changed = true;
        }

        // New: merge metadata (Req 6, 9.6)
        if (remoteSkill.SkillId != localSkill.SkillId)
        {
            localSkill.SkillId = remoteSkill.SkillId;
            changed = true;
        }

        string effectDesc = remoteSkill.EffectDescription ?? string.Empty;
        if (effectDesc != localSkill.EffectDescription)
        {
            localSkill.EffectDescription = effectDesc;
            changed = true;
        }

        if (remoteSkill.AmountPerLevel != localSkill.AmountPerLevel)
        {
            localSkill.AmountPerLevel = remoteSkill.AmountPerLevel;
            changed = true;
        }

        string groupName = remoteSkill.SkillGroupName ?? string.Empty;
        if (groupName != localSkill.SkillGroupName)
        {
            localSkill.SkillGroupName = groupName;
            changed = true;
        }

        if (remoteSkill.IsUnlocked != localSkill.IsUnlocked)
        {
            localSkill.IsUnlocked = remoteSkill.IsUnlocked;
            changed = true;
        }

        // New: merge training progress (Req 7, 9.7, 9.8)
        bool isTraining = skillInTraining != null &&
            string.Equals(skillInTraining.SkillName, skillName, StringComparison.OrdinalIgnoreCase);

        int targetLevel = isTraining ? skillInTraining.TargetLevel : 0;
        int pctComplete = isTraining ? skillInTraining.TrainingPercentageComplete : 0;
        int remainingMin = isTraining ? skillInTraining.RemainingMinutes : 0;

        if (targetLevel != localSkill.TargetLevel)
        {
            localSkill.TargetLevel = targetLevel;
            changed = true;
        }

        if (pctComplete != localSkill.TrainingPercentageComplete)
        {
            localSkill.TrainingPercentageComplete = pctComplete;
            changed = true;
        }

        if (remainingMin != localSkill.RemainingMinutes)
        {
            localSkill.RemainingMinutes = remainingMin;
            changed = true;
        }
    }

    // Reset training fields for skills NOT in remoteSkills (Req 7.3, 7.4)
    foreach (var kvp in local.Skills)
    {
        if (!remoteSkills.ContainsKey(kvp.Key))
        {
            PlayerSkill skill = kvp.Value;
            if (skill.TargetLevel != 0 || skill.TrainingPercentageComplete != 0 || skill.RemainingMinutes != 0)
            {
                skill.TargetLevel = 0;
                skill.TrainingPercentageComplete = 0;
                skill.RemainingMinutes = 0;
                changed = true;
            }
        }
    }

    return changed;
}
```

The MergeProfileData caller passes `remote.SkillInTraining` to MergeSkills:

```csharp
changed |= MergeSkills(local, remote.Skills, remote.SkillInTraining);
```

### 5. ReadOnly Wrapper Changes

#### ReadOnlyPlayerRank

Replace existing properties:

```csharp
public int Rank => _entity.Rank;
public long CurrentXp => _entity.CurrentXp;        // was CurrentXP
public long XpToNextLevel => _entity.XpToNextLevel; // was NextXP
public string RankName => _entity.RankName;         // was Title
public override string ToString() => _entity.RankName;
```

#### ReadOnlyPlayerSkill — add new properties:

```csharp
public int SkillId => _entity.SkillId;
public string EffectDescription => _entity.EffectDescription;
public int AmountPerLevel => _entity.AmountPerLevel;
public string SkillGroupName => _entity.SkillGroupName;
public bool IsUnlocked => _entity.IsUnlocked;
public int TargetLevel => _entity.TargetLevel;
public int TrainingPercentageComplete => _entity.TrainingPercentageComplete;
public int RemainingMinutes => _entity.RemainingMinutes;
```

#### ReadOnlyPlayerProfile — add new properties:

```csharp
public int CharacterId => _entity.CharacterId;
public string FirstName => _entity.FirstName;
public string LastName => _entity.LastName;
public int ActiveTimeMinutes => _entity.ActiveTimeMinutes;
```


### 6. ViewModel / Edit Buffer Changes

#### LocalRankData — rename properties:

```csharp
public class LocalRankData
{
    public int Rank { get; set; }
    public long CurrentXp { get; set; }        // was CurrentXP
    public long XpToNextLevel { get; set; }    // was NextXP
    public string RankName { get; set; } = string.Empty;  // was Title
}
```

#### LocalSkillData — add metadata properties (read-only from API, not editable):

```csharp
// New metadata fields (populated from ReadOnlyPlayerSkill, not editable in UI)
public string EffectDescription { get; set; } = string.Empty;
public int AmountPerLevel { get; set; }
public int TrainingPercentageComplete { get; set; }
public int RemainingMinutes { get; set; }
```

#### PlayerProfileViewModel

- Update `CopyRank` to use new property names (CurrentXp, XpToNextLevel, RankName).
- Update `IsRankDirty` to compare new property names.
- Update `LoadFrom` to copy new skill metadata fields into LocalSkillData.
- Update `BuildUpdateRequest` / `BuildCreateRequest` to use new property names.

### 7. PlayerProfileService Changes

#### ApplyRank — rename parameters:

```csharp
private static void ApplyRank(PlayerRank rank, int level, long curXp, long xpToNext, string rankName)
{
    rank.Rank = level;
    rank.CurrentXp = curXp;
    rank.XpToNextLevel = xpToNext;
    rank.RankName = rankName;
}
```

#### PlayerProfileUpdateRequest / PlayerProfileCreateRequest

Rename fields to match:
- `PublicTitle` → `PublicRankName`, `PublicCurrentXP` → `PublicCurrentXp`, `PublicNextXP` → `PublicXpToNextLevel`
- Same pattern for Private and Military.

### 8. UI Changes

#### FormPlayerProfile — Rank Name Labels (Req 10)

Add three read-only labels to the Designer:
- `lblPublicRankName` — positioned after `txtPublicRank`
- `lblPrivateRankName` — positioned after `txtPrivateRank`
- `lblMilitaryRankName` — positioned after `txtMilitaryRank`

In `PopulateForm()`:
```csharp
UpdateRankNameLabel(lblPublicRankName, viewModel.PublicRank.RankName);
UpdateRankNameLabel(lblPrivateRankName, viewModel.PrivateRank.RankName);
UpdateRankNameLabel(lblMilitaryRankName, viewModel.MilitaryRank.RankName);
```

Helper:
```csharp
private void UpdateRankNameLabel(Label label, string rankName)
{
    if (string.IsNullOrEmpty(rankName))
    {
        label.Visible = false;
    }
    else
    {
        label.Text = rankName;
        label.Visible = true;
    }
}
```

#### PlayerSkillBlock — Skill Metadata Display (Req 11)

Add labels to the Designer:
- `lblEffectDescription` — displays effect text below skill name
- `lblAmountPerLevel` — displays "+X% per level"
- `lblTrainingProgress` — displays "X%" training percentage
- `lblRemainingTime` — displays countdown format

In `PopulateForm()`, after existing logic:
```csharp
// Effect description (Req 11.1)
bool hasEffect = !string.IsNullOrEmpty(_skillData.EffectDescription);
lblEffectDescription.Text = _skillData.EffectDescription;
lblEffectDescription.Visible = hasEffect;

// Amount per level (Req 11.2)
bool hasAmount = _skillData.AmountPerLevel > 0;
lblAmountPerLevel.Text = string.Format("+{0}% per level", _skillData.AmountPerLevel);
lblAmountPerLevel.Visible = hasAmount;

// Training percentage (Req 11.3)
bool hasTrainingPct = _skillData.TrainingPercentageComplete > 0;
lblTrainingProgress.Text = string.Format("{0}%", _skillData.TrainingPercentageComplete);
lblTrainingProgress.Visible = hasTrainingPct;

// Remaining time (Req 11.4)
bool hasRemaining = _skillData.RemainingMinutes > 0;
lblRemainingTime.Text = FormatMinutesAsCountdown(_skillData.RemainingMinutes);
lblRemainingTime.Visible = hasRemaining;
```

Helper for countdown formatting:
```csharp
private static string FormatMinutesAsCountdown(int totalMinutes)
{
    int days = totalMinutes / (24 * 60);
    int hours = (totalMinutes % (24 * 60)) / 60;
    int minutes = totalMinutes % 60;

    string result = string.Empty;
    if (days > 0) result += string.Format("{0}d ", days);
    if (days > 0 || hours > 0) result += string.Format("{0}h ", hours);
    result += string.Format("{0}m", minutes);
    // Seconds not available from API (minutes granularity), append "0s" for consistency
    result += " 0s";
    return result.Trim();
}
```

### 9. Consumer Rename Summary (Req 13)

All references to old property names must be updated:

| Old Name | New Name | Affected Files |
|----------|----------|----------------|
| `PlayerRank.Title` | `PlayerRank.RankName` | PlayerProfileService, PlayerProfileViewModel, LocalRankData, FormPlayerProfile, PlayerProfileEndpoints, PlayerProfileParser, ReadOnlyPlayerRank, tests |
| `PlayerRank.CurrentXP` | `PlayerRank.CurrentXp` | Same as above |
| `PlayerRank.NextXP` | `PlayerRank.XpToNextLevel` | Same as above |
| `ReadOnlyPlayerRank.Title` | `ReadOnlyPlayerRank.RankName` | ViewModel, Form, tests |
| `ReadOnlyPlayerRank.CurrentXP` | `ReadOnlyPlayerRank.CurrentXp` | ViewModel, Form, tests |
| `ReadOnlyPlayerRank.NextXP` | `ReadOnlyPlayerRank.XpToNextLevel` | ViewModel, Form, tests |
| `LocalRankData.Title` | `LocalRankData.RankName` | ViewModel, Form, request builders |
| `LocalRankData.CurrentXP` | `LocalRankData.CurrentXp` | ViewModel, Form, request builders |
| `LocalRankData.NextXP` | `LocalRankData.XpToNextLevel` | ViewModel, Form, request builders |

### 10. Server Endpoint Changes (PlayerProfileEndpoints)

The server's `PlayerProfileEndpoints` maps between HTTP request/response and the shared models. After the rename:
- Update all references to `Title` → `RankName`, `CurrentXP` → `CurrentXp`, `NextXP` → `XpToNextLevel` in the endpoint projection logic.
- The wire format (JSON over HTTP) uses camelCase per the server serialization config, so the API contract changes from `title`/`currentXP`/`nextXP` to `rankName`/`currentXp`/`xpToNextLevel`.

## File Change Summary

| File | Change Type |
|------|-------------|
| `OE2EmpireTracker.Common/Models/PlayerProfile.cs` | Add 4 properties |
| `OE2EmpireTracker.Common/Models/PlayerRank.cs` | Rename 3 properties, add JsonConverter |
| `OE2EmpireTracker.Common/Models/PlayerSkill.cs` | Add 8 properties |
| `OE2EmpireTracker.Common/Models/GameApiProfileResponse.cs` | Add fields to 3 DTOs, add new DTO |
| `OE2EmpireTracker.Common/Models/ReadOnlyPlayerProfile.cs` | Add 4 properties |
| `OE2EmpireTracker.Common/Models/ReadOnlyPlayerRank.cs` | Rename 3 properties |
| `OE2EmpireTracker.Common/Models/ReadOnlyPlayerSkill.cs` | Add 8 properties |
| `OE2EmpireTracker.Common/Models/PlayerRankJsonConverter.cs` | New file |
| `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs` | Extend merge methods |
| `OE2EmpireTracker.Common/Services/PlayerProfileService.cs` | Rename in ApplyRank |
| `OE2EmpireTracker.Common/ViewModels/LocalRankData.cs` | Rename 3 properties |
| `OE2EmpireTracker.Common/ViewModels/LocalSkillData.cs` | Add 4 properties |
| `OE2EmpireTracker.Common/ViewModels/PlayerProfileViewModel.cs` | Update property refs |
| `OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.cs` | Add rank labels, update refs |
| `OE2EmpireTracker/Forms/PlayerProfile/FormPlayerProfile.Designer.cs` | Add 3 labels |
| `OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.cs` | Add metadata display |
| `OE2EmpireTracker/Forms/PlayerProfile/PlayerSkillBlock.Designer.cs` | Add 4 labels |
| `OE2EmpireTracker.Server/Endpoints/Typed/PlayerProfileEndpoints.cs` | Update property refs |
| `OE2EmpireTracker.Common/Parsers/PlayerProfileParser.cs` | Update property refs |
| `OE2EmpireTracker.Tests/*` | Update all test refs to new names |

## Testing Strategy

1. **Unit tests for PlayerRankJsonConverter**: Verify old names map to new properties, new names take precedence on conflict, invalid types default gracefully.
2. **Unit tests for MergeProfileData**: Verify all new fields are merged with "API wins" strategy, null fields leave local unchanged, negative ActiveTimeMinutes clamps to 0.
3. **Unit tests for MergeSkills**: Verify metadata fields merge, training progress sets on matching skill and resets on non-matching skills.
4. **Backward compatibility tests**: Load a PlayerData.json with old field names, verify correct mapping.
5. **Round-trip serialization tests**: Save and reload, verify new canonical names are used.
6. **Existing tests updated**: All tests referencing Title/CurrentXP/NextXP updated to new names.
