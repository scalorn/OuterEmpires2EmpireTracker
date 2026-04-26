using System;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a ColonyStructure data object and exposes typed properties,
    /// hiding all direct PropertyBag access from the UI layer.
    /// </summary>
    public class ColonyStructureViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ColonyStructure _structure;

        private readonly PlayerContext _playerContext;

        public ColonyStructureViewModel(ColonyStructure structure, PlayerContext playerContext)
        {
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // -----------------------------------------------------------------------
        // Structure state -- typed wrappers over PropertyBag
        // -----------------------------------------------------------------------

        public bool IsBuilt
        {
            get
            {
                bool v;
                _structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out v);
                return v;
            }

            set
            {
                _structure.Properties.SetProperty(GameConstants.PropBuilt, value);
            }
        }

        public bool IsStaged
        {
            get
            {
                bool v;
                _structure.Properties.GetBoolean(GameConstants.PropStaged, false, out v);
                return v;
            }

            set
            {
                _structure.Properties.SetProperty(GameConstants.PropStaged, value);
            }
        }

        public bool IsOnline
        {
            get
            {
                bool v;
                _structure.Properties.GetBoolean(GameConstants.PropOnline, false, out v);
                return v;
            }

            set
            {
                _structure.Properties.SetProperty(GameConstants.PropOnline, value);
            }
        }

        // -----------------------------------------------------------------------
        // Mining / process properties -- typed pass-throughs
        // -----------------------------------------------------------------------

        public string MiningSurvey
        {
            get => _structure.MiningSurvey;
            set => _structure.MiningSurvey = value;
        }

        public string MiningSurveyResource
        {
            get => _structure.MiningSurveyResource;
            set => _structure.MiningSurveyResource = value;
        }

        public decimal MiningLeftOvers
        {
            get => _structure.MiningLeftOvers;
            set => _structure.MiningLeftOvers = value;
        }

        public CountDownTime ProcessCompletionTime
        {
            get => _structure.ProcessCompletionTime;
            set => _structure.ProcessCompletionTime = value;
        }

        // -----------------------------------------------------------------------
        // Blueprint info
        // -----------------------------------------------------------------------

        public bool StagingResources
        {
            get => _structure.StagingResources;
            set => _structure.StagingResources = value;
        }

        public ColonyStructure Data => _structure;

        public string FlatpackBlueprintUUID => _structure.FlatpackBlueprintUUID;

        public Blueprint Blueprint => _playerContext.FindBlueprint(_structure.FlatpackBlueprintUUID);

        public string BlueprintType => Blueprint?.BluePrintType ?? string.Empty;

        public int DisplaySequence => _structure.DisplaySequence;

        // -----------------------------------------------------------------------
        // Worker assignment -- typed wrappers over AssignedWorkers PropertyBag
        // -----------------------------------------------------------------------

        public bool GetWorkerAssigned(string key)
        {
            bool v;
            _structure.AssignedWorkers.GetBoolean(key, false, out v);
            return v;
        }

        public void SetWorkerAssigned(string key, bool assigned)
        {
            _structure.AssignedWorkers.SetProperty(key, assigned);
        }

        public bool WorkerKeyExists(string key) => _structure.AssignedWorkers.ContainsKey(key);

        // -----------------------------------------------------------------------
        // Structure list commands -- operate on the parent colony's list
        // -----------------------------------------------------------------------

        public void MoveUp(Colony colony)
        {
            int mySeq = _structure.BuildQueueSequence;

            // Find the structure with the next lower BuildQueueSequence
            ColonyStructure neighbor = null;
            int neighborSeq = int.MinValue;
            foreach (var s in colony.Structures)
            {
                if (s.BuildQueueSequence < mySeq && s.BuildQueueSequence > neighborSeq)
                {
                    neighbor = s;
                    neighborSeq = s.BuildQueueSequence;
                }
            }

            if (neighbor == null)
            {
                Log.Info("MoveUp: blocked — already at top (seq={0}), structure={1}",
                    mySeq, _structure.UUID);
                return;
            }

            // Prevent moving above CC (CC should have the lowest BuildQueueSequence)
            var neighborBp = _playerContext.FindBlueprint(neighbor.FlatpackBlueprintUUID);
            if (neighborBp != null && neighborBp.BluePrintType == BlueprintTypes.ColonyCommandCentre)
            {
                Log.Info("MoveUp: blocked — cannot move above CC, structure={0} seq={1}",
                    _structure.UUID, mySeq);
                return;
            }

            // Swap BuildQueueSequence values
            _structure.BuildQueueSequence = neighborSeq;
            neighbor.BuildQueueSequence = mySeq;

            Log.Info("MoveUp: swapped structure={0} seq {1}->{2} with neighbor={3} seq {4}->{5}, colony={6}",
                _structure.UUID, mySeq, _structure.BuildQueueSequence,
                neighbor.UUID, neighborSeq, neighbor.BuildQueueSequence,
                colony.UUID);
        }

        public void MoveDown(Colony colony)
        {
            int mySeq = _structure.BuildQueueSequence;

            // Prevent moving CC down (CC should stay at the top)
            var myBp = _playerContext.FindBlueprint(_structure.FlatpackBlueprintUUID);
            if (myBp != null && myBp.BluePrintType == BlueprintTypes.ColonyCommandCentre)
            {
                Log.Info("MoveDown: blocked — CC must stay at top, structure={0}",
                    _structure.UUID);
                return;
            }

            // Find the structure with the next higher BuildQueueSequence
            ColonyStructure neighbor = null;
            int neighborSeq = int.MaxValue;
            foreach (var s in colony.Structures)
            {
                if (s.BuildQueueSequence > mySeq && s.BuildQueueSequence < neighborSeq)
                {
                    neighbor = s;
                    neighborSeq = s.BuildQueueSequence;
                }
            }

            if (neighbor == null)
            {
                Log.Info("MoveDown: blocked — already at bottom (seq={0}), structure={1}",
                    mySeq, _structure.UUID);
                return;
            }

            // Swap BuildQueueSequence values
            _structure.BuildQueueSequence = neighborSeq;
            neighbor.BuildQueueSequence = mySeq;

            Log.Info("MoveDown: swapped structure={0} seq {1}->{2} with neighbor={3} seq {4}->{5}, colony={6}",
                _structure.UUID, mySeq, _structure.BuildQueueSequence,
                neighbor.UUID, neighborSeq, neighbor.BuildQueueSequence,
                colony.UUID);
        }

        public void Delete(Colony colony)
        {
            colony.Structures.Remove(_structure);
        }
    }
}
