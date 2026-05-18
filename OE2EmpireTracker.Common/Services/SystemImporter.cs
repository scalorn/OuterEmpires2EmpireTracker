using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Imports star system data from the extracted galaxy JSON file
    /// (oe2-galaxy-systems.json) and writes it to SystemData.json in the
    /// application's compact serialization format.
    /// </summary>
    public static class SystemImporter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings OutputSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        /// <summary>
        /// Reads the source galaxy extract file, maps fields to StarSystem objects,
        /// and writes the result to the output path via SafeFileWriter.
        /// Returns the number of systems imported, or 0 if the source is missing or malformed.
        /// </summary>
        public static int Import(string sourcePath, string outputPath)
        {
            if (!File.Exists(sourcePath))
            {
                Log.Warn("SystemImporter: source file not found: {0}", sourcePath);
                return 0;
            }

            JArray entries;
            try
            {
                string json = File.ReadAllText(sourcePath);
                entries = JArray.Parse(json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SystemImporter: failed to parse source file: {0}", sourcePath);
                return 0;
            }

            var systems = new List<StarSystem>(entries.Count);

            foreach (JObject entry in entries)
            {
                var system = new StarSystem
                {
                    Id = entry.Value<int>("id"),
                    Name = entry.Value<string>("n") ?? string.Empty,
                    X = entry.Value<decimal>("x"),
                    Y = entry.Value<decimal>("y"),
                    Quadrant = entry.Value<int>("q"),
                    Sector = entry.Value<int>("s"),
                    Region = entry.Value<int>("r"),
                    Locality = entry.Value<int>("l"),
                    SpectralClass = entry.Value<string>("st") ?? string.Empty,
                    FactionId = entry.Value<int>("fid"),
                    FactionName = entry.Value<string>("fn") ?? string.Empty,
                    FactionColor = entry.Value<string>("fc") ?? string.Empty,
                    HasOrbital = entry.Value<int>("o") != 0,
                    HasSpaceport = entry.Value<int>("sp") != 0,
                    HasStarbase = entry.Value<int>("sb") != 0,
                };

                systems.Add(system);
            }

            string output = JsonConvert.SerializeObject(systems, OutputSettings);
            SafeFileWriter.WriteAllText(outputPath, output);

            Log.Info("SystemImporter: imported {0} systems to {1}", systems.Count, outputPath);
            return systems.Count;
        }
    }
}
