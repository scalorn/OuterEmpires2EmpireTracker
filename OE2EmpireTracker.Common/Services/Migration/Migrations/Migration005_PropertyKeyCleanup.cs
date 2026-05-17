using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration005_PropertyKeyCleanup
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, string> Remap = new Dictionary<string, string>
        {
            { "Blue Collar Detail(s)", GameConstants.PropBlueCollarDetail },
            { "Unassigned White Collar Detail(s)", GameConstants.PropUnassignedWhiteCollarDetail },
            { "Unassigned Specialist Detail(s)", GameConstants.PropUnassignedSpecialistDetail },
            { "Specialist Detail(s)", GameConstants.PropSpecialistDetail },
            { "White Collar Detail(s)", GameConstants.PropWhiteCollarDetail },
            { "Warehousing Capacity", GameConstants.PropWarehouseCapacity },
        };

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            int renamed = 0;

            // Player blueprints
            foreach (var bp in pc.BlueprintList)
            {
                renamed += RemapProperties(bp);
            }

            // Global (baseline) blueprints
            if (ec.GlobalBlueprintList != null)
            {
                foreach (var bp in ec.GlobalBlueprintList)
                {
                    renamed += RemapProperties(bp);
                }
            }

            Log.Info("Migration005: renamed {0} property key(s)", renamed);
        }

        private static int RemapProperties(Blueprint bp)
        {
            if (bp?.Properties == null) return 0;

            int count = 0;
            var props = bp.Properties.Properties;

            foreach (var pair in Remap)
            {
                string oldKey = pair.Key;
                string newKey = pair.Value;

                if (props.TryGetValue(oldKey, out string value))
                {
                    props[newKey] = value;
                    props.Remove(oldKey);
                    count++;
                }
            }

            return count;
        }
    }
}
