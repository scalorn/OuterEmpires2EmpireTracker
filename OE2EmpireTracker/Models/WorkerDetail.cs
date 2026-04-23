using System.Collections.Generic;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Models
{
    public class WorkerDetail
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Defines the mapping between blueprint property keys, worker key prefixes,
        /// and display names for each worker type. Used to drive worker parsing loops
        /// instead of duplicating BlueCollar/WhiteCollar/Specialist blocks.
        /// </summary>
        public static readonly WorkerTypeInfo[] WorkerTypes = new[]
        {
            new WorkerTypeInfo(
                GameConstants.WorkerIdBlueCollar,
                "BlueCollar",
                "Blue Collar",
                GameConstants.PropBlueCollarDetail,
                GameConstants.PropUnassignedBlueCollarDetail),
            new WorkerTypeInfo(
                GameConstants.WorkerIdWhiteCollar,
                "WhiteCollar",
                "White Collar",
                GameConstants.PropWhiteCollarDetail,
                GameConstants.PropUnassignedWhiteCollarDetail),
            new WorkerTypeInfo(
                GameConstants.WorkerIdSpecialist,
                "Specialist",
                "Specialist",
                GameConstants.PropSpecialistDetail,
                GameConstants.PropUnassignedSpecialistDetail),
        };

        private static readonly List<WorkerDetail> _workerDetails = GetWorkerDetails();
        private static readonly Dictionary<string, WorkerDetail> _workerDetailMapByID;
        private static readonly Dictionary<string, WorkerDetail> _workerDetailMapByName;

        public static IReadOnlyList<WorkerDetail> WorkerDetails => _workerDetails.AsReadOnly();
        public static IReadOnlyDictionary<string, WorkerDetail> WorkerDetailMapByID => _workerDetailMapByID;
        public static IReadOnlyDictionary<string, WorkerDetail> WorkerDetailMapByName => _workerDetailMapByName;

        static WorkerDetail()
        {
            _workerDetailMapByID = new Dictionary<string, WorkerDetail>();
            _workerDetailMapByName = new Dictionary<string, WorkerDetail>();
            foreach (var w in _workerDetails)
            {
                _workerDetailMapByID[w.ID] = w;
                _workerDetailMapByName[w.Name] = w;
            }
        }

        public WorkerDetail()
        {
        }

        private static List<WorkerDetail> GetWorkerDetails()
        {
            return new List<WorkerDetail>
            {
                new WorkerDetail { ID = string.Empty,   Name = string.Empty   },
                new WorkerDetail { ID = GameConstants.WorkerIdBlueCollar,   Name = GameConstants.PropBlueCollarDetail   },
                new WorkerDetail { ID = GameConstants.WorkerIdWhiteCollar,  Name = GameConstants.PropWhiteCollarDetail  },
                new WorkerDetail { ID = GameConstants.WorkerIdSpecialist,   Name = GameConstants.PropSpecialistDetail   },
            };
        }
    }

    /// <summary>
    /// Describes a worker type's blueprint property keys and display name.
    /// </summary>
    public class WorkerTypeInfo
    {
        /// <summary>Item type ID for WorkDetail items (e.g. "BlueCollarDetail"). No spaces.</summary>
        public string DetailKey { get; }
        /// <summary>Worker key prefix used in AssignedWorkers (e.g. "BlueCollar").</summary>
        public string WorkerPrefix { get; }
        /// <summary>Display name for UI (e.g. "Blue Collar").</summary>
        public string DisplayName { get; }
        /// <summary>Item type ID for unassigned workers (e.g. "UnassignedBlueCollarDetail"). No spaces.</summary>
        public string UnassignedKey { get; }
        /// <summary>Blueprint property key for the worker count (e.g. "Blue Collar Detail"). With spaces, matches JSON data.</summary>
        public string PropertyKey { get; }
        /// <summary>Blueprint property key for unassigned workers (e.g. "Unassigned Blue Collar Detail"). With spaces, matches JSON data.</summary>
        public string UnassignedPropertyKey { get; }

        public WorkerTypeInfo(
            string detailKey,
            string workerPrefix,
            string displayName,
            string propertyKey,
            string unassignedPropertyKey)
        {
            DetailKey = detailKey;
            WorkerPrefix = workerPrefix;
            DisplayName = displayName;
            UnassignedKey = "Unassigned" + detailKey;
            PropertyKey = propertyKey;
            UnassignedPropertyKey = unassignedPropertyKey;
        }
    }
}
