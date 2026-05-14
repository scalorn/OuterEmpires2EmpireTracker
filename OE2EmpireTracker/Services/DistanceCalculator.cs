using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Computes Euclidean distance between star systems using their normalized
    /// X and Y coordinates. Static utility with no state.
    /// </summary>
    public static class DistanceCalculator
    {
        private const decimal JasScalingFactor = 1.5625m;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Calculates the Euclidean distance between two star systems.
        /// Returns -1 if either argument is null.
        /// </summary>
        public static decimal Calculate(StarSystem a, StarSystem b)
        {
            if (a == null || b == null)
            {
                return -1;
            }

            decimal dx = b.X - a.X;
            decimal dy = b.Y - a.Y;
            return (decimal)Math.Sqrt((double)((dx * dx) + (dy * dy)));
        }

        /// <summary>
        /// Calculates the Euclidean distance between two systems identified by ID.
        /// Returns -1 if either system cannot be resolved via the repository.
        /// </summary>
        public static decimal Calculate(int idA, int idB, SystemRepository repo)
        {
            if (repo == null)
            {
                return -1;
            }

            StarSystem a = repo.FindById(idA);
            StarSystem b = repo.FindById(idB);

            if (a == null || b == null)
            {
                return -1;
            }

            return Calculate(a, b);
        }

        /// <summary>
        /// Calculates the total route distance for an ordered list of system IDs.
        /// Sums consecutive leg distances, skipping unresolvable legs with a warning.
        /// Returns 0 for null, empty, or single-system routes.
        /// </summary>
        public static decimal CalculateRoute(IList<int> systemIds, SystemRepository repo)
        {
            if (systemIds == null || systemIds.Count < 2 || repo == null)
            {
                return 0;
            }

            decimal total = 0;

            for (int i = 0; i < systemIds.Count - 1; i++)
            {
                StarSystem from = repo.FindById(systemIds[i]);
                StarSystem to = repo.FindById(systemIds[i + 1]);

                if (from == null || to == null)
                {
                    Log.Warn(
                        "CalculateRoute: skipping leg {0}->{1}, unresolvable system (from={2}, to={3})",
                        systemIds[i],
                        systemIds[i + 1],
                        from != null ? "ok" : "null",
                        to != null ? "ok" : "null");
                    continue;
                }

                total += Calculate(from, to);
            }

            return total;
        }

        /// <summary>
        /// Calculates the JAS (Jump Assist System) distance between two star systems.
        /// Applies the empirically-determined scaling factor (25/16) to the Euclidean distance.
        /// Returns -1 if either argument is null.
        /// </summary>
        public static int CalculateJas(StarSystem a, StarSystem b)
        {
            decimal euclidean = Calculate(a, b);
            if (euclidean < 0)
            {
                return -1;
            }

            return (int)Math.Round(euclidean / JasScalingFactor, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates the JAS (Jump Assist System) distance between two systems identified by ID.
        /// Applies the empirically-determined scaling factor (25/16) to the Euclidean distance.
        /// Returns -1 if either system cannot be resolved via the repository.
        /// </summary>
        public static int CalculateJas(int idA, int idB, SystemRepository repo)
        {
            decimal euclidean = Calculate(idA, idB, repo);
            if (euclidean < 0)
            {
                return -1;
            }

            return (int)Math.Round(euclidean / JasScalingFactor, MidpointRounding.AwayFromZero);
        }
    }
}
