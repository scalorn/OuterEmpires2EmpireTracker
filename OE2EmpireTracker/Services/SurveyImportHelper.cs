using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    public static class SurveyImportHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Searches surveys for a match by PlanetName (case-insensitive) AND SurveyID (case-insensitive).
        /// Returns the matching survey, or null if none found.
        /// </summary>
        public static Survey FindByKey(IEnumerable<Survey> surveys, string planetName, string surveyId)
        {
            if (surveys == null || string.IsNullOrEmpty(planetName) || string.IsNullOrEmpty(surveyId))
                return null;

            return surveys.FirstOrDefault(s =>
                string.Equals(s.PlanetName, planetName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.SurveyID, surveyId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a new Survey from a parsed temporary survey, assigning UUID and OwnerUUID.
        /// Copies all parsed data (PlanetName, SystemName, SurveyID, ScannedBy, DateTime, ScannerBlueprintUUID, Resources, Properties).
        /// </summary>
        public static Survey CreateFromTemp(Survey tempSurvey, string ownerUUID)
        {
            var survey = new Survey();
            survey.UUID = Guid.NewGuid().ToString();
            survey.OwnerUUID = ownerUUID;
            survey.PlanetName = tempSurvey.PlanetName;
            survey.SystemName = tempSurvey.SystemName;
            survey.SurveyID = tempSurvey.SurveyID;
            survey.ScannedBy = tempSurvey.ScannedBy;
            survey.DateTime = tempSurvey.DateTime;
            survey.ScannerBlueprintUUID = tempSurvey.ScannerBlueprintUUID;
            survey.Resources = tempSurvey.Resources;
            survey.Properties = tempSurvey.Properties;
            survey.SurveyType = tempSurvey.SurveyType;
            survey.AsteroidUUID = tempSurvey.AsteroidUUID;
            survey.ParsedMaxReserves = tempSurvey.ParsedMaxReserves;
            return survey;
        }

        /// <summary>
        /// Merges identity and data fields from source into target.
        /// Preserves target's UUID, OwnerUUID, and NickName.
        /// </summary>
        public static void MergeData(Survey target, Survey source)
        {
            target.PlanetName = source.PlanetName;
            target.SystemName = source.SystemName;
            target.SurveyID = source.SurveyID;
            target.ScannedBy = source.ScannedBy;
            target.DateTime = source.DateTime;
            target.ScannerBlueprintUUID = source.ScannerBlueprintUUID;
            target.Resources = source.Resources;
            target.Properties = source.Properties;
            target.SurveyType = source.SurveyType;
            target.AsteroidUUID = source.AsteroidUUID;
            target.ParsedMaxReserves = source.ParsedMaxReserves;
        }

        /// <summary>
        /// If the survey is an asteroid survey (SurveyType == Asteroid), computes a deterministic
        /// AsteroidUUID from SystemName:PlanetName and auto-creates the Asteroid entity if it
        /// doesn't already exist. Sets survey.AsteroidUUID.
        /// </summary>
        public static void LinkOrCreateAsteroid(Survey survey, PlayerContext playerContext)
        {
            if (survey.SurveyType != SurveyType.Asteroid) return;
            if (string.IsNullOrEmpty(survey.PlanetName) || string.IsNullOrEmpty(survey.SystemName)) return;

            string asteroidUUID = DeterministicUUID.GenerateAsteroid(survey.SystemName, survey.PlanetName);
            survey.AsteroidUUID = asteroidUUID;

            var existing = playerContext.AsteroidList.FirstOrDefault(a => a.UUID == asteroidUUID);
            Asteroid targetAsteroid;
            if (existing == null)
            {
                targetAsteroid = new Asteroid
                {
                    UUID = asteroidUUID,
                    Name = survey.PlanetName,
                    SystemName = survey.SystemName
                };

                playerContext.AddAsteroid(targetAsteroid);
                Log.Info("Auto-created asteroid '{0}' in system '{1}' UUID={2}",
                    targetAsteroid.Name, targetAsteroid.SystemName, targetAsteroid.UUID);
            }
            else
            {
                targetAsteroid = existing;
                Log.Info("Linked survey to existing asteroid '{0}' UUID={1}",
                    existing.Name, existing.UUID);
            }

            if (survey.ParsedMaxReserves != null && survey.ParsedMaxReserves.Count > 0)
            {
                var reserves = new List<AsteroidReserve>();
                foreach (var kvp in survey.ParsedMaxReserves)
                {
                    string purity = string.Empty;
                    SurveyResource res;
                    if (survey.Resources != null && survey.Resources.TryGetValue(kvp.Key, out res))
                    {
                        purity = res.Purity;
                    }

                    reserves.Add(new AsteroidReserve
                    {
                        ResourceName = kvp.Key,
                        Purity = purity,
                        MaxReserve = kvp.Value
                    });
                }

                targetAsteroid.Reserves = reserves;
                Log.Info("Populated {0} reserves on asteroid '{1}'", reserves.Count, targetAsteroid.Name);
            }

            playerContext.OnAsteroidDataChanged(targetAsteroid.UUID);
        }
    }
}
