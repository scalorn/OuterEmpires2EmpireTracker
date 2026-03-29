namespace OE2EmpireTracker.Baseline
{
    /// <summary>
    /// Represents a single worker unit assigned to a colony structure.
    /// Used for tracking labor capacity within the ColonyStatusCalculator.
    /// </summary>
    public class ColonyWorker
    {
        /// <summary>
        /// The structure that this worker is currently assigned to.
        /// </summary>
        public ColonyStructure Structure { get; set; }

        /// <summary>
        /// Categorization of the worker role (e.g., "BlueCollar1", "WhiteCollar1", "Specialist1").
        /// </summary>
        public string WorkerType { get; set; }

        /// <summary>
        /// Indicates if this worker slot is currently active/assigned to a task.
        /// </summary>
        public bool Assigned { get; set; }

        public ColonyWorker(ColonyStructure structure, string workerType, bool assigned)
        {
            Structure = structure;
            WorkerType = workerType;
            Assigned = assigned;
        }
    }
}
