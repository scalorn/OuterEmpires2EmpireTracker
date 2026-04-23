using System;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration004_ColonyImportTimestampBackfill
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            // Backfill null/empty LastImportDateTime on all colonies
            foreach (var colony in pc.ColonyList)
            {
                if (string.IsNullOrEmpty(colony.LastImportDateTime))
                {
                    colony.LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow);
                }
            }

            // Convert CountDownTime StartTime/EndTime from local to UTC
            foreach (var colony in pc.ColonyList)
            {
                foreach (var structure in colony.Structures)
                {
                    ConvertToUtc(structure.ProcessCompletionTime);
                    ConvertToUtc(structure.BuildCompletionTime);
                }
            }

            foreach (var profile in pc.PlayerProfileList)
            {
                foreach (var skill in profile.Skills.Values)
                {
                    ConvertToUtc(skill.CompletionTime);
                }
            }
        }

        private static void ConvertToUtc(CountDownTime timer)
        {
            if (timer == null) return;
            if (timer.StartTime != DateTime.MinValue)
                timer.StartTime = timer.StartTime.ToUniversalTime();
            if (timer.EndTime != DateTime.MinValue)
                timer.EndTime = timer.EndTime.ToUniversalTime();
        }
    }
}
