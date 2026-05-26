// <copyright file="FormPreferences.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms
{
    /// <summary>
    /// Preferences dialog with Thresholds, Server, and Game API tabs.
    /// </summary>
    public partial class FormPreferences : Form
    {
        private const string GameApiKeyPlaceholder = "\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Tracks the operating mode at form open to detect mode changes on save.
        /// </summary>
        private OperatingMode _originalMode;

        /// <summary>
        /// Tracks the original polling interval to detect changes on save.
        /// </summary>
        private int _originalPollingInterval;

        public FormPreferences()
        {
            InitializeComponent();

            btnOK.Click += BtnOK_Click;
            btnResetDefaults.Click += BtnResetDefaults_Click;
            btnTestConnection.Click += BtnTestConnection_Click;
            btnPushLocalToServer.Click += BtnPushLocalToServer_Click;
            btnTestGameApiConnection.Click += BtnTestGameApiConnection_Click;

            // Populate operating mode dropdown
            cmbOperatingMode.Items.Add("Local Only");
            cmbOperatingMode.Items.Add("Server Only");
            cmbOperatingMode.Items.Add("Server + Local");

            LoadPreferences();
        }

        private static void ShowValidationError(string message)
        {
            MessageBox.Show(
                message,
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void LoadPreferences()
        {
            var prefs = PreferencesStore.GetInstance().Preferences;
            PopulateThresholdFields(prefs.Thresholds);
            PopulateServerFields(prefs.ServerConnection);
            PopulateGameApiFields(prefs.GameApiConnection);
        }

        private void PopulateThresholdFields(ThresholdPreferences thresholds)
        {
            txtStructureYellow.Text = thresholds.StructureCountYellow.ToString();
            txtStructureRed.Text = thresholds.StructureCountRed.ToString();
            txtWorkerYellow.Text = ActivityRow.FormatSeconds(thresholds.WorkerRequestYellowSeconds);
            txtWorkerRed.Text = ActivityRow.FormatSeconds(thresholds.WorkerRequestRedSeconds);
            txtColonyImportYellow.Text = ActivityRow.FormatSeconds(thresholds.ColonyImportStalenessYellowSeconds);
            txtColonyImportRed.Text = ActivityRow.FormatSeconds(thresholds.ColonyImportStalenessRedSeconds);
            txtBackgroundInterval.Text = ActivityRow.FormatSeconds(thresholds.BackgroundProcessingIntervalSeconds);
            txtAdminRefresh.Text = ActivityRow.FormatSeconds(thresholds.AdminRefreshIntervalSeconds);
            txtCountdownRefresh.Text = ActivityRow.FormatSeconds(thresholds.CountdownRefreshRateSeconds);
        }

        private void PopulateServerFields(ServerConnectionSettings settings)
        {
            txtServerUrl.Text = settings.ServerUrl ?? string.Empty;
            txtThumbprint.Text = settings.TrustedThumbprint ?? string.Empty;

            // Display masked placeholder if a token is stored; otherwise leave empty
            if (!string.IsNullOrEmpty(settings.ProtectedBearerToken))
            {
                txtBearerToken.Text = "stored-token";
            }
            else
            {
                txtBearerToken.Text = string.Empty;
            }

            _originalMode = settings.Mode;
            cmbOperatingMode.SelectedIndex = (int)settings.Mode;
        }

        private void PopulateGameApiFields(GameApiConnectionSettings settings)
        {
            var sw = Stopwatch.StartNew();

            txtGameApiUrl.Text = settings.ServerUrl ?? string.Empty;
            nudPollingInterval.Value = Math.Max(1, Math.Min(60, settings.PollingIntervalMinutes));
            chkGameApiEnabled.Checked = settings.Enabled;
            _originalPollingInterval = settings.PollingIntervalMinutes;

            // Show placeholder dots if a key is stored for the current player
            string playerUUID = PlayerContext.GetInstance().CurrentPlayerUUID;
            var credManager = new GameApiCredentialManager();
            if (!string.IsNullOrEmpty(playerUUID) && credManager.HasKey(playerUUID))
            {
                txtGameApiKey.Text = GameApiKeyPlaceholder;
            }
            else
            {
                txtGameApiKey.Text = string.Empty;
            }

            sw.Stop();
            Log.Debug("PERF PopulateGameApiFields: {0}ms", sw.ElapsedMilliseconds);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!TryParseThresholds(out ThresholdPreferences thresholds))
            {
                return;
            }

            // Detect mode change for export prompt (Task 12.6)
            OperatingMode newMode = (OperatingMode)cmbOperatingMode.SelectedIndex;
            if (_originalMode == OperatingMode.ServerOnly && newMode == OperatingMode.LocalOnly)
            {
                var result = MessageBox.Show(
                    "You are switching from Server Only to Local Only.\n\n" +
                    "Local data may be stale or empty. Would you like to export " +
                    "your data from the server first?\n\n" +
                    "Click Yes to export before switching, No to switch without " +
                    "exporting, or Cancel to abort.",
                    "Export Data?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Cancel)
                {
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    MessageBox.Show(
                        "Export functionality will be available in a future update.\n" +
                        "The mode change will proceed without export.",
                        "Export Not Yet Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }

            // Save server connection settings
            var store = PreferencesStore.GetInstance();
            var serverSettings = store.Preferences.ServerConnection;
            serverSettings.ServerUrl = txtServerUrl.Text.Trim();
            serverSettings.TrustedThumbprint = txtThumbprint.Text.Trim();
            serverSettings.Mode = newMode;

            // Only update the token if the user changed it from the placeholder
            string tokenText = txtBearerToken.Text;
            if (tokenText != "stored-token" && !string.IsNullOrEmpty(tokenText))
            {
                serverSettings.ProtectedBearerToken = CredentialStore.Protect(tokenText);
            }
            else if (string.IsNullOrEmpty(tokenText))
            {
                serverSettings.ProtectedBearerToken = string.Empty;
            }

            // Save thresholds
            store.Preferences.Thresholds = thresholds;

            // Save Game API settings
            SaveGameApiSettings(store);

            store.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnResetDefaults_Click(object sender, EventArgs e)
        {
            PopulateThresholdFields(new ThresholdPreferences());
        }

        /// <summary>
        /// Task 12.2: Test Connection button — creates a temporary client and calls /health.
        /// </summary>
        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            string url = txtServerUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.Text = "Please enter a server URL.";
                return;
            }

            btnTestConnection.Enabled = false;
            lblConnectionStatus.ForeColor = SystemColors.ControlText;
            lblConnectionStatus.Text = "Testing...";

            try
            {
                string tokenText = txtBearerToken.Text;
                SecureString token;

                if (tokenText == "stored-token")
                {
                    var stored = PreferencesStore.GetInstance()
                        .Preferences.ServerConnection.ProtectedBearerToken;
                    token = CredentialStore.Unprotect(stored);
                }
                else
                {
                    token = new SecureString();
                    foreach (char c in tokenText ?? string.Empty)
                    {
                        token.AppendChar(c);
                    }

                    token.MakeReadOnly();
                }

                string thumbprint = txtThumbprint.Text.Trim();

                using (var client = new RemoteFactionClient(url, token, thumbprint))
                {
                    bool healthy = await client.CheckHealthAsync().ConfigureAwait(true);
                    if (healthy)
                    {
                        lblConnectionStatus.ForeColor = Color.Green;
                        lblConnectionStatus.Text = "Connection successful.";
                    }
                    else
                    {
                        lblConnectionStatus.ForeColor = Color.Red;
                        lblConnectionStatus.Text = "Health check failed.";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Test connection failed");
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.Text = "Failed: " + ex.Message;
            }
            finally
            {
                btnTestConnection.Enabled = true;
            }
        }

        /// <summary>
        /// Reads local PlayerData.json and BaselineData.json from disk and pushes them
        /// to the server. This is the bootstrapping path for getting local data onto an
        /// empty server. Reads files directly — does not use in-memory state.
        /// </summary>
        private async void BtnPushLocalToServer_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "This will upload your local PlayerData.json and BaselineData.json to the server, " +
                "overwriting any existing server data for your character.\n\n" +
                "Continue?",
                "Push Local Data to Server",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            btnPushLocalToServer.Enabled = false;
            lblConnectionStatus.ForeColor = SystemColors.ControlText;
            lblConnectionStatus.Text = "Pushing...";

            try
            {
                string tokenText = txtBearerToken.Text;
                System.Security.SecureString token;

                if (tokenText == "stored-token")
                {
                    var stored = PreferencesStore.GetInstance()
                        .Preferences.ServerConnection.ProtectedBearerToken;
                    token = CredentialStore.Unprotect(stored);
                }
                else
                {
                    token = new System.Security.SecureString();
                    foreach (char c in tokenText ?? string.Empty)
                    {
                        token.AppendChar(c);
                    }

                    token.MakeReadOnly();
                }

                string url = txtServerUrl.Text.Trim();
                string thumbprint = txtThumbprint.Text.Trim();

                using (var client = new RemoteFactionClient(url, token, thumbprint))
                {
                    int pushed = 0;

                    // Push player data
                    string playerPath = PlayerContext.FilePath;
                    if (System.IO.File.Exists(playerPath))
                    {
                        string playerJson = System.IO.File.ReadAllText(playerPath);
                        if (!string.IsNullOrEmpty(playerJson))
                        {
                            // Register all player profiles as characters on the server
                            var playerRoot = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerRoot>(playerJson);
                            if (playerRoot?.PlayerProfile != null)
                            {
                                foreach (var profile in playerRoot.PlayerProfile)
                                {
                                    if (string.IsNullOrEmpty(profile.UUID) || string.IsNullOrEmpty(profile.Name))
                                    {
                                        continue;
                                    }

                                    try
                                    {
                                        await client.CreateCharacterAsync(profile.Name, profile.UUID)
                                            .ConfigureAwait(true);
                                        Log.Info("Registered character on server: {0} ({1})", profile.Name, profile.UUID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warn(ex, "Failed to register character {0} (may already exist)", profile.Name);
                                    }
                                }
                            }

                            // Upload the full player data via bulk import
                            string characterUUID = playerRoot?.CurrentPlayerUUID;
                            if (!string.IsNullOrEmpty(characterUUID))
                            {
                                var importResponse = await client.BulkImportAsync(characterUUID, playerJson)
                                    .ConfigureAwait(true);
                                if (importResponse.IsSuccessStatusCode)
                                {
                                    pushed++;
                                    Log.Info("Pushed PlayerData.json to server for character {0}", characterUUID);
                                }
                                else
                                {
                                    string errorBody = await importResponse.Content.ReadAsStringAsync()
                                        .ConfigureAwait(true);
                                    Log.Warn(
                                        "Bulk import failed (HTTP {0}) for character {1}: {2}",
                                        (int)importResponse.StatusCode,
                                        characterUUID,
                                        errorBody);
                                }
                            }
                            else
                            {
                                Log.Warn("Cannot push player data: no CurrentPlayerUUID in file");
                            }
                        }
                    }
                    else
                    {
                        Log.Warn("PlayerData.json not found at {0}", playerPath);
                    }

                    // Push baseline data
                    string baselinePath = EmpireContext.FilePath;
                    if (System.IO.File.Exists(baselinePath))
                    {
                        string baselineJson = System.IO.File.ReadAllText(baselinePath);
                        if (!string.IsNullOrEmpty(baselineJson))
                        {
                            await client.UploadGlobalDataAsync("baseline", baselineJson)
                                .ConfigureAwait(true);
                            pushed++;
                            Log.Info("Pushed BaselineData.json to server as global/baseline");
                        }
                    }
                    else
                    {
                        Log.Warn("BaselineData.json not found at {0}", baselinePath);
                    }

                    lblConnectionStatus.ForeColor = Color.Green;
                    lblConnectionStatus.Text = string.Format("Push complete ({0} file(s) uploaded).", pushed);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Push local data to server failed");
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.Text = "Push failed: " + ex.Message;
            }
            finally
            {
                btnPushLocalToServer.Enabled = true;
            }
        }

        /// <summary>
        /// Saves Game API settings to preferences, encrypts the API key if changed,
        /// and updates the sync scheduler polling interval immediately.
        /// </summary>
        private void SaveGameApiSettings(PreferencesStore store)
        {
            var gameApiSettings = store.Preferences.GameApiConnection;
            gameApiSettings.ServerUrl = txtGameApiUrl.Text.Trim();
            gameApiSettings.Enabled = chkGameApiEnabled.Checked;

            int newPollingInterval = (int)nudPollingInterval.Value;
            gameApiSettings.PollingIntervalMinutes = newPollingInterval;

            // Encrypt and store API key if the user changed it from the placeholder
            string keyText = txtGameApiKey.Text;
            if (keyText != GameApiKeyPlaceholder && !string.IsNullOrEmpty(keyText))
            {
                string playerUUID = PlayerContext.GetInstance().CurrentPlayerUUID;
                if (!string.IsNullOrEmpty(playerUUID))
                {
                    var credManager = new GameApiCredentialManager();
                    credManager.StoreKey(playerUUID, keyText);
                    Log.Info("Game API key stored for character {0}", playerUUID);
                }
            }

            // Update polling interval on the sync scheduler immediately if it changed
            if (newPollingInterval != _originalPollingInterval)
            {
                GameApiContext.Instance?.SyncScheduler.UpdatePollingInterval(newPollingInterval);
                Log.Info(
                    "Game API polling interval updated from {0} to {1} minutes",
                    _originalPollingInterval,
                    newPollingInterval);
            }
        }

        /// <summary>
        /// Tests the Game API connection by creating a temporary client and calling CheckHealthAsync.
        /// </summary>
        private async void BtnTestGameApiConnection_Click(object sender, EventArgs e)
        {
            string url = txtGameApiUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Please enter a server URL.";
                return;
            }

            // Resolve the API key to use for the test
            string apiKey = ResolveGameApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Please enter an API key.";
                return;
            }

            Log.Info(
                "Game API test connection: URL='{0}', key length={1}, current player UUID='{2}'",
                url,
                apiKey.Length,
                PlayerContext.GetInstance().CurrentPlayerUUID ?? "(null)");

            btnTestGameApiConnection.Enabled = false;
            lblTestResult.ForeColor = SystemColors.ControlText;
            lblTestResult.Text = "Testing...";

            try
            {
                using (var client = new GameApiClient(url))
                {
                    var result = await client.CheckHealthAsync(apiKey).ConfigureAwait(true);
                    Log.Info("Game API test connection result: Success={0}, Message='{1}'", result.Success, result.Message);
                    if (result.Success)
                    {
                        lblTestResult.ForeColor = Color.Green;
                        lblTestResult.Text = "Connection successful.";
                    }
                    else
                    {
                        lblTestResult.ForeColor = Color.Red;
                        lblTestResult.Text = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Game API test connection failed");
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Failed: " + ex.Message;
            }
            finally
            {
                btnTestGameApiConnection.Enabled = true;
            }
        }

        /// <summary>
        /// Resolves the API key to use for testing. If the user entered a new key, uses that.
        /// If the placeholder is shown, retrieves the stored key for the current player.
        /// </summary>
        private string ResolveGameApiKey()
        {
            string keyText = txtGameApiKey.Text;
            if (keyText != GameApiKeyPlaceholder && !string.IsNullOrEmpty(keyText))
            {
                return keyText;
            }

            // Retrieve stored key for current player
            string playerUUID = PlayerContext.GetInstance().CurrentPlayerUUID;
            if (string.IsNullOrEmpty(playerUUID))
            {
                return null;
            }

            var credManager = new GameApiCredentialManager();
            SecureString secureKey = credManager.GetKey(playerUUID);
            if (secureKey == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureKey);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }

                secureKey.Dispose();
            }
        }

        private bool TryParseThresholds(out ThresholdPreferences result)
        {
            result = null;

            if (!int.TryParse(txtStructureYellow.Text.Trim(), out int structureYellow)
                || structureYellow <= 0)
            {
                ShowValidationError("Structure Count Yellow threshold must be a positive integer.");
                return false;
            }

            if (!int.TryParse(txtStructureRed.Text.Trim(), out int structureRed)
                || structureRed <= 0)
            {
                ShowValidationError("Structure Count Red threshold must be a positive integer.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(txtWorkerYellow.Text.Trim(), out long workerYellow))
            {
                ShowValidationError("Worker Request Yellow must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(txtWorkerRed.Text.Trim(), out long workerRed))
            {
                ShowValidationError("Worker Request Red must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(
                txtColonyImportYellow.Text.Trim(), out long colonyImportYellow))
            {
                ShowValidationError("Colony Import Yellow must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(
                txtColonyImportRed.Text.Trim(), out long colonyImportRed))
            {
                ShowValidationError("Colony Import Red must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(
                txtBackgroundInterval.Text.Trim(), out long backgroundInterval))
            {
                ShowValidationError("Background Interval must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(
                txtAdminRefresh.Text.Trim(), out long adminRefresh))
            {
                ShowValidationError("Admin Refresh must be a valid countdown format.");
                return false;
            }

            if (!CountdownFormatParser.TryParse(
                txtCountdownRefresh.Text.Trim(), out long countdownRefresh))
            {
                ShowValidationError("Countdown Refresh must be a valid countdown format.");
                return false;
            }

            var prefs = new ThresholdPreferences
            {
                StructureCountYellow = structureYellow,
                StructureCountRed = structureRed,
                WorkerRequestYellowSeconds = workerYellow,
                WorkerRequestRedSeconds = workerRed,
                ColonyImportStalenessYellowSeconds = colonyImportYellow,
                ColonyImportStalenessRedSeconds = colonyImportRed,
                BackgroundProcessingIntervalSeconds = backgroundInterval,
                AdminRefreshIntervalSeconds = adminRefresh,
                CountdownRefreshRateSeconds = countdownRefresh,
            };

            if (!ThresholdPreferences.Validate(prefs, out string error))
            {
                ShowValidationError(error);
                return false;
            }

            result = prefs;
            return true;
        }
    }
}
