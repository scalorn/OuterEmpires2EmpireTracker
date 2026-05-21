using System.Text.Json;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Colony planner endpoints — stateless computation endpoints that accept colony
/// configuration and return status, eligibility, and build order results.
/// No authentication required (public access).
/// </summary>
public static class ColonyPlannerEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static void MapColonyPlannerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/colony-planner")
            .AllowAnonymous();

        group.MapPost("/status", (Delegate)ComputeStatus);
        group.MapPost("/eligibility", (Delegate)CheckEligibility);
        group.MapPost("/build-order", (Delegate)OptimizeBuildOrder);
    }

    private static async Task<IResult> ComputeStatus(HttpContext httpContext)
    {
        var body = await ReadBodyAsync(httpContext);
        if (body == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        ColonyPlannerRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ColonyPlannerRequest>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        if (request == null || request.Structures == null)
        {
            return Results.BadRequest(new { error = "structures field is required" });
        }

        var result = CalculateColonyStatus(request);
        return Results.Ok(result);
    }

    private static async Task<IResult> CheckEligibility(HttpContext httpContext)
    {
        var body = await ReadBodyAsync(httpContext);
        if (body == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        ColonyPlannerRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ColonyPlannerRequest>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        if (request == null || request.Structures == null)
        {
            return Results.BadRequest(new { error = "structures field is required" });
        }

        var result = CalculateEligibility(request);
        return Results.Ok(result);
    }

    private static async Task<IResult> OptimizeBuildOrder(HttpContext httpContext)
    {
        var body = await ReadBodyAsync(httpContext);
        if (body == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        ColonyPlannerRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ColonyPlannerRequest>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        if (request == null || request.Structures == null)
        {
            return Results.BadRequest(new { error = "structures field is required" });
        }

        var result = CalculateBuildOrder(request);
        return Results.Ok(result);
    }

    // --- Computation Methods ---

    private static ColonyStatusResponse CalculateColonyStatus(ColonyPlannerRequest request)
    {
        int powerProvided = 0;
        int powerRequired = 0;
        int habitationProvision = 0;
        int habitationRequired = 0;
        int foodProvision = 0;
        int foodRequired = 0;
        int entertainmentProvided = 0;
        int entertainmentRequired = 0;
        int warehouseCapacity = 0;
        int warehouseRequired = 0;

        foreach (var structure in request.Structures!)
        {
            if (!structure.IsBuilt)
            {
                continue;
            }

            // Placeholder computation — real implementation will use Common library
            powerProvided += structure.IsOnline ? 10 : 0;
            powerRequired += 5;
            habitationProvision += 2;
            habitationRequired += 1;
            foodProvision += 1;
            foodRequired += 1;
            entertainmentProvided += 1;
            entertainmentRequired += 1;
            warehouseCapacity += 100;
            warehouseRequired += 50;
        }

        return new ColonyStatusResponse
        {
            PowerProvided = powerProvided,
            PowerRequired = powerRequired,
            HabitationProvision = habitationProvision,
            HabitationRequired = habitationRequired,
            FoodProvision = foodProvision,
            FoodRequired = foodRequired,
            EntertainmentProvided = entertainmentProvided,
            EntertainmentRequired = entertainmentRequired,
            WarehouseCapacity = warehouseCapacity,
            WarehouseRequired = warehouseRequired,
        };
    }

    private static EligibilityResponse CalculateEligibility(ColonyPlannerRequest request)
    {
        var staged = request.Structures!.Where(s => s.IsStaged && !s.IsBuilt).ToList();
        var building = request.Structures!.Where(s => !s.IsBuilt && !s.IsStaged).ToList();

        return new EligibilityResponse
        {
            Eligible = staged.Count > 0,
            StagedCount = staged.Count,
            BuildingCount = building.Count,
            FirstStagedStructure = staged.FirstOrDefault(),
        };
    }

    private static BuildOrderResponse CalculateBuildOrder(ColonyPlannerRequest request)
    {
        var steps = new List<BuildOrderStepResponse>();
        var sequence = 1;

        foreach (var structure in request.Structures!.Where(s => !s.IsBuilt))
        {
            steps.Add(new BuildOrderStepResponse
            {
                Sequence = sequence++,
                StructureName = structure.FlatpackBlueprintUUID ?? "Unknown",
                BlueprintType = "Structure",
                ResourcesRequired = new List<ResourceRequirementResponse>
                {
                    new () { ResourceName = "Construction Materials", Quantity = 100 },
                },
                TimeEstimate = "1h 30m",
            });
        }

        return new BuildOrderResponse
        {
            Steps = steps,
            TotalTimeEstimate = $"{steps.Count * 90}m",
        };
    }

    // --- Helpers ---

    private static async Task<string?> ReadBodyAsync(HttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var body = await reader.ReadToEndAsync();
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    // --- Request/Response Models ---

    private sealed class ColonyPlannerRequest
    {
        public List<PlannerStructureDto>? Structures { get; set; }

        public Dictionary<string, int>? Items { get; set; }

        public Dictionary<string, int>? PlayerSkills { get; set; }
    }

    private sealed class PlannerStructureDto
    {
        public string? FlatpackBlueprintUUID { get; set; }

        public bool IsBuilt { get; set; }

        public bool IsStaged { get; set; }

        public bool IsOnline { get; set; }

        public int BuildQueueSequence { get; set; }

        public Dictionary<string, bool>? AssignedWorkers { get; set; }
    }

    private sealed class ColonyStatusResponse
    {
        public int PowerProvided { get; set; }

        public int PowerRequired { get; set; }

        public int HabitationProvision { get; set; }

        public int HabitationRequired { get; set; }

        public int FoodProvision { get; set; }

        public int FoodRequired { get; set; }

        public int EntertainmentProvided { get; set; }

        public int EntertainmentRequired { get; set; }

        public int WarehouseCapacity { get; set; }

        public int WarehouseRequired { get; set; }
    }

    private sealed class EligibilityResponse
    {
        public bool Eligible { get; set; }

        public int StagedCount { get; set; }

        public int BuildingCount { get; set; }

        public PlannerStructureDto? FirstStagedStructure { get; set; }
    }

    private sealed class BuildOrderResponse
    {
        public List<BuildOrderStepResponse> Steps { get; set; } = new ();

        public string TotalTimeEstimate { get; set; } = string.Empty;
    }

    private sealed class BuildOrderStepResponse
    {
        public int Sequence { get; set; }

        public string StructureName { get; set; } = string.Empty;

        public string BlueprintType { get; set; } = string.Empty;

        public List<ResourceRequirementResponse> ResourcesRequired { get; set; } = new ();

        public string TimeEstimate { get; set; } = string.Empty;
    }

    private sealed class ResourceRequirementResponse
    {
        public string ResourceName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
