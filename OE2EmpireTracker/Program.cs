using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Wire up delegates for Common-hosted contexts
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
            Application.Run(new MainWindow());
        }
    }
}
