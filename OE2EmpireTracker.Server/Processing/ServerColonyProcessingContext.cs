using Newtonsoft.Json;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Processing;

/// <summary>
/// Server-side implementation of <see cref="IColonyProcessingContext"/>.
/// Loads character data from the storage backend and provides lookups
/// needed by <see cref="Colony.ProcessColony"/>.
/// </summary>
public class ServerColonyProcessingContext : IColonyProcessingContext
{
    private readonly IStorageBackend _storage;
    private readonly string _characterUUID;
    private List<Blueprint> _blueprints = new List<Blueprint>();
    private List<Survey> _surveys = new List<Survey>();
    private List<PlayerProfile> _profiles = new List<PlayerProfile>();
    private List<BlueprintType> _blueprintTypes = new List<BlueprintType>();
    private bool _blueprintsModified;

    public ServerColonyProcessingContext(IStorageBackend storage, string characterUUID)
    {
        _storage = storage;
        _characterUUID = characterUUID;
    }

    /// <summary>
    /// Gets a value indicating whether blueprints were modified during processing
    /// (e.g. research evolution created a new blueprint).
    /// </summary>
    public bool BlueprintsModified => _blueprintsModified;

    /// <summary>
    /// Loads all supporting data from storage. Must be called before ProcessColony.
    /// </summary>
    public async Task LoadDataAsync()
    {
        var bpJson = await _storage.GetCharacterDataAsync(_characterUUID, "Blueprints");
        _blueprints = bpJson != null
            ? JsonConvert.DeserializeObject<List<Blueprint>>(bpJson) ?? new List<Blueprint>()
            : new List<Blueprint>();

        var surveyJson = await _storage.GetCharacterDataAsync(_characterUUID, "Surveys");
        _surveys = surveyJson != null
            ? JsonConvert.DeserializeObject<List<Survey>>(surveyJson) ?? new List<Survey>()
            : new List<Survey>();

        var profileJson = await _storage.GetCharacterDataAsync(_characterUUID, "PlayerProfiles");
        _profiles = profileJson != null
            ? JsonConvert.DeserializeObject<List<PlayerProfile>>(profileJson) ?? new List<PlayerProfile>()
            : new List<PlayerProfile>();

        var btJson = await _storage.GetGlobalDataAsync("BlueprintTypes");
        _blueprintTypes = btJson != null
            ? JsonConvert.DeserializeObject<List<BlueprintType>>(btJson) ?? new List<BlueprintType>()
            : new List<BlueprintType>();
    }

    /// <inheritdoc/>
    public ReadOnlyBlueprint? FindBlueprint(string uuid)
    {
        var bp = _blueprints.FirstOrDefault(b => b.UUID == uuid);
        return bp != null ? new ReadOnlyBlueprint(bp) : null;
    }

    /// <inheritdoc/>
    public Survey? FindSurvey(string uuid)
    {
        return _surveys.FirstOrDefault(s => s.UUID == uuid);
    }

    /// <inheritdoc/>
    public PlayerProfile? FindPlayerProfile(string ownerUUID)
    {
        return _profiles.FirstOrDefault(p => p.UUID == ownerUUID);
    }

    /// <inheritdoc/>
    public BlueprintType? FindBlueprintType(string typeName)
    {
        return _blueprintTypes.FirstOrDefault(bt => bt.Name == typeName);
    }

    /// <inheritdoc/>
    public void AddBlueprint(Blueprint blueprint)
    {
        _blueprints.Add(blueprint);
        _blueprintsModified = true;
    }

    /// <summary>
    /// Persists modified blueprints back to storage if any were added during processing.
    /// </summary>
    public async Task SaveBlueprintsIfModifiedAsync()
    {
        if (!_blueprintsModified)
        {
            return;
        }

        var json = JsonConvert.SerializeObject(_blueprints, OE2EmpireTracker.Services.JsonSettings.SerializerSettings);
        await _storage.UpsertCharacterDataAsync(_characterUUID, "Blueprints", json);
    }
}
