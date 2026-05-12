using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Blueprint. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyBlueprint
    {
        private readonly Blueprint _entity;

        public ReadOnlyBlueprint(Blueprint entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string BaseBlueprintUUID => _entity.BaseBlueprintUUID;
        public string LegacyUUID => _entity.LegacyUUID;
        public string BluePrintType => _entity.BluePrintType;
        public int Evolution => _entity.Evolution;
        public string TechLevel => _entity.TechLevel;
        public int Class => _entity.Class;
        public int CopyCost => _entity.CopyCost;
        public string NickName => _entity.NickName;
        public string Description => _entity.Description;

        // Computed properties
        public string ExtendedName => _entity.ExtendedName;
        public string OutputItemName => _entity.OutputItemName;
        public int Quantity => _entity.Quantity;

        // Nested utility container - wrapped
        public ReadOnlyPropertyBag Properties => new ReadOnlyPropertyBag(_entity.Properties);

        // Dictionary of value types - exposed as IReadOnlyDictionary
        public IReadOnlyDictionary<string, string> Resources => _entity.Resources;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyBlueprint other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ExtendedName;
    }
}
