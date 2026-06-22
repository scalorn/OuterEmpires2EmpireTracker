using System;
using System.Collections.Generic;
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
        }
    }
}
