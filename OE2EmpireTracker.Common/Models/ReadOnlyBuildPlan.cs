using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for BuildPlan. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyBuildPlan
    {
        private readonly BuildPlan _entity;

        public ReadOnlyBuildPlan(BuildPlan entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string Description => _entity.Description;
        public string DeliveryPlanUUID => _entity.DeliveryPlanUUID;
        public bool IsActive => _entity.IsActive;

        public IReadOnlyList<ReadOnlyBuildItem> Items =>
            _entity.Items.Select(i => new ReadOnlyBuildItem(i)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyBuildPlan other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
