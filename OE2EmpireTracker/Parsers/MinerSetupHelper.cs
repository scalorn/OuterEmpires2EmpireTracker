using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Static helper for automating mining rig setup during colony import/reimport.
    /// Handles survey assignment, default survey creation, timer start, and warehouse resource seeding.
    /// </summary>
    public static class MinerSetupHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Finds the best matching real survey for the given planet, resource, purity, and maxRate.
        /// Returns null if no matching real survey exists.
        /// </summary>
        /// <remarks>
        /// Searches PlayerContext.SurveyList for real surveys (SurveyID != "DEFAULT") matching
        /// the colony planet name (case-insensitive) that contain the mined resource at the
        /// matching purity. When maxRate > 0, selects the survey whose SurveyResource Amount
        /// most closely matches the maxRate. When maxRate == 0, selects the survey with the
        /// highest Amount.
        /// </remarks>
        internal static Survey FindBestSurvey(
            string planetName, string resourceName, string purity,
            decimal maxRate, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(planetName) ||
                string.IsNullOrEmpty(resourceName) ||
                string.IsNullOrEmpty(purity) ||
                playerContext == null)
            {
                return null;
            }

            // Find all real surveys matching planet, resource, and purity
            var candidates = playerContext.SurveyList
                .Where(s =>
                    !string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.PlanetName, planetName, StringComparison.OrdinalIgnoreCase) &&
                    s.Resources != null &&
                    s.Resources.Values.Any(r =>
                        string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (maxRate > 0m)
            {
                // Select the survey whose resource Amount most closely matches maxRate
                return candidates
                    .OrderBy(s =>
                    {
                        var resource = s.Resources.Values.First(r =>
                            string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase));
                        decimal amount;
                        decimal.TryParse(resource.Amount, out amount);
                        return Math.Abs(amount - maxRate);
                    })
                    .First();
            }
            else
            {
                // maxRate == 0: select the survey with the highest Amount
                return candidates
                    .OrderByDescending(s =>
                    {
                        var resource = s.Resources.Values.First(r =>
                            string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase));
                        decimal amount;
                        decimal.TryParse(resource.Amount, out amount);
                        return amount;
                    })
                    .First();
            }
        }
    }
}
