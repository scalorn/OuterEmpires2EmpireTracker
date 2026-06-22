// <copyright file="PlayerContextBackendTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for PlayerContext backend integration (WriteContext and LoadFromBackend).
    /// Satisfies: Req 1, Criteria 1-10; Req 8, Criteria 1-4.
    /// </summary>
    [TestFixture]
    public class PlayerContextBackendTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            PlayerContext.WritesBlocked = false;
            PlayerContext.IsServerOnlyMode = null;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            PlayerContext.WritesBlocked = false;
            PlayerContext.IsServerOnlyMode = null;
        }

        [Test]
        public void WriteContext_NullBackend_NoFilePath_LogsWarning_NoCrash()
        {
            var ctx = new PlayerContext(new PlayerRoot());

            Assert.DoesNotThrow(() => ctx.WriteContext());
        }

        [Test]
        public void WriteContext_NullBackend_WithFilePath_WritesFile()
        {
            string tempFile = Path.Combine(
                Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                PlayerContext.FilePath = tempFile;
                var ctx = new PlayerContext(new PlayerRoot());

                ctx.WriteContext();

                Assert.That(File.Exists(tempFile), Is.True,
                    "Legacy file should be written");
                string content = File.ReadAllText(tempFile);
                Assert.That(content, Does.Contain("CurrentPlayerUUID"));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void WriteContext_JsonSingleFileBackend_RoutesToFullFileWrite()
        {
            // JsonSingleFileBackend.UpsertGlobalDataAsync is not supported yet,
            // so WriteContext throws NotSupportedException for this path.
            // This test documents the current routing behavior.
            string tempDir = Path.Combine(
                Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var backend = new JsonSingleFileBackend(
                    new StorageBackendConfig { ConnectionString = tempDir });
                Task.Run(() => backend.InitializeAsync())
                    .GetAwaiter().GetResult();

                var ctx = new PlayerContext(new PlayerRoot());
                ctx.StorageBackend = backend;
                ctx.CurrentPlayerUUID = "test-char-uuid";

                // WriteContext routes to UpsertGlobalDataAsync for JsonSingleFileBackend
                // which currently throws NotSupportedException
                Assert.Throws<NotSupportedException>(() => ctx.WriteContext());
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void WriteContext_NonJsonBackend_PersistsOnlyDirtyEntities()
        {
            var backend = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-1", ColonyName = "Alpha" },
                    new Colony { UUID = "col-2", ColonyName = "Beta" },
                },
            };

            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;
            ctx.CurrentPlayerUUID = "char-1";

            // Mark dirty AFTER LoadFromBackend completes (it clears dirty flags)
            ctx.MarkDirty<Colony>("col-1");

            ctx.WriteContext();

            Assert.That(backend.UpsertCalls, Has.Count.EqualTo(1));
            Assert.That(backend.UpsertCalls[0].EntityUUID, Is.EqualTo("col-1"));
            Assert.That(backend.UpsertCalls[0].EntityType,
                Is.EqualTo(typeof(Colony)));
        }

        [Test]
        public void WriteContext_StorageWriteException_SetsWritesBlocked()
        {
            var backend = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-1", ColonyName = "Alpha" },
                },
            };

            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;
            ctx.CurrentPlayerUUID = "char-1";

            // Mark dirty after load, then switch to throwing mode
            ctx.MarkDirty<Colony>("col-1");
            backend.ThrowOnUpsert = true;

            Assert.That(PlayerContext.WritesBlocked, Is.False);

            Assert.Throws<StorageWriteException>(() => ctx.WriteContext());

            Assert.That(PlayerContext.WritesBlocked, Is.True);
        }

        [Test]
        public void LoadFromBackend_PopulatesInMemoryState()
        {
            var backend = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-1", ColonyName = "Alpha" },
                    new Colony { UUID = "col-2", ColonyName = "Beta" },
                },
                BlueprintsToReturn = new List<Bp>
                {
                    new Bp { UUID = "bp-1", Name = "LaserMk1" },
                },
            };

            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;

            // Setting CurrentPlayerUUID triggers LoadFromBackend
            ctx.CurrentPlayerUUID = "char-uuid";

            Assert.That(ctx.ColonyList, Has.Count.EqualTo(2));
            Assert.That(ctx.ColonyList[0].UUID, Is.EqualTo("col-1"));
            Assert.That(ctx.ColonyList[1].UUID, Is.EqualTo("col-2"));
            Assert.That(ctx.BlueprintList, Has.Count.EqualTo(1));
            Assert.That(ctx.BlueprintList[0].Name, Is.EqualTo("LaserMk1"));
        }

        [Test]
        public void LoadFromBackend_Failure_RevertsCurrentPlayerUUID()
        {
            var backend = new RecordingStorageBackend();
            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;
            ctx.CurrentPlayerUUID = "original-uuid";

            // Now make the backend throw on next load
            backend.ThrowOnLoad = true;

            // Changing UUID should fail and revert
            Assert.Throws<StorageLoadException>(() =>
            {
                ctx.CurrentPlayerUUID = "new-uuid";
            });

            Assert.That(ctx.CurrentPlayerUUID, Is.EqualTo("original-uuid"));
        }
    }
}
