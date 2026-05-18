using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for asteroid CRUD operations.
/// </summary>
public sealed class AsteroidService
{
    private readonly DataService _dataService;
    private readonly ILogger<AsteroidService> _logger;

    public AsteroidService(DataService dataService, ILogger<AsteroidService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(AsteroidCreateRequest request)
    {
        var asteroid = new Asteroid
        {
            UUID = Guid.NewGuid().ToString(),
            Name = request.Name,
            SystemName = request.SystemName,
            Reserves = request.Reserves ?? new List<AsteroidReserve>(),
        };

        _dataService.AddAsteroid(asteroid);
        _dataService.OnAsteroidDataChanged(asteroid.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created asteroid {Name} ({UUID})", asteroid.Name, asteroid.UUID);
    }

    public void Update(string uuid, AsteroidUpdateRequest request)
    {
        var asteroid = _dataService.Asteroids.FirstOrDefault(a => a.UUID == uuid);
        if (asteroid is null)
        {
            _logger.LogWarning("Update failed: asteroid {UUID} not found", uuid);
            return;
        }

        asteroid.Name = request.Name;
        asteroid.SystemName = request.SystemName;
        asteroid.Reserves = request.Reserves ?? new List<AsteroidReserve>();
        _dataService.IsDirty = true;
        _dataService.OnAsteroidDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated asteroid {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveAsteroid(uuid);
        _dataService.OnAsteroidDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted asteroid {UUID}", uuid);
    }
}
