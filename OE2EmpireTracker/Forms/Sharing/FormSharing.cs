// <copyright file="FormSharing.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Sharing
{
    /// <summary>
    /// Form for managing sharing/visibility rules.
    /// </summary>
    public partial class FormSharing : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate;
        private PlayerContext playerContext;

        public FormSharing()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            dgvRules.DataError += DgvRules_DataError;
            dgvRules.CellValueChanged += DgvRules_CellValueChanged;
            dgvRules.CurrentCellDirtyStateChanged += DgvRules_CurrentCellDirtyStateChanged;

            btnAddRule.Click += BtnAddRule_Click;
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            btnSave.Click += BtnSave_Click;
            btnReload.Click += BtnReload_Click;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;

            Load += FormSharing_Load;
        }

        public void BeginProgrammaticUpdate()
        {
            _isProgrammaticUpdate++;
        }

        public void EndProgrammaticUpdate()
        {
            _isProgrammaticUpdate--;
        }

        /// <summary>
        /// Determines whether any non-Public row has an empty or whitespace-only Target UUID.
        /// Extracted for testability.
        /// </summary>
        /// <param name="grid">The DataGridView containing sharing rules.</param>
        /// <param name="targetTypeColumnIndex">Column index for Target Type.</param>
        /// <param name="targetUUIDColumnIndex">Column index for Target UUID.</param>
        /// <returns>True if any non-Public row has an empty Target UUID.</returns>
        internal static bool HasEmptyTargetUUID(
            DataGridView grid,
            int targetTypeColumnIndex,
            int targetUUIDColumnIndex)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                string targetType = row.Cells[targetTypeColumnIndex].Value?.ToString() ?? string.Empty;
                if (string.Equals(targetType, "Public", StringComparison.Ordinal))
                {
                    continue;
                }

                string targetUUID = row.Cells[targetUUIDColumnIndex].Value?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetUUID))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        private async void FormSharing_Load(object sender, EventArgs e)
        {
            await LoadRulesAsync().ConfigureAwait(true);
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            Log.Info("Player changed, reloading sharing rules");
            _ = LoadRulesAsync();
        }

        private async Task LoadRulesAsync()
        {
            var ctx = ServerContext.Instance;
            if (ctx?.Client == null || !ctx.Client.IsConnected)
            {
                Log.Info("Server not connected, cannot load sharing rules");
                using var guard = new ProgrammaticUpdateGuard(this);
                dgvRules.Rows.Clear();
                UpdateSaveButtonState();
                return;
            }

            string characterUUID = playerContext.CurrentPlayer?.UUID;
            if (string.IsNullOrEmpty(characterUUID))
            {
                Log.Info("No current player selected, clearing sharing rules");
                using var guard = new ProgrammaticUpdateGuard(this);
                dgvRules.Rows.Clear();
                UpdateSaveButtonState();
                return;
            }

            try
            {
                string json = await ctx.Client.GetSharingRulesAsync(characterUUID).ConfigureAwait(false);

                if (InvokeRequired)
                {
                    Invoke(new Action(() => PopulateGrid(json)));
                }
                else
                {
                    PopulateGrid(json);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load sharing rules");
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                        MessageBox.Show(
                            this,
                            "Failed to load sharing rules: " + ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)));
                }
                else
                {
                    MessageBox.Show(
                        this,
                        "Failed to load sharing rules: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void PopulateGrid(string json)
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvRules.Rows.Clear();

            if (string.IsNullOrEmpty(json))
            {
                UpdateSaveButtonState();
                sw.Stop();
                Log.Info("PERF PopulateGrid {0}ms (empty)", sw.ElapsedMilliseconds);
                return;
            }

            var rules = JsonConvert.DeserializeObject<List<SharingRuleDto>>(json);
            if (rules == null)
            {
                UpdateSaveButtonState();
                sw.Stop();
                Log.Info("PERF PopulateGrid {0}ms (null)", sw.ElapsedMilliseconds);
                return;
            }

            foreach (var rule in rules)
            {
                int rowIndex = dgvRules.Rows.Add();
                var row = dgvRules.Rows[rowIndex];
                row.Cells[colTargetType.Index].Value = rule.TargetType ?? "Faction";
                row.Cells[colTargetUUID.Index].Value = rule.TargetUUID ?? string.Empty;
                row.Cells[colDataType.Index].Value = rule.DataType ?? "All";
                row.Tag = rule.Id;
            }

            UpdateSaveButtonState();
            sw.Stop();
            Log.Info("PERF PopulateGrid {0}ms ({1} rules)", sw.ElapsedMilliseconds, rules.Count);
        }

        private void BtnAddRule_Click(object sender, EventArgs e)
        {
            int rowIndex = dgvRules.Rows.Add();
            var row = dgvRules.Rows[rowIndex];

            using var guard = new ProgrammaticUpdateGuard(this);
            row.Cells[colTargetType.Index].Value = "Faction";
            row.Cells[colTargetUUID.Index].Value = string.Empty;
            row.Cells[colDataType.Index].Value = "All";

            UpdateSaveButtonState();
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (dgvRules.SelectedRows.Count == 0)
            {
                return;
            }

            using var guard = new ProgrammaticUpdateGuard(this);
            foreach (DataGridViewRow row in dgvRules.SelectedRows)
            {
                if (!row.IsNewRow)
                {
                    dgvRules.Rows.Remove(row);
                }
            }

            UpdateSaveButtonState();
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            var ctx = ServerContext.Instance;
            if (ctx?.Client == null || !ctx.Client.IsConnected)
            {
                MessageBox.Show(
                    this,
                    "Server is not connected.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string characterUUID = playerContext.CurrentPlayer?.UUID;
            if (string.IsNullOrEmpty(characterUUID))
            {
                MessageBox.Show(
                    this,
                    "No player selected.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var rules = new List<SharingRuleDto>();
            foreach (DataGridViewRow row in dgvRules.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var rule = new SharingRuleDto
                {
                    Id = row.Tag as string ?? string.Empty,
                    TargetType = row.Cells[colTargetType.Index].Value?.ToString() ?? "Faction",
                    TargetUUID = GetTargetUUIDForSave(row),
                    DataType = GetDataTypeForSave(row),
                    OwnerCharacterUUID = characterUUID,
                };

                rules.Add(rule);
            }

            string json = JsonConvert.SerializeObject(rules);

            try
            {
                var response = await ctx.Client.PutSharingRulesAsync(characterUUID, json).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => PopulateGrid(responseBody)));
                    }
                    else
                    {
                        PopulateGrid(responseBody);
                    }

                    Log.Info("Sharing rules saved successfully");
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string errorMessage = string.Format(
                        "Failed to save sharing rules. Server returned {0}: {1}",
                        (int)response.StatusCode,
                        errorBody);

                    Log.Warn(errorMessage);

                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                            MessageBox.Show(
                                this,
                                errorMessage,
                                "Save Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error)));
                    }
                    else
                    {
                        MessageBox.Show(
                            this,
                            errorMessage,
                            "Save Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save sharing rules");
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                        MessageBox.Show(
                            this,
                            "Failed to save sharing rules: " + ex.Message,
                            "Save Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)));
                }
                else
                {
                    MessageBox.Show(
                        this,
                        "Failed to save sharing rules: " + ex.Message,
                        "Save Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnReload_Click(object sender, EventArgs e)
        {
            await LoadRulesAsync().ConfigureAwait(true);
        }

        private void DgvRules_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            UpdateSaveButtonState();
        }

        private void DgvRules_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            if (dgvRules.IsCurrentCellDirty)
            {
                dgvRules.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void UpdateSaveButtonState()
        {
            bool hasEmptyUUID = HasEmptyTargetUUID(dgvRules, colTargetType.Index, colTargetUUID.Index);
            btnSave.Enabled = !hasEmptyUUID;
        }

        /// <summary>
        /// Gets the TargetUUID value for serialization. Public rules use "public" sentinel.
        /// </summary>
        private string GetTargetUUIDForSave(DataGridViewRow row)
        {
            string targetType = row.Cells[colTargetType.Index].Value?.ToString() ?? "Faction";
            if (string.Equals(targetType, "Public", StringComparison.Ordinal))
            {
                return "public";
            }

            return row.Cells[colTargetUUID.Index].Value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Gets the DataType value for serialization. "All" maps to null (all data types).
        /// </summary>
        private string GetDataTypeForSave(DataGridViewRow row)
        {
            string dataType = row.Cells[colDataType.Index].Value?.ToString() ?? "All";
            if (string.Equals(dataType, "All", StringComparison.Ordinal))
            {
                return null;
            }

            return dataType;
        }

        private void DgvRules_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn(
                "dgvRules DataError at [{0},{1}]: {2}",
                e.RowIndex,
                e.ColumnIndex,
                e.Exception?.Message);
            e.ThrowException = false;
        }
    }
}
