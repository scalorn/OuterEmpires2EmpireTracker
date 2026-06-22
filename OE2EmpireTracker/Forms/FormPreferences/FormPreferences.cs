// <copyright file="FormPreferences.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
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
        private const string GameApiSecretPlaceholder = "\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF\u25CF";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Tracks the operating mode at form open to detect mode changes on save.
        /// </summary>
        private OperatingMode _originalMode;

        /// <summary>
        /// Tracks the original polling interval to detect changes on save.
        /// </summary>
        private int _originalPollingInterval;

        /// <summary>
        /// Maps character dropdown index to player UUID for the Game API tab.
        /// </summary>
        private List<string> _gameApiCharacterUUIDs = new List<string>();

        /// <summary>
        /// Tracks the last valid TPS value for reverting on invalid input.
        /// </summary>
        private string _lastValidTps = "1.000";

        public FormPreferences()
        {
            InitializeComponent();

            btnOK.Click += BtnOK_Click;
            btnResetDefaults.Click += BtnResetDefaults_Click;
            btnTestConnection.Click += BtnTestConnection_Click;
            btnPushLocalToServer.Click += BtnPushLocalToServer_Click;
            btnTestGameApiConnection.Click += BtnTestGameApiConnection_Click;
            cmbGameApiCharacter.SelectedIndexChanged += CmbGameApiCharacter_SelectedIndexChanged;
            txtTpsLimit.Leave += TxtTpsLimit_Leave;
            btnBrowseStoragePath.Click += BtnBrowseStoragePath_Click;
            btnMigrateStorage.Click += BtnMigrateStorage_Click;

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
            PopulateStorageFields(prefs);
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
            nudOverflowHorizon.Value = Math.Max(1, Math.Min(336, thresholds.OverflowPredictionHorizonHours));
            nudUnderutilizedStockpile.Value = Math.Max(1, Math.Min(168, thresholds.UnderutilizedRefiningStockpileHours));
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
            txtGameApiAppId.Text = settings.AppId ?? string.Empty;
            txtGameApiClientId.Text = settings.ClientId ?? string.Empty;
            nudPollingInterval.Value = Math.Max(1, Math.Min(60, settings.PollingIntervalMinutes));
            chkGameApiEnabled.Checked = settings.Enabled;
            _originalPollingInterval = settings.PollingIntervalMinutes;

            // Populate character dropdown with all player profiles
            cmbGameApiCharacter.Items.Clear();
            _gameApiCharacterUUIDs.Clear();
            var profiles = PlayerContext.GetInstance().GetReadOnlyPlayerProfileList();
            string currentPlayerUUID = PlayerContext.GetInstance().CurrentPlayerUUID;
            int selectedIndex = -1;
            for (int i = 0; i < profiles.Count; i++)
            {
                cmbGameApiCharacter.Items.Add(profiles[i].Name);
                _gameApiCharacterUUIDs.Add(profiles[i].UUID);
                if (profiles[i].UUID == currentPlayerUUID)
                {
                    selectedIndex = i;
                }
            }

            if (selectedIndex >= 0)
            {
                cmbGameApiCharacter.SelectedIndex = selectedIndex;
            }
            else if (cmbGameApiCharacter.Items.Count > 0)
            {
                cmbGameApiCharacter.SelectedIndex = 0;
            }

            txtTpsLimit.Text = settings.Tps.ToString("F3", CultureInfo.InvariantCulture);
            _lastValidTps = txtTpsLimit.Text;
            nudDetailRefreshHours.Value = Math.Max(1, settings.DetailRefreshHours);
            nudMaxInflightRequests.Value = Math.Max(1, Math.Min(100, settings.MaxInflightRequests));

            // Show placeholder dots if a secret is stored for the selected character
            string playerUUID = GetSelectedCharacterUUID();
            var credManager = new GameApiCredentialManager();
            if (!string.IsNullOrEmpty(playerUUID) && credManager.HasKey(playerUUID))
            {
                txtGameApiSecret.Text = GameApiSecretPlaceholder;
            }
            else
            {
                txtGameApiSecret.Text = string.Empty;
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
        /// Saves Game API settings to preferences, encrypts the secret if changed,
        /// and updates the sync scheduler polling interval immediately.
        /// </summary>
        private void SaveGameApiSettings(PreferencesStore store)
        {
            var gameApiSettings = store.Preferences.GameApiConnection;
            gameApiSettings.ServerUrl = txtGameApiUrl.Text.Trim();
            gameApiSettings.AppId = txtGameApiAppId.Text.Trim();
            gameApiSettings.ClientId = txtGameApiClientId.Text.Trim();
            gameApiSettings.Enabled = chkGameApiEnabled.Checked;

            int newPollingInterval = (int)nudPollingInterval.Value;
            gameApiSettings.PollingIntervalMinutes = newPollingInterval;
            gameApiSettings.Tps = double.Parse(txtTpsLimit.Text, CultureInfo.InvariantCulture);
            gameApiSettings.DetailRefreshHours = (int)nudDetailRefreshHours.Value;
            gameApiSettings.MaxInflightRequests = (int)nudMaxInflightRequests.Value;

            // Encrypt and store secret if the user changed it from the placeholder
            string secretText = txtGameApiSecret.Text;
            if (secretText != GameApiSecretPlaceholder && !string.IsNullOrEmpty(secretText))
            {
                string playerUUID = GetSelectedCharacterUUID();
                if (!string.IsNullOrEmpty(playerUUID))
                {
                    try
                    {
                        var credManager = new GameApiCredentialManager();
                        credManager.StoreKey(playerUUID, secretText);
                        Log.Info("Game API secret stored for character {0}", playerUUID);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to store Game API secret for character {0}", playerUUID);
                        MessageBox.Show(
                            "Failed to save secret: " + ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
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
        /// Tests the Game API connection by performing a token exchange with the entered credentials.
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

            string appId = txtGameApiAppId.Text.Trim();
            if (string.IsNullOrEmpty(appId))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Please enter an App ID.";
                return;
            }

            string clientId = txtGameApiClientId.Text.Trim();
            if (string.IsNullOrEmpty(clientId))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Please enter a Client ID.";
                return;
            }

            // Resolve the secret to use for the test
            string secret = ResolveGameApiSecret();
            if (string.IsNullOrEmpty(secret))
            {
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Please enter a secret.";
                return;
            }

            Log.Info(
                "Game API test connection: URL='{0}', appId='{1}', clientId='{2}', secret length={3}",
                url,
                appId,
                clientId,
                secret.Length);

            btnTestGameApiConnection.Enabled = false;
            lblTestResult.ForeColor = SystemColors.ControlText;
            lblTestResult.Text = "Testing...";

            try
            {
                using (var client = new OE2EmpireTracker.Common.Client.GameApiTypedClient(url, appId, 0.9))
                {
                    bool connected = await client.TestConnectionAsync(appId, clientId, secret).ConfigureAwait(true);
                    Log.Info("Game API test connection result: Success={0}", connected);
                    if (connected)
                    {
                        lblTestResult.ForeColor = Color.Green;
                        lblTestResult.Text = "Connected successfully";
                    }
                    else
                    {
                        lblTestResult.ForeColor = Color.Red;
                        lblTestResult.Text = "Connection test returned false";
                    }
                }
            }
            catch (OE2EmpireTracker.Common.Client.ApiHttpException ex)
            {
                Log.Warn("Game API test connection failed: HTTP {0}", ex.StatusCode);
                lblTestResult.ForeColor = Color.Red;
                lblTestResult.Text = "Failed: HTTP " + ex.StatusCode;
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
        /// Resolves the secret to use for testing. If the user entered a new secret, uses that.
        /// If the placeholder is shown, retrieves the stored secret for the selected character.
        /// </summary>
        private string ResolveGameApiSecret()
        {
            string secretText = txtGameApiSecret.Text;
            if (secretText != GameApiSecretPlaceholder && !string.IsNullOrEmpty(secretText))
            {
                return secretText;
            }

            // Retrieve stored secret for selected character
            string playerUUID = GetSelectedCharacterUUID();
            if (string.IsNullOrEmpty(playerUUID))
            {
                return null;
            }

            var credManager = new GameApiCredentialManager();
            SecureString secureSecret = credManager.GetKey(playerUUID);
            if (secureSecret == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureSecret);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }

                secureSecret.Dispose();
            }
        }

        /// <summary>
        /// Gets the UUID of the currently selected character in the Game API dropdown.
        /// </summary>
        private string GetSelectedCharacterUUID()
        {
            int index = cmbGameApiCharacter.SelectedIndex;
            if (index >= 0 && index < _gameApiCharacterUUIDs.Count)
            {
                return _gameApiCharacterUUIDs[index];
            }

            return null;
        }

        /// <summary>
        /// Updates the secret field when the character selection changes.
        /// Shows placeholder if the selected character has a stored secret, or empty if not.
        /// </summary>
        private void CmbGameApiCharacter_SelectedIndexChanged(object sender, EventArgs e)
        {
            string playerUUID = GetSelectedCharacterUUID();
            if (string.IsNullOrEmpty(playerUUID))
            {
                txtGameApiSecret.Text = string.Empty;
                return;
            }

            var credManager = new GameApiCredentialManager();
            if (credManager.HasKey(playerUUID))
            {
                txtGameApiSecret.Text = GameApiSecretPlaceholder;
            }
            else
            {
                txtGameApiSecret.Text = string.Empty;
            }
        }

        /// <summary>
        /// Clamps TPS value to [0.1, 100.0] on valid numeric input, or reverts to
        /// the last valid value when the input is non-numeric or empty.
        /// </summary>
        private void TxtTpsLimit_Leave(object sender, EventArgs e)
        {
            string text = txtTpsLimit.Text.Trim();

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                double clamped = Math.Max(0.001, Math.Min(100.0, value));
                string formatted = clamped.ToString("F3", CultureInfo.InvariantCulture);
                txtTpsLimit.Text = formatted;
                _lastValidTps = formatted;
            }
            else
            {
                txtTpsLimit.Text = _lastValidTps;
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
                OverflowPredictionHorizonHours = (int)nudOverflowHorizon.Value,
                UnderutilizedRefiningStockpileHours = (int)nudUnderutilizedStockpile.Value,
            };

            if (!ThresholdPreferences.Validate(prefs, out string error))
            {
                ShowValidationError(error);
                return false;
            }

            result = prefs;
            return true;
        }

        /// <summary>
        /// Populates the Storage tab controls from current preferences.
        /// </summary>
        /// <param name="prefs">The current UI preferences.</param>
        private void PopulateStorageFields(UIPreferences prefs)
        {
            cmbStorageBackendType.Items.Clear();
            cmbStorageBackendType.Items.Add("JSON Single File");
            cmbStorageBackendType.Items.Add("SQLite");

            var store = PreferencesStore.GetInstance();
            var currentType = store.ParseStorageBackendType(prefs.StorageBackendType);

            switch (currentType)
            {
                case StorageBackendType.Sqlite:
                    cmbStorageBackendType.SelectedIndex = 1;
                    break;
                default:
                    cmbStorageBackendType.SelectedIndex = 0;
                    break;
            }

            txtStoragePath.Text = prefs.StoragePath ?? string.Empty;
            lblCurrentBackend.Text = "Current: " + currentType.ToString();
        }

        /// <summary>
        /// Opens a folder browser for the storage path.
        /// </summary>
        private void BtnBrowseStoragePath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select storage folder";
                if (!string.IsNullOrEmpty(txtStoragePath.Text))
                {
                    dialog.SelectedPath = txtStoragePath.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtStoragePath.Text = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// Applies the selected backend type and migrates data from the current backend.
        /// </summary>
        private void BtnMigrateStorage_Click(object sender, EventArgs e)
        {
            var store = PreferencesStore.GetInstance();
            var currentType = store.ParseStorageBackendType(store.Preferences.StorageBackendType);
            StorageBackendType selectedType = cmbStorageBackendType.SelectedIndex == 1
                ? StorageBackendType.Sqlite
                : StorageBackendType.JsonSingleFile;

            string selectedPath = txtStoragePath.Text.Trim();

            if (selectedType == currentType)
            {
                MessageBox.Show(
                    "Already using this backend.",
                    "No Change",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Save new preferences
            string originalBackendType = store.Preferences.StorageBackendType;
            string originalPath = store.Preferences.StoragePath;
            store.Preferences.StorageBackendType = selectedType.ToString();
            store.Preferences.StoragePath = selectedPath;
            store.Save();

            // Create new backend
            IStorageBackend newBackend;
            try
            {
                var config = store.ResolveStorageConfig();
                newBackend = Task.Run(() => StorageBackendFactory.CreateAsync(selectedType, config))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create {0} backend", selectedType);
                store.Preferences.StorageBackendType = originalBackendType;
                store.Preferences.StoragePath = originalPath;
                store.Save();
                MessageBox.Show(
                    "Failed to create backend:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Show progress and migrate
            var currentBackend = PlayerContext.GetInstance().StorageBackend;

            // If current backend is null (legacy file mode), create a source backend for migration
            if (currentBackend == null)
            {
                string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                var sourceConfig = new StorageBackendConfig { ConnectionString = sourceDir };
                currentBackend = new JsonSingleFileBackend(sourceConfig);
                Task.Run(() => currentBackend.InitializeAsync()).GetAwaiter().GetResult();
            }

            var progressForm = new FormMigrationProgress();
            progressForm.Show(this);
            var progress = new Progress<MigrationProgress>(p => progressForm.UpdateProgress(p));

            try
            {
                var migrationService = new MigrationService();
                Task.Run(() => migrationService.MigrateAsync(currentBackend, newBackend, progress))
                    .GetAwaiter().GetResult();
                progressForm.Close();

                // Swap backends in contexts
                PlayerContext.GetInstance().StorageBackend = newBackend;
                var empireCtx = EmpireContext.GetInstance();
                empireCtx.StorageBackendType = selectedType;
                empireCtx.StorageBackend = newBackend;

                lblCurrentBackend.Text = "Current: " + selectedType.ToString();
                Log.Info("Storage migrated from {0} to {1}", currentType, selectedType);
                MessageBox.Show(
                    "Migration complete!",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressForm.Close();
                Log.Error(ex, "Storage migration from {0} to {1} failed", currentType, selectedType);
                store.Preferences.StorageBackendType = originalBackendType;
                store.Preferences.StoragePath = originalPath;
                store.Save();
                MessageBox.Show(
                    "Migration failed:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
