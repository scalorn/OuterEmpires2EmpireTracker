using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ColonyStructure. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyColonyStructure
    {
        private readonly ColonyStructure _entity;

        public ReadOnlyColonyStructure(ColonyStructure entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string FlatpackBlueprintUUID => _entity.FlatpackBlueprintUUID;
        public int DisplaySequence => _entity.DisplaySequence;
        public int BuildingID => _entity.BuildingID;
        public int BuildQueueSequence => _entity.BuildQueueSequence;

        public ReadOnlyPropertyBag Properties => new ReadOnlyPropertyBag(_entity.Properties);
        public ReadOnlyPropertyBag AssignedWorkers => new ReadOnlyPropertyBag(_entity.AssignedWorkers);

        public ReadOnlyCountDownTime BuildCompletionTime =>
            _entity.BuildCompletionTime != null ? new ReadOnlyCountDownTime(_entity.BuildCompletionTime) : null;

        public ReadOnlyCountDownTime ProcessCompletionTime =>
            _entity.ProcessCompletionTime != null ? new ReadOnlyCountDownTime(_entity.ProcessCompletionTime) : null;

        public string MiningSurvey => _entity.MiningSurvey;
        public string MiningSurveyResource => _entity.MiningSurveyResource;
        public decimal MiningLeftOvers => _entity.MiningLeftOvers;
        public string RefiningResource => _entity.RefiningResource;
        public string RefiningResourcePurity => _entity.RefiningResourcePurity;
        public string ResearchingBlueprintUUID => _entity.ResearchingBlueprintUUID;
        public string ManufacturingBlueprintUUID => _entity.ManufacturingBlueprintUUID;
        public string ManufacturingCommodityName => _entity.ManufacturingCommodityName;
        public int ManufacturingQuantity => _entity.ManufacturingQuantity;
        public int ManufacturingCompleted => _entity.ManufacturingCompleted;
        public bool StagingResources => _entity.StagingResources;
#pragma warning disable CS0618 // Obsolete members exposed for backward compatibility
        public string CurrentAttitude => _entity.CurrentAttitude;
        public int ContentmentIndex => _entity.ContentmentIndex;
#pragma warning restore CS0618
        public int WageLevel => _entity.WageLevel;

        public IReadOnlyDictionary<string, ReadOnlyColonyStructureStatus> Statuses =>
            _entity.Statuses.ToDictionary(kvp => kvp.Key, kvp => new ReadOnlyColonyStructureStatus(kvp.Value));

        public bool IsBuiltAndOnline => _entity.IsBuiltAndOnline;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyColonyStructure other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
