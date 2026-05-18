using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for supply chain CRUD operations.
/// </summary>
public sealed class SupplyChainService
{
    private readonly DataService _dataService;
    private readonly ILogger<SupplyChainService> _logger;

    public SupplyChainService(DataService dataService, ILogger<SupplyChainService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(SupplyChainCreateRequest request)
    {
        var chain = new SupplyChain
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            IsActive = request.IsActive,
            Stages = request.Stages ?? new List<SupplyChainStage>(),
        };

        _dataService.AddSupplyChain(chain);
        _dataService.OnSupplyChainDataChanged(chain.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created supply chain {Name} ({UUID})", chain.Name, chain.UUID);
    }

    public void Update(string uuid, SupplyChainUpdateRequest request)
    {
        var chain = _dataService.SupplyChains.FirstOrDefault(c => c.UUID == uuid);
        if (chain is null)
        {
            _logger.LogWarning("Update failed: supply chain {UUID} not found", uuid);
            return;
        }

        chain.Name = request.Name;
        chain.IsActive = request.IsActive;
        chain.Stages = request.Stages ?? new List<SupplyChainStage>();
        _dataService.IsDirty = true;
        _dataService.OnSupplyChainDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated supply chain {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveSupplyChain(uuid);
        _dataService.OnSupplyChainDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted supply chain {UUID}", uuid);
    }
}
