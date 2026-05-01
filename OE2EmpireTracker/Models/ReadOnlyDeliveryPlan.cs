using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for DeliveryPlan. Exposes only getter properties.
    /// Does NOT expose: any setters, mutation methods, or CalculateLoadList.
    /// </summary>
    public class ReadOnlyDeliveryPlan
    {
        private readonly DeliveryPlan _entity;

        public ReadOnlyDeliveryPlan(DeliveryPlan entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string RouteUUID => _entity.RouteUUID;
        public string ShipUUID => _entity.ShipUUID;
        public bool Completed => _entity.Completed;

        public IReadOnlyList<ReadOnlyDeliveryPlanStop> Stops =>
            _entity.Stops.Select(s => new ReadOnlyDeliveryPlanStop(s)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyDeliveryPlan other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
