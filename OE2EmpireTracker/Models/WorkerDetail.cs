using System.Collections.Generic;

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
            new WorkerTypeInfo("BlueCollarDetail", "BlueCollar", "Blue Collar"),
            new WorkerTypeInfo("WhiteCollarDetail", "WhiteCollar", "White Collar"),
            new WorkerTypeInfo("SpecialistDetail", "Specialist", "Specialist"),
        };

        private static readonly List<WorkerDetail> _workerDetails = getWorkerDetails();
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

        private static List<WorkerDetail> getWorkerDetails()
        {
            return new List<WorkerDetail>
            {
                new WorkerDetail { ID = "",   Name = ""   },
                new WorkerDetail { ID = "BlueCollarDetail",   Name = "Blue Collar Detail"   },
                new WorkerDetail { ID = "WhiteCollarDetail",  Name = "White Collar Detail"  },
                new WorkerDetail { ID = "SpecialistDetail",   Name = "Specialist Detail"    },
            };
        }
    }

    /// <summary>
    /// Describes a worker type's blueprint property keys and display name.
    /// </summary>
    public class WorkerTypeInfo
    {
        /// <summary>Blueprint property key for the worker count (e.g. "BlueCollarDetail").</summary>
        public string DetailKey { get; }
        /// <summary>Worker key prefix used in AssignedWorkers (e.g. "BlueCollar").</summary>
        public string WorkerPrefix { get; }
        /// <summary>Display name for UI (e.g. "Blue Collar").</summary>
        public string DisplayName { get; }
        /// <summary>Blueprint property key for unassigned workers (e.g. "UnassignedBlueCollarDetail").</summary>
        public string UnassignedKey { get; }

        public WorkerTypeInfo(string detailKey, string workerPrefix, string displayName)
        {
            DetailKey = detailKey;
            WorkerPrefix = workerPrefix;
            DisplayName = displayName;
            UnassignedKey = "Unassigned" + detailKey;
        }
    }
}
