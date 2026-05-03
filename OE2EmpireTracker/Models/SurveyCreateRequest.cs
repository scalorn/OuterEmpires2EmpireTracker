using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new survey.
    /// No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class SurveyCreateRequest
    {
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