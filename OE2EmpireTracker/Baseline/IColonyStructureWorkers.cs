using System;

namespace OE2EmpireTracker.Baseline
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
        public bool IsWorkerAssigned(ColonyStructure structure, string workerKey)
        {
            bool assigned = false;
            structure.AssignedWorkers.getBoolean(workerKey, false, out assigned);
            return assigned;
        }

        public void SetWorkerAssigned(ColonyStructure structure, string workerKey, bool assigned)
        {
            structure.AssignedWorkers.setProperty(workerKey, assigned);
        }

        public void GetStructureState(ColonyStructure structure, out bool built, out bool staged, out bool online)
        {
            built = false;
            structure.Properties.getBoolean("Built", false, out built);
            staged = false;
            structure.Properties.getBoolean("Staged", false, out staged);
            online = false;
            structure.Properties.getBoolean("Online", false, out online);
        }

        public bool IsUnassignedWorkerAvailable(string workerKey)
        {
            // TODO: FIXME: Need to look into the colony's warehouse and verify there is a worker available with the given workerKey (e.g., "BlueCollarDetail").
            return true;
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
