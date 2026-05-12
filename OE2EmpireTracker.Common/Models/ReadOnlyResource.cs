using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Resource. Exposes only getter properties.
    /// Does NOT expose: any setters, static members, or mutation methods.
    /// </summary>
    public class ReadOnlyResource
    {
        private readonly Resource _entity;

        public ReadOnlyResource(Resource entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Name => _entity.Name;
        public ResourceGroup.ResourceGroupEnum Group => _entity.ResourceGroup;
        public bool Synthetic => _entity.ResourceGroup == ResourceGroup.ResourceGroupEnum.Synthetic;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyResource other)
                return string.Equals(_entity.Name, other._entity.Name);
            return false;
        }

        public override int GetHashCode() => _entity.Name?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
