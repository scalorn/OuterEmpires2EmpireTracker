using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Spatial index for fast range queries over star systems.
    /// Divides the galaxy into a grid of fixed-size cells and assigns each system
    /// to its cell. Range queries use bounding-box cell overlap followed by exact
    /// Euclidean distance filtering.
    /// </summary>
    public class SystemGridIndex
    {
        /// <summary>
        /// JAS scaling factor matching DistanceCalculator (25/16).
        /// Converts coordinate distance to JAS units.
        /// </summary>
        private const decimal JasScalingFactor = 1.5625m;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<(int CellX, int CellY), List<int>> _grid =
            new Dictionary<(int CellX, int CellY), List<int>>();

        private readonly SystemRepository _repository;

        private readonly decimal _cellSizeCoord;

        private readonly int _cellSizeJas;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemGridIndex"/> class.
        /// Builds the spatial grid from all systems in the repository.
        /// </summary>
        /// <param name="repository">The system repository containing all star systems.</param>
        /// <param name="cellSizeJas">The cell size in JAS units (default 50).</param>
        public SystemGridIndex(SystemRepository repository, int cellSizeJas = 50)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cellSizeJas = cellSizeJas;
            _cellSizeCoord = cellSizeJas * JasScalingFactor;

            BuildGrid();
        }

        /// <summary>
        /// Computes the set of system IDs within the given JAS range of the specified system.
        /// Uses bounding-box cell overlap to narrow candidates, then filters by exact distance.
        /// </summary>
        /// <param name="systemId">The center system ID.</param>
        /// <param name="rangeJas">The range in JAS units.</param>
        /// <returns>A set of system IDs within range (includes the center system itself).</returns>
        public HashSet<int> ComputeSystemsInRange(int systemId, int rangeJas)
        {
            var result = new HashSet<int>();

            StarSystem center = _repository.FindById(systemId);
            if (center == null)
            {
                Log.Warn("ComputeSystemsInRange: system {0} not found in repository", systemId);
                return result;
            }

            decimal rangeCoord = rangeJas * JasScalingFactor;
            decimal rangeSquared = rangeCoord * rangeCoord;

            int minCellX = (int)Math.Floor((double)((center.X - rangeCoord) / _cellSizeCoord));
            int maxCellX = (int)Math.Floor((double)((center.X + rangeCoord) / _cellSizeCoord));
            int minCellY = (int)Math.Floor((double)((center.Y - rangeCoord) / _cellSizeCoord));
            int maxCellY = (int)Math.Floor((double)((center.Y + rangeCoord) / _cellSizeCoord));

            for (int cx = minCellX; cx <= maxCellX; cx++)
            {
                for (int cy = minCellY; cy <= maxCellY; cy++)
                {
                    if (!_grid.TryGetValue((cx, cy), out List<int> cellSystems))
                    {
                        continue;
                    }

                    foreach (int candidateId in cellSystems)
                    {
                        StarSystem candidate = _repository.FindById(candidateId);
                        if (candidate == null)
                        {
                            continue;
                        }

                        decimal dx = candidate.X - center.X;
                        decimal dy = candidate.Y - center.Y;
                        decimal distSquared = (dx * dx) + (dy * dy);

                        if (distSquared <= rangeSquared)
                        {
                            result.Add(candidateId);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether two systems are within the specified JAS range of each other.
        /// </summary>
        /// <param name="systemIdA">The first system ID.</param>
        /// <param name="systemIdB">The second system ID.</param>
        /// <param name="rangeJas">The maximum range in JAS units.</param>
        /// <returns>True if the systems are within range; false otherwise.</returns>
        public bool IsInRange(int systemIdA, int systemIdB, int rangeJas)
        {
            StarSystem a = _repository.FindById(systemIdA);
            StarSystem b = _repository.FindById(systemIdB);

            if (a == null || b == null)
            {
                return false;
            }

            decimal rangeCoord = rangeJas * JasScalingFactor;
            decimal rangeSquared = rangeCoord * rangeCoord;

            decimal dx = b.X - a.X;
            decimal dy = b.Y - a.Y;
            decimal distSquared = (dx * dx) + (dy * dy);

            return distSquared <= rangeSquared;
        }

        private void BuildGrid()
        {
            foreach (StarSystem system in _repository.Systems)
            {
                int cellX = (int)Math.Floor((double)(system.X / _cellSizeCoord));
                int cellY = (int)Math.Floor((double)(system.Y / _cellSizeCoord));

                var key = (cellX, cellY);
                if (!_grid.TryGetValue(key, out List<int> cellList))
                {
                    cellList = new List<int>();
                    _grid[key] = cellList;
                }

                cellList.Add(system.Id);
            }

            Log.Info(
                "SystemGridIndex built: {0} systems across {1} cells (cell size {2} JAS)",
                _repository.Count,
                _grid.Count,
                _cellSizeJas);
        }
    }
}
