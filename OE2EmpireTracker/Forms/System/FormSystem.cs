// -----------------------------------------------------------------------
// <copyright file="FormSystem.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

namespace OE2EmpireTracker.Forms
{
    /// <summary>
    /// MDI child form for viewing and editing star system data.
    /// </summary>
    public partial class FormSystem : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate;

        private SystemRepository _systemRepository;
        private List<SystemViewModel> _viewModels = new List<SystemViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FormSystem"/> class.
        /// </summary>
        public FormSystem()
        {
            _isProgrammaticUpdate++;

            InitializeComponent();

            var empireContext = EmpireContext.GetInstance();
            _systemRepository = empireContext.SystemRepository;

            txtSearch.TextChanged += TxtSearch_TextChanged;
            dgvSystems.SelectionChanged += DgvSystems_SelectionChanged;
            dgvSystems.DataError += DgvSystems_DataError;
            btnSave.Click += BtnSave_Click;
            btnReimport.Click += BtnReimport_Click;

            _systemRepository.SystemDataChanged += OnSystemDataChanged;
            EmpireContext.PlayerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;

            PopulateGrid();

            Shown += (s, ev) =>
            {
                _isProgrammaticUpdate--;
            };
        }

        /// <inheritdoc/>
        public void BeginProgrammaticUpdate()
        {
            _isProgrammaticUpdate++;
        }

        /// <inheritdoc/>
        public void EndProgrammaticUpdate()
        {
            _isProgrammaticUpdate--;
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _systemRepository.SystemDataChanged -= OnSystemDataChanged;
            EmpireContext.PlayerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        private void OnSystemDataChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSystemDataChanged(sender, e)));
                return;
            }

            PopulateGrid();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            int selectedId = -1;
            if (dgvSystems.CurrentRow != null && dgvSystems.CurrentRow.Index >= 0
                && dgvSystems.CurrentRow.Index < _viewModels.Count)
            {
                selectedId = _viewModels[dgvSystems.CurrentRow.Index].Id;
            }

            dgvSystems.Rows.Clear();
            _viewModels.Clear();

            string filter = txtSearch.Text.Trim();
            IEnumerable<StarSystem> systems = _systemRepository.Systems;

            if (!string.IsNullOrEmpty(filter))
            {
                systems = systems.Where(s =>
                    s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (var system in systems)
            {
                var vm = new SystemViewModel(new ReadOnlyStarSystem(system));
                _viewModels.Add(vm);
                dgvSystems.Rows.Add(vm.Id, vm.Name, vm.GridLocation, vm.SpectralClass, vm.FactionName);
            }

            // Restore selection
            if (selectedId >= 0)
            {
                for (int i = 0; i < _viewModels.Count; i++)
                {
                    if (_viewModels[i].Id == selectedId)
                    {
                        dgvSystems.CurrentCell = dgvSystems.Rows[i].Cells[0];
                        break;
                    }
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateGrid: {0}ms items={1}", sw.ElapsedMilliseconds, _viewModels.Count);
        }

        private void PopulateDetail()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            if (dgvSystems.CurrentRow == null || dgvSystems.CurrentRow.Index < 0
                || dgvSystems.CurrentRow.Index >= _viewModels.Count)
            {
                ClearDetail();
                sw.Stop();
                return;
            }

            var vm = _viewModels[dgvSystems.CurrentRow.Index];

            lblIdValue.Text = vm.Id.ToString();
            lblNameValue.Text = vm.Name;
            lblXValue.Text = vm.X.ToString();
            lblYValue.Text = vm.Y.ToString();
            lblQuadrantValue.Text = vm.Quadrant.ToString();
            lblSectorValue.Text = vm.Sector.ToString();
            lblRegionValue.Text = vm.Region.ToString();
            lblLocalityValue.Text = vm.Locality.ToString();
            lblSpectralClassValue.Text = vm.SpectralClass;

            txtFactionName.Text = vm.FactionName;
            txtFactionColor.Text = vm.FactionColor;
            chkHasOrbital.Checked = vm.HasOrbital;
            chkHasSpaceport.Checked = vm.HasSpaceport;
            chkHasStarbase.Checked = vm.HasStarbase;

            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateDetail: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearDetail()
        {
            lblIdValue.Text = string.Empty;
            lblNameValue.Text = string.Empty;
            lblXValue.Text = string.Empty;
            lblYValue.Text = string.Empty;
            lblQuadrantValue.Text = string.Empty;
            lblSectorValue.Text = string.Empty;
            lblRegionValue.Text = string.Empty;
            lblLocalityValue.Text = string.Empty;
            lblSpectralClassValue.Text = string.Empty;
            txtFactionName.Text = string.Empty;
            txtFactionColor.Text = string.Empty;
            chkHasOrbital.Checked = false;
            chkHasSpaceport.Checked = false;
            chkHasStarbase.Checked = false;
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtFactionName.Enabled = enabled;
            txtFactionColor.Enabled = enabled;
            chkHasOrbital.Enabled = enabled;
            chkHasSpaceport.Enabled = enabled;
            chkHasStarbase.Enabled = enabled;
            btnSave.Enabled = enabled;
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            PopulateGrid();
        }

        private void DgvSystems_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            PopulateDetail();
        }

        private void DgvSystems_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn(
                "dgvSystems DataError at [{0},{1}]: {2}",
                e.RowIndex,
                e.ColumnIndex,
                e.Exception?.Message);
            e.ThrowException = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (dgvSystems.CurrentRow == null || dgvSystems.CurrentRow.Index < 0
                || dgvSystems.CurrentRow.Index >= _viewModels.Count)
            {
                return;
            }

            var vm = _viewModels[dgvSystems.CurrentRow.Index];
            int systemId = vm.Id;

            string factionName = txtFactionName.Text.Trim();
            string factionColor = txtFactionColor.Text.Trim();
            bool hasOrbital = chkHasOrbital.Checked;
            bool hasSpaceport = chkHasSpaceport.Checked;
            bool hasStarbase = chkHasStarbase.Checked;

            _systemRepository.UpdateSystem(systemId, s =>
            {
                s.FactionName = factionName;
                s.FactionColor = factionColor;
                s.HasOrbital = hasOrbital;
                s.HasSpaceport = hasSpaceport;
                s.HasStarbase = hasStarbase;
            });

            Log.Info("Saved system {0} ({1})", systemId, vm.Name);
        }

        private void BtnReimport_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Re-importing will overwrite all manual faction and infrastructure edits.\n\nAre you sure?",
                "Confirm Re-import",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string sourcePath = "oe2-galaxy-systems.json";

            // Resolve relative to exe directory first, then try solution root
            if (!System.IO.File.Exists(sourcePath))
            {
                string exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string candidate = System.IO.Path.Combine(exeDir, "..", "..", "..", sourcePath);
                if (System.IO.File.Exists(candidate))
                {
                    sourcePath = candidate;
                }
            }

            string outputPath = SystemRepository.FilePath;

            int count = SystemImporter.Import(sourcePath, outputPath);
            if (count > 0)
            {
                _systemRepository.Load(outputPath);
                Log.Info("Re-imported {0} systems", count);
            }
            else
            {
                MessageBox.Show(
                    "Import returned 0 systems. Check that oe2-galaxy-systems.json exists.",
                    "Import Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
