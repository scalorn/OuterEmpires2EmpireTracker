using System;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Abstracts access to worker assignment state for a colony structure.
    /// Allows the calculator to operate against actual or ideal worker data.
    /// </summary>
    public interface IColonyStructureWorkers
    {
        /// <summary>
        /// Returns whether the named worker slot is assigned for the given structure.
        /// </summary>
        bool IsWorkerAssigned(ColonyStructure structure, string workerKey);

        /// <summary>
        /// Persists the resolved worker assignment state back to the structure.
        /// </summary>
        void SetWorkerAssigned(ColonyStructure structure, string workerKey, bool assigned);

        /// <summary>
        /// Returns the Built, Staged and Online state for the given structure.
        /// </summary>
        void GetStructureState(ColonyStructure structure, out bool built, out bool staged, out bool online);

        bool IsUnassignedWorkerAvailable(string workerKey);
    }

    /// <summary>
    /// Returns actual worker assignment state from the structure's AssignedWorkers property bag.
    /// </summary>
    public class ActualColonyStructureWorkers : IColonyStructureWorkers
    {
        private readonly Colony _colony;

        public ActualColonyStructureWorkers(Colony colony = null)
        {
            _colony = colony;
        }

        public bool IsWorkerAssigned(ColonyStructure structure, string workerKey)
        {
            bool assigned = false;
            structure.AssignedWorkers.GetBoolean(workerKey, false, out assigned);
            return assigned;
        }

        public void SetWorkerAssigned(ColonyStructure structure, string workerKey, bool assigned)
        {
            structure.AssignedWorkers.SetProperty(workerKey, assigned);
        }

        public void GetStructureState(ColonyStructure structure, out bool built, out bool staged, out bool online)
        {
            built = false;
            structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
            staged = false;
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
            online = false;
            structure.Properties.GetBoolean(GameConstants.PropOnline, false, out online);
        }

        public bool IsUnassignedWorkerAvailable(string workerKey)
        {
            if (_colony == null) return true; // fallback when no colony context

            int total = _colony.Items.CountByType(
                OE2EmpireTracker.Models.ItemType.ItemTypeEnum.WorkDetail, workerKey);

            // Subtract locked quantity if LockTracking is available
            int locked = 0;
            if (_colony.Locks != null)
            {
                locked = _colony.Locks.GetLockedQuantity(
                    OE2EmpireTracker.Models.ItemType.ItemTypeEnum.WorkDetail, workerKey);
            }

            return (total - locked) > 0;
        }
    }

    /// <summary>
    /// Returns true for every worker slot, simulating a fully-staffed colony.
    /// Used to calculate ideal resource status when all workers are provided.
    /// </summary>
    public class IdealColonyStructureWorkers : IColonyStructureWorkers
    {
        public bool IsWorkerAssigned(ColonyStructure structure, string workerKey)
        {
            return true;
        }

        /// <summary>
        /// No-op: ideal simulation must not mutate actual worker assignment data.
        /// </summary>
        public void SetWorkerAssigned(ColonyStructure structure, string workerKey, bool assigned)
        {
            // intentionally empty
        }

        /// <summary>
        /// Returns ideal state: Built=true, Staged=false, Online=true.
        /// </summary>
        public void GetStructureState(ColonyStructure structure, out bool built, out bool staged, out bool online)
        {
            built = true;
            staged = false;
            online = true;
        }

        public bool IsUnassignedWorkerAvailable(string workerKey)
        {
            return true;
        }
    }
}
