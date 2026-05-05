using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.Asteroid
{
    public partial class FormAsteroid : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;
        private AsteroidService _asteroidService;
        private AsteroidViewModel _viewModel = new AsteroidViewModel();

        public FormAsteroid()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _asteroidService = new AsteroidService(playerContext);

            lvwAsteroids.View = View.Details;
            lvwAsteroids.Columns.Add("Name", 100);
            lvwAsteroids.Columns.Add("System", 70);
            lvwAsteroids.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwAsteroids.FullRowSelect = true;
            lvwAsteroids.MultiSelect = false;
            lvwAsteroids.ItemSelectionChanged += LvwAsteroids_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtAsteroidName.TextChanged += TxtAsteroidName_TextChanged;
            txtSystemName.TextChanged += TxtSystemName_TextChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;

            cmdAddReserve.Click += CmdAddReserve_Click;
            cmdRemoveReserve.Click += CmdRemoveReserve_Click;
            dgvReserves.CellEndEdit += DgvReserves_CellEndEdit;

            PopulateResourceCombo();
            txtReserveResourceFilter.TextChanged += (s, ev) => PopulateResourceCombo();
            PopulatePurityCombo();
            PopulateAsteroidList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.AsteroidDataChanged += OnAsteroidDataChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    CmdSave_Click(this, EventArgs.Empty);
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.AsteroidDataChanged -= OnAsteroidDataChanged;
            base.OnFormClosed(e);
        }

        // Layout
        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - 12;
            if (listHeight < 50) listHeight = 50;
            lvwAsteroids.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int gridWidth = w - 6;
            dgvReserves.Width = gridWidth;
            dgvLinkedSurveys.Width = gridWidth;
        }

        // Asteroid List
        private void PopulateAsteroidList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _viewModel.UUID;
            lvwAsteroids.Items.Clear();

            var readOnlyAsteroids = playerContext.GetReadOnlyAsteroidList();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                readOnlyAsteroids = readOnlyAsteroids.Where(a =>
                    a.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.SystemName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            readOnlyAsteroids = CollectionSortHelper.OrderReadOnlyAsteroids(readOnlyAsteroids).ToList();

            var refCounter = new AsteroidReferenceCounter(
                playerContext.SurveyList.ToList(),
                playerContext.GetCurrentPlayerBuildPlans(),
                playerContext.DeliveryRouteList.ToList());

            foreach (var roAsteroid in readOnlyAsteroids)
            {
                var report = refCounter.CountReferences(roAsteroid.UUID);
                var item = new ListViewItem(roAsteroid.Name) { Tag = roAsteroid };
                item.SubItems.Add(roAsteroid.SystemName);
                item.SubItems.Add(report.TotalCount.ToString());
                lvwAsteroids.Items.Add(item);
                if (roAsteroid.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateAsteroidList: {0}ms items={1}", sw.ElapsedMilliseconds, readOnlyAsteroids.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulateAsteroidList(); }

        private void LvwAsteroids_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlyAsteroid roAsteroid)
            {
                if (!PromptUnsavedChanges())
                {
                    using var guard = new ProgrammaticUpdateGuard(this);
                    e.Item.Selected = false;
                    return;
                }

                _viewModel.LoadFrom(roAsteroid);
                PopulateForm();
            }
            else if (!e.IsSelected && lvwAsteroids.SelectedItems.Count == 0)
            {
                _viewModel.Reset();
                ClearForm();
            }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_viewModel.IsNew)
            {
                ClearForm();
                return;
            }

            txtAsteroidName.Text = _viewModel.Name;
            txtSystemName.Text = _viewModel.SystemName;
            PopulateReservesGrid();
            PopulateLinkedSurveys();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtAsteroidName.Text = string.Empty;
            txtSystemName.Text = string.Empty;
            dgvReserves.Rows.Clear();
            dgvLinkedSurveys.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtAsteroidName.Enabled = enabled;
            txtSystemName.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvReserves.Enabled = enabled;
            cmdAddReserve.Enabled = enabled;
            cmdRemoveReserve.Enabled = enabled;
        }

        // Combo helpers
        private void PopulateResourceCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbReserveResource.Items.Clear();
            var resources = EmpireContext.GetInstance()?.ResourceList;
            if (resources != null)
            {
                string filter = txtReserveResourceFilter.Text.Trim();
                var filtered = resources.OrderBy(r => r.Name).AsEnumerable();
                if (!string.IsNullOrEmpty(filter))
                    filtered = filtered.Where(r => r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                foreach (var r in filtered)
                    cmbReserveResource.Items.Add(r.Name);
            }

            if (cmbReserveResource.Items.Count > 0) cmbReserveResource.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateResourceCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulatePurityCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbReservePurity.Items.Clear();
            foreach (var p in ResourcePurity.Purities)
            {
                if (p.ID != ResourcePurity.PurityEnum.None)
                    cmbReservePurity.Items.Add(p.Name);
            }

            if (cmbReservePurity.Items.Count > 0) cmbReservePurity.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulatePurityCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        // Reserves grid
        private void PopulateReservesGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvReserves.Rows.Clear();
            if (_viewModel.IsNew) return;

            foreach (var reserve in _viewModel.Reserves)
            {
                int rowIdx = dgvReserves.Rows.Add(
                    reserve.ResourceName,
                    reserve.Purity,
                    reserve.MaxReserve.ToString(),
                    reserve.CurrentReserve.ToString(),
                    reserve.ResetTimestamp);
                dgvReserves.Rows[rowIdx].Tag = reserve;
                dgvReserves.Rows[rowIdx].Cells[colResource.Index].ReadOnly = true;
                dgvReserves.Rows[rowIdx].Cells[colPurity.Index].ReadOnly = true;
                dgvReserves.Rows[rowIdx].Cells[colMaxReserve.Index].ReadOnly = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateReservesGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvReserves_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew || e.RowIndex < 0) return;
            if (e.RowIndex >= _viewModel.Reserves.Count) return;
            var reserve = _viewModel.Reserves[e.RowIndex];
            var row = dgvReserves.Rows[e.RowIndex];
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;

            if (e.ColumnIndex == colCurrentReserve.Index)
            {
                if (int.TryParse(valStr, out int val)) reserve.CurrentReserve = val;
            }
            else if (e.ColumnIndex == colResetTimestamp.Index)
            {
                reserve.ResetTimestamp = valStr;
            }
        }

        private void CmdAddReserve_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;
            string resourceName = cmbReserveResource.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resourceName)) return;
            string purity = cmbReservePurity.SelectedItem?.ToString() ?? string.Empty;
            if (!int.TryParse(txtMaxReserve.Text.Trim(), out int maxReserve) || maxReserve <= 0)
            {
                MessageBox.Show(
                    "Enter a valid max reserve.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int.TryParse(txtCurrentReserve.Text.Trim(), out int currentReserve);

            var reserve = new AsteroidReserve
            {
                ResourceName = resourceName,
                Purity = purity,
                MaxReserve = maxReserve,
                CurrentReserve = currentReserve
            };

            _viewModel.AddReserve(reserve);
            PopulateReservesGrid();
            Log.Info("Added reserve: {0} ({1}) max={2}", resourceName, purity, maxReserve);
        }

        private void CmdRemoveReserve_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew || dgvReserves.SelectedRows.Count == 0) return;
            int index = dgvReserves.SelectedRows[0].Index;
            _viewModel.RemoveReserve(index);
            PopulateReservesGrid();
            Log.Info("Removed reserve at index {0}", index);
        }

        // Linked Surveys grid
        private void PopulateLinkedSurveys()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvLinkedSurveys.Rows.Clear();
            if (_viewModel.IsNew) return;

            var linkedSurveys = playerContext.SurveyList
                .Where(s => s.AsteroidUUID == _viewModel.UUID)
                .ToList();

            foreach (var survey in linkedSurveys)
            {
                var owner = playerContext.PlayerProfileList
                    .FirstOrDefault(p => p.UUID == survey.OwnerUUID);
                string playerName = owner?.Name ?? survey.ScannedBy ?? string.Empty;

                if (survey.Resources != null)
                {
                    foreach (var kvp in survey.Resources)
                    {
                        var r = kvp.Value;
                        dgvLinkedSurveys.Rows.Add(playerName, r.Resource, r.Purity, r.Amount);
                    }
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateLinkedSurveys: {0}ms", sw.ElapsedMilliseconds);
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (!PromptUnsavedChanges()) return;

            _viewModel.Reset();
            _viewModel.Name = "New Asteroid";
            var created = _asteroidService.Create(_viewModel.BuildCreateRequest());
            _viewModel.LoadFrom(created);
            PopulateAsteroidList();
            PopulateForm();
            Log.Info("Created new asteroid");
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;

            var refCounter = new AsteroidReferenceCounter(
                playerContext.SurveyList.ToList(),
                playerContext.GetCurrentPlayerBuildPlans(),
                playerContext.DeliveryRouteList.ToList());
            var report = refCounter.CountReferences(_viewModel.UUID);
            if (report.TotalCount > 0)
            {
                MessageBox.Show(
                    string.Format(
                        "Cannot delete asteroid \"{0}\" \u2014 it is referenced by {1} survey(s), build item(s), or route stop(s).",
                        _viewModel.Name,
                        report.TotalCount),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete asteroid \"{0}\"?", _viewModel.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            _asteroidService.Delete(_viewModel.UUID);
            _viewModel.Reset();
            PopulateAsteroidList();
            ClearForm();
            Log.Info("Deleted asteroid");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;
            string name = txtAsteroidName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _viewModel.Name = name;
            _viewModel.SystemName = txtSystemName.Text.Trim();
            var updated = _asteroidService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            _viewModel.LoadFrom(updated);
            PopulateAsteroidList();
            Log.Info("Saved asteroid \"{0}\"", _viewModel.Name);
        }

        private void TxtAsteroidName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            _viewModel.Name = txtAsteroidName.Text;
        }

        private void TxtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            _viewModel.SystemName = txtSystemName.Text;
        }

        // Unsaved changes prompt
        private bool PromptUnsavedChanges()
        {
            if (!_viewModel.IsDirty) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Save before continuing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes)
            {
                CmdSave_Click(this, EventArgs.Empty);
            }

            return true;
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

            _viewModel.Reset();
            PopulateAsteroidList();
            ClearForm();
        }

        private void OnAsteroidDataChanged(object sender, AsteroidDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnAsteroidDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            PopulateAsteroidList();
            if (!_viewModel.IsNew && _viewModel.UUID == e.AsteroidUUID)
            {
                var refreshed = playerContext.GetReadOnlyAsteroidList()
                    .FirstOrDefault(a => a.UUID == e.AsteroidUUID);
                if (refreshed != null)
                {
                    _viewModel.LoadFrom(refreshed);
                    PopulateForm();
                }
            }
        }
    }
}