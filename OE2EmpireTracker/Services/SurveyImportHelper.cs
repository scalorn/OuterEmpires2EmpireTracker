using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

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
        }
    }
}
