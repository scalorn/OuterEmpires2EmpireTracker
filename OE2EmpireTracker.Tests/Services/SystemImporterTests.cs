// <copyright file="SystemImporterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based and unit tests for SystemImporter.
    /// Feature: systems-model
    /// </summary>
    [TestFixture]
    public class SystemImporterTests
    {
        private readonly List<string> _tempFiles = new List<string>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                string bak = file + ".bak";
                if (File.Exists(bak))
                {
                    File.Delete(bak);
                }

                string tmp = file + ".tmp";
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
            }

            _tempFiles.Clear();
        }

        // ---------------------------------------------------------------
        // Helper: Create a temp file path and track it for cleanup
        // ---------------------------------------------------------------

        private string CreateTempFile()
        {
            var path = Path.GetTempFileName();
            _tempFiles.Add(path);
            return path;
        }

        // ---------------------------------------------------------------
        // Property 12: Import field mapping preserves all source data
        // For any valid source record (with id, n, x, y, q, s, r, l, st,
        // fid, fn, fc as strings/numbers and o, sp, sb as 0 or 1), the
        // import mapping SHALL produce a StarSystem where all fields match.
        // **Validates: Requirements 8.2, 8.3**
        // ---------------------------------------------------------------

        [Test]
        public void Property12_ImportFieldMapping_PreservesAllSourceData()
        {
            var sourceGen =
                from id in Gen.Choose(1, 100000)
                from name in Gen.Elements("Sol", "Proxima", "Vega", "Rigel", "Deneb")
                from x in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                from y in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                from q in Gen.Choose(1, 4)
                from s in Gen.Choose(1, 4)
                from r in Gen.Choose(1, 4)
                from l in Gen.Choose(1, 4)
                from st in Gen.Elements("M", "K", "G", "F", "W", "X")
                from fid in Gen.Choose(0, 50)
                from fn in Gen.Elements(string.Empty, "Galactic", "Empire", "Rebels")
                from fc in Gen.Elements(string.Empty, "#FF0000", "#00FF00", "#0000FF")
                from o in Gen.Elements(0, 1)
                from sp in Gen.Elements(0, 1)
                from sb in Gen.Elements(0, 1)
                select new { id, name, x, y, q, s, r, l, st, fid, fn, fc, o, sp, sb };

            var arb = Arb.From(sourceGen);

            var prop = Prop.ForAll(arb, record =>
            {
                var entry = new JObject
                {
                    ["id"] = record.id,
                    ["n"] = record.name,
                    ["x"] = record.x,
                    ["y"] = record.y,
                    ["q"] = record.q,
                    ["s"] = record.s,
                    ["r"] = record.r,
                    ["l"] = record.l,
                    ["st"] = record.st,
                    ["fid"] = record.fid,
                    ["fn"] = record.fn,
                    ["fc"] = record.fc,
                    ["o"] = record.o,
                    ["sp"] = record.sp,
                    ["sb"] = record.sb,
                };
                var sourceArray = new JArray { entry };

                var sourcePath = CreateTempFile();
                var outputPath = CreateTempFile();
                File.WriteAllText(sourcePath, sourceArray.ToString());

                int count = SystemImporter.Import(sourcePath, outputPath);
                if (count != 1)
                {
                    return false;
                }

                string outputJson = File.ReadAllText(outputPath);
                var systems = JsonConvert.DeserializeObject<List<StarSystem>>(outputJson);
                if (systems == null || systems.Count != 1)
                {
                    return false;
                }

                var sys = systems[0];
                return sys.Id == record.id
                    && sys.Name == record.name
                    && sys.X == record.x
                    && sys.Y == record.y
                    && sys.Quadrant == record.q
                    && sys.Sector == record.s
                    && sys.Region == record.r
                    && sys.Locality == record.l
                    && sys.SpectralClass == record.st
                    && sys.FactionId == record.fid
                    && sys.FactionName == record.fn
                    && sys.FactionColor == record.fc
                    && sys.HasOrbital == (record.o != 0)
                    && sys.HasSpaceport == (record.sp != 0)
                    && sys.HasStarbase == (record.sb != 0);
            });

            prop.QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 13: Import is idempotent
        // Running import twice on the same source produces byte-identical
        // SystemData.json output.
        // **Validates: Requirements 8.6**
        // ---------------------------------------------------------------

        [Test]
        public void Property13_Import_IsIdempotent()
        {
            var sourceGen =
                from count in Gen.Choose(1, 5)
                from records in Gen.ListOf(count,
                    from id in Gen.Choose(1, 100000)
                    from name in Gen.Elements("Sol", "Proxima", "Vega", "Rigel")
                    from x in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                    from y in Gen.Choose(-1000000, 1000000).Select(v => v / 1000m)
                    from q in Gen.Choose(1, 4)
                    from s in Gen.Choose(1, 4)
                    from r in Gen.Choose(1, 4)
                    from l in Gen.Choose(1, 4)
                    from st in Gen.Elements("M", "K", "G", "F", "W", "X")
                    from fid in Gen.Choose(0, 50)
                    from fn in Gen.Elements(string.Empty, "Galactic", "Empire")
                    from fc in Gen.Elements(string.Empty, "#FF0000", "#00FF00")
                    from o in Gen.Elements(0, 1)
                    from sp in Gen.Elements(0, 1)
                    from sb in Gen.Elements(0, 1)
                    select new JObject
                    {
                        ["id"] = id,
                        ["n"] = name,
                        ["x"] = x,
                        ["y"] = y,
                        ["q"] = q,
                        ["s"] = s,
                        ["r"] = r,
                        ["l"] = l,
                        ["st"] = st,
                        ["fid"] = fid,
                        ["fn"] = fn,
                        ["fc"] = fc,
                        ["o"] = o,
                        ["sp"] = sp,
                        ["sb"] = sb,
                    })
                select new JArray(records.ToArray());

            var arb = Arb.From<JArray>(sourceGen);

            var prop = Prop.ForAll(arb, sourceArray =>
            {
                var sourcePath = CreateTempFile();
                var outputPath = CreateTempFile();
                File.WriteAllText(sourcePath, sourceArray.ToString());

                // First import
                SystemImporter.Import(sourcePath, outputPath);
                byte[] firstOutput = File.ReadAllBytes(outputPath);

                // Second import (same source, same output path)
                SystemImporter.Import(sourcePath, outputPath);
                byte[] secondOutput = File.ReadAllBytes(outputPath);

                return firstOutput.SequenceEqual(secondOutput);
            });

            prop.QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Unit tests: SystemImporter error cases
        // Satisfies: Req 8, Criterion 5
        // ---------------------------------------------------------------

        [Test]
        public void Import_MissingSourceFile_ReturnsZero()
        {
            var sourcePath = Path.Combine(
                Path.GetTempPath(), Guid.NewGuid() + ".json");
            var outputPath = CreateTempFile();

            int result = SystemImporter.Import(sourcePath, outputPath);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Import_MalformedSourceFile_ReturnsZero()
        {
            var sourcePath = CreateTempFile();
            var outputPath = CreateTempFile();
            File.WriteAllText(sourcePath, "this is not valid json {{{");

            int result = SystemImporter.Import(sourcePath, outputPath);

            Assert.That(result, Is.EqualTo(0));
        }
    }
}
