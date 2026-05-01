using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for BuildItem. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyBuildItem
    {
        private readonly BuildItem _entity;

        public ReadOnlyBuildItem(BuildItem entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public BuildItemType ItemType => _entity.ItemType;
        public BuildItemStatus Status => _entity.Status;
        public string BlueprintUUID => _entity.BlueprintUUID;
        public string ItemName => _entity.ItemName;
        public string CommodityName => _entity.CommodityName;
        public string ShipTemplateUUID => _entity.ShipTemplateUUID;
        public int Quantity => _entity.Quantity;
        public DestinationType BuildLocationType => _entity.BuildLocationType;
        public string BuildLocationUUID => _entity.BuildLocationUUID;
        public string StructureUUID => _entity.StructureUUID;
        public DestinationType AssemblyLocationType => _entity.AssemblyLocationType;
        public string AssemblyLocationUUID => _entity.AssemblyLocationUUID;
        public string ParentBuildItemUUID => _entity.ParentBuildItemUUID;
        public string Recipient => _entity.Recipient;
        public string Notes => _entity.Notes;
        public int SequenceInStructure => _entity.SequenceInStructure;
        public string DependsOnUUID => _entity.DependsOnUUID;
        public string MiningResource => _entity.MiningResource;
        public string MiningSurveyUUID => _entity.MiningSurveyUUID;
        public string RefiningResource => _entity.RefiningResource;
        public string RefiningPurity => _entity.RefiningPurity;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyBuildItem other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ItemName;
    }
}
