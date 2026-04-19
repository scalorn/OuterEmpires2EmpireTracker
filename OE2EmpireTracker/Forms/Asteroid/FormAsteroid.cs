using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Asteroid
{
    public partial class FormAsteroid : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private Models.Asteroid _selectedAsteroid;

        public FormAsteroid()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwAsteroids.View = View.Details;
            lvwAsteroids.Columns.Add("Name", 100);
            lvwAsteroids.Columns.Add("System", 70);
            lvwAsteroids.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwAsteroids.FullRowSelect = true;
            lvwAsteroids.MultiSelect = false;
            lvwAsteroids.ItemSelectionChanged += lvwAsteroids_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtAsteroidName.TextChanged += txtAsteroidName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            cmdAddReserve.Click += cmdAddReserve_Click;
            cmdRemoveReserve.Click += cmdRemoveReserve_Click;
            dgvReserves.CellEndEdit += dgvReserves_CellEndEdit;

            PopulateResourceCombo();
            PopulatePurityCombo();
            PopulateAsteroidList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }
        // Layout
        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwAsteroids.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
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
            string selectedUUID = _selectedAsteroid?.UUID;
            lvwAsteroids.Items.Clear();

            var asteroids = playerContext.AsteroidList.ToList();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                asteroids = asteroids.Where(a =>
                    a.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.SystemName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            asteroids = asteroids.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();

            var refCounter = new AsteroidReferenceCounter(
                playerContext.SurveyList.ToList(),
                playerContext.GetCurrentPlayerBuildPlans(),
                playerContext.DeliveryRouteList.ToList());

            foreach (var asteroid in asteroids)
            {
                var report = refCounter.CountReferences(asteroid.UUID);
                var item = new ListViewItem(asteroid.Name) { Tag = asteroid };
                item.SubItems.Add(asteroid.SystemName);
                item.SubItems.Add(report.TotalCount.ToString());
                lvwAsteroids.Items.Add(item);
                if (asteroid.UUID == selectedUUID) item.Selected = true;
            }
            sw.Stop();
            Log.Info("PERF PopulateAsteroidList: {0}ms items={1}", sw.ElapsedMilliseconds, asteroids.Count);
        }

        private void txtFilter_TextChanged(object sender, EventArgs e) { PopulateAsteroidList(); }

        private void lvwAsteroids_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Models.Asteroid asteroid)
            { _selectedAsteroid = asteroid; PopulateForm(); }
            else if (!e.IsSelected && lvwAsteroids.SelectedItems.Count == 0)
            { _selectedAsteroid = null; ClearForm(); }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedAsteroid == null) { ClearForm(); return; }
            txtAsteroidName.Text = _selectedAsteroid.Name;
            txtSystemName.Text = _selectedAsteroid.SystemName;
            PopulateReservesGrid();
            PopulateLinkedSurveys();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtAsteroidName.Text = "";
            txtSystemName.Text = "";
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
            cmbReserveResource.Items.Clear();
            var resources = EmpireContext.GetInstance()?.ResourceList;
            if (resources != null)
            {
                foreach (var r in resources.OrderBy(r => r.Name))
                    cmbReserveResource.Items.Add(r.Name);
            }
            if (cmbReserveResource.Items.Count > 0) cmbReserveResource.SelectedIndex = 0;
        }

        private void PopulatePurityCombo()
        {
            cmbReservePurity.Items.Clear();
            foreach (var p in ResourcePurity.Purities)
            {
                if (p.ID != ResourcePurity.PurityEnum.None)
                    cmbReservePurity.Items.Add(p.Name);
            }
            if (cmbReservePurity.Items.Count > 0) cmbReservePurity.SelectedIndex = 0;
        }

        // Reserves grid
        private void PopulateReservesGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvReserves.Rows.Clear();
            if (_selectedAsteroid == null) return;

            foreach (var reserve in _selectedAsteroid.Reserves)
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
        }

        private void dgvReserves_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedAsteroid == null || e.RowIndex < 0) return;
            var row = dgvReserves.Rows[e.RowIndex];
            if (!(row.Tag is AsteroidReserve reserve)) return;
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";

            if (e.ColumnIndex == colCurrentReserve.Index)
            {
                if (int.TryParse(valStr, out int val)) reserve.CurrentReserve = val;
            }
            else if (e.ColumnIndex == colResetTimestamp.Index)
            {
                reserve.ResetTimestamp = valStr;
            }
        }

        private void cmdAddReserve_Click(object sender, EventArgs e)
        {
            if (_selectedAsteroid == null) return;
            string resourceName = cmbReserveResource.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(resourceName)) return;
            string purity = cmbReservePurity.SelectedItem?.ToString() ?? "";
            if (!int.TryParse(txtMaxReserve.Text.Trim(), out int maxReserve) || maxReserve <= 0)
            {
                MessageBox.Show("Enter a valid max reserve.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _selectedAsteroid.Reserves.Add(reserve);
            PopulateReservesGrid();
            Log.Info("Added reserve: {0} ({1}) max={2}", resourceName, purity, maxReserve);
        }

        private void cmdRemoveReserve_Click(object sender, EventArgs e)
        {
            if (_selectedAsteroid == null || dgvReserves.SelectedRows.Count == 0) return;
            var reserve = dgvReserves.SelectedRows[0].Tag as AsteroidReserve;
            if (reserve == null) return;
            _selectedAsteroid.Reserves.Remove(reserve);
            PopulateReservesGrid();
            Log.Info("Removed reserve: {0} ({1})", reserve.ResourceName, reserve.Purity);
        }
        // Linked Surveys grid
        private void PopulateLinkedSurveys()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvLinkedSurveys.Rows.Clear();
            if (_selectedAsteroid == null) return;

            var linkedSurveys = playerContext.SurveyList
                .Where(s => s.AsteroidUUID == _selectedAsteroid.UUID)
                .ToList();

            foreach (var survey in linkedSurveys)
            {
                var owner = playerContext.PlayerProfileList
                    .FirstOrDefault(p => p.UUID == survey.OwnerUUID);
                string playerName = owner?.Name ?? survey.ScannedBy ?? "";

                if (survey.Resources != null)
                {
                    foreach (var kvp in survey.Resources)
                    {
                        var r = kvp.Value;
                        dgvLinkedSurveys.Rows.Add(playerName, r.Resource, r.Purity, r.Amount);
                    }
                }
            }
        }

        // CRUD
        private void cmdNew_Click(object sender, EventArgs e)
        {
            var asteroid = new Models.Asteroid
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Asteroid"
            };
            playerContext.AsteroidList.Add(asteroid);
            playerContext.WriteContext();
            _selectedAsteroid = asteroid;
            PopulateAsteroidList();
            PopulateForm();
            Log.Info("Created new asteroid");
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedAsteroid == null) return;

            var refCounter = new AsteroidReferenceCounter(
                playerContext.SurveyList.ToList(),
                playerContext.GetCurrentPlayerBuildPlans(),
                playerContext.DeliveryRouteList.ToList());
            var report = refCounter.CountReferences(_selectedAsteroid.UUID);
            if (report.TotalCount > 0)
            {
                MessageBox.Show(
                    string.Format("Cannot delete asteroid \"{0}\" \u2014 it is referenced by {1} survey(s), build item(s), or route stop(s).",
                        _selectedAsteroid.Name, report.TotalCount),
                    "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete asteroid \"{0}\"?", _selectedAsteroid.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.AsteroidList.Remove(_selectedAsteroid);
            playerContext.WriteContext();
            _selectedAsteroid = null;
            PopulateAsteroidList();
            ClearForm();
            Log.Info("Deleted asteroid");
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedAsteroid == null) return;
            string name = txtAsteroidName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            { MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _selectedAsteroid.Name = name;
            _selectedAsteroid.SystemName = txtSystemName.Text.Trim();
            playerContext.WriteContext();
            PopulateAsteroidList();
            Log.Info("Saved asteroid \"{0}\"", _selectedAsteroid.Name);
        }

        private void txtAsteroidName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedAsteroid == null) return;
            _selectedAsteroid.Name = txtAsteroidName.Text;
        }

        private void txtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedAsteroid == null) return;
            _selectedAsteroid.SystemName = txtSystemName.Text;
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            _selectedAsteroid = null;
            PopulateAsteroidList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }
    }
}