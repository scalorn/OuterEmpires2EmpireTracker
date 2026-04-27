using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for SerializationSorter generic sort helpers.
    /// Uses seeded random generation to verify sorting invariants across many inputs.
    /// </summary>
    [TestFixture]
    [Category("Feature: json-deterministic-order")]
    public class SerializationSorterPropertyTests
    {
        private const int Iterations = 100;

        /// <summary>
        /// Simple wrapper class with a string Key property for testing SortByString.
        /// </summary>
        private class StringKeyEntity
        {
            public string Key { get; set; }
        }

        /// <summary>
        /// Generates a random string key that may be null, empty, or a random ASCII string.
        /// Null and empty keys are generated ~10% of the time each to ensure edge case coverage.
        /// </summary>
        private static string GenerateRandomStringKey(Random rng)
        {
            int roll = rng.Next(10);
            if (roll == 0) return null;
            if (roll == 1) return string.Empty;
            int len = rng.Next(1, 20);
            var chars = new char[len];
            for (int c = 0; c < len; c++)
                chars[c] = (char)rng.Next(32, 127); // printable ASCII
            return new string(chars);
        }

        /// <summary>
        /// Generates an array of 0-50 StringKeyEntity elements with random string keys.
        /// </summary>
        private static StringKeyEntity[] GenerateStringKeyArray(Random rng)
        {
            int count = rng.Next(0, 51);
            var arr = new StringKeyEntity[count];
            for (int i = 0; i < count; i++)
                arr[i] = new StringKeyEntity { Key = GenerateRandomStringKey(rng) };
            return arr;
        }

        /// <summary>
        /// **Validates: Requirements 1.1, 2.1, 2.2, 2.4, 2.5, 2.6, 2.7, 3.1, 3.2, 5.2, 5.3, 6.1, 8.1, 8.2**
        ///
        /// Property 1: String key sorting produces ascending ordinal order.
        /// For any array of entities with string keys, SortByString returns a new array
        /// where each element's key is less than or equal to the next element's key under StringComparer.Ordinal,
        /// and null/empty keys appear before non-empty keys.
        /// </summary>
        [Test]
        [Category("Property 1: String key sorting")]
        public void StringKeySort_ProducesAscendingOrdinalOrder()
        {
            var rng = new Random(42);
            for (int iter = 0; iter < Iterations; iter++)
            {
                var input = GenerateStringKeyArray(rng);
                var sorted = SerializationSorter.SortByString(input, e => e.Key);

                // Assert length preserved
                Assert.That(sorted.Length, Is.EqualTo(input.Length),
                    string.Format("Iteration {0}: sorted length should match input length", iter));

                // Assert ascending ordinal order (using coalesced keys, matching implementation)
                for (int i = 0; i < sorted.Length - 1; i++)
                {
                    string currentKey = sorted[i].Key ?? string.Empty;
                    string nextKey = sorted[i + 1].Key ?? string.Empty;
                    int cmp = StringComparer.Ordinal.Compare(currentKey, nextKey);
                    Assert.That(cmp, Is.LessThanOrEqualTo(0),
                        string.Format("Iteration {0}: element [{1}] key '{2}' should be <= element [{3}] key '{4}' (ordinal)", iter, i, sorted[i].Key ?? "(null)", i + 1, sorted[i + 1].Key ?? "(null)"));
                }

                // Assert null/empty keys appear before non-empty keys
                bool seenNonEmpty = false;
                for (int i = 0; i < sorted.Length; i++)
                {
                    string key = sorted[i].Key;
                    bool isNullOrEmpty = string.IsNullOrEmpty(key);
                    if (!isNullOrEmpty)
                    {
                        seenNonEmpty = true;
                    }
                    else if (seenNonEmpty)
                    {
                        Assert.Fail(string.Format(
                            "Iteration {0}: null/empty key at index {1} appears after non-empty key",
                            iter, i));
                    }
                }
            }
        }

        /// <summary>
        /// Simple wrapper class with an int Key property for testing SortByInt.
        /// </summary>
        private class IntKeyEntity
        {
            public int Key { get; set; }
        }

        /// <summary>
        /// Generates an array of 0-50 IntKeyEntity elements with random int keys.
        /// </summary>
        private static IntKeyEntity[] GenerateIntKeyArray(Random rng)
        {
            int count = rng.Next(0, 51);
            var arr = new IntKeyEntity[count];
            for (int i = 0; i < count; i++)
                arr[i] = new IntKeyEntity { Key = rng.Next(int.MinValue, int.MaxValue) };
            return arr;
        }

        /// <summary>
        /// **Validates: Requirements 2.3, 4.1, 5.1, 9.1**
        ///
        /// Property 2: Numeric key sorting produces ascending numeric order.
        /// For any array of entities with int keys, SortByInt returns a new array
        /// where each element's key is less than or equal to the next element's key numerically.
        /// </summary>
        [Test]
        [Category("Property 2: Numeric key sorting")]
        public void IntKeySort_ProducesAscendingNumericOrder()
        {
            var rng = new Random(42);
            for (int iter = 0; iter < Iterations; iter++)
            {
                var input = GenerateIntKeyArray(rng);
                var sorted = SerializationSorter.SortByInt(input, e => e.Key);

                // Assert length preserved
                Assert.That(sorted.Length, Is.EqualTo(input.Length),
                    string.Format("Iteration {0}: sorted length should match input length", iter));

                // Assert ascending numeric order
                for (int i = 0; i < sorted.Length - 1; i++)
                {
                    Assert.That(sorted[i].Key, Is.LessThanOrEqualTo(sorted[i + 1].Key),
                        string.Format("Iteration {0}: element [{1}] key {2} should be <= element [{3}] key {4}", iter, i, sorted[i].Key, i + 1, sorted[i + 1].Key));
                }
            }
        }

        /// <summary>
        /// Wrapper class with a string primary key and int secondary key for testing SortByStringThenInt.
        /// </summary>
        private class StringIntKeyEntity
        {
            public string Key1 { get; set; }
            public int Key2 { get; set; }
        }

        /// <summary>
        /// Wrapper class with two string keys for testing SortByStringThenString.
        /// </summary>
        private class StringStringKeyEntity
        {
            public string Key1 { get; set; }
            public string Key2 { get; set; }
        }

        /// <summary>
        /// Generates an array of 0-50 StringIntKeyEntity elements with random composite keys.
        /// </summary>
        private static StringIntKeyEntity[] GenerateStringIntKeyArray(Random rng)
        {
            int count = rng.Next(0, 51);
            var arr = new StringIntKeyEntity[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new StringIntKeyEntity
                {
                    Key1 = GenerateRandomStringKey(rng),
                    Key2 = rng.Next(int.MinValue, int.MaxValue)
                };
            }

            return arr;
        }

        /// <summary>
        /// Generates an array of 0-50 StringStringKeyEntity elements with random composite keys.
        /// </summary>
        private static StringStringKeyEntity[] GenerateStringStringKeyArray(Random rng)
        {
            int count = rng.Next(0, 51);
            var arr = new StringStringKeyEntity[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new StringStringKeyEntity
                {
                    Key1 = GenerateRandomStringKey(rng),
                    Key2 = GenerateRandomStringKey(rng)
                };
            }

            return arr;
        }

        /// <summary>
        /// **Validates: Requirements 7.1, 7.2, 7.3, 10.1**
        ///
        /// Property 3: Composite key sorting produces ascending composite order.
        /// For any array of entities with composite keys (string+int or string+string),
        /// the sort helper returns a new array where elements are ordered by the first key
        /// ascending, then by the second key ascending within ties.
        /// </summary>
        [Test]
        [Category("Property 3: Composite key sorting")]
        public void CompositeKeySort_ProducesAscendingCompositeOrder()
        {
            var rng = new Random(42);
            for (int iter = 0; iter < Iterations; iter++)
            {
                // --- Test SortByStringThenInt ---
                var siInput = GenerateStringIntKeyArray(rng);
                var siSorted = SerializationSorter.SortByStringThenInt(siInput, e => e.Key1, e => e.Key2);

                // Assert length preserved
                Assert.That(siSorted.Length, Is.EqualTo(siInput.Length),
                    string.Format("StringThenInt iteration {0}: sorted length should match input length", iter));

                // Assert composite ascending order: primary string key, then secondary int key
                for (int i = 0; i < siSorted.Length - 1; i++)
                {
                    string curKey1 = siSorted[i].Key1 ?? string.Empty;
                    string nxtKey1 = siSorted[i + 1].Key1 ?? string.Empty;
                    int cmpPrimary = StringComparer.Ordinal.Compare(curKey1, nxtKey1);
                    Assert.That(cmpPrimary, Is.LessThanOrEqualTo(0),
                        string.Format("StringThenInt iteration {0}: element [{1}] key1 '{2}' should be <= element [{3}] key1 '{4}' (ordinal)", iter, i, siSorted[i].Key1 ?? "(null)", i + 1, siSorted[i + 1].Key1 ?? "(null)"));

                    if (cmpPrimary == 0)
                    {
                        Assert.That(siSorted[i].Key2, Is.LessThanOrEqualTo(siSorted[i + 1].Key2),
                            string.Format("StringThenInt iteration {0}: tied key1 '{1}', element [{2}] key2 {3} should be <= element [{4}] key2 {5}", iter, siSorted[i].Key1 ?? "(null)", i, siSorted[i].Key2, i + 1, siSorted[i + 1].Key2));
                    }
                }

                // --- Test SortByStringThenString ---
                var ssInput = GenerateStringStringKeyArray(rng);
                var ssSorted = SerializationSorter.SortByStringThenString(ssInput, e => e.Key1, e => e.Key2);

                // Assert length preserved
                Assert.That(ssSorted.Length, Is.EqualTo(ssInput.Length),
                    string.Format("StringThenString iteration {0}: sorted length should match input length", iter));

                // Assert composite ascending order: primary string key, then secondary string key
                for (int i = 0; i < ssSorted.Length - 1; i++)
                {
                    string curKey1 = ssSorted[i].Key1 ?? string.Empty;
                    string nxtKey1 = ssSorted[i + 1].Key1 ?? string.Empty;
                    int cmpPrimary = StringComparer.Ordinal.Compare(curKey1, nxtKey1);
                    Assert.That(cmpPrimary, Is.LessThanOrEqualTo(0),
                        string.Format("StringThenString iteration {0}: element [{1}] key1 '{2}' should be <= element [{3}] key1 '{4}' (ordinal)", iter, i, ssSorted[i].Key1 ?? "(null)", i + 1, ssSorted[i + 1].Key1 ?? "(null)"));

                    if (cmpPrimary == 0)
                    {
                        string curKey2 = ssSorted[i].Key2 ?? string.Empty;
                        string nxtKey2 = ssSorted[i + 1].Key2 ?? string.Empty;
                        int cmpSecondary = StringComparer.Ordinal.Compare(curKey2, nxtKey2);
                        Assert.That(cmpSecondary, Is.LessThanOrEqualTo(0),
                            string.Format("StringThenString iteration {0}: tied key1 '{1}', element [{2}] key2 '{3}' should be <= element [{4}] key2 '{5}' (ordinal)", iter, ssSorted[i].Key1 ?? "(null)", i, ssSorted[i].Key2 ?? "(null)", i + 1, ssSorted[i + 1].Key2 ?? "(null)"));
                    }
                }
            }
        }

        /// <summary>
        /// **Validates: Requirements 13.1**
        ///
        /// Property 5: Sorting produces a new array without modifying the source.
        /// For any input array, SortByString returns a new array instance,
        /// and the original array's element order remains unchanged after the sort completes.
        /// </summary>
        [Test]
        [Category("Property 5: Source array unchanged")]
        public void SortByString_DoesNotModifySourceArray()
        {
            var rng = new Random(42);
            for (int iter = 0; iter < Iterations; iter++)
            {
                var input = GenerateStringKeyArray(rng);

                // Snapshot original element order (by reference)
                var originalOrder = new StringKeyEntity[input.Length];
                Array.Copy(input, originalOrder, input.Length);

                var sorted = SerializationSorter.SortByString(input, e => e.Key);

                // Assert returned array is a different instance (not same reference)
                if (input.Length > 0)
                {
                    Assert.That(sorted, Is.Not.SameAs(input),
                        string.Format("Iteration {0}: sorted array should be a different instance from input", iter));
                }

                // Assert original array element order is unchanged
                Assert.That(input.Length, Is.EqualTo(originalOrder.Length),
                    string.Format("Iteration {0}: input length should be unchanged", iter));

                for (int i = 0; i < input.Length; i++)
                {
                    Assert.That(input[i], Is.SameAs(originalOrder[i]),
                        string.Format("Iteration {0}: input[{1}] reference should be unchanged after sort", iter, i));
                }
            }
        }

        /// <summary>
        /// Generates a Dictionary with 0-20 random string key-value pairs.
        /// Keys are random printable ASCII strings of length 1-15.
        /// </summary>
        private static Dictionary<string, string> GenerateRandomStringDictionary(Random rng)
        {
            int count = rng.Next(0, 21);
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < count; i++)
            {
                string key = GenerateNonNullRandomKey(rng);
                if (!dict.ContainsKey(key))
                    dict[key] = GenerateNonNullRandomKey(rng);
            }

            return dict;
        }

        /// <summary>
        /// Generates a non-null random string key of length 1-15 (printable ASCII).
        /// Dictionary keys cannot be null, so this variant always returns a non-null string.
        /// </summary>
        private static string GenerateNonNullRandomKey(Random rng)
        {
            int len = rng.Next(1, 16);
            var chars = new char[len];
            for (int c = 0; c < len; c++)
                chars[c] = (char)rng.Next(32, 127); // printable ASCII
            return new string(chars);
        }

        /// <summary>
        /// **Validates: Requirements 11.1, 11.2, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6**
        ///
        /// Property 4: Dictionary serialization produces sorted key order.
        /// For any Dictionary with string keys, serializing it with SortedDictionaryContractResolver
        /// produces JSON where property names appear in ascending ordinal string order.
        /// </summary>
        [Test]
        [Category("Property 4: Dictionary key ordering")]
        public void DictionarySerialization_ProducesAscendingKeyOrder()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SortedDictionaryContractResolver()
            };

            var rng = new Random(42);
            for (int iter = 0; iter < Iterations; iter++)
            {
                var dict = GenerateRandomStringDictionary(rng);
                var json = JsonConvert.SerializeObject(dict, settings);
                var jobj = JObject.Parse(json);
                var keys = jobj.Properties().Select(p => p.Name).ToList();

                // Assert all dictionary keys are present in the JSON
                Assert.That(keys.Count, Is.EqualTo(dict.Count),
                    string.Format("Iteration {0}: JSON property count should match dictionary count", iter));

                // Assert keys are in ascending ordinal string order
                for (int i = 0; i < keys.Count - 1; i++)
                {
                    int cmp = StringComparer.Ordinal.Compare(keys[i], keys[i + 1]);
                    Assert.That(cmp, Is.LessThan(0),
                        string.Format("Iteration {0}: JSON key '{1}' at index {2} should be < key '{3}' at index {4} (ordinal)", iter, keys[i], i, keys[i + 1], i + 1));
                }
            }
        }
    }
}
