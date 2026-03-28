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
    }
}
