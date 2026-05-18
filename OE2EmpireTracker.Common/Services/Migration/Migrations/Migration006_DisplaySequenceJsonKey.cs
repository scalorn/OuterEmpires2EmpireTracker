using NLog;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Services.Migration
{
    /// <summary>
    /// Migration006: Completes the gameSequence -> DisplaySequence JSON key rename.
    ///
    /// ColonyStructure.DisplaySequence previously serialized as "gameSequence" via
    /// [JsonProperty("gameSequence")]. That attribute has been replaced with a
    /// write-only legacy shim so old files still deserialize correctly, while new
    /// saves write the field as "displaySequence".
    ///
    /// This migration is a no-op at runtime (data is already in memory under the
    /// correct property). Its sole purpose is to bump DataVersion so that the next
    /// save writes "displaySequence" instead of "gameSequence" for all structures.
    /// </summary>
    public static class Migration006_DisplaySequenceJsonKey
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            int structureCount = 0;
            foreach (var colony in pc.ColonyList)
                structureCount += colony.Structures.Count;

            Log.Info(
                "Migration006: DisplaySequence JSON key rename applied to {0} structure(s) on next save",
                structureCount);
        }
    }
}
