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
            int index = colony.Structures.IndexOf(_structure);
            if (index <= 0)
            {
                Log.Info("MoveUp: blocked — index={0} (at top or not found), structure={1}",
                    index, _structure.UUID);
                return;
            }

            // Prevent moving a non-CC structure into position 0 (CC must always be first)
            if (index == 1)
            {
                var firstBp = _playerContext.FindBlueprint(colony.Structures[0].FlatpackBlueprintUUID);
                if (firstBp != null && firstBp.BluePrintType == BlueprintTypes.ColonyCommandCentre)
                {
                    Log.Info("MoveUp: blocked — cannot move above CC at position 0, structure={0} index={1}",
                        _structure.UUID, index);
                    return;
                }
            }

            colony.Structures.RemoveAt(index);
            colony.Structures.Insert(index - 1, _structure);
            Log.Info("MoveUp: moved structure={0} from index={1} to index={2}, colony={3} structureCount={4}",
                _structure.UUID, index, index - 1, colony.UUID, colony.Structures.Count);
        }

        public void MoveDown(Colony colony)
        {
            int index = colony.Structures.IndexOf(_structure);
            if (index < 0 || index >= colony.Structures.Count - 1)
            {
                Log.Info("MoveDown: blocked — index={0} count={1} (at bottom or not found), structure={2}",
                    index, colony.Structures.Count, _structure.UUID);
                return;
            }

            // Prevent moving the CC away from position 0
            if (index == 0)
            {
                var bp = _playerContext.FindBlueprint(_structure.FlatpackBlueprintUUID);
                if (bp != null && bp.BluePrintType == BlueprintTypes.ColonyCommandCentre)
                {
                    Log.Info("MoveDown: blocked — CC must stay at position 0, structure={0}",
                        _structure.UUID);
                    return;
                }
            }

            colony.Structures.RemoveAt(index);
            colony.Structures.Insert(index + 1, _structure);
            Log.Info("MoveDown: moved structure={0} from index={1} to index={2}, colony={3} structureCount={4}",
                _structure.UUID, index, index + 1, colony.UUID, colony.Structures.Count);
        }

        public void Delete(Colony colony)
        {
            colony.Structures.Remove(_structure);
        }
    }
}
