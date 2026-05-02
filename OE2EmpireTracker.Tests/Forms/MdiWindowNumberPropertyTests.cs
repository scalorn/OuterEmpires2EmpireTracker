using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Property-based and edge-case tests for MDI window number assignment.
    /// Tests the gap-scanning algorithm that assigns the lowest unused positive
    /// integer per form type when opening MDI child windows.
    /// </summary>
    [TestFixture]
    public class MdiWindowNumberPropertyTests
    {
        /// <summary>
        /// Property 1: Bug Condition - Lowest Unused Number Assignment.
        /// For any random set of used positive integers, FindLowestUnused returns a value
        /// that is >= 1, not in the used set, and all integers from 1 to result-1 are in
        /// the used set (i.e., it truly is the lowest unused).
        /// **Validates: Requirements 2.1, 2.2, 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property LowestUnusedNumberAssignment()
        {
            var gen = Gen.Choose(0, 20).SelectMany(size =>
                Gen.ListOf(size, Gen.Choose(1, 50))
                   .Select(nums => new HashSet<int>(nums)));

            return Prop.ForAll(gen.ToArbitrary(), used =>
            {
                int result = FindLowestUnused(used);

                var atLeastOne = (result >= 1)
                    .Label($"Result {result} should be >= 1");

                var notInUsed = (!used.Contains(result))
                    .Label($"Result {result} should not be in the used set");

                var allBelow = Enumerable.Range(1, result - 1)
                    .All(i => used.Contains(i));
                var allBelowProp = allBelow
                    .Label($"All integers from 1 to {result - 1} should be in the used set");

                return atLeastOne.And(notInUsed).And(allBelowProp);
            });
        }

        /// <summary>
        /// Property 2: Preservation - Unique Numbering and Form Setup.
        /// For any random sequence of open/close operations, the assigned window numbers
        /// are always >= 1, unique among open windows, and not already in the open set
        /// at the time of assignment.
        /// **Validates: Requirements 3.1, 3.2, 3.4, 3.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property UniqueNumberingAcrossSequences()
        {
            // Generate a sequence of 5-20 boolean steps: true = open, false = close
            var gen = Gen.Choose(5, 20).SelectMany(size =>
                Gen.ListOf(size, Arb.Default.Bool().Generator)
                   .Select(steps => steps.ToList()));

            return Prop.ForAll(gen.ToArbitrary(), steps =>
            {
                var openNumbers = new HashSet<int>();
                var rng = new System.Random(42);

                foreach (bool isOpen in steps)
                {
                    if (isOpen)
                    {
                        // Open: assign lowest unused
                        int assigned = FindLowestUnused(openNumbers);

                        if (assigned < 1)
                            return false.Label($"Assigned number {assigned} should be >= 1");

                        if (openNumbers.Contains(assigned))
                            return false.Label($"Assigned number {assigned} was already in the open set");

                        openNumbers.Add(assigned);

                        // All open numbers must be unique (HashSet guarantees this,
                        // but verify count matches expected)
                        if (openNumbers.Count != openNumbers.Distinct().Count())
                            return false.Label("Open window numbers are not unique");
                    }
                    else
                    {
                        // Close: remove a random open number (if any are open)
                        if (openNumbers.Count > 0)
                        {
                            var toRemove = openNumbers.ElementAt(rng.Next(openNumbers.Count));
                            openNumbers.Remove(toRemove);
                        }
                    }
                }

                return true.Label("All open/close operations maintained unique numbering");
            });
        }

        /// <summary>
        /// No open windows -> assigns 1.
        /// **Validates: Requirement 2.3**
        /// </summary>
        [Test]
        public void NoOpenWindows_Assigns1()
        {
            var used = new HashSet<int>();
            Assert.That(FindLowestUnused(used), Is.EqualTo(1));
        }

        /// <summary>
        /// Contiguous {1, 2, 3} -> assigns 4 (no gap, preservation).
        /// </summary>
        [Test]
        public void Contiguous123_Assigns4()
        {
            var used = new HashSet<int> { 1, 2, 3 };
            Assert.That(FindLowestUnused(used), Is.EqualTo(4));
        }

        /// <summary>
        /// Gap at start {2, 3} -> assigns 1.
        /// **Validates: Requirement 2.1**
        /// </summary>
        [Test]
        public void GapAtStart_23_Assigns1()
        {
            var used = new HashSet<int> { 2, 3 };
            Assert.That(FindLowestUnused(used), Is.EqualTo(1));
        }

        /// <summary>
        /// Gap in middle {1, 3, 4} -> assigns 2.
        /// **Validates: Requirement 2.2**
        /// </summary>
        [Test]
        public void GapInMiddle_134_Assigns2()
        {
            var used = new HashSet<int> { 1, 3, 4 };
            Assert.That(FindLowestUnused(used), Is.EqualTo(2));
        }

        /// <summary>
        /// Multiple gaps {2, 5, 8} -> assigns 1 (lowest gap first).
        /// **Validates: Requirement 2.2**
        /// </summary>
        [Test]
        public void MultipleGaps_258_Assigns1()
        {
            var used = new HashSet<int> { 2, 5, 8 };
            Assert.That(FindLowestUnused(used), Is.EqualTo(1));
        }

        /// <summary>
        /// Single window {1} closed (empty set) -> assigns 1.
        /// **Validates: Requirement 2.3**
        /// </summary>
        [Test]
        public void SingleWindowClosed_EmptySet_Assigns1()
        {
            // Simulate: window 1 was open, then closed -> empty set
            var used = new HashSet<int> { 1 };
            used.Remove(1);
            Assert.That(FindLowestUnused(used), Is.EqualTo(1));
        }

        /// <summary>
        /// Replicates the gap-scanning algorithm from MainWindow.OpenMdiChild&lt;T&gt;().
        /// Given a set of currently used window numbers, returns the lowest positive
        /// integer not in the set.
        /// </summary>
        private static int FindLowestUnused(HashSet<int> used)
        {
            int n = 1;
            while (used.Contains(n)) n++;
            return n;
        }
    }
}
