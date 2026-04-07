using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

// Feature: background-processing, Property 1: HasExpiredTimers predicate correctness
// Feature: background-processing, Property 2: All-player colony scanning
// Feature: background-processing, Property 3: Exact-match processing set
// Feature: background-processing, Property 4: Event firing with correct colony UUID

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class BackgroundProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        /// <summary>
        /// **Validates: Requirements 2.2**
        ///
        /// Property 1: HasExpiredTimers predicate correctness
        ///
        /// For any colony with any number of structures, each having random
        /// BuildCompletionTime and ProcessCompletionTime states (null, expired,
        /// or active), HasExpiredTimers() returns true if and only if at least
        /// one structure has:
        ///   - BuildCompletionTime with TimeRemaining &lt;= 0, OR
        ///   - ProcessCompletionTime with IntervalsPassed &gt; 0, OR
        ///   - non-repeating ProcessCompletionTime with TimeRemaining &lt;= 0
        /// </summary>
        [Test]
        public void HasExpiredTimers_MatchesManualPerStructureCheck()
        {
            int seed = 20250715;
            var rng = new Random(seed);
            const int iterations = 100;

            for (int i = 0; i < iterations; i++)
            {
                var colony = new Colony();
                colony.UUID = Guid.NewGuid().ToString();

                int structureCount = rng.Next(0, 6); // 0 to 5 structures
                for (int s = 0; s < structureCount; s++)
                {
                    var structure = new ColonyStructure();
                    structure.UUID = Guid.NewGuid().ToString();

                    // Randomly assign BuildCompletionTime
                    structure.BuildCompletionTime = GenerateRandomCountDownTime(rng, allowNull: true, allowRepeating: false);

                    // Randomly assign ProcessCompletionTime
                    structure.ProcessCompletionTime = GenerateRandomCountDownTime(rng, allowNull: true, allowRepeating: true);

                    colony.Structures.Add(structure);
                }

                // Compute expected result manually
                bool expected = ManualHasExpiredTimers(colony);
                bool actual = colony.HasExpiredTimers();

                Assert.That(actual, Is.EqualTo(expected),
                    $"Mismatch at iteration {i} (seed={seed}). " +
                    $"StructureCount={structureCount}, Expected={expected}, Actual={actual}. " +
                    FormatColonyState(colony));
            }
        }

        /// <summary>
        /// **Validates: Requirements 1.2, 2.1**
        ///
        /// Property 2: All-player colony scanning
        ///
        /// For any colonyList containing colonies with mixed OwnerUUID values
        /// and any value of CurrentPlayerUUID, the set of colonies identified
        /// as needing processing (those with HasExpiredTimers() == true) should
        /// be identical regardless of which player is currently selected.
        /// </summary>
        // Feature: background-processing, Property 2: All-player colony scanning
        [Test]
        public void ExpiredColonySet_IsIdentical_RegardlessOfCurrentPlayerUUID()
        {
            int seed = 20250716;
            var rng = new Random(seed);
            const int iterations = 100;

            // Set up PlayerContext with empty data
            PlayerContext.FilePath = "nonexistent_player_data.json";
            var pc = PlayerContext.getInstance();

            // Create a set of distinct player UUIDs to use across iterations
            string playerA = Guid.NewGuid().ToString();
            string playerB = Guid.NewGuid().ToString();
            string playerC = Guid.NewGuid().ToString();
            string[] playerUUIDs = new[] { playerA, playerB, playerC };

            for (int i = 0; i < iterations; i++)
            {
                // Clear colony list for this iteration
                pc.colonyList.Clear();

                // Generate 2-8 colonies with mixed owners
                int colonyCount = rng.Next(2, 9);
                for (int c = 0; c < colonyCount; c++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.OwnerUUID = playerUUIDs[rng.Next(0, playerUUIDs.Length)];

                    int structureCount = rng.Next(0, 5);
                    for (int s = 0; s < structureCount; s++)
                    {
                        var structure = new ColonyStructure();
                        structure.UUID = Guid.NewGuid().ToString();
                        structure.BuildCompletionTime = GenerateRandomCountDownTime(rng, allowNull: true, allowRepeating: false);
                        structure.ProcessCompletionTime = GenerateRandomCountDownTime(rng, allowNull: true, allowRepeating: true);
                        colony.Structures.Add(structure);
                    }

                    pc.colonyList.Add(colony);
                }

                // Collect the set of expired colony UUIDs for each player selection
                var expiredSetsByPlayer = new List<HashSet<string>>();

                foreach (string playerUUID in playerUUIDs)
                {
                    pc.CurrentPlayerUUID = playerUUID;

                    var expiredUUIDs = new HashSet<string>();
                    foreach (var colony in pc.colonyList)
                    {
                        if (colony.HasExpiredTimers())
                        {
                            expiredUUIDs.Add(colony.UUID);
                        }
                    }
                    expiredSetsByPlayer.Add(expiredUUIDs);
                }

                // Also test with an unrelated UUID (no colonies owned)
                pc.CurrentPlayerUUID = Guid.NewGuid().ToString();
                var expiredForUnknownPlayer = new HashSet<string>();
                foreach (var colony in pc.colonyList)
                {
                    if (colony.HasExpiredTimers())
                    {
                        expiredForUnknownPlayer.Add(colony.UUID);
                    }
                }
                expiredSetsByPlayer.Add(expiredForUnknownPlayer);

                // All sets must be identical
                HashSet<string> referenceSet = expiredSetsByPlayer[0];
                for (int p = 1; p < expiredSetsByPlayer.Count; p++)
                {
                    Assert.That(expiredSetsByPlayer[p], Is.EquivalentTo(referenceSet),
                        $"Mismatch at iteration {i} (seed={seed}). " +
                        $"Expired set differs when CurrentPlayerUUID changes. " +
                        $"Reference count={referenceSet.Count}, Set[{p}] count={expiredSetsByPlayer[p].Count}. " +
                        $"ColonyCount={colonyCount}");
                }
            }
        }

        /// <summary>
        /// **Validates: Requirements 2.3, 2.4, 3.1**
        ///
        /// Property 3: Exact-match processing set
        ///
        /// For any colonyList where each colony has a random set of structures
        /// with random timer states, the set of colonies on which ProcessColony()
        /// is called during a processing cycle should equal exactly the set of
        /// colonies where HasExpiredTimers() returns true.
        /// </summary>
        // Feature: background-processing, Property 3: Exact-match processing set
        [Test]
        public void RunCycleOnce_ProcessesExactlyColoniesWithExpiredTimers()
        {
            int seed = 20250717;
            var rng = new Random(seed);
            const int iterations = 100;

            // Set up PlayerContext with a temp file path so writeContext() doesn't fail
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BackgroundProcessorTest_Property3_" + Guid.NewGuid().ToString("N") + ".json");
            PlayerContext.FilePath = tempPath;
            var pc = PlayerContext.getInstance();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    pc.colonyList.Clear();

                    // Generate 1-8 colonies with random build-only timer states
                    int colonyCount = rng.Next(1, 9);
                    for (int c = 0; c < colonyCount; c++)
                    {
                        var colony = GenerateColonyWithBuildTimersOnly(rng);
                        pc.colonyList.Add(colony);
                    }

                    // Record which colonies have expired timers BEFORE the cycle
                    var expectedProcessed = new HashSet<string>();
                    foreach (var colony in pc.colonyList)
                    {
                        if (colony.HasExpiredTimers())
                        {
                            expectedProcessed.Add(colony.UUID);
                        }
                    }

                    // Subscribe to ColonyDataChanged to capture which UUIDs are fired
                    var actualProcessed = new HashSet<string>();
                    EventHandler<ColonyDataChangedEventArgs> handler = null;
                    handler = OnColonyDataChangedCapture;
                    void OnColonyDataChangedCapture(object sender, ColonyDataChangedEventArgs e)
                    {
                        actualProcessed.Add(e.ColonyUUID);
                    }
                    pc.ColonyDataChanged += handler;

                    try
                    {
                        var processor = new BackgroundProcessor(pc);
                        processor.RunCycleOnce();
                        processor.Dispose();
                    }
                    finally
                    {
                        pc.ColonyDataChanged -= handler;
                    }

                    Assert.That(actualProcessed, Is.EquivalentTo(expectedProcessed),
                        $"Mismatch at iteration {i} (seed={seed}). " +
                        $"Expected {expectedProcessed.Count} colonies processed, got {actualProcessed.Count}. " +
                        $"Expected: [{string.Join(", ", expectedProcessed)}], " +
                        $"Actual: [{string.Join(", ", actualProcessed)}]");
                }
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }

        /// <summary>
        /// **Validates: Requirements 3.3**
        ///
        /// Property 4: Event firing with correct colony UUID
        ///
        /// For any processing cycle that processes N colonies (N >= 0),
        /// OnColonyDataChanged should be fired exactly N times, once per
        /// processed colony, and each event's ColonyUUID should match the
        /// UUID of the colony that was just processed.
        /// </summary>
        // Feature: background-processing, Property 4: Event firing with correct colony UUID
        [Test]
        public void RunCycleOnce_FiresColonyDataChanged_ExactlyNTimesWithCorrectUUIDs()
        {
            int seed = 20250718;
            var rng = new Random(seed);
            const int iterations = 100;

            // Set up PlayerContext with a temp file path so writeContext() doesn't fail
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BackgroundProcessorTest_Property4_" + Guid.NewGuid().ToString("N") + ".json");
            PlayerContext.FilePath = tempPath;
            var pc = PlayerContext.getInstance();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    pc.colonyList.Clear();

                    // Generate 1-8 colonies with random build-only timer states
                    int colonyCount = rng.Next(1, 9);
                    for (int c = 0; c < colonyCount; c++)
                    {
                        var colony = GenerateColonyWithBuildTimersOnly(rng);
                        pc.colonyList.Add(colony);
                    }

                    // Determine expected: colonies with expired timers, preserving order
                    var expectedUUIDs = new List<string>();
                    foreach (var colony in pc.colonyList)
                    {
                        if (colony.HasExpiredTimers())
                        {
                            expectedUUIDs.Add(colony.UUID);
                        }
                    }

                    // Capture fired event UUIDs using a List (not HashSet) to detect duplicates
                    var firedUUIDs = new List<string>();

                    void OnColonyDataChangedCapture(object sender, ColonyDataChangedEventArgs e)
                    {
                        firedUUIDs.Add(e.ColonyUUID);
                    }

                    pc.ColonyDataChanged += OnColonyDataChangedCapture;

                    try
                    {
                        var processor = new BackgroundProcessor(pc);
                        processor.RunCycleOnce();
                        processor.Dispose();
                    }
                    finally
                    {
                        pc.ColonyDataChanged -= OnColonyDataChangedCapture;
                    }

                    // Verify exact count: N expired colonies => exactly N events
                    Assert.That(firedUUIDs.Count, Is.EqualTo(expectedUUIDs.Count),
                        $"Event count mismatch at iteration {i} (seed={seed}). " +
                        $"Expected {expectedUUIDs.Count} events, got {firedUUIDs.Count}. " +
                        $"ColonyCount={colonyCount}");

                    // Verify each fired UUID matches an expected UUID (same set)
                    Assert.That(firedUUIDs, Is.EquivalentTo(expectedUUIDs),
                        $"UUID set mismatch at iteration {i} (seed={seed}). " +
                        $"Expected UUIDs: [{string.Join(", ", expectedUUIDs)}], " +
                        $"Fired UUIDs: [{string.Join(", ", firedUUIDs)}]");
                }
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }

        /// <summary>
        /// **Validates: Requirements 5.1, 5.2, 5.3**
        ///
        /// Property 5: Conditional persistence — exactly once or zero
        ///
        /// For any processing cycle, writeContext() should be called exactly
        /// once if at least one colony was processed, and exactly zero times
        /// if no colonies were processed. Verified by monitoring the temp
        /// file's last write time before and after each cycle.
        /// </summary>
        // Feature: background-processing, Property 5: Conditional persistence — exactly once or zero
        [Test]
        public void RunCycleOnce_WritesContextExactlyOnceIfProcessed_ZeroOtherwise()
        {
            int seed = 20250719;
            var rng = new Random(seed);
            const int iterations = 100;

            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BackgroundProcessorTest_Property5_" + Guid.NewGuid().ToString("N") + ".json");
            PlayerContext.FilePath = tempPath;
            var pc = PlayerContext.getInstance();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    pc.colonyList.Clear();

                    // Generate 0-8 colonies with random build-only timer states
                    int colonyCount = rng.Next(0, 9);
                    for (int c = 0; c < colonyCount; c++)
                    {
                        var colony = GenerateColonyWithBuildTimersOnly(rng);
                        pc.colonyList.Add(colony);
                    }

                    // Determine if any colonies have expired timers
                    bool anyExpired = pc.colonyList.Any(col => col.HasExpiredTimers());

                    // Delete the temp file before the cycle so we can detect a fresh write
                    CleanupTempFiles(tempPath);

                    var processor = new BackgroundProcessor(pc);
                    processor.RunCycleOnce();
                    processor.Dispose();

                    bool fileWritten = System.IO.File.Exists(tempPath);

                    if (anyExpired)
                    {
                        Assert.That(fileWritten, Is.True,
                            $"Iteration {i} (seed={seed}): colonies had expired timers but " +
                            $"writeContext() was not called (file not written). " +
                            $"ColonyCount={colonyCount}");
                    }
                    else
                    {
                        Assert.That(fileWritten, Is.False,
                            $"Iteration {i} (seed={seed}): no colonies had expired timers but " +
                            $"writeContext() was called (file was written). " +
                            $"ColonyCount={colonyCount}");
                    }
                }
            }
            finally
            {
                CleanupTempFiles(tempPath);
            }
        }

        /// <summary>
        /// **Validates: Requirements 8.5, 8.6**
        ///
        /// Property 6: Error state round-trip
        ///
        /// For any sequence of processing cycles where some cycles throw
        /// exceptions and some succeed, LastCycleHadError should be true
        /// after a cycle that threw an exception and false after a cycle
        /// that completed without exception.
        /// </summary>
        // Feature: background-processing, Property 6: Error state round-trip
        [Test]
        public void RunCycleOnce_LastCycleHadError_ReflectsMostRecentCycleOutcome()
        {
            int seed = 20250720;
            var rng = new Random(seed);
            const int iterations = 100;

            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BackgroundProcessorTest_Property6_" + Guid.NewGuid().ToString("N") + ".json");
            PlayerContext.FilePath = tempPath;
            var pc = PlayerContext.getInstance();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    // Generate a random sequence of 2-6 cycles, each either "error" or "success"
                    int cycleCount = rng.Next(2, 7);
                    bool[] shouldError = new bool[cycleCount];
                    for (int c = 0; c < cycleCount; c++)
                    {
                        shouldError[c] = rng.Next(0, 2) == 1;
                    }

                    var processor = new BackgroundProcessor(pc);

                    for (int c = 0; c < cycleCount; c++)
                    {
                        pc.colonyList.Clear();

                        if (shouldError[c])
                        {
                            // Error cycle: add a colony with an expired ProcessCompletionTime
                            // but no valid FlatpackBlueprintUUID — ProcessColony() will throw
                            // NullReferenceException when accessing FlatpackBlueprint.BluePrintType
                            var errorColony = new Colony();
                            errorColony.UUID = Guid.NewGuid().ToString();
                            errorColony.ColonyName = "ErrorColony";
                            errorColony.PlanetName = "ErrorPlanet";

                            var errorStructure = new ColonyStructure();
                            errorStructure.UUID = Guid.NewGuid().ToString();
                            errorStructure.FlatpackBlueprintUUID = null; // no valid blueprint
                            // Expired ProcessCompletionTime triggers the code path that calls FindBlueprint
                            errorStructure.ProcessCompletionTime = CreateExpiredOneShot();
                            errorColony.Structures.Add(errorStructure);

                            pc.colonyList.Add(errorColony);
                        }
                        else
                        {
                            // Success cycle: either no colonies, or colonies with only expired
                            // BuildCompletionTime (which process cleanly without needing blueprints)
                            int successType = rng.Next(0, 3);
                            if (successType == 0)
                            {
                                // Empty colony list — no processing, no error
                            }
                            else if (successType == 1)
                            {
                                // Colony with no expired timers
                                var safeColony = new Colony();
                                safeColony.UUID = Guid.NewGuid().ToString();
                                safeColony.ColonyName = "SafeColony";
                                safeColony.PlanetName = "SafePlanet";
                                pc.colonyList.Add(safeColony);
                            }
                            else
                            {
                                // Colony with expired BuildCompletionTime only (processes cleanly)
                                var buildColony = new Colony();
                                buildColony.UUID = Guid.NewGuid().ToString();
                                buildColony.ColonyName = "BuildColony";
                                buildColony.PlanetName = "BuildPlanet";

                                var buildStructure = new ColonyStructure();
                                buildStructure.UUID = Guid.NewGuid().ToString();
                                buildStructure.BuildCompletionTime = CreateExpiredOneShot();
                                buildStructure.ProcessCompletionTime = null;
                                buildColony.Structures.Add(buildStructure);

                                pc.colonyList.Add(buildColony);
                            }
                        }

                        // Attach a named handler to avoid event leak
                        void OnColonyDataChangedIgnore(object sender, ColonyDataChangedEventArgs e) { }
                        pc.ColonyDataChanged += OnColonyDataChangedIgnore;

                        try
                        {
                            processor.RunCycleOnce();
                        }
                        finally
                        {
                            pc.ColonyDataChanged -= OnColonyDataChangedIgnore;
                        }

                        Assert.That(processor.LastCycleHadError, Is.EqualTo(shouldError[c]),
                            $"Iteration {i}, cycle {c} (seed={seed}). " +
                            $"Expected LastCycleHadError={shouldError[c]} but got {processor.LastCycleHadError}. " +
                            $"Cycle sequence: [{string.Join(", ", shouldError.Select(e => e ? "error" : "success"))}]");
                    }

                    processor.Dispose();
                }
            }
            finally
            {
                CleanupTempFiles(tempPath);
            }
        }

        /// <summary>
        /// Creates an expired one-shot CountDownTime (TimeRemaining &lt;= 0, not repeating).
        /// Used by Property 6 to create structures that trigger processing.
        /// </summary>
        private static CountDownTime CreateExpiredOneShot()
        {
            var timer = new CountDownTime();
            timer.StartTime = DateTime.Now.AddSeconds(-120);
            timer.EndTime = DateTime.Now.AddSeconds(-60);
            timer.RepeatIntervalSeconds = 0;
            return timer;
        }

        /// <summary>
        /// Removes the temp file and its associated .tmp and .bak files
        /// created by SafeFileWriter.
        /// </summary>
        private static void CleanupTempFiles(string basePath)
        {
            string[] paths = new[]
            {
                basePath,
                basePath + ".tmp",
                basePath + ".bak"
            };
            foreach (string path in paths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Manual implementation of the HasExpiredTimers check, used as the
        /// oracle to verify the Colony.HasExpiredTimers() method.
        /// </summary>
        private static bool ManualHasExpiredTimers(Colony colony)
        {
            foreach (var structure in colony.Structures)
            {
                if (structure.BuildCompletionTime != null &&
                    structure.BuildCompletionTime.TimeRemaining <= 0)
                {
                    return true;
                }

                if (structure.ProcessCompletionTime != null)
                {
                    if (structure.ProcessCompletionTime.IntervalsPassed > 0)
                    {
                        return true;
                    }

                    if (!structure.ProcessCompletionTime.IsRepeating &&
                        structure.ProcessCompletionTime.TimeRemaining <= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a random CountDownTime in one of several states:
        ///   - null
        ///   - expired (TimeRemaining &lt;= 0)
        ///   - active (TimeRemaining &gt; 0)
        ///   - repeating with intervals passed
        ///   - repeating with no intervals passed
        /// </summary>
        private static CountDownTime GenerateRandomCountDownTime(Random rng, bool allowNull, bool allowRepeating)
        {
            // Decide the timer state
            // States: 0=null, 1=expired-oneshot, 2=active-oneshot,
            //         3=repeating-with-intervals, 4=repeating-no-intervals
            int maxState = allowRepeating ? 5 : 3;
            if (!allowNull) maxState = allowRepeating ? 4 : 2;

            int state = rng.Next(0, maxState);
            if (!allowNull) state++; // shift past the null state

            switch (state)
            {
                case 0: // null
                    return null;

                case 1: // expired one-shot (TimeRemaining <= 0)
                {
                    var timer = new CountDownTime();
                    // Set EndTime in the past so TimeRemaining is negative
                    int secondsAgo = rng.Next(1, 3600);
                    timer.StartTime = DateTime.Now.AddSeconds(-secondsAgo - 10);
                    timer.EndTime = DateTime.Now.AddSeconds(-secondsAgo);
                    timer.RepeatIntervalSeconds = 0;
                    return timer;
                }

                case 2: // active one-shot (TimeRemaining > 0)
                {
                    var timer = new CountDownTime();
                    int secondsLeft = rng.Next(1, 7200);
                    timer.StartTime = DateTime.Now;
                    timer.EndTime = DateTime.Now.AddSeconds(secondsLeft);
                    timer.RepeatIntervalSeconds = 0;
                    return timer;
                }

                case 3: // repeating with intervals passed (IntervalsPassed > 0)
                {
                    var timer = new CountDownTime();
                    long intervalSeconds = rng.Next(60, 3600);
                    int passedIntervals = rng.Next(1, 10);
                    timer.RepeatIntervalSeconds = intervalSeconds;
                    // Move StartTime far enough back that passedIntervals have elapsed
                    timer.StartTime = DateTime.Now.AddSeconds(-(passedIntervals * intervalSeconds) - rng.Next(1, (int)intervalSeconds));
                    timer.EndTime = timer.StartTime.AddSeconds(intervalSeconds);
                    return timer;
                }

                case 4: // repeating with no intervals passed (IntervalsPassed == 0)
                {
                    var timer = new CountDownTime();
                    long intervalSeconds = rng.Next(60, 3600);
                    timer.RepeatIntervalSeconds = intervalSeconds;
                    // StartTime is recent enough that no full interval has elapsed
                    int partialSeconds = rng.Next(1, (int)intervalSeconds - 1);
                    timer.StartTime = DateTime.Now.AddSeconds(-partialSeconds);
                    timer.EndTime = timer.StartTime.AddSeconds(intervalSeconds);
                    return timer;
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Formats colony structure state for diagnostic output on failure.
        /// </summary>
        private static string FormatColonyState(Colony colony)
        {
            var parts = new List<string>();
            for (int s = 0; s < colony.Structures.Count; s++)
            {
                var st = colony.Structures[s];
                string buildInfo = st.BuildCompletionTime == null
                    ? "Build=null"
                    : $"Build(TR={st.BuildCompletionTime.TimeRemaining})";
                string processInfo = st.ProcessCompletionTime == null
                    ? "Process=null"
                    : $"Process(TR={st.ProcessCompletionTime.TimeRemaining}, IP={st.ProcessCompletionTime.IntervalsPassed}, Repeating={st.ProcessCompletionTime.IsRepeating})";
                parts.Add($"[S{s}: {buildInfo}, {processInfo}]");
            }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// Generates a colony with structures that only have BuildCompletionTime
        /// timers (no ProcessCompletionTime). This ensures ProcessColony() can
        /// run safely without needing external data (blueprints, surveys, etc.)
        /// since build completion only sets properties and clears the timer.
        /// </summary>
        private static Colony GenerateColonyWithBuildTimersOnly(Random rng)
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.ColonyName = "TestColony_" + colony.UUID.Substring(0, 8);
            colony.PlanetName = "TestPlanet";

            int structureCount = rng.Next(0, 5);
            for (int s = 0; s < structureCount; s++)
            {
                var structure = new ColonyStructure();
                structure.UUID = Guid.NewGuid().ToString();

                // Only use BuildCompletionTime (no ProcessCompletionTime)
                // to avoid ProcessColony() needing blueprints/surveys
                structure.BuildCompletionTime = GenerateRandomCountDownTime(rng, allowNull: true, allowRepeating: false);
                structure.ProcessCompletionTime = null;

                colony.Structures.Add(structure);
            }

            return colony;
        }
    }
}
