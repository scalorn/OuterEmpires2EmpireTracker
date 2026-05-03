using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Survey. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlySurvey
    {
        private readonly Survey _entity;

        public ReadOnlySurvey(Survey entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string PlanetName => _entity.PlanetName;
        public string SystemName => _entity.SystemName;
        public string SurveyID => _entity.SurveyID;
        public string NickName => _entity.NickName;
        public SurveyType SurveyType => _entity.SurveyType;
        public string AsteroidUUID => _entity.AsteroidUUID;
        public string ScannedBy => _entity.ScannedBy;
        public string DateTime => _entity.DateTime;
        public string ScannerBlueprintUUID => _entity.ScannerBlueprintUUID;
        public IReadOnlyDictionary<string, string> Properties => _entity.Properties;

        // Computed property
        public string ExtendedName => _entity.ExtendedName;

        // Dictionary of nested types - wrapped
        public IReadOnlyDictionary<string, ReadOnlySurveyResource> Resources =>
            _entity.Resources.ToDictionary(kvp => kvp.Key, kvp => new ReadOnlySurveyResource(kvp.Value));

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlySurvey other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ExtendedName;
    }
}
