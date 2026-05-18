using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for survey CRUD operations.
/// </summary>
public sealed class SurveyService
{
    private readonly DataService _dataService;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(DataService dataService, ILogger<SurveyService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(SurveyCreateRequest request)
    {
        var survey = new Survey(request.PlanetName ?? string.Empty)
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            PlanetName = request.PlanetName,
            SystemName = request.SystemName,
            SurveyID = request.SurveyID,
            NickName = request.NickName,
            ScannedBy = request.ScannedBy,
            DateTime = request.DateTime,
            ScannerBlueprintUUID = request.ScannerBlueprintUUID,
            AsteroidUUID = request.AsteroidUUID,
            SurveyType = request.SurveyType,
        };

        if (request.Resources is not null)
        {
            survey.Resources = new Dictionary<string, SurveyResource>(request.Resources);
        }

        if (request.Properties is not null)
        {
            survey.Properties = new Dictionary<string, string>(request.Properties);
        }

        _dataService.AddSurvey(survey);
        _dataService.OnSurveyDataChanged(survey.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created survey {Name} ({UUID})", survey.PlanetName, survey.UUID);
    }

    public void Update(string uuid, SurveyUpdateRequest request)
    {
        var survey = _dataService.Surveys.FirstOrDefault(s => s.UUID == uuid);
        if (survey is null)
        {
            _logger.LogWarning("Update failed: survey {UUID} not found", uuid);
            return;
        }

        survey.PlanetName = request.PlanetName;
        survey.SystemName = request.SystemName;
        survey.SurveyID = request.SurveyID;
        survey.NickName = request.NickName;
        survey.ScannedBy = request.ScannedBy;
        survey.DateTime = request.DateTime;
        survey.ScannerBlueprintUUID = request.ScannerBlueprintUUID;
        survey.AsteroidUUID = request.AsteroidUUID;
        survey.SurveyType = request.SurveyType;

        if (request.Resources is not null)
        {
            survey.Resources = new Dictionary<string, SurveyResource>(request.Resources);
        }

        if (request.Properties is not null)
        {
            survey.Properties = new Dictionary<string, string>(request.Properties);
        }

        _dataService.IsDirty = true;
        _dataService.OnSurveyDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated survey {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveSurvey(uuid);
        _dataService.OnSurveyDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted survey {UUID}", uuid);
    }
}
