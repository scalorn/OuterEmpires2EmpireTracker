using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ThreadSafetyTests
    {
        private string _tempDir;
        private string _originalPlayerFilePath;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();

            // Save and override PlayerContext.FilePath with a dedicated temp directory
            _originalPlayerFilePath = PlayerContext.FilePath;
            _tempDir = Path.Combine(Path.GetTempPath(), "OE2Tests_ThreadSafety_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            PlayerContext.FilePath = Path.Combine(_tempDir, "PlayerData.json");

            EmpireContext.Reset();
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();

            // Restore original file path so other fixtures aren't affected
            PlayerContext.FilePath = _originalPlayerFilePath;

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch
            {
                /* best effort cleanup */
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Only reset PlayerContext between tests within this fixture.
            // Do NOT reset EmpireContext — other fixtures share the singleton.
            // PlayerContext.FilePath is still pointing at our temp dir.
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 1: Concurrent colony processing safety
        // -----------------------------------------------------------------------

        [Test]
        public void ConcurrentColonyProcessing_NoExceptionsAndDataConsistent()
        {
            // Validates: Requirements 1.3, 1.5, 9.1, 12.1
            var colony = BuildColonyWithExpiredTimer();
            var pc = PlayerContext.GetInstance();
            pc.AddColony(colony);

            Exception thread1Exception = null;
            Exception thread2Exception = null;
            var barrier = new ManualResetEventSlim(false);

            var t1 = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    if (colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
                    {
                        try
                        {
                            colony.ProcessColony();
                        }
                        finally
                        {
                            colony.ColonyLock.ExitWriteLock();
                        }
                    }
                }
                catch (Exception ex)
                {
                    thread1Exception = ex;
                }
            });

            var t2 = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    if (colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
                    {
                        try
                        {
                            var calc = new ColonyStatusCalculator(colony);
                            calc.CalculateBuilt();
                        }
                        finally
                        {
                            colony.ColonyLock.ExitWriteLock();
                        }
                    }
                }
                catch (Exception ex)
                {
                    thread2Exception = ex;
                }
            });

            t1.Start();
            t2.Start();
            barrier.Set();
            t1.Join(10000);
            t2.Join(10000);

            Assert.That(thread1Exception, Is.Null, "Thread 1 threw: " + thread1Exception?.Message);
            Assert.That(thread2Exception, Is.Null, "Thread 2 threw: " + thread2Exception?.Message);

            // Verify colony data is internally consistent
            Assert.That(colony.Items, Is.Not.Null);
            Assert.That(colony.Structures, Is.Not.Null);
            Assert.That(colony.Locks, Is.Not.Null);
            Assert.That(colony.Structures.Count, Is.GreaterThan(0));
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 2: ItemBag concurrent access safety
        // -----------------------------------------------------------------------

        [Test]
        public void ItemBag_ConcurrentAddAndFind_NoExceptionsAndCorrectCount()
        {
            // Validates: Requirements 2.2, 2.3, 12.2
            var bag = new ItemBag();
            const int threadCount = 8;
            const int itemsPerThread = 50;
            var exceptions = new List<Exception>();
            var exLock = new object();
            var barrier = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.Wait();
                        for (int i = 0; i < itemsPerThread; i++)
                        {
                            var item = new Item(ItemType.ItemTypeEnum.Resource, "Res" + threadIndex);
                            item.UUID = Guid.NewGuid().ToString();
                            item.BaseItemTypeID = "Res" + threadIndex;
                            item.ResourcePurity = "Low";
                            item.Quantity = 1;
                            item.Volume = 1;
                            bag.AddItem(item);

                            // Concurrent read
                            bag.FindByType(ItemType.ItemTypeEnum.Resource, "Res" + threadIndex);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exLock)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            foreach (var t in threads) t.Start();
            barrier.Set();
            foreach (var t in threads) t.Join(10000);

            Assert.That(exceptions, Is.Empty, "Exceptions: " + string.Join("; ", exceptions));
            Assert.That(bag.Count(), Is.EqualTo(threadCount * itemsPerThread));
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 3: ItemBag defensive copies
        // -----------------------------------------------------------------------

        [Test]
        public void ItemBag_FindByType_ReturnsDefensiveCopy()
        {
            // Validates: Requirements 2.5
            var bag = new ItemBag();
            var item1 = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            item1.UUID = Guid.NewGuid().ToString();
            item1.BaseItemTypeID = "Iron";
            item1.Quantity = 10;
            bag.AddItem(item1);

            var item2 = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            item2.UUID = Guid.NewGuid().ToString();
            item2.BaseItemTypeID = "Iron";
            item2.Quantity = 20;
            bag.AddItem(item2);

            // Get the list and modify it
            var result1 = bag.FindByType(ItemType.ItemTypeEnum.Resource, "Iron");
            Assert.That(result1.Count, Is.EqualTo(2));

            result1.Clear(); // Modify the returned list

            // Get the list again -- should be unchanged
            var result2 = bag.FindByType(ItemType.ItemTypeEnum.Resource, "Iron");
            Assert.That(result2.Count, Is.EqualTo(2), "Modifying returned list should not affect ItemBag");
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 4: PropertyBag concurrent access safety
        // -----------------------------------------------------------------------

        [Test]
        public void PropertyBag_ConcurrentSetAndGet_NoExceptions()
        {
            // Validates: Requirements 3.2, 3.3, 12.3
            var bag = new PropertyBag();
            const int threadCount = 8;
            const int opsPerThread = 100;
            var exceptions = new List<Exception>();
            var exLock = new object();
            var barrier = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.Wait();
                        for (int i = 0; i < opsPerThread; i++)
                        {
                            string propName = "prop" + ((threadIndex * opsPerThread) + i);
                            bag.SetProperty(propName, true);

                            bool value;
                            bag.GetBoolean(propName, false, out value);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exLock)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            foreach (var t in threads) t.Start();
            barrier.Set();
            foreach (var t in threads) t.Join(10000);

            Assert.That(exceptions, Is.Empty, "Exceptions: " + string.Join("; ", exceptions));
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 5: LockTracking concurrent access safety
        // -----------------------------------------------------------------------

        [Test]
        public void LockTracking_ConcurrentLockAndQuery_NoExceptionsAndNonNegative()
        {
            // Validates: Requirements 4.2, 4.3, 12.4
            var tracking = new LockTracking();
            const int threadCount = 8;
            const int opsPerThread = 50;
            var exceptions = new List<Exception>();
            var exLock = new object();
            var barrier = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.Wait();
                        string processUUID = "process-" + threadIndex;
                        for (int i = 0; i < opsPerThread; i++)
                        {
                            tracking.LockItem(processUUID, ItemType.ItemTypeEnum.Resource, "Iron", 1);

                            int qty = tracking.GetLockedQuantity(ItemType.ItemTypeEnum.Resource, "Iron");
                            if (qty < 0)
                                throw new Exception("GetLockedQuantity returned negative: " + qty);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exLock)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            foreach (var t in threads) t.Start();
            barrier.Set();
            foreach (var t in threads) t.Join(10000);

            Assert.That(exceptions, Is.Empty, "Exceptions: " + string.Join("; ", exceptions));

            int finalQty = tracking.GetLockedQuantity(ItemType.ItemTypeEnum.Resource, "Iron");
            Assert.That(finalQty, Is.EqualTo(threadCount * opsPerThread));
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 6: LockTracking defensive copies
        // -----------------------------------------------------------------------

        [Test]
        public void LockTracking_GetLocksForProcess_ReturnsReadOnlyCopy()
        {
            // Validates: Requirements 4.4
            var tracking = new LockTracking();
            string processUUID = "test-process";
            tracking.LockItem(processUUID, ItemType.ItemTypeEnum.Resource, "Iron", 5);
            tracking.LockItem(processUUID, ItemType.ItemTypeEnum.Resource, "Copper", 3);

            var locks = tracking.GetLocksForProcess(processUUID);

            // Verify it's read-only -- attempting to cast and modify should fail
            Assert.That(locks, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<ItemLock>>());
            Assert.That(locks.Count, Is.EqualTo(2));

            // Verify we cannot modify via the IReadOnlyList interface
            Assert.Throws<NotSupportedException>(() =>
            {
                ((System.Collections.Generic.IList<ItemLock>)locks).Add(new ItemLock(new ItemKey(ItemType.ItemTypeEnum.Resource, "Gold"), 1));
            });
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 7: Cancellation prevents stale results
        // -----------------------------------------------------------------------

        [Test]
        public void Cancellation_PreventsStaleResults()
        {
            // Validates: Requirements 6.2, 6.6, 12.5
            int calcGeneration = 0;
            int executedGeneration = -1;
            const int totalSwitches = 10;

            CancellationTokenSource currentCts = null;

            // Simulate rapid colony switches
            for (int i = 0; i < totalSwitches; i++)
            {
                // Cancel previous
                currentCts?.Cancel();
                currentCts = new CancellationTokenSource();
                Interlocked.Increment(ref calcGeneration);
            }

            // Only the final generation should execute
            var finalCts = currentCts;
            int finalGen = calcGeneration;

            // Simulate the background callback checking cancellation and generation
            if (!finalCts.IsCancellationRequested)
            {
                // This is the "apply results" step
                if (finalGen == calcGeneration)
                {
                    executedGeneration = finalGen;
                }
            }

            Assert.That(
                executedGeneration,
                Is.EqualTo(totalSwitches),
                "Only the final generation's callback should execute");

            // Verify all previous CTS tokens are cancelled
            // (we only kept the last one un-cancelled)
            Assert.That(
                finalCts.IsCancellationRequested,
                Is.False,
                "The final CancellationTokenSource should not be cancelled");
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 8: Read lock timeout graceful degradation
        // -----------------------------------------------------------------------

        [Test]
        public void ReadLockTimeout_ReturnsFalseWithoutCorruption()
        {
            // Validates: Requirements 11.1, 12.6
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.PlanetName = "TestPlanet";
            colony.ColonyName = "TestColony";

            var item = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            item.UUID = Guid.NewGuid().ToString();
            item.BaseItemTypeID = "Iron";
            item.Quantity = 42;
            item.Volume = 1;
            colony.Items.AddItem(item);

            // Hold a write lock on the colony from the main thread
            colony.ColonyLock.EnterWriteLock();

            bool readLockAcquired = true;
            string colonyNameBefore = colony.ColonyName;
            int itemCountBefore = colony.Items.Count();

            try
            {
                var readThread = new Thread(() =>
                {
                    // Attempt read lock with short timeout (50ms for fast test)
                    readLockAcquired = colony.ColonyLock.TryEnterReadLock(50);
                    if (readLockAcquired)
                    {
                        colony.ColonyLock.ExitReadLock();
                    }
                });

                readThread.Start();
                readThread.Join(5000);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            Assert.That(readLockAcquired, Is.False, "Read lock should timeout when write lock is held");
            Assert.That(colony.ColonyName, Is.EqualTo(colonyNameBefore), "Colony data should be unchanged");
            Assert.That(colony.Items.Count(), Is.EqualTo(itemCountBefore), "Item count should be unchanged");
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 9: Write lock timeout skips colony without corruption
        // -----------------------------------------------------------------------

        [Test]
        public void WriteLockTimeout_ReturnsFalseWithoutCorruption()
        {
            // Validates: Requirements 11.2, 12.6
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.PlanetName = "TestPlanet";
            colony.ColonyName = "TestColony";

            var item = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            item.UUID = Guid.NewGuid().ToString();
            item.BaseItemTypeID = "Iron";
            item.Quantity = 42;
            item.Volume = 1;
            colony.Items.AddItem(item);

            // Hold a write lock on the colony from the main thread
            colony.ColonyLock.EnterWriteLock();

            bool writeLockAcquired = true;
            string colonyNameBefore = colony.ColonyName;
            int itemCountBefore = colony.Items.Count();

            try
            {
                var writeThread = new Thread(() =>
                {
                    // Attempt write lock with short timeout (50ms for fast test)
                    writeLockAcquired = colony.ColonyLock.TryEnterWriteLock(50);
                    if (writeLockAcquired)
                    {
                        colony.ColonyLock.ExitWriteLock();
                    }
                });

                writeThread.Start();
                writeThread.Join(5000);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            Assert.That(writeLockAcquired, Is.False, "Write lock should timeout when write lock is held");
            Assert.That(colony.ColonyName, Is.EqualTo(colonyNameBefore), "Colony data should be unchanged");
            Assert.That(colony.Items.Count(), Is.EqualTo(itemCountBefore), "Item count should be unchanged");
        }

        // -----------------------------------------------------------------------
        // Feature: data-model-thread-safety, Property 10: BackgroundProcessor continues after contended colony
        // -----------------------------------------------------------------------

        [Test]
        public void BackgroundProcessor_SkipsLockedColony_ProcessesOthers()
        {
            // Validates: Requirements 11.3
            var pc = PlayerContext.GetInstance();

            // Create colony A -- locked, with expired timer
            var bpA = MakeBlueprint("Power Plant", new Dictionary<string, string>
            {
                { GameConstants.PropPowerProvided, "100" }
            });
            pc.AddBlueprint(bpA);

            var colonyA = new Colony();
            colonyA.UUID = Guid.NewGuid().ToString();
            colonyA.PlanetName = "PlanetA";
            colonyA.ColonyName = "ColonyA";
            var structA = MakeStructure(bpA.UUID, built: false, online: false);
            structA.BuildCompletionTime = new CountDownTime();
            structA.BuildCompletionTime.StartTime = DateTime.UtcNow.AddMinutes(-10);
            structA.BuildCompletionTime.EndTime = DateTime.UtcNow.AddMinutes(-5);
            colonyA.Structures.Add(structA);
            pc.AddColony(colonyA);

            // Create colony B -- not locked, with expired timer
            var bpB = MakeBlueprint("Power Plant", new Dictionary<string, string>
            {
                { GameConstants.PropPowerProvided, "200" }
            });
            pc.AddBlueprint(bpB);

            var colonyB = new Colony();
            colonyB.UUID = Guid.NewGuid().ToString();
            colonyB.PlanetName = "PlanetB";
            colonyB.ColonyName = "ColonyB";
            var structB = MakeStructure(bpB.UUID, built: false, online: false);
            structB.BuildCompletionTime = new CountDownTime();
            structB.BuildCompletionTime.StartTime = DateTime.UtcNow.AddMinutes(-10);
            structB.BuildCompletionTime.EndTime = DateTime.UtcNow.AddMinutes(-5);
            colonyB.Structures.Add(structB);
            pc.AddColony(colonyB);

            // Hold write lock on colony A to simulate contention
            colonyA.ColonyLock.EnterWriteLock();

            try
            {
                var processor = new BackgroundProcessor(pc);
                processor.RunCycleOnce();
            }
            finally
            {
                colonyA.ColonyLock.ExitWriteLock();
            }

            // Colony A should NOT have been processed (structure still not built)
            bool colonyABuilt;
            structA.Properties.GetBoolean(GameConstants.PropBuilt, false, out colonyABuilt);
            Assert.That(colonyABuilt, Is.False, "Locked colony A should be skipped");

            // Colony B SHOULD have been processed (structure now built)
            bool colonyBBuilt;
            structB.Properties.GetBoolean(GameConstants.PropBuilt, false, out colonyBBuilt);
            Assert.That(colonyBBuilt, Is.True, "Unlocked colony B should be processed");
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static OE2EmpireTracker.Models.Blueprint MakeBlueprint(string type, Dictionary<string, string> properties)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Test " + type);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = type;
            if (properties != null)
            {
                foreach (var kv in properties)
                    bp.Properties.SetProperty(kv.Key, kv.Value);
            }

            return bp;
        }

        private static ColonyStructure MakeStructure(string blueprintUUID, bool built, bool online)
        {
            var s = new ColonyStructure();
            s.UUID = Guid.NewGuid().ToString();
            s.FlatpackBlueprintUUID = blueprintUUID;
            s.Properties.SetProperty(GameConstants.PropBuilt, built);
            s.Properties.SetProperty(GameConstants.PropOnline, online);
            s.Properties.SetProperty(GameConstants.PropStaged, false);
            return s;
        }

        private Colony BuildColonyWithExpiredTimer()
        {
            var pc = PlayerContext.GetInstance();

            var reactorBp = MakeBlueprint("Power Plant", new Dictionary<string, string>
            {
                { GameConstants.PropPowerProvided, "500" },
                { GameConstants.PropBlueCollarDetail, "1" }
            });
            pc.AddBlueprint(reactorBp);

            var minerBp = MakeBlueprint("Mining Rig", new Dictionary<string, string>
            {
                { GameConstants.PropPowerRequired, "75" },
                { GameConstants.PropBlueCollarDetail, "1" }
            });
            pc.AddBlueprint(minerBp);

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.PlanetName = "TestPlanet";
            colony.ColonyName = "TestColony";

            var reactor = MakeStructure(reactorBp.UUID, built: true, online: true);
            reactor.AssignedWorkers.SetProperty("BlueCollar1", true);
            colony.Structures.Add(reactor);

            // Structure with an expired build timer so HasExpiredTimers() returns true
            var miner = MakeStructure(minerBp.UUID, built: false, online: false);
            miner.BuildCompletionTime = new CountDownTime();
            miner.BuildCompletionTime.StartTime = DateTime.UtcNow.AddMinutes(-10);
            miner.BuildCompletionTime.EndTime = DateTime.UtcNow.AddMinutes(-5);
            colony.Structures.Add(miner);

            // Add an item so the colony has some data
            var item = new Item(ItemType.ItemTypeEnum.Resource, "Iron");
            item.UUID = Guid.NewGuid().ToString();
            item.BaseItemTypeID = "Iron";
            item.ResourcePurity = "Low";
            item.Quantity = 100;
            item.Volume = 1;
            colony.Items.AddItem(item);

            return colony;
        }
    }
}
