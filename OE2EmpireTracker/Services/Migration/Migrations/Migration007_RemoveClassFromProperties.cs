using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Services.Migration
{
    /// <summary>
    /// Migration007: Remove "Class" from blueprint PropertyBags.
    ///
    /// The BlueprintScanner parsed "Class" from HTML into the PropertyBag but
    /// did not remove it after extracting the value into Blueprint.Class (int).
    /// This left a stale "Class" entry in the PropertyBag that triggered
    /// "unknown property" warnings on the Blueprint form.
    /// </summary>
    public static class Migration007_RemoveClassFromProperties
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            int removed = 0;

            foreach (var bp in pc.BlueprintList)
                if (RemoveClass(bp)) removed++;

            if (ec.GlobalBlueprintList != null)
                foreach (var bp in ec.GlobalBlueprintList)
                {
                    if (RemoveClass(bp)) removed++;
                }

            Log.Info("Migration007: removed 'Class' from {0} blueprint PropertyBag(s)", removed);
        }

        private static bool RemoveClass(Blueprint bp)
        {
            if (bp?.Properties == null) return false;
            if (!bp.Properties.ContainsKey("Class")) return false;

            // Ensure Blueprint.Class has the value before removing
            string classStr;
            bp.Properties.GetString("Class", null, out classStr);
            if (classStr != null)
            {
                int cls;
                if (int.TryParse(classStr, out cls) && bp.Class == 0)
                    bp.Class = cls;
            }

            bp.Properties.Remove("Class");
            return true;
        }
    }
}
