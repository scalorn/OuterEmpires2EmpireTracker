using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Survey mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus deserialization, migration, and SurveyImportHelper called by the service)
    /// mutates Survey objects.
    /// </summary>
    public class SurveyService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public SurveyService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing survey, persists, and fires the change event.
        /// </summary>
        public ReadOnlySurvey Update(string uuid, SurveyUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var survey = _playerContext.FindMutableSurvey(uuid);
            if (survey == null)
            {
                throw new InvalidOperationException("Survey not found: " + uuid);
            }

            Log.Info(
                "SurveyService.Update: UUID={0} planet='{1}' -> '{2}'",
                uuid,
                survey.PlanetName,
                request.PlanetName);

            survey.PlanetName = request.PlanetName;
            survey.SystemName = request.SystemName;
            survey.SurveyID = request.SurveyID;
            survey.NickName = request.NickName;
            survey.ScannedBy = request.ScannedBy;
            survey.DateTime = request.DateTime;
            survey.ScannerBlueprintUUID = request.ScannerBlueprintUUID;
            survey.SurveyType = request.SurveyType;
            survey.AsteroidUUID = request.AsteroidUUID;

            survey.Resources = DeepCopyResources(request.Resources);
            survey.Properties = new Dictionary<string, string>(request.Properties);

            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(uuid);
            return new ReadOnlySurvey(survey);
        }

        /// <summary>
        /// Creates a new survey, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlySurvey Create(SurveyCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var survey = new Survey();
            survey.UUID = Guid.NewGuid().ToString();
            survey.OwnerUUID = _playerContext.CurrentPlayerUUID;
            survey.PlanetName = request.PlanetName;
            survey.SystemName = request.SystemName;
            survey.SurveyID = request.SurveyID;
            survey.NickName = request.NickName;
            survey.ScannedBy = request.ScannedBy;
            survey.DateTime = request.DateTime;
            survey.ScannerBlueprintUUID = request.ScannerBlueprintUUID;
            survey.SurveyType = request.SurveyType;
            survey.AsteroidUUID = request.AsteroidUUID;
            survey.Resources = DeepCopyResources(request.Resources);
            survey.Properties = new Dictionary<string, string>(request.Properties);

            Log.Info(
                "SurveyService.Create: planet='{0}' UUID={1}",
                survey.PlanetName,
                survey.UUID);

            _playerContext.AddSurvey(survey);
            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(survey.UUID);
            return new ReadOnlySurvey(survey);
        }

        /// <summary>
        /// Removes a survey. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var survey = _playerContext.FindMutableSurvey(uuid);
            if (survey == null)
            {
                return;
            }

            Log.Info("SurveyService.Delete: UUID={0} planet='{1}'", uuid, survey.PlanetName);

            _playerContext.RemoveSurvey(survey);
            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(uuid);
        }

        /// <summary>
        /// Imports a parsed temporary survey. Deduplicates by PlanetName+SurveyID,
        /// merges into existing or creates new, handles asteroid auto-linking, persists, and fires event.
        /// </summary>
        public ReadOnlySurvey Import(Survey tempSurvey)
        {
            if (tempSurvey == null)
            {
                throw new ArgumentNullException(nameof(tempSurvey));
            }

            var existing = SurveyImportHelper.FindByKey(
                _playerContext.GetCurrentPlayerSurveys(),
                tempSurvey.PlanetName,
                tempSurvey.SurveyID);

            Survey survey;
            if (existing != null)
            {
                Log.Info(
                    "SurveyService.Import: merging into existing UUID={0} planet='{1}'",
                    existing.UUID,
                    existing.PlanetName);
                SurveyImportHelper.MergeData(existing, tempSurvey);
                survey = existing;
            }
            else
            {
                survey = SurveyImportHelper.CreateFromTemp(tempSurvey, _playerContext.CurrentPlayerUUID);
                Log.Info(
                    "SurveyService.Import: created new UUID={0} planet='{1}'",
                    survey.UUID,
                    survey.PlanetName);
                _playerContext.AddSurvey(survey);
            }

            SurveyImportHelper.LinkOrCreateAsteroid(survey, _playerContext);

            _playerContext.WriteContext();
            _playerContext.OnSurveyDataChanged(survey.UUID);
            return new ReadOnlySurvey(survey);
        }

        private static Dictionary<string, SurveyResource> DeepCopyResources(Dictionary<string, SurveyResource> source)
        {
            var copy = new Dictionary<string, SurveyResource>();
            if (source != null)
            {
                foreach (var kvp in source)
                {
                    copy[kvp.Key] = new SurveyResource(kvp.Value.Resource, kvp.Value.Purity, kvp.Value.Amount);
                }
            }

            return copy;
        }
    }
}
