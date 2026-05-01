using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for DeliveryPlanStop. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyDeliveryPlanStop
    {
        private readonly DeliveryPlanStop _entity;

        public ReadOnlyDeliveryPlanStop(DeliveryPlanStop entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string ColonyUUID => _entity.ColonyUUID;
        public int Sequence => _entity.Sequence;
        public bool StopCompleted => _entity.StopCompleted;
        public DestinationType DestinationType => _entity.DestinationType;
        public string DestinationUUID => _entity.DestinationUUID;

        public IReadOnlyList<ReadOnlyDeliveryItem> DropOff =>
            _entity.DropOff.Select(x => new ReadOnlyDeliveryItem(x)).ToList();

        public IReadOnlyList<ReadOnlyDeliveryItem> PickUp =>
            _entity.PickUp.Select(x => new ReadOnlyDeliveryItem(x)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyDeliveryPlanStop other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"Stop {_entity.Sequence}";
    }
}
