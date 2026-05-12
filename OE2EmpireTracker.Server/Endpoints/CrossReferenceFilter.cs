using System.Text.Json;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Filters cross-references in shared data to remove references to entities
/// the viewer does not have access to (Req 15).
/// Scans JSON objects for properties ending in "UUID" and nulls out values
/// that reference entities the viewer cannot access.
/// </summary>
public static class CrossReferenceFilter
{
    private const string UuidSuffix = "UUID";

    /// <summary>
    /// Filters a list of JSON elements by nulling out UUID references that the viewer
    /// cannot access. Builds an accessible-UUID set from the viewer's own data and
    /// data explicitly shared with them.
    /// </summary>
    public static async Task<List<JsonElement>> FilterAsync(
        List<JsonElement> items,
        string viewerCharacterUUID,
        IStorageBackend storage)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var accessibleUUIDs = await BuildAccessibleSetAsync(viewerCharacterUUID, storage);
        var filtered = new List<JsonElement>(items.Count);

        foreach (var item in items)
        {
            var filteredItem = FilterElement(item, accessibleUUIDs);
            filtered.Add(filteredItem);
        }

        return filtered;
    }

    private static async Task<HashSet<string>> BuildAccessibleSetAsync(
        string viewerCharacterUUID,
        IStorageBackend storage)
    {
        var accessible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The viewer's own character UUID is always accessible
        accessible.Add(viewerCharacterUUID);

        // Extract all UUIDs from the viewer's own data
        var viewerData = await storage.GetAllCharacterDataAsync(viewerCharacterUUID);
        if (viewerData != null)
        {
            ExtractUUIDsFromJson(viewerData, accessible);
        }

        // Extract UUIDs from data shared with the viewer
        var allCharacters = await storage.GetAllCharactersAsync();
        foreach (var character in allCharacters)
        {
            if (character.UUID == viewerCharacterUUID)
            {
                continue;
            }

            var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
            var sharesWithViewer = rules.Any(r =>
                (r.TargetType == SharingTargetType.Character && r.TargetUUID == viewerCharacterUUID) ||
                (r.TargetType == SharingTargetType.Faction));

            if (!sharesWithViewer)
            {
                continue;
            }

            var charData = await storage.GetAllCharacterDataAsync(character.UUID);
            if (charData != null)
            {
                ExtractUUIDsFromJson(charData, accessible);
            }
        }

        // Global data UUIDs are always accessible
        var globalBlueprintTypes = await storage.GetGlobalDataAsync("BlueprintTypes");
        if (globalBlueprintTypes != null)
        {
            ExtractUUIDsFromJson(globalBlueprintTypes, accessible);
        }

        return accessible;
    }

    private static void ExtractUUIDsFromJson(string json, HashSet<string> uuids)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            ExtractUUIDsFromElement(doc.RootElement, uuids);
        }
        catch (JsonException)
        {
            // Skip malformed JSON
        }
    }

    private static void ExtractUUIDsFromElement(JsonElement element, HashSet<string> uuids)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "UUID" || property.Name.EndsWith(UuidSuffix, StringComparison.Ordinal))
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var val = property.Value.GetString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                uuids.Add(val);
                            }
                        }
                    }

                    ExtractUUIDsFromElement(property.Value, uuids);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractUUIDsFromElement(item, uuids);
                }

                break;
        }
    }

    private static JsonElement FilterElement(JsonElement element, HashSet<string> accessibleUUIDs)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return element;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteFilteredObject(writer, element, accessibleUUIDs);
        }

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static void WriteFilteredObject(
        Utf8JsonWriter writer,
        JsonElement element,
        HashSet<string> accessibleUUIDs)
    {
        writer.WriteStartObject();

        foreach (var property in element.EnumerateObject())
        {
            // Check if this is a cross-reference UUID property (not the entity's own UUID)
            if (IsCrossReferenceProperty(property.Name) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                var refValue = property.Value.GetString();
                if (!string.IsNullOrEmpty(refValue) && !accessibleUUIDs.Contains(refValue))
                {
                    // Null out inaccessible reference
                    writer.WriteNull(property.Name);
                    continue;
                }
            }

            writer.WritePropertyName(property.Name);
            WriteFilteredValue(writer, property.Value, accessibleUUIDs);
        }

        writer.WriteEndObject();
    }

    private static void WriteFilteredValue(
        Utf8JsonWriter writer,
        JsonElement value,
        HashSet<string> accessibleUUIDs)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                WriteFilteredObject(writer, value, accessibleUUIDs);
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                {
                    WriteFilteredValue(writer, item, accessibleUUIDs);
                }

                writer.WriteEndArray();
                break;

            default:
                value.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// Determines if a property name is a cross-reference UUID (references another entity)
    /// vs. the entity's own identity UUID.
    /// Own-identity: "UUID" alone. Cross-references: "OwnerUUID", "ColonyUUID", "BlueprintUUID", etc.
    /// </summary>
    private static bool IsCrossReferenceProperty(string propertyName)
    {
        if (!propertyName.EndsWith(UuidSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        // "UUID" alone is the entity's own identity — not a cross-reference
        if (propertyName == "UUID")
        {
            return false;
        }

        return true;
    }
}
