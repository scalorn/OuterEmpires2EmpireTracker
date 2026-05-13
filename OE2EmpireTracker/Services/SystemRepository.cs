using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    public class SystemRepository
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private List<StarSystem> _systems = new List<StarSystem>();

        private Dictionary<int, StarSystem> _idIndex = new Dictionary<int, StarSystem>();

        private Dictionary<string, StarSystem> _nameIndex =
            new Dictionary<string, StarSystem>(StringComparer.OrdinalIgnoreCase);

        private string _filePath;

        public event EventHandler SystemDataChanged;

        public static string FilePath { get; set; } = "SystemData.json";

        public int Count => _systems.Count;

        public IReadOnlyList<StarSystem> Systems => _systems;

        public StarSystem FindById(int id)
        {
            _idIndex.TryGetValue(id, out StarSystem system);
            return system;
        }

        public StarSystem FindByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            _nameIndex.TryGetValue(name, out StarSystem system);
            return system;
        }

        public IEnumerable<StarSystem> FindByGrid(int? quadrant, int? sector, int? region, int? locality)
        {
            return _systems.Where(s =>
                (!quadrant.HasValue || s.Quadrant == quadrant.Value) &&
                (!sector.HasValue || s.Sector == sector.Value) &&
                (!region.HasValue || s.Region == region.Value) &&
                (!locality.HasValue || s.Locality == locality.Value));
        }

        public IEnumerable<StarSystem> SearchByName(string partial)
        {
            if (string.IsNullOrEmpty(partial))
            {
                return Enumerable.Empty<StarSystem>();
            }

            return _systems.Where(s =>
                s.Name.IndexOf(partial, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public IEnumerable<StarSystem> FindWithSpaceport()
        {
            return _systems.Where(s => s.HasSpaceport);
        }

        public IEnumerable<StarSystem> FindWithStarbase()
        {
            return _systems.Where(s => s.HasStarbase);
        }

        public IEnumerable<StarSystem> FindWithInfrastructure()
        {
            return _systems.Where(s => s.HasOrbital || s.HasSpaceport || s.HasStarbase);
        }

        public IEnumerable<StarSystem> FindByFaction(int factionId)
        {
            return _systems.Where(s => s.FactionId == factionId);
        }

        public IEnumerable<(int FactionId, string Name, string Color, int Count)> GetFactionSummary()
        {
            return _systems
                .Where(s => s.FactionId != 0)
                .GroupBy(s => s.FactionId)
                .Select(g =>
                {
                    var first = g.First();
                    return (first.FactionId, first.FactionName, first.FactionColor, g.Count());
                });
        }

        public void UpdateSystem(int id, Action<StarSystem> mutator)
        {
            var system = FindById(id);
            if (system == null)
            {
                Log.Warn("UpdateSystem: system with Id {0} not found, no-op", id);
                return;
            }

            mutator(system);
            Save();
            SystemDataChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ReplaceAll(List<StarSystem> systems)
        {
            _systems = systems ?? new List<StarSystem>();
            RebuildIndexes();
            Save();
            SystemDataChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Load(string filePath)
        {
            _filePath = filePath;

            if (!File.Exists(filePath))
            {
                Log.Warn("SystemData file not found at {0}, initializing with 0 systems", filePath);
                _systems = new List<StarSystem>();
                RebuildIndexes();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Log.Warn("SystemData file is empty at {0}, initializing with 0 systems", filePath);
                    _systems = new List<StarSystem>();
                    RebuildIndexes();
                    return;
                }

                var systems = JsonConvert.DeserializeObject<List<StarSystem>>(json);
                _systems = systems ?? new List<StarSystem>();
                RebuildIndexes();
                Log.Info("Loaded {0} star systems from {1}", _systems.Count, filePath);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Malformed JSON in SystemData file {0}, initializing with 0 systems", filePath);
                _systems = new List<StarSystem>();
                RebuildIndexes();
            }
            catch (IOException ex)
            {
                Log.Error(ex, "I/O error reading SystemData file {0}, initializing with 0 systems", filePath);
                _systems = new List<StarSystem>();
                RebuildIndexes();
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = FilePath;
            }

            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.None
            };

            string json = JsonConvert.SerializeObject(_systems, settings);
            SafeFileWriter.WriteAllText(_filePath, json);
            Log.Debug("Saved {0} star systems to {1}", _systems.Count, _filePath);
        }

        private void RebuildIndexes()
        {
            _idIndex = new Dictionary<int, StarSystem>(_systems.Count);
            _nameIndex = new Dictionary<string, StarSystem>(_systems.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var system in _systems)
            {
                _idIndex[system.Id] = system;

                if (!string.IsNullOrEmpty(system.Name))
                {
                    if (_nameIndex.ContainsKey(system.Name))
                    {
                        Log.Warn(
                            "Duplicate system name '{0}' (Id {1}), first-wins in name index",
                            system.Name,
                            system.Id);
                    }
                    else
                    {
                        _nameIndex[system.Name] = system;
                    }
                }
            }
        }
    }
}
