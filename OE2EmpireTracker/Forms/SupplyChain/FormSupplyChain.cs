using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.SupplyChain
{
    public partial class FormSupplyChain : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private Models.SupplyChain _selectedChain;

        public FormSupplyChain()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwChains.View = View.Details;
            lvwChains.Columns.Add("Name", 140);
            lvwChains.Columns.Add("Active", 50);
            lvwChains.FullRowSelect = true;
            lvwChains.MultiSelect = false;
            lvwChains.ItemSelectionChanged += LvwChains_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtChainName.TextChanged += TxtChainName_TextChanged;
            chkActive.CheckedChanged += ChkActive_CheckedChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;

            cmdAddStage.Click += CmdAddStage_Click;
            cmdUpdateStage.Click += CmdUpdateStage_Click;
            cmdRemoveStage.Click += CmdRemoveStage_Click;
            cmdMoveUp.Click += CmdMoveUp_Click;
            cmdMoveDown.Click += CmdMoveDown_Click;

            dgvStages.SelectionChanged += DgvStages_SelectionChanged;

            cmbLocationType.SelectedIndexChanged += CmbLocationType_SelectedIndexChanged;

            PopulateStageTypeCombos();
            PopulateResourceCombo();
            PopulatePurityCombo();
            PopulateChainList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // Layout
        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new Size(220, h - 6);
            flpDetail.Size = new Size(w - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwChains.Size = new Size(w - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            dgvStages.Width = w - 6;
        }

        // Chain List
        private void PopulateChainList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedChain?.UUID;
            lvwChains.Items.Clear();

            var chains = playerContext.SupplyChainList.ToList();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                chains = chains.Where(c =>
                    c.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            chains = CollectionSortHelper.OrderSupplyChains(chains).ToList();

            foreach (var chain in chains)
            {
                var item = new ListViewItem(chain.Name) { Tag = chain };
                item.SubItems.Add(chain.IsActive ? "Yes" : "No");
                if (!chain.IsActive)
                {
                    item.ForeColor = Color.Gray;
                    item.Font = new Font(lvwChains.Font, FontStyle.Italic);
                }

                lvwChains.Items.Add(item);
                if (chain.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateChainList: {0}ms items={1}", sw.ElapsedMilliseconds, chains.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulateChainList(); }

        private void LvwChains_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Models.SupplyChain chain)
            {
                _selectedChain = chain;
                PopulateForm();
            }
            else if (!e.IsSelected && lvwChains.SelectedItems.Count == 0)
            {
                _selectedChain = null;
                ClearForm();
            }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedChain == null)
            {
                ClearForm();
                return;
            }

            txtChainName.Text = _selectedChain.Name;
            chkActive.Checked = _selectedChain.IsActive;
            PopulateStagesGrid();
            PopulateRouteCombo();
            UpdateFlowSummary();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtChainName.Text = string.Empty;
            chkActive.Checked = true;
            dgvStages.Rows.Clear();
            ClearStageEditPanel();
            txtFlowSummary.Text = string.Empty;
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtChainName.Enabled = enabled;
            chkActive.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvStages.Enabled = enabled;
            cmdAddStage.Enabled = enabled;
            cmdUpdateStage.Enabled = enabled;
            cmdRemoveStage.Enabled = enabled;
            cmdMoveUp.Enabled = enabled;
            cmdMoveDown.Enabled = enabled;
            cmbStageType.Enabled = enabled;
            cmbLocationType.Enabled = enabled;
            cmbLocation.Enabled = enabled;
            cmbResource.Enabled = enabled;
            cmbPurity.Enabled = enabled;
            txtSequence.Enabled = enabled;
            txtThreshold.Enabled = enabled;
            txtRate.Enabled = enabled;
            cmbRoute.Enabled = enabled;
        }

        private void ClearStageEditPanel()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtSequence.Text = string.Empty;
            if (cmbStageType.Items.Count > 0) cmbStageType.SelectedIndex = 0;
            if (cmbLocationType.Items.Count > 0) cmbLocationType.SelectedIndex = 0;
            cmbLocation.DataSource = null;
            cmbLocation.Items.Clear();
            if (cmbResource.Items.Count > 0) cmbResource.SelectedIndex = 0;
            if (cmbPurity.Items.Count > 0) cmbPurity.SelectedIndex = 0;
            txtThreshold.Text = string.Empty;
            txtRate.Text = string.Empty;
            cmbRoute.DataSource = null;
            cmbRoute.Items.Clear();
        }

        // Combo helpers
        private void PopulateStageTypeCombos()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbStageType.Items.Clear();
            foreach (var val in Enum.GetValues(typeof(SupplyChainStageType)))
                cmbStageType.Items.Add(val);
            if (cmbStageType.Items.Count > 0) cmbStageType.SelectedIndex = 0;

            cmbLocationType.Items.Clear();
            cmbLocationType.Items.Add(DestinationType.Colony);
            cmbLocationType.Items.Add(DestinationType.Station);
            cmbLocationType.Items.Add(DestinationType.Asteroid);
            cmbLocationType.Items.Add(DestinationType.Ship);
            if (cmbLocationType.Items.Count > 0) cmbLocationType.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateStageTypeCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateResourceCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbResource.Items.Clear();
            var resources = EmpireContext.GetInstance()?.ResourceList;
            if (resources != null)
            {
                foreach (var r in resources.OrderBy(r => r.Name))
                    cmbResource.Items.Add(r.Name);
            }

            if (cmbResource.Items.Count > 0) cmbResource.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateResourceCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulatePurityCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbPurity.Items.Clear();
            foreach (var p in ResourcePurity.Purities)
            {
                if (p.ID != ResourcePurity.PurityEnum.None)
                    cmbPurity.Items.Add(p.Name);
            }

            if (cmbPurity.Items.Count > 0) cmbPurity.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulatePurityCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateLocationCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbLocation.DataSource = null;
            cmbLocation.Items.Clear();

            if (cmbLocationType.SelectedItem == null) return;
            var locType = (DestinationType)cmbLocationType.SelectedItem;

            var items = new List<KeyValuePair<string, string>>();
            switch (locType)
            {
                case DestinationType.Colony:
                    foreach (var c in CollectionSortHelper.OrderColonies(playerContext.ColonyList))
                        items.Add(new KeyValuePair<string, string>(c.UUID, c.ColonyName));
                    break;
                case DestinationType.Station:
                    foreach (var s in CollectionSortHelper.OrderStations(playerContext.StationList))
                        items.Add(new KeyValuePair<string, string>(s.UUID, s.Name));
                    break;
                case DestinationType.Asteroid:
                    foreach (var a in CollectionSortHelper.OrderAsteroids(playerContext.AsteroidList))
                        items.Add(new KeyValuePair<string, string>(a.UUID, a.Name));
                    break;
                case DestinationType.Ship:
                    foreach (var sh in CollectionSortHelper.OrderShips(playerContext.ShipList))
                        items.Add(new KeyValuePair<string, string>(sh.UUID, sh.Name));
                    break;
            }

            if (items.Count > 0)
            {
                cmbLocation.DataSource = items;
                cmbLocation.DisplayMember = "Value";
                cmbLocation.ValueMember = "Key";
            }

            sw.Stop();
            Log.Info("PERF PopulateLocationCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateRouteCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbRoute.DataSource = null;
            cmbRoute.Items.Clear();

            var routes = CollectionSortHelper.OrderDeliveryRoutes(playerContext.DeliveryRouteList).ToList();
            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var r in routes)
                items.Add(new KeyValuePair<string, string>(r.UUID, r.Name));

            cmbRoute.DataSource = items;
            cmbRoute.DisplayMember = "Value";
            cmbRoute.ValueMember = "Key";
            sw.Stop();
            Log.Info("PERF PopulateRouteCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbLocationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateLocationCombo();
        }

        // Stages grid
        private void PopulateStagesGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvStages.Rows.Clear();
            if (_selectedChain == null) return;

            foreach (var stage in CollectionSortHelper.OrderSupplyChainStages(_selectedChain.Stages))
            {
                string locationName = ResolveLocationName(stage.LocationType, stage.LocationUUID);
                string resourceDisplay = stage.ResourceName;
                if (!string.IsNullOrEmpty(stage.ResourcePurity))
                    resourceDisplay += " (" + stage.ResourcePurity + ")";
                string routeName = ResolveRouteName(stage.DeliveryRouteUUID);
                string thresholdStr = stage.AccumulationThreshold > 0 ? stage.AccumulationThreshold.ToString() : string.Empty;
                string rateStr = stage.ProductionRatePerHour > 0 ? stage.ProductionRatePerHour.ToString("F1") : string.Empty;

                int rowIdx = dgvStages.Rows.Add(
                    stage.Sequence.ToString(),
                    stage.StageType.ToString(),
                    locationName,
                    resourceDisplay,
                    thresholdStr,
                    rateStr,
                    routeName);
                dgvStages.Rows[rowIdx].Tag = stage;
            }

            sw.Stop();
            Log.Info("PERF PopulateStagesGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private string ResolveLocationName(DestinationType locType, string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return string.Empty;
            switch (locType)
            {
                case DestinationType.Colony:
                    var colony = playerContext.ColonyList.FirstOrDefault(c => c.UUID == uuid);
                    return colony?.ColonyName ?? uuid;
                case DestinationType.Station:
                    var station = playerContext.StationList.FirstOrDefault(s => s.UUID == uuid);
                    return station?.Name ?? uuid;
                case DestinationType.Asteroid:
                    var asteroid = playerContext.AsteroidList.FirstOrDefault(a => a.UUID == uuid);
                    return asteroid?.Name ?? uuid;
                case DestinationType.Ship:
                    var ship = playerContext.ShipList.FirstOrDefault(s => s.UUID == uuid);
                    return ship?.Name ?? uuid;
                default:
                    return uuid;
            }
        }

        private string ResolveRouteName(string routeUUID)
        {
            if (string.IsNullOrEmpty(routeUUID)) return string.Empty;
            var route = playerContext.DeliveryRouteList.FirstOrDefault(r => r.UUID == routeUUID);
            return route?.Name ?? routeUUID;
        }

        private void DgvStages_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvStages.SelectedRows.Count == 0) return;
            var stage = dgvStages.SelectedRows[0].Tag as SupplyChainStage;
            if (stage == null) return;
            PopulateStageEditFromStage(stage);
        }

        private void PopulateStageEditFromStage(SupplyChainStage stage)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            txtSequence.Text = stage.Sequence.ToString();
            cmbStageType.SelectedItem = stage.StageType;
            cmbLocationType.SelectedItem = stage.LocationType;
            PopulateLocationCombo();

            // Select the location
            if (!string.IsNullOrEmpty(stage.LocationUUID) && cmbLocation.DataSource != null)
                cmbLocation.SelectedValue = stage.LocationUUID;

            // Resource and purity
            for (int i = 0; i < cmbResource.Items.Count; i++)
            {
                if (cmbResource.Items[i].ToString() == stage.ResourceName)
                {
                    cmbResource.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < cmbPurity.Items.Count; i++)
            {
                if (cmbPurity.Items[i].ToString() == stage.ResourcePurity)
                {
                    cmbPurity.SelectedIndex = i;
                    break;
                }
            }

            txtThreshold.Text = stage.AccumulationThreshold > 0 ? stage.AccumulationThreshold.ToString() : string.Empty;
            txtRate.Text = stage.ProductionRatePerHour > 0 ? stage.ProductionRatePerHour.ToString("F1") : string.Empty;

            PopulateRouteCombo();
            if (!string.IsNullOrEmpty(stage.DeliveryRouteUUID))
                cmbRoute.SelectedValue = stage.DeliveryRouteUUID;
            sw.Stop();
            Log.Info("PERF PopulateStageEditFromStage: {0}ms", sw.ElapsedMilliseconds);
        }

        // Stage CRUD
        private SupplyChainStage BuildStageFromPanel()
        {
            int.TryParse(txtSequence.Text.Trim(), out int seq);
            var stageType = cmbStageType.SelectedItem is SupplyChainStageType st ? st : SupplyChainStageType.Mine;
            var locType = cmbLocationType.SelectedItem is DestinationType dt ? dt : DestinationType.Colony;
            string locUUID = cmbLocation.SelectedValue?.ToString() ?? string.Empty;
            string resource = cmbResource.SelectedItem?.ToString() ?? string.Empty;
            string purity = cmbPurity.SelectedItem?.ToString() ?? string.Empty;
            int.TryParse(txtThreshold.Text.Trim(), out int threshold);
            decimal.TryParse(txtRate.Text.Trim(), out decimal rate);
            string routeUUID = cmbRoute.SelectedValue?.ToString() ?? string.Empty;

            return new SupplyChainStage
            {
                Sequence = seq,
                StageType = stageType,
                LocationType = locType,
                LocationUUID = locUUID,
                ResourceName = resource,
                ResourcePurity = purity,
                AccumulationThreshold = threshold,
                ProductionRatePerHour = rate,
                DeliveryRouteUUID = routeUUID
            };
        }

        private void CmdAddStage_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null) return;
            var stage = BuildStageFromPanel();
            // Auto-assign sequence if blank
            if (stage.Sequence == 0 && _selectedChain.Stages.Count > 0)
                stage.Sequence = _selectedChain.Stages.Max(s => s.Sequence) + 1;
            else if (stage.Sequence == 0)
                stage.Sequence = 1;
            _selectedChain.Stages.Add(stage);
            PopulateStagesGrid();
            UpdateFlowSummary();
            Log.Info("Added stage seq={0} type={1}", stage.Sequence, stage.StageType);
        }

        private void CmdUpdateStage_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null || dgvStages.SelectedRows.Count == 0) return;
            var existing = dgvStages.SelectedRows[0].Tag as SupplyChainStage;
            if (existing == null) return;

            var updated = BuildStageFromPanel();
            existing.Sequence = updated.Sequence;
            existing.StageType = updated.StageType;
            existing.LocationType = updated.LocationType;
            existing.LocationUUID = updated.LocationUUID;
            existing.ResourceName = updated.ResourceName;
            existing.ResourcePurity = updated.ResourcePurity;
            existing.AccumulationThreshold = updated.AccumulationThreshold;
            existing.ProductionRatePerHour = updated.ProductionRatePerHour;
            existing.DeliveryRouteUUID = updated.DeliveryRouteUUID;

            PopulateStagesGrid();
            UpdateFlowSummary();
            Log.Info("Updated stage seq={0} type={1}", existing.Sequence, existing.StageType);
        }

        private void CmdRemoveStage_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null || dgvStages.SelectedRows.Count == 0) return;
            var stage = dgvStages.SelectedRows[0].Tag as SupplyChainStage;
            if (stage == null) return;
            _selectedChain.Stages.Remove(stage);
            PopulateStagesGrid();
            UpdateFlowSummary();
            Log.Info("Removed stage seq={0} type={1}", stage.Sequence, stage.StageType);
        }

        private void CmdMoveUp_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null || dgvStages.SelectedRows.Count == 0) return;
            var stage = dgvStages.SelectedRows[0].Tag as SupplyChainStage;
            if (stage == null) return;
            var sorted = CollectionSortHelper.OrderSupplyChainStages(_selectedChain.Stages).ToList();
            int idx = sorted.IndexOf(stage);
            if (idx <= 0) return;
            int prevSeq = sorted[idx - 1].Sequence;
            sorted[idx - 1].Sequence = stage.Sequence;
            stage.Sequence = prevSeq;
            PopulateStagesGrid();
            UpdateFlowSummary();
        }

        private void CmdMoveDown_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null || dgvStages.SelectedRows.Count == 0) return;
            var stage = dgvStages.SelectedRows[0].Tag as SupplyChainStage;
            if (stage == null) return;
            var sorted = CollectionSortHelper.OrderSupplyChainStages(_selectedChain.Stages).ToList();
            int idx = sorted.IndexOf(stage);
            if (idx < 0 || idx >= sorted.Count - 1) return;
            int nextSeq = sorted[idx + 1].Sequence;
            sorted[idx + 1].Sequence = stage.Sequence;
            stage.Sequence = nextSeq;
            PopulateStagesGrid();
            UpdateFlowSummary();
        }

        // Flow Summary
        private void UpdateFlowSummary()
        {
            if (_selectedChain == null || _selectedChain.Stages.Count == 0)
            {
                txtFlowSummary.Text = string.Empty;
                return;
            }

            var sorted = CollectionSortHelper.OrderSupplyChainStages(_selectedChain.Stages).ToList();
            var parts = new List<string>();
            foreach (var stage in sorted)
            {
                string locName = ResolveLocationName(stage.LocationType, stage.LocationUUID);
                string desc = stage.StageType.ToString();
                if (!string.IsNullOrEmpty(locName))
                    desc += "@" + locName;
                if (stage.AccumulationThreshold > 0)
                    desc += "(" + stage.AccumulationThreshold + ")";
                parts.Add(desc);
            }

            txtFlowSummary.Text = string.Join(" \u2192 ", parts);
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            var chain = new Models.SupplyChain
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Supply Chain",
                OwnerUUID = playerContext.CurrentPlayerUUID ?? string.Empty,
                IsActive = true
            };

            playerContext.AddSupplyChain(chain);
            playerContext.WriteContext();
            _selectedChain = chain;
            PopulateChainList();
            PopulateForm();
            Log.Info("Created new supply chain");
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null) return;
            var result = MessageBox.Show(
                string.Format("Delete supply chain \"{0}\"?", _selectedChain.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.RemoveSupplyChain(_selectedChain);
            playerContext.WriteContext();
            _selectedChain = null;
            PopulateChainList();
            ClearForm();
            Log.Info("Deleted supply chain");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedChain == null) return;
            string name = txtChainName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Name cannot be empty.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Validate threshold stages have routes
            foreach (var stage in _selectedChain.Stages)
            {
                if (stage.AccumulationThreshold > 0 && string.IsNullOrEmpty(stage.DeliveryRouteUUID))
                {
                    if (stage.StageType == SupplyChainStageType.PickUp ||
                        stage.StageType == SupplyChainStageType.Refine ||
                        stage.StageType == SupplyChainStageType.Deliver)
                    {
                        MessageBox.Show(
                            string.Format(
                                "Stage {0} ({1}) has a threshold but no delivery route assigned.",
                                stage.Sequence,
                                stage.StageType),
                            "Validation",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            _selectedChain.Name = name;
            playerContext.WriteContext();
            PopulateChainList();
            Log.Info("Saved supply chain \"{0}\"", _selectedChain.Name);
        }

        private void TxtChainName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedChain == null) return;
            _selectedChain.Name = txtChainName.Text;
        }

        private void ChkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedChain == null) return;
            _selectedChain.IsActive = chkActive.Checked;
            Log.Info("Supply chain \"{0}\" IsActive={1}", _selectedChain.Name, _selectedChain.IsActive);
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
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

            _selectedChain = null;
            PopulateChainList();
            ClearForm();
        }
    }
}