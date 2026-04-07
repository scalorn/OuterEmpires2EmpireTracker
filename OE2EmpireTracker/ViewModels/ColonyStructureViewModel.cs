using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a ColonyStructure data object and exposes typed properties,
    /// hiding all direct PropertyBag access from the UI layer.
    /// </summary>
    public class ColonyStructureViewModel
    {
        private readonly ColonyStructure _structure;
        private readonly PlayerContext _playerContext;

        public ColonyStructure Data => _structure;

        public ColonyStructureViewModel(ColonyStructure structure, PlayerContext playerContext)
        {
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // -----------------------------------------------------------------------
        // Structure state — typed wrappers over PropertyBag
        // -----------------------------------------------------------------------

        public bool IsBuilt
        {
            get { bool v; _structure.Properties.getBoolean(GameConstants.PropBuilt, false, out v); return v; }
            set { _structure.Properties.setProperty(GameConstants.PropBuilt, value); }
        }

        public bool IsStaged
        {
            get { bool v; _structure.Properties.getBoolean(GameConstants.PropStaged, false, out v); return v; }
            set { _structure.Properties.setProperty(GameConstants.PropStaged, value); }
        }

        public bool IsOnline
        {
            get { bool v; _structure.Properties.getBoolean(GameConstants.PropOnline, false, out v); return v; }
            set { _structure.Properties.setProperty(GameConstants.PropOnline, value); }
        }

        // -----------------------------------------------------------------------
        // Worker assignment — typed wrappers over AssignedWorkers PropertyBag
        // -----------------------------------------------------------------------

        public bool GetWorkerAssigned(string key)
        {
            bool v;
            _structure.AssignedWorkers.getBoolean(key, false, out v);
            return v;
        }

        public void SetWorkerAssigned(string key, bool assigned)
        {
            _structure.AssignedWorkers.setProperty(key, assigned);
        }

        public bool WorkerKeyExists(string key) => _structure.AssignedWorkers.ContainsKey(key);

        // -----------------------------------------------------------------------
        // Mining / process properties — typed pass-throughs
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

        public string FlatpackBlueprintUUID => _structure.FlatpackBlueprintUUID;

        public Blueprint Blueprint => _playerContext.FindBlueprint(_structure.FlatpackBlueprintUUID);

        public string BlueprintType => Blueprint?.BluePrintType ?? string.Empty;

        public int GameSequence => _structure.gameSequence;

        // -----------------------------------------------------------------------
        // Structure list commands — operate on the parent colony's list
        // -----------------------------------------------------------------------

        public void MoveUp(Colony colony)
        {
            int index = colony.Structures.IndexOf(_structure);
            if (index <= 0) return;
            colony.Structures.RemoveAt(index);
            colony.Structures.Insert(index - 1, _structure);
        }

        public void MoveDown(Colony colony)
        {
            int index = colony.Structures.IndexOf(_structure);
            if (index < 0 || index >= colony.Structures.Count - 1) return;
            colony.Structures.RemoveAt(index);
            colony.Structures.Insert(index + 1, _structure);
        }

        public void Delete(Colony colony)
        {
            colony.Structures.Remove(_structure);
        }
    }
}
