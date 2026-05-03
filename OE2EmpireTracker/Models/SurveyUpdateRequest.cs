using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing survey.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class SurveyUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlySurvey Original { get; set; }

        public string PlanetName { get; set; }

        public string SystemName { get; set; }

        public string SurveyID { get; set; }

        public string NickName { get; set; }

        public string ScannedBy { get; set; }

        public string DateTime { get; set; }

        public string ScannerBlueprintUUID { get; set; }

        public string AsteroidUUID { get; set; }

        public SurveyType SurveyType { get; set; }

        public Dictionary<string, SurveyResource> Resources { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }
}