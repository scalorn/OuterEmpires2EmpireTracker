using System.Collections.Generic;

namespace OE2EmpireTracker.Data
{
    public class WorkerDetail
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

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
                new WorkerDetail { ID = "BlueCollarDetail",   Name = "Blue Collar Detail"   },
                new WorkerDetail { ID = "WhiteCollarDetail",  Name = "White Collar Detail"  },
                new WorkerDetail { ID = "SpecialistDetail",   Name = "Specialist Detail"    },
            };
        }
    }
}
