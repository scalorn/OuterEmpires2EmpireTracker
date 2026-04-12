using NLog;
using OE2EmpireTracker.Services;
using System;

namespace OE2EmpireTracker.Services.Migration
{
    public static class Migration003_SurveyDateTimeNormalization
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            foreach (var survey in pc.SurveyList)
            {
                string original = survey.DateTime;

                // Already ISO?
                if (SurveyDateTimeParser.TryParseIso(original, out _)) continue;

                // Try game format
                if (SurveyDateTimeParser.TryParseGameFormat(original, out DateTime parsed))
                {
                    survey.DateTime = SurveyDateTimeParser.ToIsoString(parsed);
                    continue;
                }

                // Try common .NET formats
                if (DateTime.TryParse(original, out DateTime fallback))
                {
                    survey.DateTime = SurveyDateTimeParser.ToIsoString(fallback);
                    continue;
                }

                // Unparseable or null/empty — replace with now, log warning
                Log.Warn("Survey '{0}': replacing unparseable DateTime '{1}' with current time",
                    survey.PlanetName ?? "(unknown)", original ?? "(null)");
                survey.DateTime = SurveyDateTimeParser.ToIsoString(DateTime.Now);
            }
        }
    }
}
