using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class EvolutionChainServiceTests
    {
        #region Helpers

        private static Bp MakeBlueprint(string uuid, string baseBlueprintUUID, int evolution)
        {
            var bp = new Bp("TestBP");
            bp.UUID = uuid;
            bp.BaseBlueprintUUID = baseBlueprintUUID;
            bp.Evolution = evolution;
            return bp;
        }

        #endregion

        #region Property 1: Chain resolution produces a complete, ordered ancestor list

        /// <summary>
        /// // Feature: evolution-graph, Property 1: Chain resolution produces a complete, ordered ancestor list
        /// Generate random chains of 1–16 blueprints with valid baseBlueprintUUID links;
        /// verify output is sorted ascending by Evolution and contains all chain members
        /// including the start blueprint.
        /// **Validates: Requirements 1.1, 1.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ChainResolution_ProducesCompleteOrderedAncestorList()
        {
            // Generate a chain length between 1 and 16
            var chainGen = Gen.Choose(1, 16).SelectMany(chainLength =>
            {
                // Generate distinct evolution levels sorted ascending
                // Pick chainLength distinct values from 0..15, then sort them
                var evolutionLevelsGen = Gen.Shuffle(Enumerable.Range(0, 16).ToArray())
                    .Select(shuffled => shuffled.Take(chainLength).OrderBy(x => x).ToArray());

                return evolutionLevelsGen.Select(evLevels =>
                {
                    // Build a chain of blueprints with linked baseBlueprintUUID
                    var blueprints = new List<Bp>();
                    for (int i = 0; i < evLevels.Length; i++)
                    {
                        string uuid = $"bp-{i}";
                        string baseUuid = i > 0 ? $"bp-{i - 1}" : null;
                        blueprints.Add(MakeBlueprint(uuid, baseUuid, evLevels[i]));
                    }

                    return blueprints;
                });
            });

            return Prop.ForAll(chainGen.ToArbitrary(), chain =>
            {
                // The start blueprint is the last one (highest evolution)
                var start = chain[chain.Count - 1];

                // Build a lookup for the resolver
                var lookup = chain.ToDictionary(bp => bp.UUID);
                Bp resolver(string uuid) =>
                    lookup.TryGetValue(uuid, out var bp) ? bp : null;

                var result = EvolutionChainService.ResolveChain(start, resolver);

                // Verify: result contains all chain members
                bool containsAll = chain.All(bp =>
                    result.Any(r => r.UUID == bp.UUID));

                // Verify: result count matches chain count
                bool correctCount = result.Count == chain.Count;

                // Verify: result is sorted by Evolution ascending
                bool isSorted = true;
                for (int i = 1; i < result.Count; i++)
                {
                    if (result[i].Evolution < result[i - 1].Evolution)
                    {
                        isSorted = false;
                        break;
                    }
                }

                // Verify: start blueprint is included
                bool containsStart = result.Any(r => r.UUID == start.UUID);

                return containsAll
                    .Label($"Contains all chain members: expected {chain.Count} UUIDs present")
                    .And(correctCount)
                    .Label($"Correct count: expected={chain.Count}, got={result.Count}")
                    .And(isSorted)
                    .Label("Result sorted by Evolution ascending")
                    .And(containsStart)
                    .Label("Result contains the start blueprint");
            });
        }

        #endregion

        #region Property 2: Graph data contains only numeric properties from the BlueprintType

        // Known numeric property names (Integer, Decimal, Time) from BlueprintPropertyValidation
        private static readonly string[] KnownNumericProperties = new[]
        {
            "Accuracy", "Mass", "Health", "Shield Hitpoints",           // Integer
            "Acceleration Rate", "Rate of Fire", "Mining Yield",        // Decimal
            "Manufacture Run Time"                                      // Time
        };

        // Known non-numeric property names (CheckBox, ComboBox) from BlueprintPropertyValidation
        private static readonly string[] KnownNonNumericProperties = new[]
        {
            "Can Manufacture", "Can Research", "Consumable",            // CheckBox
            "Commodity Industry"                                        // ComboBox
        };

        // Unknown property names (not in BlueprintPropertyValidation → Unknown type)
        private static readonly string[] UnknownProperties = new[]
        {
            "Ammo Type", "License Career", "Material Focus"
        };

        /// <summary>
        /// // Feature: evolution-graph, Property 2: Graph data contains only numeric properties from the BlueprintType
        /// Generate random BlueprintType.Properties arrays mixing numeric and non-numeric types;
        /// build graph data and verify all output keys are numeric and in the source array.
        /// **Validates: Requirements 2.1, 2.2, 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property GraphData_ContainsOnlyNumericPropertiesFromBlueprintType()
        {
            // Combine all candidate property names
            var allProperties = KnownNumericProperties
                .Concat(KnownNonNumericProperties)
                .Concat(UnknownProperties)
                .ToArray();

            // Generator: pick a random non-empty subset of properties (1..allProperties.Length)
            var propsGen = Gen.Shuffle(allProperties)
                .SelectMany(shuffled =>
                    Gen.Choose(1, shuffled.Length)
                        .Select(count => shuffled.Take(count).ToArray()));

            // Generator: chain of 2–5 blueprints with varying numeric values
            var testDataGen = propsGen.SelectMany(selectedProps =>
                Gen.Choose(2, 5).SelectMany(chainLen =>
                {
                    // For each blueprint in the chain, generate random values for numeric props
                    var chainGen = Gen.Sequence(
                        Enumerable.Range(0, chainLen).Select(idx =>
                            Gen.Choose(1, 1000).Select(val => new { Index = idx, Value = val })
                        )
                    );

                    return chainGen.Select(chainValues =>
                    {
                        var values = chainValues.ToArray();
                        var chain = new List<Bp>();

                        for (int i = 0; i < values.Length; i++)
                        {
                            var bp = MakeBlueprint($"bp-{i}", i > 0 ? $"bp-{i - 1}" : null, i);

                            // Set numeric properties with varying values
                            foreach (var propName in selectedProps)
                            {
                                var propType = BlueprintPropertyValidation.GetPropertyType(propName);
                                if (propType == PropertyValueType.Integer ||
                                    propType == PropertyValueType.Decimal)
                                {
                                    // Use different values per evolution to ensure change
                                    bp.Properties.setProperty(propName, (double)(values[i].Value + i * 10));
                                }
                                else if (propType == PropertyValueType.Time)
                                {
                                    int secs = values[i].Value + i * 60;
                                    bp.Properties.setProperty(propName, $"{secs}s");
                                }
                                // Non-numeric properties: set string values (should be filtered out)
                                else if (propType == PropertyValueType.CheckBox ||
                                         propType == PropertyValueType.Boolean)
                                {
                                    bp.Properties.setProperty(propName, "true");
                                }
                                else if (propType == PropertyValueType.ComboBox)
                                {
                                    bp.Properties.setProperty(propName, "SomeValue");
                                }
                                else
                                {
                                    bp.Properties.setProperty(propName, "FreeFormText");
                                }
                            }

                            chain.Add(bp);
                        }

                        return new { Chain = chain, SelectedProps = selectedProps };
                    });
                })
            );

            return Prop.ForAll(testDataGen.ToArbitrary(), testData =>
            {
                var result = EvolutionChainService.BuildGraphData(testData.Chain, testData.SelectedProps);

                // Every key in Series must exist in the source properties array
                bool allKeysInSource = result.Series.Keys.All(k =>
                    testData.SelectedProps.Contains(k));

                // Every key in Series must be a numeric type (Integer, Decimal, or Time)
                bool allKeysNumeric = result.Series.Keys.All(k =>
                {
                    var t = BlueprintPropertyValidation.GetPropertyType(k);
                    return t == PropertyValueType.Integer ||
                           t == PropertyValueType.Decimal ||
                           t == PropertyValueType.Time;
                });

                // No non-numeric property should appear in Series
                var nonNumericInSource = testData.SelectedProps.Where(p =>
                {
                    var t = BlueprintPropertyValidation.GetPropertyType(p);
                    return t != PropertyValueType.Integer &&
                           t != PropertyValueType.Decimal &&
                           t != PropertyValueType.Time;
                }).ToArray();

                bool noNonNumericInSeries = !nonNumericInSource.Any(p => result.Series.ContainsKey(p));

                return allKeysInSource
                    .Label("All Series keys exist in source BlueprintType.Properties array")
                    .And(allKeysNumeric)
                    .Label("All Series keys are numeric types (Integer, Decimal, or Time)")
                    .And(noNonNumericInSeries)
                    .Label("No non-numeric properties appear in Series");
            });
        }

        #endregion

        #region Property 3: Unchanged properties are excluded and NoChanges flag is correct

        // A small set of known numeric property names for controlled generation
        private static readonly string[] IntegerProps = new[] { "Accuracy", "Mass", "Health", "Shield Hitpoints" };
        private static readonly string[] DecimalProps = new[] { "Acceleration Rate", "Rate of Fire", "Mining Yield" };
        private static readonly string[] TimeProps = new[] { "Manufacture Run Time" };

        /// <summary>
        /// // Feature: evolution-graph, Property 3: Unchanged properties are excluded and NoChanges flag is correct
        /// Generate chains where some/all numeric properties are identical; verify unchanged
        /// properties are excluded and NoChanges flag is correct.
        /// **Validates: Requirements 2.4, 2.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property UnchangedProperties_AreExcluded_And_NoChangesFlag_IsCorrect()
        {
            // All numeric property names we'll use
            var allNumeric = IntegerProps.Concat(DecimalProps).Concat(TimeProps).ToArray();

            // Generator: pick 1..allNumeric.Length properties, split into "changed" and "unchanged" sets
            var testDataGen = Gen.Choose(1, allNumeric.Length).SelectMany(propCount =>
                Gen.Shuffle(allNumeric).SelectMany(shuffled =>
                {
                    var selectedProps = shuffled.Take(propCount).ToArray();

                    // Decide how many of the selected props will be "unchanged" (0..propCount)
                    return Gen.Choose(0, selectedProps.Length).SelectMany(unchangedCount =>
                    {
                        var unchangedProps = selectedProps.Take(unchangedCount).ToArray();
                        var changedProps = selectedProps.Skip(unchangedCount).ToArray();

                        // Chain length 2..6
                        return Gen.Choose(2, 6).SelectMany(chainLen =>
                        {
                            // Generate a non-zero base value for each property (1..500)
                            var baseValGen = Gen.Choose(1, 500);
                            // Generate a non-zero delta for changed properties (1..200)
                            var deltaGen = Gen.Choose(1, 200);

                            return baseValGen.SelectMany(baseVal =>
                                deltaGen.Select(delta =>
                                {
                                    var chain = new List<Bp>();
                                    for (int i = 0; i < chainLen; i++)
                                    {
                                        var bp = MakeBlueprint($"bp-{i}", i > 0 ? $"bp-{i - 1}" : null, i);

                                        foreach (var propName in unchangedProps)
                                        {
                                            var propType = BlueprintPropertyValidation.GetPropertyType(propName);
                                            if (propType == PropertyValueType.Time)
                                                bp.Properties.setProperty(propName, $"{baseVal}s");
                                            else
                                                bp.Properties.setProperty(propName, (double)baseVal);
                                        }

                                        foreach (var propName in changedProps)
                                        {
                                            // Ev0 gets baseVal, subsequent evolutions get baseVal + delta*i
                                            int val = baseVal + delta * i;
                                            var propType = BlueprintPropertyValidation.GetPropertyType(propName);
                                            if (propType == PropertyValueType.Time)
                                                bp.Properties.setProperty(propName, $"{val}s");
                                            else
                                                bp.Properties.setProperty(propName, (double)val);
                                        }

                                        chain.Add(bp);
                                    }

                                    return new
                                    {
                                        Chain = chain,
                                        AllProps = selectedProps,
                                        UnchangedProps = unchangedProps,
                                        ChangedProps = changedProps
                                    };
                                })
                            );
                        });
                    });
                })
            );

            return Prop.ForAll(testDataGen.ToArbitrary(), testData =>
            {
                var result = EvolutionChainService.BuildGraphData(testData.Chain, testData.AllProps);

                // Verify: unchanged properties do NOT appear in Series
                bool noUnchangedInSeries = !testData.UnchangedProps.Any(p => result.Series.ContainsKey(p));

                // Verify: if ALL numeric properties are unchanged (changedProps is empty),
                // NoChanges is true and Series is empty
                bool noChangesCorrect;
                if (testData.ChangedProps.Length == 0)
                {
                    noChangesCorrect = result.NoChanges && result.Series.Count == 0;
                }
                else
                {
                    // At least one property changed → NoChanges should be false
                    noChangesCorrect = !result.NoChanges;
                }

                // Verify: changed properties DO appear in Series (they have non-zero base and did change)
                bool changedPropsPresent = testData.ChangedProps.All(p => result.Series.ContainsKey(p));

                return noUnchangedInSeries
                    .Label($"Unchanged properties excluded from Series (unchanged={string.Join(",", testData.UnchangedProps)})")
                    .And(noChangesCorrect)
                    .Label($"NoChanges flag correct: ChangedProps.Length={testData.ChangedProps.Length}, NoChanges={result.NoChanges}, Series.Count={result.Series.Count}")
                    .And(changedPropsPresent)
                    .Label($"Changed properties present in Series (changed={string.Join(",", testData.ChangedProps)})");
            });
        }

        #endregion

        #region Property 4: Percentage normalization formula

        /// <summary>
        /// // Feature: evolution-graph, Property 4: Percentage normalization formula
        /// Generate chains with random numeric property values; verify each percentage
        /// equals (value / ev0Value) * 100 and Ev0 is always 100%.
        /// **Validates: Requirements 3.1, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PercentageNormalization_MatchesFormula_And_Ev0IsAlways100()
        {
            // Use known numeric property names: Integer and Decimal types
            var numericProps = new[] { "Accuracy", "Mass", "Acceleration Rate" };

            // Generator: chain of 2–6 blueprints with random non-zero values that vary
            var testDataGen = Gen.Choose(2, 6).SelectMany(chainLen =>
            {
                // For each property, generate a non-zero Ev0 value (1..500)
                // and per-evolution deltas (1..200) to ensure values change
                var ev0ValGen = Gen.Choose(1, 500);
                var deltaGen = Gen.Choose(1, 200);

                return ev0ValGen.SelectMany(ev0Val =>
                    deltaGen.Select(delta =>
                    {
                        var chain = new List<Bp>();
                        // Track the raw values per property per blueprint for verification
                        var rawValues = new Dictionary<string, double[]>();

                        foreach (var propName in numericProps)
                        {
                            rawValues[propName] = new double[chainLen];
                        }

                        for (int i = 0; i < chainLen; i++)
                        {
                            var bp = MakeBlueprint($"bp-{i}", i > 0 ? $"bp-{i - 1}" : null, i);

                            foreach (var propName in numericProps)
                            {
                                // Ev0 gets ev0Val, subsequent evolutions get ev0Val + delta * i
                                // This ensures non-zero Ev0 and values that change
                                double val = ev0Val + delta * i;
                                bp.Properties.setProperty(propName, val);
                                rawValues[propName][i] = val;
                            }

                            chain.Add(bp);
                        }

                        return new
                        {
                            Chain = chain,
                            Props = numericProps,
                            RawValues = rawValues,
                            ChainLen = chainLen
                        };
                    })
                );
            });

            return Prop.ForAll(testDataGen.ToArbitrary(), testData =>
            {
                var result = EvolutionChainService.BuildGraphData(testData.Chain, testData.Props);

                // All properties should be present (non-zero Ev0, values change)
                bool allPropsPresent = testData.Props.All(p => result.Series.ContainsKey(p));

                // Verify percentage formula for every data point
                bool formulaCorrect = true;
                string formulaError = "";

                foreach (var propName in testData.Props)
                {
                    if (!result.Series.ContainsKey(propName))
                        continue;

                    var points = result.Series[propName];
                    double ev0Value = testData.RawValues[propName][0];

                    foreach (var point in points)
                    {
                        // Find the raw value for this evolution level
                        int idx = point.Evolution;
                        if (idx < 0 || idx >= testData.ChainLen)
                        {
                            formulaCorrect = false;
                            formulaError = $"Unexpected evolution level {idx} for {propName}";
                            break;
                        }

                        double rawVal = testData.RawValues[propName][idx];
                        double expectedPercent = (rawVal / ev0Value) * 100.0;

                        if (Math.Abs(expectedPercent - point.Percent) > 0.0001)
                        {
                            formulaCorrect = false;
                            formulaError = $"Property '{propName}' at Ev{idx}: expected {expectedPercent:F4}%, got {point.Percent:F4}%";
                            break;
                        }
                    }

                    if (!formulaCorrect) break;
                }

                // Verify Ev0 is always 100% for all graphed properties
                bool ev0Is100 = true;
                string ev0Error = "";

                foreach (var propName in testData.Props)
                {
                    if (!result.Series.ContainsKey(propName))
                        continue;

                    var points = result.Series[propName];
                    var ev0Point = points.FirstOrDefault(p => p.Evolution == 0);

                    if (Math.Abs(ev0Point.Percent - 100.0) > 0.0001)
                    {
                        ev0Is100 = false;
                        ev0Error = $"Property '{propName}' Ev0 percent: expected 100%, got {ev0Point.Percent:F4}%";
                        break;
                    }
                }

                return allPropsPresent
                    .Label($"All numeric properties present in Series")
                    .And(formulaCorrect)
                    .Label($"Percentage formula correct: {formulaError}")
                    .And(ev0Is100)
                    .Label($"Ev0 is always 100%: {ev0Error}");
            });
        }

        #endregion

        #region Unit Tests: Edge Cases

        /// <summary>
        /// Ev0 blueprint with no base — ResolveChain returns a single-element list.
        /// **Validates: Requirements 1.3**
        /// </summary>
        [Test]
        public void ResolveChain_Ev0WithNoBase_ReturnsSingleElement()
        {
            var bp = MakeBlueprint("bp-0", null, 0);
            Bp resolver(string uuid) => null;

            var result = EvolutionChainService.ResolveChain(bp, resolver);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo("bp-0"));
        }

        /// <summary>
        /// Broken chain (missing UUID mid-chain) — terminates at last resolved blueprint.
        /// **Validates: Requirements 1.4**
        /// </summary>
        [Test]
        public void ResolveChain_BrokenChain_TerminatesAtLastResolved()
        {
            // Ev0 -> Ev1 -> Ev2, but resolver cannot find Ev0
            var ev0 = MakeBlueprint("bp-0", null, 0);
            var ev1 = MakeBlueprint("bp-1", "bp-0", 1);
            var ev2 = MakeBlueprint("bp-2", "bp-1", 2);

            var lookup = new Dictionary<string, Bp>
            {
                // bp-0 intentionally missing from lookup
                { "bp-1", ev1 },
                { "bp-2", ev2 }
            };
            Bp resolver(string uuid) =>
                lookup.TryGetValue(uuid, out var bp) ? bp : null;

            // Start from Ev2 — walks to Ev1, then tries bp-0 which is missing
            var result = EvolutionChainService.ResolveChain(ev2, resolver);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].UUID, Is.EqualTo("bp-1"));
            Assert.That(result[1].UUID, Is.EqualTo("bp-2"));
        }

        /// <summary>
        /// Circular reference detection — stops when visited UUID is re-encountered.
        /// **Validates: Requirements 1.5**
        /// </summary>
        [Test]
        public void ResolveChain_CircularReference_TerminatesWithoutInfiniteLoop()
        {
            // bp-A points to bp-B, bp-B points to bp-A
            var bpA = MakeBlueprint("bp-A", "bp-B", 0);
            var bpB = MakeBlueprint("bp-B", "bp-A", 1);

            var lookup = new Dictionary<string, Bp>
            {
                { "bp-A", bpA },
                { "bp-B", bpB }
            };
            Bp resolver(string uuid) =>
                lookup.TryGetValue(uuid, out var bp) ? bp : null;

            var result = EvolutionChainService.ResolveChain(bpA, resolver);

            Assert.That(result, Has.Count.LessThanOrEqualTo(2));
        }

        /// <summary>
        /// Zero base value exclusion — property with Ev0 value of 0 is excluded from graph data.
        /// **Validates: Requirements 3.2**
        /// </summary>
        [Test]
        public void BuildGraphData_ZeroEv0Value_ExcludesProperty()
        {
            var ev0 = MakeBlueprint("bp-0", null, 0);
            ev0.Properties.setProperty("Accuracy", 0.0);

            var ev1 = MakeBlueprint("bp-1", "bp-0", 1);
            ev1.Properties.setProperty("Accuracy", 100.0);

            var chain = new List<Bp> { ev0, ev1 };
            var props = new[] { "Accuracy" };

            var result = EvolutionChainService.BuildGraphData(chain, props);

            Assert.That(result.Series.ContainsKey("Accuracy"), Is.False);
        }

        /// <summary>
        /// Time property parsing — "1d 2h 30m 15s" → 95415 seconds.
        /// (1×86400 + 2×3600 + 30×60 + 15 = 95415)
        /// </summary>
        [Test]
        public void ParseTimeToSeconds_FullTimeString_ReturnsCorrectSeconds()
        {
            var result = EvolutionChainService.ParseTimeToSeconds("1d 2h 30m 15s");

            Assert.That(result, Is.EqualTo(95415.0));
        }

        /// <summary>
        /// All properties unchanged — NoChanges is true and Series is empty.
        /// **Validates: Requirements 2.4, 2.5**
        /// </summary>
        [Test]
        public void BuildGraphData_AllUnchanged_NoChangesTrue_SeriesEmpty()
        {
            var ev0 = MakeBlueprint("bp-0", null, 0);
            ev0.Properties.setProperty("Accuracy", 50.0);

            var ev1 = MakeBlueprint("bp-1", "bp-0", 1);
            ev1.Properties.setProperty("Accuracy", 50.0);

            var ev2 = MakeBlueprint("bp-2", "bp-1", 2);
            ev2.Properties.setProperty("Accuracy", 50.0);

            var chain = new List<Bp> { ev0, ev1, ev2 };
            var props = new[] { "Accuracy" };

            var result = EvolutionChainService.BuildGraphData(chain, props);

            Assert.That(result.NoChanges, Is.True);
            Assert.That(result.Series, Is.Empty);
        }

        #endregion
    }
}
