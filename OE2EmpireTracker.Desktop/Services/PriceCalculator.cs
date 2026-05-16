using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Injectable price calculator service for the Avalonia Desktop app.
/// Delegates to the static PriceCalculator in Common for computation logic.
/// </summary>
public sealed class PriceCalculator
{
    private readonly DataService _dataService;
    private readonly ILogger<PriceCalculator> _logger;

    public PriceCalculator(DataService dataService, ILogger<PriceCalculator> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Computes the price of a commodity using the specified pricing plan.
    /// </summary>
    public ComputedPrice ComputeCommodityPrice(Commodity commodity, PricingPlan plan)
    {
        return OE2EmpireTracker.Services.PriceCalculator.ComputeCommodityPrice(plan, commodity);
    }

    /// <summary>
    /// Computes the price of a blueprint using the specified pricing plan and manufacturing hours.
    /// </summary>
    public ComputedPrice ComputeBlueprintPrice(Blueprint blueprint, PricingPlan plan, decimal manufacturingHours)
    {
        var readOnly = new ReadOnlyBlueprint(blueprint);
        return OE2EmpireTracker.Services.PriceCalculator.ComputeBlueprintPrice(plan, readOnly, manufacturingHours);
    }

    /// <summary>
    /// Gets the first available pricing plan for the current player, or null if none exist.
    /// </summary>
    public PricingPlan? GetFirstPlanForCurrentPlayer()
    {
        return _dataService.PricingPlans
            .FirstOrDefault(p => p.OwnerUUID == _dataService.CurrentPlayerUUID);
    }

    /// <summary>
    /// Gets all pricing plans for the current player.
    /// </summary>
    public IReadOnlyList<PricingPlan> GetCurrentPlayerPlans()
    {
        return _dataService.PricingPlans
            .Where(p => p.OwnerUUID == _dataService.CurrentPlayerUUID)
            .ToList();
    }
}
