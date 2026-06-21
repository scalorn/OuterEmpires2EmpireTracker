using System.Text.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Services;

/// <summary>
/// Decomposes a BaselineData.json payload into individual canonical records.
/// Each reference data section is stored independently via the storage backend.
/// </summary>
public class BaselineDecompositionService
{
    private static readonly string[] KnownSectionKeys =
    [
        "GameConstants",
        "ShipClass",
        "BlueprintType",
        "TechLevel",
        "Commodity",
        "RefiningRecipe",
        "ResearchTime",
        "Resource",
    ];

    private readonly ILogger<BaselineDecompositionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaselineDecompositionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public BaselineDecompositionService(ILogger<BaselineDecompositionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Decomposes a baseline JSON payload into individual canonical records.
    /// Throws <see cref="System.Text.Json.JsonException"/> on invalid JSON.
    /// Throws <see cref="InvalidOperationException"/> if no sections are present.
    /// </summary>
    /// <param name="json">The raw JSON payload string.</param>
    /// <param name="storage">The storage backend to persist decomposed sections.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DecomposeAsync(string json, IStorageBackend storage)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse baseline JSON payload");
            throw;
        }

        using (document)
        {
            var root = document.RootElement;
            int processedCount = 0;

            foreach (var key in KnownSectionKeys)
            {
                if (root.TryGetProperty(key, out var section))
                {
                    var sectionJson = section.GetRawText();
                    await storage.UpsertGlobalDataAsync(key, sectionJson);
                    processedCount++;
                    _logger.LogInformation("Decomposed section '{SectionKey}'", key);
                }
            }

            // Blueprint decomposition: store each blueprint individually with empty owner
            if (root.TryGetProperty("Blueprint", out var blueprintSection))
            {
                var blueprintJson = blueprintSection.GetRawText();
                var blueprints = JsonConvert.DeserializeObject<List<Blueprint>>(blueprintJson)
                    ?? new List<Blueprint>();

                foreach (var blueprint in blueprints)
                {
                    await storage.UpsertBlueprintAsync(string.Empty, blueprint);
                }

                processedCount++;
                _logger.LogInformation(
                    "Decomposed {Count} blueprints as global records",
                    blueprints.Count);
            }

            if (processedCount == 0)
            {
                throw new InvalidOperationException(
                    "No valid sections found in baseline payload");
            }

            _logger.LogInformation(
                "Baseline decomposition complete: {Count} sections processed",
                processedCount);
        }
    }
}