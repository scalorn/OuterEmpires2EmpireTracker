using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for SurveyResource. Exposes only getter properties.
    /// </summary>
    public class ReadOnlySurveyResource
    {
        private readonly SurveyResource _entity;

        public ReadOnlySurveyResource(SurveyResource entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Resource => _entity.Resource;
        public string Purity => _entity.Purity;
        public string Amount => _entity.Amount;
        public string ExtendedName => _entity.ExtendedName;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlySurveyResource other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ExtendedName;
    }
}
