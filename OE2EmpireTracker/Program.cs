using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker
{
    internal static class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Wire up delegates for Common-hosted contexts
            MigrationRunner.OnMigrationFailed = msg =>
                System.Windows.Forms.MessageBox.Show(msg, "Migration Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            EmpireContext.RunMigrations = (ec, pc) => MigrationRunner.Run(ec, pc);
            EmpireContext.FixupBlueprintProperties = bp => BlueprintScanner.FixupFlatpackProperties(bp);
            PlayerContext.IsServerOnlyMode = () =>
            {
                var sc = Client.ServerContext.Instance;
                return sc != null && sc.Mode == Client.OperatingMode.ServerOnly;
            };
            PlayerContext.IsServerConnected = () =>
            {
                var sc = Client.ServerContext.Instance;
                return sc?.Client?.IsConnected == true;
            };
            PlayerContext.PushToServer = async (characterUUID, jsonContent) =>
            {
                var sc = Client.ServerContext.Instance;
                if (sc?.SyncManager == null || sc.SyncManager.Mode == Client.OperatingMode.LocalOnly)
                    return;
                await sc.SyncManager.WriteToServerAsync(characterUUID, "player-data", jsonContent).ConfigureAwait(false);
            };
            PlayerContext.ExportFromServer = async (characterUUID) =>
            {
                var sc = Client.ServerContext.Instance;
                if (sc?.Client == null || !sc.Client.IsConnected)
                    return null;
                return await sc.Client.ExportCharacterDataAsync(characterUUID).ConfigureAwait(false);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            InitializeStorage();
            Application.Run(new MainWindow());
        }

        private static void InitializeStorage()
        {
            var prefs = PreferencesStore.GetInstance();
            var type = prefs.ParseStorageBackendType(prefs.Preferences.StorageBackendType);
            var config = prefs.ResolveStorageConfig();

            IStorageBackend backend;
            try
            {
                backend = Task.Run(() => StorageBackendFactory.CreateAsync(type, config))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize {0} storage backend", type);
                var result = MessageBox.Show(
                    $"Failed to initialize {type} backend:\n{ex.Message}\n\n" +
                    "Fall back to JSON file storage?",
                    "Storage Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    type = StorageBackendType.JsonSingleFile;
                    config = new StorageBackendConfig
                    {
                        ConnectionString = AppDomain.CurrentDomain.BaseDirectory,
                    };
                    backend = Task.Run(() => StorageBackendFactory.CreateAsync(type, config))
                        .GetAwaiter().GetResult();
                }
                else
                {
                    Environment.Exit(1);
                    return;
                }
            }

            var playerCtx = PlayerContext.GetInstance();
            playerCtx.StorageBackend = backend;

            var empireCtx = EmpireContext.GetInstance();
            empireCtx.StorageBackendType = type;
            empireCtx.StorageBackend = backend;

            OfferMigration(backend, type);
        }

        /// <summary>
        /// Detects whether previous backend data exists on disk by checking for known file/folder patterns.
        /// Returns the detected source backend type, or null if no data found.
        /// </summary>
        /// <returns>The detected <see cref="StorageBackendType"/>, or null if nothing found.</returns>
        private static StorageBackendType? DetectPreviousBackendData()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (File.Exists(Path.Combine(baseDir, "PlayerData.json")))
            {
                return StorageBackendType.JsonSingleFile;
            }

            if (Directory.Exists(Path.Combine(baseDir, "data", "characters")))
            {
                return StorageBackendType.JsonMultiFile;
            }

            if (File.Exists(Path.Combine(baseDir, "OE2EmpireTracker.db")))
            {
                return StorageBackendType.Sqlite;
            }

            return null;
        }

        /// <summary>
        /// Offers migration if a previous backend's data is detected and the current backend is empty.
        /// </summary>
        /// <param name="currentBackend">The currently configured storage backend.</param>
        /// <param name="currentType">The currently configured backend type.</param>
        private static void OfferMigration(IStorageBackend currentBackend, StorageBackendType currentType)
        {
            var detectedType = DetectPreviousBackendData();
            if (detectedType == null || detectedType.Value == currentType)
            {
                return;
            }

            // Check if the current backend is empty
            var characterUUIDs = Task.Run(() => currentBackend.GetAllCharacterUUIDsAsync())
                .GetAwaiter().GetResult();
            if (characterUUIDs.Count > 0)
            {
                return;
            }

            var dialogResult = MessageBox.Show(
                $"Existing {detectedType.Value} data was detected.\n" +
                $"Would you like to migrate it to the new {currentType} backend?",
                "Migrate Data?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (dialogResult == DialogResult.Cancel)
            {
                Environment.Exit(0);
                return;
            }

            if (dialogResult == DialogResult.No)
            {
                // Start fresh — continue with empty backend
                return;
            }

            // Migrate: create source backend and run migration
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var sourceConfig = new StorageBackendConfig { ConnectionString = baseDir };
            var sourceBackend = Task.Run(() => StorageBackendFactory.CreateAsync(detectedType.Value, sourceConfig))
                .GetAwaiter().GetResult();

            try
            {
                var migrationService = new MigrationService();
                Task.Run(() => migrationService.MigrateAsync(sourceBackend, currentBackend))
                    .GetAwaiter().GetResult();
                Log.Info("Migration from {0} to {1} completed successfully", detectedType.Value, currentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Migration from {0} to {1} failed", detectedType.Value, currentType);
                MessageBox.Show(
                    $"Migration failed:\n{ex.Message}\n\nThe application will continue with an empty backend.",
                    "Migration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
